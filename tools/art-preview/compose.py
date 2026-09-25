"""Arma las hojas de vista previa a partir de los .raw que vuelca ArtPreview (dotnet run).

Uso:
  python3 tools/art-preview/compose.py <raw_nuevo> [--antes <raw_viejo>] --out docs/previews
Genera:
  arte-arcilla.png      tablero de Parejas (cartas + íconos), tesoros y vidas; si se pasa --antes, lado a lado.
  simbolos-parejas.png  los 14 íconos x 4 variantes sobre la cara crema de la carta.
"""
import argparse
import os
import struct

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
FB = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'
FS = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-SemiBold.ttf'

NEW_KINDS = ['Planet', 'Rocket', 'Comet', 'Star', 'Moon', 'Ufo', 'Satellite', 'Sun', 'Helmet', 'Asteroid',
             'Telescope', 'Crystal', 'Constellation', 'Drop']
NEW_NAMES = ['Planeta', 'Cohete', 'Cometa', 'Estrella', 'Luna', 'Platillo', 'Satélite', 'Sol', 'Casco',
             'Asteroide', 'Telescopio', 'Cristal', 'Constelación', 'Gota (Stroop)']


def load(path):
    b = open(path, 'rb').read()
    n = struct.unpack('<i', b[:4])[0]
    return Image.frombytes('RGBA', (n, n), b[4:4 + n * n * 4]).transpose(Image.FLIP_TOP_BOTTOM)


def night(w, h):
    top, mid, bot = (0x10, 0x1A, 0x58), (0x08, 0x0E, 0x3A), (0x04, 0x06, 0x1C)
    im = Image.new('RGBA', (w, h))
    px = im.load()
    for y in range(h):
        t = y / (h - 1)
        a, b, k = (top, mid, t / 0.5) if t < 0.5 else (mid, bot, (t - 0.5) / 0.5)
        c = tuple(int(a[i] + (b[i] - a[i]) * k) for i in range(3)) + (255,)
        for x in range(w):
            px[x, y] = c
    return im


def paste(dst, im, x, y, size):
    dst.alpha_composite(im.resize((size, size), Image.LANCZOS), (int(x), int(y)))


def card(dst, raw, face, sym, x, y, c):
    paste(dst, load(f'{raw}/card_{face}.raw'), x, y, c)
    if face != 'Back' and sym:
        s = int(c * 0.66)
        cx = x + c * 0.5
        cy = y + c * (0.13 + 0.79) / 2
        paste(dst, load(f'{raw}/sym_{sym}.raw'), cx - s / 2, cy - s / 2, s)


def panel(dst, raw, x0, y0, title, board_syms):
    d = ImageDraw.Draw(dst)
    d.text((x0 + 330, y0), title, font=ImageFont.truetype(FB, 46), fill=(255, 255, 255), anchor='mt')
    c = 160
    faces = ['Back', 'Front', 'Back', 'Matched', 'Front', 'Back', 'Back', 'Front', 'Matched', 'Back', 'Front', 'Back']
    si = 0
    for i, f in enumerate(faces):
        sym = None
        if f == 'Front':
            sym = board_syms[si % len(board_syms)]
            si += 1
        elif f == 'Matched':
            sym = board_syms[-1]
        card(dst, raw, f, sym, x0 + 10 + (i % 4) * c, y0 + 80 + (i // 4) * c, c)

    ty = y0 + 80 + 3 * c + 40
    d.text((x0 + 330, ty), 'Ruta del Tesoro', font=ImageFont.truetype(FS, 30), fill=(200, 210, 255), anchor='mt')
    for i in range(6):
        tx = x0 + 20 + i * 105
        d.rounded_rectangle((tx, ty + 50, tx + 94, ty + 144), 20, fill=(0x7D, 0xD3, 0xFC))
        paste(dst, load(f'{raw}/treasure_{i}.raw'), tx + 7, ty + 57, 80)

    hy = ty + 180
    ov = Image.new('RGBA', dst.size, (0, 0, 0, 0))
    ImageDraw.Draw(ov).rounded_rectangle((x0 + 210, hy, x0 + 450, hy + 90), 45, fill=(0, 0, 0, 72))
    dst.alpha_composite(ov)
    for i, name in enumerate(['heart_full', 'heart_full', 'heart_lost']):
        paste(dst, load(f'{raw}/{name}.raw'), x0 + 225 + i * 72, hy + 9, 72)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--antes')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)

    new_syms = ['Planet_0', 'Rocket_0', 'Helmet_0', 'Comet_0', 'Ufo_0']
    if a.antes:
        im = night(1400, 1060)
        panel(im, a.antes, 20, 30, 'Antes', ['Apple_0', 'Mushroom_0', 'Sun_0', 'Balloon_2', 'Fish_0'])
        panel(im, a.raw, 720, 30, 'Ahora', new_syms)
    else:
        im = night(700, 1060)
        panel(im, a.raw, 20, 30, 'Parejas · Ruta del Tesoro', new_syms)
    im.convert('RGB').save(os.path.join(a.out, 'arte-arcilla.png'))

    # Hoja de íconos: 14 filas x 4 variantes, cada uno sobre la cara crema.
    c = 128
    lab = 250
    sheet = night(lab + 4 * c + 20, len(NEW_KINDS) * c + 20)
    d = ImageDraw.Draw(sheet)
    f = ImageFont.truetype(FS, 28)
    for r, k in enumerate(NEW_KINDS):
        y = 10 + r * c
        d.text((20, y + c / 2), NEW_NAMES[r], font=f, fill=(230, 235, 255), anchor='lm')
        for v in range(4):
            card(sheet, a.raw, 'Front', f'{k}_{v}', lab + v * c, y, c)
    sheet.convert('RGB').save(os.path.join(a.out, 'simbolos-parejas.png'))
    print('OK')


if __name__ == '__main__':
    main()
