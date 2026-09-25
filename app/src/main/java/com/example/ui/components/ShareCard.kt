package com.example.ui.components

import android.content.ClipData
import android.content.Context
import android.content.Intent
import android.graphics.Bitmap
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Canvas
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.Shadow
import androidx.compose.ui.graphics.asAndroidBitmap
import androidx.compose.ui.graphics.drawscope.CanvasDrawScope
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.inset
import androidx.compose.ui.text.TextMeasurer
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.drawText
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.font.createFontFamilyResolver
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Constraints
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.LayoutDirection
import androidx.compose.ui.unit.sp
import androidx.core.content.FileProvider
import com.example.model.RankTier
import com.example.ui.theme.Clay
import com.example.ui.theme.FredokaFamily
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.io.File
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin
import kotlin.random.Random

/**
 * Tarjeta para compartir (imagen 1080x1920, formato de historia) con el sello de la app: cielo nocturno con
 * estrellas, el escudo de la liga sobre rayos de su color, el titular ("Subí a Plata"), dónde, trofeos y racha,
 * y la marca. Se dibuja fuera de pantalla con las mismas funciones que la app ([drawLeagueShield], Fredoka), así
 * se ve igual que adentro, y se comparte con la hoja de compartir de Android (WhatsApp, Instagram, etc.).
 */
object ShareCard {
  data class Content(
    val headline: String,       // "Subí a Plata" / "Estoy en liga Oro"
    val subtitle: String,       // "en Secuencia Lumínica" / "Mi liga general"
    val tier: RankTier,
    val rating: Int,
    val streak: Int
  )

  private const val W = 1080
  private const val H = 1920

  /** Dibuja la tarjeta. Puede correr fuera del hilo principal. */
  fun render(context: Context, content: Content): Bitmap {
    val image = ImageBitmap(W, H)
    val density = Density(1f) // 1 unidad = 1 px de la imagen
    val measurer = TextMeasurer(createFontFamilyResolver(context), density, LayoutDirection.Ltr)
    CanvasDrawScope().draw(density, LayoutDirection.Ltr, Canvas(image), Size(W.toFloat(), H.toFloat())) {
      drawCard(content, measurer)
    }
    return image.asAndroidBitmap()
  }

  /** Genera la imagen, la guarda en caché y abre la hoja de compartir con ella y [text]. */
  suspend fun share(context: Context, content: Content, text: String) {
    val uri = withContext(Dispatchers.Default) {
      val bitmap = render(context, content)
      val dir = File(context.cacheDir, "share").apply { mkdirs() }
      val file = File(dir, "neurovida-liga.png")
      file.outputStream().use { bitmap.compress(Bitmap.CompressFormat.PNG, 100, it) }
      FileProvider.getUriForFile(context, "${context.packageName}.fileprovider", file)
    }
    val send = Intent(Intent.ACTION_SEND)
      .setType("image/png")
      .putExtra(Intent.EXTRA_STREAM, uri)
      .putExtra(Intent.EXTRA_TEXT, text)
      .addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
    send.clipData = ClipData.newRawUri("", uri) // para que la app elegida reciba el permiso de lectura
    context.startActivity(Intent.createChooser(send, "Compartir").addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION))
  }

  private fun DrawScope.drawCard(content: Content, measurer: TextMeasurer) {
    val w = size.width
    val h = size.height
    val pal = content.tier.palette()

    // Cielo nocturno de la app + nebulosas celeste y coral
    drawRect(Brush.verticalGradient(listOf(Color(0xFF101A58), Color(0xFF080E3A), Color(0xFF04061C))))
    glow(Offset(w * 0.88f, h * 0.10f), 950f, Clay.Sky.copy(alpha = 0.30f))
    glow(Offset(w * 0.08f, h * 0.92f), 950f, Clay.Coral.copy(alpha = 0.26f))
    glow(Offset(w * 0.15f, h * 0.45f), 700f, Clay.Grape.copy(alpha = 0.16f))

    // Estrellas (siempre las mismas: la tarjeta no cambia al volver a generarla)
    val rnd = Random(7)
    repeat(170) { i ->
      val p = Offset(rnd.nextFloat() * w, rnd.nextFloat() * h)
      val r = 1.5f + rnd.nextFloat() * 3.5f
      val c = if (i % 6 == 0) Color(0xFFFFC38A) else Color.White
      drawCircle(c.copy(alpha = 0.3f + rnd.nextFloat() * 0.7f), r, p)
    }

    // Marca arriba
    val brand = TextStyle(fontFamily = FredokaFamily, fontWeight = FontWeight.Bold, fontSize = 64.sp, color = Color.White)
    centered(measurer, "NeuroVida", brand, 130f)
    sparkle(Offset(w / 2f + 190f, 150f), 22f, Clay.Sun)

    // Escudo sobre rayos del color de la liga
    val shieldW = 470f
    val shieldH = shieldW * 1.12f
    val c = Offset(w / 2f, 720f)
    glow(c, 560f, pal.glow.copy(alpha = 0.55f))
    val rays = 14
    for (i in 0 until rays) {
      val a = (i * 360f / rays - 90f) * PI.toFloat() / 180f
      val half = 0.085f
      val reach = 640f
      val ray = Path().apply {
        moveTo(c.x, c.y)
        lineTo(c.x + cos(a - half) * reach, c.y + sin(a - half) * reach)
        lineTo(c.x + cos(a + half) * reach, c.y + sin(a + half) * reach)
        close()
      }
      drawPath(ray, Brush.radialGradient(listOf(pal.light.copy(alpha = 0.30f), Color.Transparent), c, reach))
    }
    inset(c.x - shieldW / 2f, c.y - shieldH / 2f, w - (c.x + shieldW / 2f), h - (c.y + shieldH / 2f)) {
      drawLeagueShield(content.tier, pips = 1)
    }

    // Titular, dónde y cifras
    val headline = TextStyle(
      fontFamily = FredokaFamily, fontWeight = FontWeight.Bold, fontSize = 116.sp, lineHeight = 124.sp,
      color = pal.light, textAlign = TextAlign.Center, shadow = Shadow(Clay.Ink, Offset(0f, 10f), 0f)
    )
    var y = centered(measurer, content.headline, headline, 1070f)
    val sub = TextStyle(fontFamily = FredokaFamily, fontWeight = FontWeight.SemiBold, fontSize = 54.sp, color = Color.White, textAlign = TextAlign.Center)
    y = centered(measurer, content.subtitle, sub, y + 18f)

    val num = TextStyle(fontFamily = FredokaFamily, fontWeight = FontWeight.Bold, fontSize = 92.sp, color = Clay.Sun, textAlign = TextAlign.Center)
    val lbl = TextStyle(fontFamily = FredokaFamily, fontSize = 38.sp, color = Color(0xFFB4BFEA), textAlign = TextAlign.Center)
    val stats = buildList {
      add("${content.rating}" to "trofeos")
      if (content.streak > 0) add("${content.streak}" to if (content.streak == 1) "día de racha" else "días de racha")
    }
    val statsTop = y + 110f
    val colW = w / stats.size
    stats.forEachIndexed { i, (n, l) ->
      val left = colW * i
      val nb = measurer.measure(n, num, constraints = Constraints.fixedWidth(colW.toInt()))
      drawText(nb, topLeft = Offset(left, statsTop))
      val lb = measurer.measure(l, lbl, constraints = Constraints.fixedWidth(colW.toInt()))
      drawText(lb, topLeft = Offset(left, statsTop + nb.size.height - 6f))
    }
    if (stats.size == 2) drawCircle(Color(0xFFB4BFEA).copy(alpha = 0.6f), 7f, Offset(w / 2f, statsTop + 70f))

    // Pie
    val foot = TextStyle(fontFamily = FredokaFamily, fontWeight = FontWeight.SemiBold, fontSize = 40.sp, color = Color.White.copy(alpha = 0.7f), textAlign = TextAlign.Center)
    centered(measurer, "Juega. Entrena. Sube de liga.", foot, h - 170f)
  }

  /** Texto centrado a todo el ancho (con márgenes); devuelve la coordenada Y de su borde inferior. */
  private fun DrawScope.centered(measurer: TextMeasurer, text: String, style: TextStyle, top: Float): Float {
    val margin = 70f
    val layout = measurer.measure(text, style.copy(textAlign = TextAlign.Center), constraints = Constraints.fixedWidth((size.width - margin * 2).toInt()))
    drawText(layout, topLeft = Offset(margin, top))
    return top + layout.size.height
  }

  private fun DrawScope.glow(c: Offset, r: Float, color: Color) {
    drawCircle(Brush.radialGradient(listOf(color, Color.Transparent), c, r), radius = r, center = c)
  }

  private fun DrawScope.sparkle(c: Offset, r: Float, color: Color) {
    val p = Path().apply {
      moveTo(c.x, c.y - r)
      quadraticBezierTo(c.x, c.y, c.x + r, c.y)
      quadraticBezierTo(c.x, c.y, c.x, c.y + r)
      quadraticBezierTo(c.x, c.y, c.x - r, c.y)
      quadraticBezierTo(c.x, c.y, c.x, c.y - r)
      close()
    }
    drawPath(p, color)
  }
}
