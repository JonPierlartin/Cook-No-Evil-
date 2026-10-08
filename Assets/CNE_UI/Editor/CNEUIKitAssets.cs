using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CookNoEvil.UI.EditorTools
{
    /// <summary>
    /// Paketle gelen asset'leri denetler, bozuksa onarir. "Kur" her calistiginda cagrilir:
    ///  • Art altindaki PNG'ler: Sprite tipi, 9-dilim kenarlari, PPU, pivot, mipmap kapali.
    ///  • Resources/CNE_UISoundSet: yoksa ya da klipleri eksikse asagidaki ses tablosuyla doldurulur.
    ///  • Resources/CNE_Languages: dillerin gorunusu (bayrak, ad); yoksa Turkce + English ile kurulur, eksik bayraklar tamamlanir.
    ///    Hangi dillerin secilebilecegini Unity Localization belirler; bu liste yalnizca gorunus bilgisidir.
    /// Ses tablosunun tek kaynagi burasidir; tools/build_package.py paketteki CNE_UISoundSet.asset'i
    /// bu dosyadan okuyarak uretir. Kullanicinin elle yaptigi ayarlara dokunulmaz, yalnizca eksik/bozuk olan tamamlanir.
    /// </summary>
    static class CNEUIKitAssets
    {
        const string Root = CNEUIKitBuilder.Root;
        public const string SoundSetPath = Root + "/Resources/CNE_UISoundSet.asset";
        public const string LanguagesPath = Root + "/Resources/CNE_Languages.asset";
        const string AudioFolder = Root + "/Audio/UI/";
        const string FlagFolder = Root + "/Art/Flags/";

        struct SoundDefault
        {
            public readonly CNEUISound sound;
            public readonly float volume, pitchJitter, minInterval;
            public readonly string[] clips;

            public SoundDefault(CNEUISound sound, float volume, float pitchJitter, float minInterval, params string[] clips)
            {
                this.sound = sound;
                this.volume = volume;
                this.pitchJitter = pitchJitter;
                this.minInterval = minInterval;
                this.clips = clips;
            }
        }

        // ses, seviye, perde oynamasi (+/-), en kisa tekrar araligi (sn), klipler (Audio/UI)
        static readonly SoundDefault[] Sounds =
        {
            new SoundDefault(CNEUISound.Hover,        0.55f, 0.05f, 0.035f, "ui_hover_01", "ui_hover_02", "ui_hover_03"),
            new SoundDefault(CNEUISound.Press,        0.80f, 0.04f, 0.03f,  "ui_press_01", "ui_press_02", "ui_press_03"),
            new SoundDefault(CNEUISound.Release,      0.60f, 0.04f, 0.03f,  "ui_release_01", "ui_release_02"),
            new SoundDefault(CNEUISound.Confirm,      0.75f, 0.01f, 0.15f,  "ui_confirm_bell"),
            new SoundDefault(CNEUISound.Back,         0.80f, 0.04f, 0.08f,  "ui_back"),
            new SoundDefault(CNEUISound.SwitchOn,     0.85f, 0.03f, 0.05f,  "ui_switch_on"),
            new SoundDefault(CNEUISound.SwitchOff,    0.85f, 0.03f, 0.05f,  "ui_switch_off"),
            new SoundDefault(CNEUISound.KnobTick,     0.70f, 0.04f, 0.025f, "ui_knob_tick_01", "ui_knob_tick_02", "ui_knob_tick_03"),
            new SoundDefault(CNEUISound.SliderTick,   0.60f, 0.06f, 0.03f,  "ui_slider_tick"),
            new SoundDefault(CNEUISound.PanelOpen,    0.80f, 0.02f, 0.10f,  "ui_panel_open"),
            new SoundDefault(CNEUISound.PanelClose,   0.70f, 0.02f, 0.10f,  "ui_panel_close"),
            new SoundDefault(CNEUISound.FlagFlap,     0.80f, 0.06f, 0.10f,  "ui_flag_flap"),
            new SoundDefault(CNEUISound.NeonOn,       0.70f, 0.00f, 0.30f,  "ui_neon_on"),
            new SoundDefault(CNEUISound.NeedleDrop,   0.75f, 0.03f, 0.12f,  "ui_needle_drop"),
            new SoundDefault(CNEUISound.Error,        0.60f, 0.00f, 0.20f,  "ui_error_buzz"),
            new SoundDefault(CNEUISound.BoardPress,   0.85f, 0.04f, 0.05f,  "ui_board_press"),
            new SoundDefault(CNEUISound.CashRegister, 0.80f, 0.01f, 0.20f,  "ui_cash_register"),
        };

        // ---------------------------------------------------------------- sprite ice aktarma ayarlari
        /// <summary>Kit gorsellerinin ice aktarma ayarlarini denetler; uymayanlari duzeltip yeniden ice aktarir.</summary>
        public static void EnsureSpriteImportSettings()
        {
            int fixedCount = 0;
            var specs = CNEUIKitSpriteSpecs.All;
            for (int i = 0; i < specs.Length; i++)
            {
                var spec = specs[i];
                var importer = AssetImporter.GetAtPath(spec.path) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogError("[CNE UI] Görsel bulunamadı: " + spec.path);
                    continue;
                }
                if (Matches(importer, spec)) continue;
                Apply(importer, spec);
                importer.SaveAndReimport();
                fixedCount++;
            }
            if (fixedCount > 0)
                Debug.Log("[CNE UI] " + fixedCount + " görselin içe aktarma ayarı kit değerlerine getirildi.");
        }

        static bool Matches(TextureImporter importer, CNEUIKitSpriteSpecs.Spec spec)
        {
            if (spec.cursor) return importer.textureType == TextureImporterType.Cursor;
            if (importer.textureType != TextureImporterType.Sprite) return false;
            if (importer.spriteImportMode != SpriteImportMode.Single) return false;
            if (importer.mipmapEnabled) return false;
            if (Mathf.Abs(importer.spritePixelsPerUnit - spec.ppu) > 0.01f) return false;
            if (!Near(importer.spriteBorder, spec.border)) return false;
            if (spec.CustomPivot)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteAlignment != (int)SpriteAlignment.Custom) return false;
                if (Mathf.Abs(settings.spritePivot.x - spec.pivot.x) > 0.001f || Mathf.Abs(settings.spritePivot.y - spec.pivot.y) > 0.001f) return false;
            }
            return true;
        }

        static bool Near(Vector4 a, Vector4 b)
        {
            return Mathf.Abs(a.x - b.x) < 0.01f && Mathf.Abs(a.y - b.y) < 0.01f && Mathf.Abs(a.z - b.z) < 0.01f && Mathf.Abs(a.w - b.w) < 0.01f;
        }

        static void Apply(TextureImporter importer, CNEUIKitSpriteSpecs.Spec spec)
        {
            var s = new TextureImporterSettings();
            importer.ReadTextureSettings(s);
            s.textureType = spec.cursor ? TextureImporterType.Cursor : TextureImporterType.Sprite;
            s.mipmapEnabled = false;
            s.sRGBTexture = true;
            s.alphaIsTransparency = true;
            s.readable = spec.cursor;
            s.npotScale = TextureImporterNPOTScale.None;
            s.filterMode = FilterMode.Bilinear;
            s.wrapMode = spec.repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            if (!spec.cursor)
            {
                s.spriteMode = (int)SpriteImportMode.Single;
                s.spritePixelsPerUnit = spec.ppu;
                s.spriteBorder = spec.border;
                s.spriteMeshType = SpriteMeshType.FullRect;
                s.spriteAlignment = (int)(spec.CustomPivot ? SpriteAlignment.Custom : SpriteAlignment.Center);
                s.spritePivot = spec.pivot;
                s.spriteGenerateFallbackPhysicsShape = false;
            }
            importer.SetTextureSettings(s);
            importer.maxTextureSize = 2048;
            importer.textureCompression = spec.compressed ? TextureImporterCompression.Compressed : TextureImporterCompression.Uncompressed;
        }

        // ---------------------------------------------------------------- ses seti
        /// <summary>Resources/CNE_UISoundSet'i yukler; yoksa kurar, klibi eksik olan sesleri tablodan tamamlar.</summary>
        public static CNEUISoundSet EnsureSoundSet()
        {
            var set = LoadOrCreate<CNEUISoundSet>(SoundSetPath);
            if (set == null) return null;

            var entries = new List<CNEUISoundSet.Entry>();
            if (set.entries != null) entries.AddRange(set.entries);
            bool changed = false;
            for (int i = 0; i < Sounds.Length; i++)
            {
                var d = Sounds[i];
                var entry = entries.Find(e => e != null && e.sound == d.sound);
                if (entry != null && HasClips(entry.clips)) continue;

                var clips = LoadClips(d.clips);
                if (clips.Length == 0)
                {
                    Debug.LogWarning("[CNE UI] " + d.sound + " sesi için klip bulunamadı (" + AudioFolder + ").");
                    continue;
                }
                if (entry == null)
                {
                    entry = new CNEUISoundSet.Entry();
                    entry.sound = d.sound;
                    entry.volume = d.volume;
                    entry.pitchJitter = d.pitchJitter;
                    entry.minInterval = d.minInterval;
                    entries.Add(entry);
                }
                entry.clips = clips;
                changed = true;
            }

            if (changed)
            {
                set.entries = entries.ToArray();
                EditorUtility.SetDirty(set);
                Debug.Log("[CNE UI] Ses seti tamamlandı: " + SoundSetPath);
            }
            return set;
        }

        static bool HasClips(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return false;
            for (int i = 0; i < clips.Length; i++)
                if (clips[i] == null) return false;
            return true;
        }

        static AudioClip[] LoadClips(string[] names)
        {
            var result = new List<AudioClip>();
            for (int i = 0; i < names.Length; i++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + names[i] + ".wav");
                if (clip != null) result.Add(clip);
            }
            return result.ToArray();
        }

        // ---------------------------------------------------------------- dil listesi
        /// <summary>Resources/CNE_Languages'i yukler; yoksa Turkce + English ile kurar, eksik bayraklari tamamlar.</summary>
        public static CNELanguageList EnsureLanguageList()
        {
            var list = LoadOrCreate<CNELanguageList>(LanguagesPath);
            if (list == null) return null;

            bool changed = false;
            if (list.languages == null) list.languages = new List<CNELanguageDef>();
            if (list.languages.Count == 0)
            {
                list.languages.Add(NewLanguage("tr", "Türkçe"));
                list.languages.Add(NewLanguage("en", "English"));
                changed = true;
            }
            for (int i = 0; i < list.languages.Count; i++)
            {
                var def = list.languages[i];
                if (def == null || def.flag != null || string.IsNullOrEmpty(def.code)) continue;
                var flag = AssetDatabase.LoadAssetAtPath<Sprite>(FlagFolder + DefaultFlagFor(def.code) + ".png");
                if (flag == null) continue;
                def.flag = flag;
                changed = true;
            }
            if (changed)
            {
                EditorUtility.SetDirty(list);
                Debug.Log("[CNE UI] Dil görünüşleri tamamlandı: " + LanguagesPath);
            }
            return list;
        }

        static CNELanguageDef NewLanguage(string code, string nativeName)
        {
            var def = new CNELanguageDef();
            def.code = code;
            def.nativeName = nativeName;
            return def;
        }

        /// <summary>Dil kodundan bayrak dosyasi adi: en -> flag_us, en-GB -> flag_gb, pt-BR -> flag_br, de -> flag_de.</summary>
        static string DefaultFlagFor(string code)
        {
            string c = code.Trim().ToLowerInvariant();
            int dash = c.IndexOf('-');
            if (dash > 0 && c.Length - dash - 1 == 2) return "flag_" + c.Substring(dash + 1);
            if (dash > 0) c = c.Substring(0, dash);
            return c == "en" ? "flag_us" : "flag_" + c;
        }

        // ---------------------------------------------------------------- ortak
        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            if (File.Exists(path))
            {
                // Dosya var ama okunamiyor (ornegin betik baglantisi kopmus): silmek yerine kenara al.
                string aside = Root + "/" + Path.GetFileNameWithoutExtension(path) + "_bozuk.asset";
                AssetDatabase.DeleteAsset(aside);
                string error = AssetDatabase.MoveAsset(path, aside);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError("[CNE UI] " + path + " okunamadı ve taşınamadı: " + error);
                    return null;
                }
                Debug.LogWarning("[CNE UI] " + path + " okunamadı; " + aside + " olarak kenara alındı, yenisi kuruluyor.");
            }

            CNEUIKitBuilder.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
