using UnityEngine;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia;
using NeuroVida.Games.Parejas;
using NeuroVida.Games.Stroop;
using NeuroVida.Games.Comparacion;
using NeuroVida.Games.CambioChip;
using NeuroVida.Games.RutaTesoro;
using NeuroVida.Games.Series;
using NeuroVida.Games.Calculo;
using NeuroVida.Games.Anagramas;

namespace NeuroVida.Bridge
{
    /// <summary>
    /// Punto de entrada Nativo -> Unity. El lado Kotlin invoca
    /// <c>UnityPlayer.UnitySendMessage("GameBridge", "InitializeGameConfig", json)</c> —
    /// ver plantilla en
    /// <c>unity/NeuroVidaCore/NativeBridgeTemplates/android/UnityBridgeController.kt</c>.
    ///
    /// Convención fijada acá (documentar en el lado nativo cuando se conecte de verdad):
    /// GameObject en la escena llamado EXACTAMENTE "GameBridge", con este componente.
    ///
    /// <paramref name="json"/> siempre se parsea como <see cref="SequenceInitConfig"/> --
    /// el contrato de entrada (user_id/game_id/config con level/base_intensity/timed/
    /// age_band/sound_enabled) es genérico entre juegos a propósito desde el día 1 del
    /// roadmap (ver NeuroVida/CLAUDE.md), así que "Parejas Ocultas" (Fase 2) lo reusa tal
    /// cual en vez de tener su propia clase de config duplicada.
    /// </summary>
    public class GameEntryPoint : MonoBehaviour
    {
        [SerializeField] private SequenceGameController sequenceGameController;
        [SerializeField] private CardsGameController cardsGameController;
        [SerializeField] private StroopGameController stroopGameController;
        [SerializeField] private ComparisonGameController comparisonGameController;
        [SerializeField] private ChipGameController chipGameController;
        [SerializeField] private TreasureGameController treasureGameController;
        [SerializeField] private SeriesGameController seriesGameController;
        [SerializeField] private CalculoGameController calculoGameController;
        [SerializeField] private AnagramGameController anagramGameController;

        /// <summary>Los 9 juegos arman su interfaz una sola vez para 1080x1920 vertical: al girar el teléfono se
        /// desarmaban. Se fija la orientación antes de cargar la escena, además de Player Settings (Portrait) y del
        /// manifest de la app, para que no dependa de que el export esté al día.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LockPortrait()
        {
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = ScreenOrientation.Portrait;
        }

        /// <summary>Botón Atrás de Android (llega como Escape): cierra la pantalla de Unity y vuelve a la app.
        /// A mitad de partida no se guarda nada (el resultado solo se envía al terminar).</summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) NativeBridge.CloseGameScreen();
        }

        /// <summary>Invocado por <c>UnityPlayer.UnitySendMessage</c> desde el lado nativo.
        /// <paramref name="json"/> es un <see cref="SequenceInitConfig"/> serializado.</summary>
        public void InitializeGameConfig(string json)
        {
            var config = JsonUtility.FromJson<SequenceInitConfig>(json);
            if (config?.config == null)
            {
                Debug.LogError("[GameEntryPoint] JSON de configuración inválido o incompleto: " + json);
                return;
            }

            switch (config.game_id)
            {
                case SequenceGameController.GameId:
                    if (sequenceGameController == null)
                    {
                        // Sin wiring en el Editor todavía: se crea el controlador por
                        // código, mismo criterio que el resto de la UI de este piloto
                        // (ver nota en SequenceGameController). Una vez migrado a
                        // prefab, asignar la referencia en el Inspector evita esto.
                        var go = new GameObject("SequenceGameController");
                        go.transform.SetParent(transform, false);
                        sequenceGameController = go.AddComponent<SequenceGameController>();
                    }
                    sequenceGameController.gameObject.SetActive(true);
                    sequenceGameController.StartSession(config);
                    break;
                case CardsGameController.GameId:
                    if (cardsGameController == null)
                    {
                        var go = new GameObject("CardsGameController");
                        go.transform.SetParent(transform, false);
                        cardsGameController = go.AddComponent<CardsGameController>();
                    }
                    cardsGameController.gameObject.SetActive(true);
                    cardsGameController.StartSession(config);
                    break;
                case StroopGameController.GameId:
                    if (stroopGameController == null)
                    {
                        var go = new GameObject("StroopGameController");
                        go.transform.SetParent(transform, false);
                        stroopGameController = go.AddComponent<StroopGameController>();
                    }
                    stroopGameController.gameObject.SetActive(true);
                    stroopGameController.StartSession(config);
                    break;
                case ComparisonGameController.GameId:
                    if (comparisonGameController == null)
                    {
                        var go = new GameObject("ComparisonGameController");
                        go.transform.SetParent(transform, false);
                        comparisonGameController = go.AddComponent<ComparisonGameController>();
                    }
                    comparisonGameController.gameObject.SetActive(true);
                    comparisonGameController.StartSession(config);
                    break;
                case ChipGameController.GameId:
                    if (chipGameController == null)
                    {
                        var go = new GameObject("ChipGameController");
                        go.transform.SetParent(transform, false);
                        chipGameController = go.AddComponent<ChipGameController>();
                    }
                    chipGameController.gameObject.SetActive(true);
                    chipGameController.StartSession(config);
                    break;
                case TreasureGameController.GameId:
                    if (treasureGameController == null)
                    {
                        var go = new GameObject("TreasureGameController");
                        go.transform.SetParent(transform, false);
                        treasureGameController = go.AddComponent<TreasureGameController>();
                    }
                    treasureGameController.gameObject.SetActive(true);
                    treasureGameController.StartSession(config);
                    break;
                case SeriesGameController.GameId:
                    if (seriesGameController == null)
                    {
                        var go = new GameObject("SeriesGameController");
                        go.transform.SetParent(transform, false);
                        seriesGameController = go.AddComponent<SeriesGameController>();
                    }
                    seriesGameController.gameObject.SetActive(true);
                    seriesGameController.StartSession(config);
                    break;
                case CalculoGameController.GameId:
                    if (calculoGameController == null)
                    {
                        var go = new GameObject("CalculoGameController");
                        go.transform.SetParent(transform, false);
                        calculoGameController = go.AddComponent<CalculoGameController>();
                    }
                    calculoGameController.gameObject.SetActive(true);
                    calculoGameController.StartSession(config);
                    break;
                case AnagramGameController.GameId:
                    if (anagramGameController == null)
                    {
                        var go = new GameObject("AnagramGameController");
                        go.transform.SetParent(transform, false);
                        anagramGameController = go.AddComponent<AnagramGameController>();
                    }
                    anagramGameController.gameObject.SetActive(true);
                    anagramGameController.StartSession(config);
                    break;
                default:
                    Debug.LogError($"[GameEntryPoint] game_id {config.game_id} no tiene un controlador registrado todavía.");
                    break;
            }
        }
    }
}
