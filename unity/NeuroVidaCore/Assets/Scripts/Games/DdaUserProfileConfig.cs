using System;

namespace NeuroVida.Games
{
    /// <summary>
    /// Puerto 1:1 de <c>com.example.games.parejas.DdaUserProfileConfig</c> /
    /// <c>ddaProfileConfigFor</c> (Kotlin). Piloto de perfiles por edad — hoy cubre
    /// Parejas Ocultas y Secuencia Lumínica del lado Android; acá se porta completo
    /// (no solo los campos que usa Secuencia) para que el próximo juego migrado a Unity
    /// no tenga que redefinirlo.
    /// </summary>
    public enum AgeBand
    {
        Senior,
        Adult,
        Under18
    }

    [Serializable]
    public struct DdaUserProfileConfig
    {
        public float WeightAccuracy;
        public float WeightReactionTime;
        public float MinPreviewExposureMs;
        public bool AllowStrictTimeouts;
        public float IdealTouchTargetMm;
        public float MinSpacingMm;
        public float MaxSpacingMm;

        public static DdaUserProfileConfig For(AgeBand ageBand)
        {
            switch (ageBand)
            {
                case AgeBand.Senior:
                    return new DdaUserProfileConfig
                    {
                        WeightAccuracy = 0.85f,
                        WeightReactionTime = 0.15f,
                        MinPreviewExposureMs = 1500f,
                        AllowStrictTimeouts = false,
                        IdealTouchTargetMm = 20f,
                        MinSpacingMm = 3.2f,
                        MaxSpacingMm = 12.5f
                    };
                case AgeBand.Under18:
                    return new DdaUserProfileConfig
                    {
                        WeightAccuracy = 0.60f,
                        WeightReactionTime = 0.40f,
                        MinPreviewExposureMs = 800f,
                        AllowStrictTimeouts = false,
                        IdealTouchTargetMm = 17f,
                        MinSpacingMm = 2.5f,
                        MaxSpacingMm = 8f
                    };
                case AgeBand.Adult:
                default:
                    return new DdaUserProfileConfig
                    {
                        WeightAccuracy = 0.65f,
                        WeightReactionTime = 0.35f,
                        MinPreviewExposureMs = 500f,
                        AllowStrictTimeouts = true,
                        IdealTouchTargetMm = 15f,
                        MinSpacingMm = 1.5f,
                        MaxSpacingMm = 6f
                    };
            }
        }

        public static AgeBand ParseAgeBand(string raw)
        {
            switch ((raw ?? "ADULT").ToUpperInvariant())
            {
                case "SENIOR": return AgeBand.Senior;
                case "UNDER_18": return AgeBand.Under18;
                default: return AgeBand.Adult;
            }
        }
    }
}
