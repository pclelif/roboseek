using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Robot.UI.Production
{
    [DefaultExecutionOrder(50)]
    public sealed class UILocalization : MonoBehaviour
    {
        private const string LanguageKey = "RoboSeek.Language";
        private readonly Dictionary<Text, string> english = new Dictionary<Text, string>();
        public static bool IsTurkish => false;
        public static event Action LanguageChanged;

        private void Awake() { Capture(); }
        private void OnEnable() { LanguageChanged += Apply; }
        private void Start() { Apply(); }
        private void OnDisable() { LanguageChanged -= Apply; }

        // Language switching is temporarily unavailable; saved Turkish preferences
        // are ignored so every entry point uses English.
        public void ToggleLanguage() { }

        private void Capture()
        {
            foreach (var label in GetComponentsInChildren<Text>(true))
                if (label != null && !english.ContainsKey(label)) english.Add(label, label.text);
        }

        private void Apply()
        {
            Capture();
            foreach (var entry in english)
                if (entry.Key != null) entry.Key.text = Translate(entry.Value);
            var root = GetComponent<UIRootController>();
            // The bottom lobby button is the map selector; preserve its map text.
            if (root != null && root.language != null && (root.hub == null || root.hub.mapButton != root.language))
                root.language.GetComponentInChildren<Text>().text = IsTurkish ? "DİL: TÜRKÇE" : "LANGUAGE: ENGLISH";
            if (root != null && root.resume != null) root.resume.GetComponentInChildren<Text>().text = Translate("RETURN TO GAME");
            var lobbyTitle = transform.Find("LobbyScreen/RightControlDeck/DeckLabel")?.GetComponent<Text>();
            if (lobbyTitle != null) lobbyTitle.text = Translate("LOBBY");
            root?.hub?.Refresh();
            root?.ApplyTypography();
        }

        public static string Translate(string value)
        {
            if (!IsTurkish || string.IsNullOrEmpty(value)) return value;
            return Turkish.TryGetValue(value, out string translated) ? translated : value;
        }

        public static string EnglishColor(string value)
        {
            if (string.IsNullOrEmpty(value)) return "DEFAULT";
            // Theme names are authored in Turkish. Normalize their dotted/dotless
            // characters before looking them up in the English lobby vocabulary.
            string normalized = value.Trim().ToUpperInvariant()
                .Replace("Ç", "C").Replace("Ğ", "G").Replace("İ", "I")
                .Replace("İ", "I").Replace("Ö", "O").Replace("Ş", "S")
                .Replace("Ü", "U").Replace("ı", "I");
            switch (normalized)
            {
                case "SİYAH": case "SIYAH": case "BLACK": return "BLACK";
                case "KIRMIZI": case "RED": return "RED";
                case "TURUNCU": case "ORANGE": return "ORANGE";
                case "SARI": case "YELLOW": return "YELLOW";
                case "YEŞİL": case "YESIL": case "GREEN": return "GREEN";
                case "MAVİ": case "MAVI": case "BLUE": return "BLUE";
                case "MOR": case "PURPLE": return "PURPLE";
                case "PEMBE": case "PINK": return "PINK";
                case "KAHVERENGİ": case "KAHVERENGI": case "BROWN": return "BROWN";
                case "BEYAZ": case "WHITE": return "WHITE";
                default: return value.Trim().ToUpperInvariant();
            }
        }

        private static readonly Dictionary<string, string> Turkish = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "WHERE IS MY TOY?", "OYUNCAĞIM NEREDE?" }, { "GAME MODE", "OYUN MODU" }, { "MAP", "HARİTA" },
            { "SOLO", "TEK KİŞİLİK" }, { "MULTIPLAYER", "ÇOK OYUNCULU" }, { "START GAME", "OYUNU BAŞLAT" },
            { "ROBOT COLOR", "ROBOT RENGİ" }, { "SETTINGS", "AYARLAR" }, { "CONTROLS", "KONTROLLER" },
            { "BACK", "GERİ" }, { "QUIT", "ÇIKIŞ" }, { "LANGUAGE: ENGLISH", "DİL: TÜRKÇE" },
            { "LANGUAGE: TURKISH", "DİL: İNGİLİZCE" }, { "SOLO: Search for hidden toys alone!", "TEK KİŞİLİK: Gizli oyuncakları tek başına bul!" },
            { "MULTIPLAYER: Co-op mode coming soon!", "ÇOK OYUNCULU: Eşli mod yakında!" }, { "Select a Game Mode to begin.", "Başlamak için bir oyun modu seç." },
            { "TARGET RETRIEVED", "HEDEF BULUNDU" }, { "FIND THESE", "BUNLARI BUL" }, { "FIND ALL 3 TARGETS", "3 HEDEFİ DE BUL" }, { "GO!", "BAŞLA!" },
            { "RETURN TO GAME", "OYUNA DÖN" }, { "LOBBY", "LOBİ" },
            { "PAUSED", "DURAKLATILDI" }, { "RESUME", "DEVAM ET" }, { "RESTART ROUND", "TURU YENİDEN BAŞLAT" },
            { "QUIT TO LOBBY", "LOBİYE DÖN" }, { "CANCEL", "İPTAL" }, { "CONFIRM", "ONAYLA" },
            { "ROUND COMPLETE", "TUR TAMAMLANDI" }, { "ROUND OVER", "TUR BİTTİ" }, { "TARGETS FOUND", "BULUNAN HEDEFLER" },
            { "NEXT ROUND", "SONRAKİ TUR" }, { "RETURN TO LOBBY", "LOBİYE DÖN" }, { "TIME", "SÜRE" }, { "SCORE", "SKOR" },
            { "MOVE", "HAREKET" }, { "JUMP", "ZIPLA" }, { "CAMERA", "KAMERA" }, { "SCAN & RETRIEVE", "TARA VE AL" },
            { "ATTACK", "SALDIRI" }, { "PAUSE", "DURAKLAT" }, { "LOOK AROUND", "ETRAFA BAK" },
            { "AUDIO", "SES" }, { "GRAPHICS", "GÖRÜNTÜ" }, { "DISPLAY MODE", "EKRAN MODU" },
            { "RESOLUTION", "ÇÖZÜNÜRLÜK" }, { "QUALITY", "KALİTE" }, { "VSYNC", "DİKEY EŞLEME" },
            { "VIEW CONTROLS", "KONTROLLERİ GÖR" }, { "Mouse Sensitivity", "Fare Hassasiyeti" },
            { "Master Volume", "Ana Ses" }, { "Music Volume", "Müzik Sesi" }, { "SFX Volume", "Efekt Sesi" },
            { "RESTART ROUND?", "TUR YENİDEN BAŞLATILSIN MI?" }, { "Your current progress will be lost.", "Mevcut ilerlemen kaybolacak." },
            { "QUIT TO LOBBY?", "LOBİYE DÖNÜLSÜN MÜ?" }, { "Your current round will end.", "Mevcut turun sona erecek." },
            { "RESTART", "YENİDEN BAŞLAT" },
            { "BLACK", "SİYAH" }, { "RED", "KIRMIZI" }, { "ORANGE", "TURUNCU" }, { "YELLOW", "SARI" },
            { "GREEN", "YEŞİL" }, { "BLUE", "MAVİ" }, { "PURPLE", "MOR" }, { "PINK", "PEMBE" },
            { "BROWN", "KAHVERENGİ" }, { "WHITE", "BEYAZ" }, { "DEFAULT", "VARSAYILAN" },
            { "CITY", "ŞEHİR" }, { "ADVENTURE", "MACERA" }, { "TOWN", "KASABA" }, { "ARENA", "ARENA" },
            { "POLYGON", "KASABA" }, { "POLYGONSTARTER", "ARENA" }
        };
    }
}
