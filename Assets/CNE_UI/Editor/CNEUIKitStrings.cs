using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace CookNoEvil.UI.EditorTools
{
    /// <summary>
    /// Kitin metin kaynagi: Assets/CNE_UI/Localization/CNE_Strings.csv (ilk sutun anahtar, digerleri dil kodu).
    /// Yalnizca editorde okunur: kurulumda prefablarin varsayilan yazisi (Turkce) ve CNE_UI tablosu buradan gelir.
    /// Oyunda metinler Unity Localization'daki CNE_UI tablosundan okunur.
    /// </summary>
    sealed class CNEUIKitStrings
    {
        public const string CsvPath = CNEUIKitBuilder.Root + "/Localization/CNE_Strings.csv";
        public const string DefaultLanguage = "tr";

        readonly List<string> columns = new List<string>();          // dil kodlari, CSV sirasiyla
        readonly List<string> keys = new List<string>();
        readonly Dictionary<string, string[]> rows = new Dictionary<string, string[]>();

        public IList<string> Keys { get { return keys; } }
        public IList<string> Languages { get { return columns; } }

        static CNEUIKitStrings cached;
        static DateTime cachedStamp;

        /// <summary>CSV'yi okur (degismediyse onbellekten). Yoksa null.</summary>
        public static CNEUIKitStrings Load()
        {
            if (!File.Exists(CsvPath))
            {
                Debug.LogError("[CNE UI] Metin tablosu bulunamadı: " + CsvPath);
                return null;
            }
            var stamp = File.GetLastWriteTimeUtc(CsvPath);
            if (cached != null && stamp == cachedStamp) return cached;
            var table = new CNEUIKitStrings();
            table.Parse(File.ReadAllText(CsvPath, Encoding.UTF8));
            cached = table;
            cachedStamp = stamp;
            return table;
        }

        /// <summary>Anahtarin varsayilan dildeki (Turkce) metni; yoksa "#anahtar".</summary>
        public string Default(string key)
        {
            string value = Get(key, DefaultLanguage);
            if (string.IsNullOrEmpty(value) && columns.Count > 0) value = Get(key, columns[0]);
            return string.IsNullOrEmpty(value) ? "#" + key : value;
        }

        public string Get(string key, string column)
        {
            string[] cells;
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(column) || !rows.TryGetValue(key, out cells)) return null;
            int c = columns.FindIndex(x => string.Equals(x, column, StringComparison.OrdinalIgnoreCase));
            return c >= 0 && c < cells.Length ? cells[c] : null;
        }

        /// <summary>Locale koduna uyan sutun: once tam kod (pt-BR), sonra dil kismi (tr-TR -> tr). Yoksa null.</summary>
        public string FindColumn(string localeCode)
        {
            if (string.IsNullOrEmpty(localeCode)) return null;
            foreach (var c in columns)
                if (string.Equals(c, localeCode, StringComparison.OrdinalIgnoreCase)) return c;
            int dash = localeCode.IndexOf('-');
            string language = dash > 0 ? localeCode.Substring(0, dash) : localeCode;
            foreach (var c in columns)
                if (string.Equals(c, language, StringComparison.OrdinalIgnoreCase)) return c;
            return null;
        }

        void Parse(string text)
        {
            var table = ParseCsv(text);
            if (table.Count == 0) return;
            var header = table[0];
            for (int c = 1; c < header.Count; c++) columns.Add(header[c].Trim());
            for (int r = 1; r < table.Count; r++)
            {
                var row = table[r];
                if (row.Count == 0) continue;
                string key = row[0].Trim();
                if (key.Length == 0 || key[0] == '#' || rows.ContainsKey(key)) continue; // bos satir, yorum, tekrar
                var cells = new string[columns.Count];
                for (int c = 0; c < cells.Length && c + 1 < row.Count; c++) cells[c] = row[c + 1].Replace("\\n", "\n");
                rows.Add(key, cells);
                keys.Add(key);
            }
        }

        /// <summary>RFC 4180 CSV: tirnakli alanlar, "" kacisi, tirnak icinde satir sonu, basta BOM.</summary>
        static List<List<string>> ParseCsv(string text)
        {
            var result = new List<List<string>>();
            if (string.IsNullOrEmpty(text)) return result;
            int start = text[0] == '﻿' ? 1 : 0;
            var row = new List<string>();
            var sb = new StringBuilder();
            bool quoted = false;
            for (int i = start; i < text.Length; i++)
            {
                char ch = text[i];
                if (quoted)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { sb.Append('"'); i++; }
                        else quoted = false;
                    }
                    else sb.Append(ch);
                }
                else if (ch == '"') quoted = true;
                else if (ch == ',') { row.Add(sb.ToString()); sb.Length = 0; }
                else if (ch == '\r') { }
                else if (ch == '\n')
                {
                    row.Add(sb.ToString());
                    sb.Length = 0;
                    result.Add(row);
                    row = new List<string>();
                }
                else sb.Append(ch);
            }
            if (sb.Length > 0 || row.Count > 0)
            {
                row.Add(sb.ToString());
                result.Add(row);
            }
            return result;
        }
    }
}
