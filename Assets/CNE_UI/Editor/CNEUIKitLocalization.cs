using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

namespace CookNoEvil.UI.EditorTools
{
    /// <summary>
    /// Kit metinlerini Unity Localization'a yazar: "CNE_UI" string tablo koleksiyonu (projenin UIStrings'ine dokunmaz).
    ///  - Koleksiyon yoksa Assets/CNE_UI/Localization/Tables altina, projede tanimli Locale'lerle kurulur.
    ///  - Yalnizca projede tanimli dillere yazar; yeni dil (Locale) eklemez.
    ///  - Yalnizca bos girdileri doldurur; dolu girdilere (cevirmen duzeltmeleri) dokunmaz.
    ///  - CSV'de sutunu olmayan dil icin girdi acmaz (Localization'in eksik ceviri davranisi gecerli kalir).
    /// Pencere acmaz; otomasyonda guvenle cagrilabilir.
    /// </summary>
    static class CNEUIKitLocalization
    {
        public const string TableFolder = CNEUIKitBuilder.Root + "/Localization/Tables";

        /// <summary>Tabloyu kurar/gunceller. Localization ayarlari ya da hic Locale yoksa false.</summary>
        public static bool SyncStringTable()
        {
            if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
            {
                Debug.LogWarning("[CNE UI] Localization Settings bulunamadı; CNE_UI tablosu kurulmadı (Project Settings ▸ Localization).");
                return false;
            }
            var strings = CNEUIKitStrings.Load();
            if (strings == null) return false;

            var locales = LocalizationEditorSettings.GetLocales();
            if (locales == null || locales.Count == 0)
            {
                Debug.LogWarning("[CNE UI] Projede hiç Locale yok; CNE_UI tablosu kurulmadı.");
                return false;
            }

            var collection = LocalizationEditorSettings.GetStringTableCollection(CNELocalization.TableName);
            if (collection == null)
            {
                CNEUIKitBuilder.EnsureFolder(TableFolder);
                collection = LocalizationEditorSettings.CreateStringTableCollection(CNELocalization.TableName, TableFolder, locales);
                Debug.Log("[CNE UI] " + CNELocalization.TableName + " tablosu kuruldu: " + TableFolder);
            }
            else
            {
                foreach (var locale in locales)
                    if (locale != null && !collection.ContainsTable(locale.Identifier)) collection.AddNewTable(locale.Identifier);
            }

            int written = 0;
            foreach (var table in collection.StringTables)
            {
                if (table == null) continue;
                string column = strings.FindColumn(table.LocaleIdentifier.Code);
                if (column == null) continue;
                bool changed = false;
                foreach (var key in strings.Keys)
                {
                    var entry = table.GetEntry(key);
                    if (entry != null && !string.IsNullOrEmpty(entry.Value)) continue;
                    string value = strings.Get(key, column);
                    if (string.IsNullOrEmpty(value)) continue;
                    table.AddEntry(key, value);
                    written++;
                    changed = true;
                }
                if (changed) EditorUtility.SetDirty(table);
            }
            if (written > 0)
            {
                EditorUtility.SetDirty(collection.SharedData);
                AssetDatabase.SaveAssets();
            }
            Debug.Log("[CNE UI] " + CNELocalization.TableName + " tablosu güncel (" + written + " girdi yazıldı, " + collection.StringTables.Count + " dil).");
            return true;
        }
    }
}
