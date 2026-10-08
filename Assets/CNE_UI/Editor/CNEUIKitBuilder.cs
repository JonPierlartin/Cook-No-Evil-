using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CookNoEvil.UI.EditorTools
{
    /// <summary>
    /// Tools > Cook No Evil > UI Kit menusu ve otomasyon girisi.
    ///  - BuildSilent(): pencere acmaz, sahne acmaz/olusturmaz/kaydetmez. TMP Essential Resources yoksa sessizce
    ///    ice aktarir ve bitince kendiliginden devam eder. Fontlari, Resources asset'lerini, CNE_UI metin tablosunu
    ///    ve prefablari kurar (prefablar gecici bir onizleme sahnesinde uretilir; acik sahneye dokunulmaz).
    ///  - "Kur + demo sahnesi": ayni kurulum + kitin kendi demo sahnesi (insanlar icin; pencere acar).
    /// Tekrar calistirmak guvenlidir: prefablar ayni yere (ayni GUID ile) yeniden yazilir.
    /// </summary>
    public static class CNEUIKitBuilder
    {
        public const string Version = "0.2.0";
        public const string Root = "Assets/CNE_UI";
        const string MenuPath = "Tools/Cook No Evil/UI Kit/";
        public const string DemoScenePath = Root + "/Scenes/CNE_UI_Demo.unity";
        public const string DemoDataFolder = Root + "/Demo";

        static bool tmpImportPending;
        static double tmpImportStartedAt;

        // ---------------------------------------------------------------- otomasyon
        /// <summary>
        /// Otomasyon icin kurulum. Pencere acmaz; sahne acmaz, olusturmaz, kaydetmez; acik sahneye dokunmaz.
        /// Sirasiyla: TMP Essential Resources (yoksa sessizce ice aktarilir, bitince bu metot kendiliginden
        /// yeniden calisir), gorsellerin ice aktarma ayarlari, TMP font asset'leri ve materyalleri,
        /// Resources asset'leri, CNE_UI metin tablosu (Unity Localization), prefablar.
        /// Donus: kurulum bu cagrida bittiyse true; TMP ice aktarimi bekleniyorsa ya da hata olduysa false.
        /// Durum konsolda "[CNE UI]" ile baslayan satirlarla yazilir; basari satiri "Sessiz kurulum tamam".
        /// </summary>
        public static bool BuildSilent()
        {
            if (!TmpEssentialsPresent())
            {
                ImportTmpEssentials(() => BuildSilent());
                return false;
            }
            try
            {
                BuildCore();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("[CNE UI] Sessiz kurulum yarıda kaldı (yukarıdaki hata).");
                return false;
            }
            Debug.Log("[CNE UI] Sessiz kurulum tamam (UI Kit " + Version + "). Prefablar: " + Root + "/Prefabs");
            return true;
        }

        [MenuItem(MenuPath + "Sessiz kur (sahneye dokunmaz)", false, 0)]
        static void BuildSilentMenu()
        {
            BuildSilent();
        }

        /// <summary>Kurulumun ortak kismi. Sahneye dokunmaz.</summary>
        static Kit BuildCore()
        {
            var kit = new Kit();
            kit.LoadArt();
            kit.PrepareFonts();
            kit.EnsureResources();
            CNEUIKitLocalization.SyncStringTable();
            EnsureFolder(Root + "/Prefabs");
            kit.BuildPrefabs();
            AssetDatabase.SaveAssets();
            return kit;
        }

        // ---------------------------------------------------------------- insanlar icin (pencereli)
        [MenuItem(MenuPath + "Kur + demo sahnesi (pencereli)", false, 20)]
        public static void BuildWithDemo()
        {
            if (!TmpEssentialsPresent())
            {
                bool import = EditorUtility.DisplayDialog("TextMeshPro gerekli",
                    "Projede TMP Essential Resources yok (TMP ayarları ve SDF shader'ları).\n\n" +
                    "Şimdi içe aktarılsın mı? Bitince kurulum kendiliğinden devam eder.",
                    "İçe aktar", "Vazgeç");
                if (import) ImportTmpEssentials(BuildWithDemo);
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                EditorUtility.DisplayProgressBar("Cook No Evil UI Kit", "Fontlar ve prefablar hazırlanıyor…", 0.3f);
                var kit = BuildCore();
                EditorUtility.DisplayProgressBar("Cook No Evil UI Kit", "Demo sahnesi kuruluyor…", 0.85f);
                EnsureFolder(Root + "/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                kit.BuildDemoScene(scene);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            var demo = AssetDatabase.LoadAssetAtPath<SceneAsset>(DemoScenePath);
            if (demo != null) EditorGUIUtility.PingObject(demo);
            EditorUtility.DisplayDialog("Cook No Evil · UI Kit " + Version,
                "Kurulum tamam.\n\n• Prefablar: " + Root + "/Prefabs\n• Demo sahnesi açık: " + DemoScenePath +
                "\n\nDemo, kitin kendi ayar deposunu (" + DemoDataFolder + ") kullanır; oyunun ayarlarına dokunmaz.",
                "Tamam");
        }

        [MenuItem(MenuPath + "Yalnızca TMP fontlarını oluştur", false, 40)]
        public static void BuildFontsOnly()
        {
            if (!TmpEssentialsPresent())
            {
                Debug.LogWarning("[CNE UI] Önce TMP Essential Resources gerekli (UI Kit menüsünden sessiz içe aktarılabilir).");
                return;
            }
            new Kit().PrepareFonts();
            AssetDatabase.SaveAssets();
            Debug.Log("[CNE UI] TMP font asset'leri hazır: " + Root + "/Fonts");
        }

        [MenuItem(MenuPath + "Kit metinlerini Localization tablosuna yaz (CNE_UI)", false, 41)]
        public static void SyncStrings()
        {
            CNEUIKitLocalization.SyncStringTable();
        }

        [MenuItem(MenuPath + "TMP Essential Resources'ı sessiz içe aktar", false, 42)]
        public static void ImportTmpEssentialsSilent()
        {
            ImportTmpEssentials(null);
        }

        [MenuItem(MenuPath + "Demo sahnesini aç", false, 60)]
        public static void OpenDemoScene()
        {
            if (!File.Exists(DemoScenePath))
            {
                EditorUtility.DisplayDialog("Cook No Evil · UI Kit", "Demo sahnesi yok. Önce 'Kur + demo sahnesi' komutunu çalıştırın.", "Tamam");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(DemoScenePath);
        }

        // ---------------------------------------------------------------- TMP Essential Resources
        /// <summary>
        /// TMP ayarlarini Resources/"TMP Settings"ten okur (Essential Resources ile gelir); yoksa TMP_Settings'in
        /// statik ozellikleri NullReferenceException verir. TMP_Settings.instance burada kullanilmiyor:
        /// bulamazsa TMP kendi ice aktarma penceresini acar.
        /// </summary>
        static bool TmpEssentialsPresent()
        {
            return Resources.Load<TMP_Settings>("TMP Settings") != null;
        }

        /// <summary>TMP Essential Resources'i pencere acmadan ice aktarir; bitince onDone'u (varsa) cagirir.</summary>
        static void ImportTmpEssentials(Action onDone)
        {
            if (TmpEssentialsPresent())
            {
                if (onDone != null) onDone();
                return;
            }
            if (tmpImportPending && EditorApplication.timeSinceStartup - tmpImportStartedAt < 120.0)
            {
                Debug.Log("[CNE UI] TMP Essential Resources içe aktarımı sürüyor; bitince kurulum kendiliğinden devam edecek.");
                return;
            }

            tmpImportPending = true;
            tmpImportStartedAt = EditorApplication.timeSinceStartup;
            AssetDatabase.ImportPackageCallback completed = null;
            AssetDatabase.ImportPackageFailedCallback failed = null;
            AssetDatabase.ImportPackageCallback cancelled = null;
            Action unhook = () =>
            {
                AssetDatabase.importPackageCompleted -= completed;
                AssetDatabase.importPackageFailed -= failed;
                AssetDatabase.importPackageCancelled -= cancelled;
                tmpImportPending = false;
            };
            completed = packageName =>
            {
                if (!IsTmpEssentials(packageName)) return;
                unhook();
                Debug.Log("[CNE UI] TMP Essential Resources içe aktarıldı.");
                if (onDone != null) EditorApplication.delayCall += () => onDone();
            };
            failed = (packageName, error) =>
            {
                if (!IsTmpEssentials(packageName)) return;
                unhook();
                Debug.LogError("[CNE UI] TMP Essential Resources içe aktarılamadı: " + error);
            };
            cancelled = packageName =>
            {
                if (!IsTmpEssentials(packageName)) return;
                unhook();
                Debug.LogWarning("[CNE UI] TMP Essential Resources içe aktarımı iptal edildi.");
            };
            AssetDatabase.importPackageCompleted += completed;
            AssetDatabase.importPackageFailed += failed;
            AssetDatabase.importPackageCancelled += cancelled;

            Debug.Log("[CNE UI] TMP Essential Resources sessizce içe aktarılıyor" +
                      (onDone != null ? "; bitince kurulum kendiliğinden devam edecek." : "."));
            try
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
            }
            catch (Exception e)
            {
                unhook();
                Debug.LogException(e);
                Debug.LogError("[CNE UI] TMP Essential Resources içe aktarılamadı. Elle: Window ▸ TextMeshPro ▸ Import TMP Essential Resources.");
            }
        }

        static bool IsTmpEssentials(string packageName)
        {
            return packageName != null && packageName.IndexOf("TMP Essential", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }

    /// <summary>Kit kurucusu: butun sprite/font/materyal referanslarini tutar ve prefablari uretir.</summary>
    sealed class Kit
    {
        const string Root = CNEUIKitBuilder.Root;

        // Prefablar kurulurken nesnelerin olusturuldugu gecici onizleme sahnesi (acik sahne kirlenmesin).
        static Scene stage;

        // ---------------------------------------------------------------- asset'ler
        Sprite shapeS, shapeL, outlineS, ringS, bevelS, panel, sign, boardTile, frame, arrow, glow, white;
        Sprite rail, fill, knob, housing, paddle, led, rotary, tick, vuFace, vuNeedle, record, strip, background;
        Sprite flagTr;
        Texture2D cursor;

        TMP_FontAsset fontHeading, fontBoard, fontBody, fontLogo, fontDigits;
        Material matNeonTitle, matBoard, matLogoLila, matLogoCyan, matDigits, matButton;

        // ---------------------------------------------------------------- prefablar
        GameObject pButtonNormal, pButtonPrimary, pButtonBack, pButtonDanger, pArrowButton;
        GameObject pSign, pSlider, pSwitch, pRotary, pLanguagePicker, pJukebox, pVUMeter, pSettings, pMainMenu;

        public void LoadArt()
        {
            CNEUIKitAssets.EnsureSpriteImportSettings();
            shapeS = S("Kit/ui_shape_r16");
            shapeL = S("Kit/ui_shape_r28");
            outlineS = S("Kit/ui_outline_r16");
            ringS = S("Kit/ui_ring_r16");
            bevelS = S("Kit/ui_bevel_r16");
            panel = S("Kit/ui_panel_formika");
            sign = S("Kit/ui_sign_plate");
            boardTile = S("Kit/ui_board_tile");
            frame = S("Kit/ui_frame_chrome");
            arrow = S("Kit/ui_arrow");
            glow = S("Kit/ui_glow");
            white = S("Kit/ui_white");
            rail = S("Kit/ui_slider_rail");
            fill = S("Kit/ui_slider_fill");
            knob = S("Kit/ui_knob");
            housing = S("Kit/ui_switch_housing");
            paddle = S("Kit/ui_switch_paddle");
            led = S("Kit/ui_led");
            rotary = S("Kit/ui_rotary_knob");
            tick = S("Kit/ui_tick");
            vuFace = S("Kit/ui_vu_face");
            vuNeedle = S("Kit/ui_vu_needle");
            record = S("Kit/ui_record");
            strip = S("Kit/ui_jukebox_strip");
            background = S("Misc/bg_menu_demo");
            flagTr = S("Flags/flag_tr");
            cursor = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Misc/cursor_kurdan.png");
        }

        static Sprite S(string relative)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/" + relative + ".png");
            if (sprite == null) Debug.LogError("[CNE UI] Sprite bulunamadı: " + relative + " (Texture Type = Sprite olmalı)");
            return sprite;
        }

        // ---------------------------------------------------------------- fontlar
        public void PrepareFonts()
        {
            CNEUIKitBuilder.EnsureFolder(Root + "/Fonts/Materials");
            fontHeading = EnsureFont("Righteous-Regular.ttf", "Righteous SDF");
            fontBoard = EnsureFont("Bungee-Regular.ttf", "Bungee SDF");
            fontBody = EnsureFont("VarelaRound-Regular.ttf", "VarelaRound SDF");
            fontLogo = EnsureFont("Pacifico-Regular.ttf", "Pacifico SDF");
            fontDigits = EnsureFont("ShareTechMono-Regular.ttf", "ShareTechMono SDF");

            var fallback = TMP_Settings.defaultFontAsset;
            if (fontBody == null) fontBody = fallback;
            if (fontHeading == null) fontHeading = fontBody;
            if (fontBoard == null) fontBoard = fontHeading;
            if (fontLogo == null) fontLogo = fontHeading;
            if (fontDigits == null) fontDigits = fontBody;

            // ShareTechMono'da Ğ ğ İ Ş ş yok; rakam ekranina Turkce harf yazilirsa VarelaRound'dan gelsin.
            // Diger dort font Turkce harflerin tamamini iceriyor; yine de gelecekteki diller icin ayni yedek.
            AddFallback(fontDigits, fontBody);
            AddFallback(fontHeading, fontBody);
            AddFallback(fontBoard, fontBody);
            AddFallback(fontLogo, fontBody);

            matNeonTitle = Preset(fontHeading, "Neon", m =>
            {
                Glow(m, CNEPalette.NeonLila.WithAlpha(0.85f), 0.55f, 0.45f);
            });
            matButton = Preset(fontHeading, "Button", m =>
            {
                Underlay(m, new Color(1f, 1f, 1f, 0.35f), 0f, -0.9f, 0f);
            });
            matBoard = Preset(fontBoard, "Board", m =>
            {
                Underlay(m, CNEPalette.Navy.WithAlpha(0.9f), 0.7f, -0.9f, 0f);
            });
            matLogoLila = Preset(fontLogo, "Neon Lila", m =>
            {
                Outline(m, CNEPalette.LilaLight, 0.08f);
                Glow(m, CNEPalette.NeonLila.WithAlpha(0.95f), 0.75f, 0.35f);
            });
            matLogoCyan = Preset(fontLogo, "Neon Cyan", m =>
            {
                Outline(m, new Color(0.75f, 1f, 1f, 1f), 0.08f);
                Glow(m, CNEPalette.Cyan.WithAlpha(0.95f), 0.75f, 0.35f);
            });
            matDigits = Preset(fontDigits, "Cyan Glow", m =>
            {
                Glow(m, CNEPalette.Cyan.WithAlpha(0.6f), 0.4f, 0.6f);
            });
        }

        static TMP_FontAsset EnsureFont(string ttf, string assetName)
        {
            string assetPath = Root + "/Fonts/" + assetName + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/" + ttf);
            if (font == null)
            {
                Debug.LogError("[CNE UI] Font dosyası yok: " + ttf);
                return null;
            }

            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (asset == null)
            {
                Debug.LogError("[CNE UI] TMP font asset oluşturulamadı: " + ttf);
                return null;
            }
            asset.name = assetName;
            // CreateFontAsset taban materyale mobil SDF shader'ini verir; o shader'da parlama (glow) yok.
            // TMP'nin kendi "Create > Font Asset" menusu gibi tam SDF shader'ina geciyoruz.
            UseFullSdfShader(asset.material);
            AssetDatabase.CreateAsset(asset, assetPath);
            if (asset.atlasTextures != null)
            {
                for (int i = 0; i < asset.atlasTextures.Length; i++)
                {
                    var tex = asset.atlasTextures[i];
                    if (tex == null) continue;
                    tex.name = assetName + " Atlas" + (i > 0 ? " " + i : "");
                    AssetDatabase.AddObjectToAsset(tex, asset);
                }
            }
            if (asset.material != null)
            {
                asset.material.name = assetName + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static void UseFullSdfShader(Material material)
        {
            if (material == null) return;
            var sdf = Shader.Find("TextMeshPro/Distance Field");
            if (sdf == null || material.shader == sdf) return;
            material.shader = sdf; // ayni adli ozellikler (_MainTex, _GradientScale, agirliklar) korunur
            ShaderUtilities.UpdateShaderRatios(material);
        }

        static void AddFallback(TMP_FontAsset font, TMP_FontAsset fallback)
        {
            if (font == null || fallback == null || font == fallback) return;
            if (font.fallbackFontAssetTable == null) font.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (font.fallbackFontAssetTable.Contains(fallback)) return;
            font.fallbackFontAssetTable.Add(fallback);
            EditorUtility.SetDirty(font);
        }

        static Material Preset(TMP_FontAsset font, string suffix, Action<Material> setup)
        {
            if (font == null || font.material == null) return null;
            string path = Root + "/Fonts/Materials/" + font.name + " - " + suffix + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            mat = new Material(font.material);
            mat.name = font.name + " - " + suffix;
            UseFullSdfShader(mat); // eski/mobil taban materyalden kopyalansa bile glow/underlay calissin
            setup(mat);
            ShaderUtilities.UpdateShaderRatios(mat);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void Glow(Material m, Color color, float outer, float power)
        {
            if (!m.HasProperty("_GlowColor")) return;
            m.EnableKeyword("GLOW_ON");
            m.SetColor("_GlowColor", color);
            m.SetFloat("_GlowOffset", 0f);
            m.SetFloat("_GlowInner", 0.05f);
            m.SetFloat("_GlowOuter", outer);
            m.SetFloat("_GlowPower", power);
        }

        static void Underlay(Material m, Color color, float x, float y, float softness)
        {
            if (!m.HasProperty("_UnderlayColor")) return;
            m.EnableKeyword("UNDERLAY_ON");
            m.SetColor("_UnderlayColor", color);
            m.SetFloat("_UnderlayOffsetX", x);
            m.SetFloat("_UnderlayOffsetY", y);
            m.SetFloat("_UnderlayDilate", 0f);
            m.SetFloat("_UnderlaySoftness", softness);
        }

        static void Outline(Material m, Color color, float width)
        {
            if (!m.HasProperty("_OutlineColor")) return;
            m.EnableKeyword("OUTLINE_ON"); // yalnizca mobil SDF'te gerekir; tam SDF'te zararsiz
            m.SetColor("_OutlineColor", color);
            m.SetFloat("_OutlineWidth", width);
        }

        // ---------------------------------------------------------------- Resources asset'leri
        public void EnsureResources()
        {
            CNEUIKitBuilder.EnsureFolder(Root + "/Resources");
            CNEUIKitAssets.EnsureSoundSet();
            CNEUIKitAssets.EnsureLanguageList();
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- temel yardimcilar
        static GameObject UI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            if (parent != null) go.transform.SetParent(parent, false);
            else if (stage.IsValid()) SceneManager.MoveGameObjectToScene(go, stage);
            return go;
        }

        static RectTransform Rt(GameObject go)
        {
            return (RectTransform)go.transform;
        }

        static RectTransform Rt(Transform t)
        {
            return (RectTransform)t;
        }

        static RectTransform Place(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rt = Rt(go);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Ebeveyni doldurur; l/r/t/b iceri dogru bosluk (negatif: disari).</summary>
        static RectTransform Stretch(GameObject go, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            var rt = Rt(go);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        static Image Img(GameObject go, Sprite sprite, Color color, Image.Type type, bool raycast)
        {
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sprite == null ? Image.Type.Simple : type;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        TextMeshProUGUI Txt(GameObject go, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align, Material material)
        {
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            if (material != null) t.fontSharedMaterial = material;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        static void AutoSize(TMP_Text t, float min, float max)
        {
            t.enableAutoSizing = true;
            t.fontSizeMin = min;
            t.fontSizeMax = max;
        }

        /// <summary>Anahtarin varsayilan (Turkce) metni; prefabda tablo yuklenene kadar bu gorunur.</summary>
        static string Text(string key)
        {
            var strings = CNEUIKitStrings.Load();
            return strings != null ? strings.Default(key) : "#" + key;
        }

        static CNELocalizedText Loc(TMP_Text t, string key)
        {
            var loc = t.gameObject.AddComponent<CNELocalizedText>();
            loc.text = new LocalizedString(CNELocalization.TableName, key);
            t.text = Text(key);
            return loc;
        }

        static void HardShadow(Transform parent, Sprite shape, float offset, float alpha)
        {
            var sh = UI("Shadow", parent);
            Stretch(sh, 0f, 0f, offset, -offset);
            Img(sh, shape, CNEPalette.ShadowTint.WithAlpha(alpha), Image.Type.Sliced, false);
            sh.transform.SetAsFirstSibling();
        }

        static GameObject Save(GameObject go, string name)
        {
            string path = Root + "/Prefabs/" + name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject Instance(GameObject prefab, Transform parent)
        {
            if (parent != null) return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (stage.IsValid()) return (GameObject)PrefabUtility.InstantiatePrefab(prefab, stage);
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        static void SetKey(GameObject root, string key)
        {
            var loc = root.GetComponentInChildren<CNELocalizedText>(true);
            if (loc == null) return;
            loc.text = new LocalizedString(CNELocalization.TableName, key);
            var t = loc.GetComponent<TMP_Text>();
            if (t != null) t.text = Text(key);
        }

        // ---------------------------------------------------------------- prefablar
        public void BuildPrefabs()
        {
            stage = EditorSceneManager.NewPreviewScene();
            try
            {
                pButtonNormal = Save(BuildButton("CNE_Button", CNEButtonRole.Normal, CNEPalette.Lila, "common.ok", new Vector2(300f, 76f)), "CNE_Button");
                pButtonPrimary = Save(BuildButton("CNE_Button_Primary", CNEButtonRole.Primary, CNEPalette.Lettuce, "common.ok", new Vector2(300f, 76f)), "CNE_Button_Primary");
                pButtonBack = Save(BuildButton("CNE_Button_Back", CNEButtonRole.Back, CNEPalette.Cheese, "common.back", new Vector2(260f, 76f)), "CNE_Button_Back");
                pButtonDanger = Save(BuildButton("CNE_Button_Danger", CNEButtonRole.Danger, CNEPalette.Tomato, "common.no", new Vector2(260f, 76f)), "CNE_Button_Danger");
                pArrowButton = Save(BuildArrowButton("CNE_ArrowButton"), "CNE_ArrowButton");
                pSign = Save(BuildSign("CNE_SignTitle", "settings.title", new Vector2(380f, 88f)), "CNE_SignTitle");
                pSlider = Save(BuildSlider("CNE_Slider"), "CNE_Slider");
                pSwitch = Save(BuildSwitch("CNE_Switch"), "CNE_Switch");
                pRotary = Save(BuildRotary("CNE_RotarySelector"), "CNE_RotarySelector");
                pLanguagePicker = Save(BuildLanguagePicker("CNE_LanguagePicker"), "CNE_LanguagePicker");
                pJukebox = Save(BuildJukebox("CNE_JukeboxStrip", new Vector2(440f, 80f)), "CNE_JukeboxStrip");
                pVUMeter = Save(BuildVUMeter("CNE_VUMeter"), "CNE_VUMeter");
                pSettings = Save(BuildSettings("CNE_SettingsPanel"), "CNE_SettingsPanel");
                pMainMenu = Save(BuildMainMenu("CNE_MainMenu"), "CNE_MainMenu");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(stage);
                stage = default(Scene);
            }
        }

        GameObject BuildButton(string name, CNEButtonRole role, Color color, string key, Vector2 size)
        {
            var root = UI(name, null);
            Rt(root).sizeDelta = size;
            var le = root.AddComponent<LayoutElement>();
            le.preferredWidth = size.x;
            le.preferredHeight = size.y;
            le.minHeight = size.y;

            HardShadow(root.transform, shapeS, 5f, 0.55f);

            var face = UI("Face", root.transform);
            Stretch(face);
            var faceImg = Img(face, shapeS, color, Image.Type.Sliced, true);
            var mask = face.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            var glint = UI("Glint", face.transform);
            var glintRt = Place(glint, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-size.x, 0f), new Vector2(64f, size.y * 2.4f));
            glintRt.localRotation = Quaternion.Euler(0f, 0f, -22f);
            Img(glint, glow, new Color(1f, 1f, 1f, 0.5f), Image.Type.Simple, false);

            var bevel = UI("Bevel", face.transform);
            Stretch(bevel);
            Img(bevel, bevelS, Color.white, Image.Type.Sliced, false);

            var label = UI("Label", face.transform);
            Stretch(label, 18f, 18f, 6f, 10f);
            var tmp = Txt(label, "", fontHeading, 32f, CNEPalette.Navy, TextAlignmentOptions.Center, matButton);
            AutoSize(tmp, 18f, 32f);
            tmp.characterSpacing = 2f;
            Loc(tmp, key);

            var outline = UI("Outline", face.transform);
            Stretch(outline);
            Img(outline, outlineS, Color.white, Image.Type.Sliced, false);

            var button = root.AddComponent<Button>();
            button.targetGraphic = faceImg;
            button.transition = Selectable.Transition.None;

            var fb = root.AddComponent<CNEButtonFeedback>();
            fb.face = Rt(face);
            fb.glint = glintRt;
            fb.role = role;
            return root;
        }

        GameObject BuildArrowButton(string name)
        {
            var root = UI(name, null);
            Rt(root).sizeDelta = new Vector2(34f, 34f);
            var face = UI("Face", root.transform);
            Stretch(face);
            var img = Img(face, arrow, CNEPalette.Navy, Image.Type.Simple, true);
            img.preserveAspect = true;
            var button = root.AddComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.None;
            var fb = root.AddComponent<CNEButtonFeedback>();
            fb.face = Rt(face);
            fb.hoverScale = 1.18f;
            fb.hoverLift = 0f;
            fb.pressSink = 2f;
            fb.pressSquash = new Vector2(0.86f, 0.86f);
            fb.playClickSound = false; // jukebox igne sesini kendisi calar
            return root;
        }

        GameObject BuildSign(string name, string key, Vector2 size)
        {
            var root = UI(name, null);
            Rt(root).sizeDelta = size;
            HardShadow(root.transform, shapeS, 6f, 0.55f);

            var plate = UI("Plate", root.transform);
            Stretch(plate);
            Img(plate, sign, Color.white, Image.Type.Sliced, false);

            var halo = UI("Glow", root.transform);
            Stretch(halo, -30f, -30f, -18f, -18f);
            var haloImg = Img(halo, glow, CNEPalette.NeonLila.WithAlpha(0.45f), Image.Type.Simple, false);

            var label = UI("Label", root.transform);
            Stretch(label, 26f, 26f, 8f, 12f);
            var tmp = Txt(label, "", fontHeading, 42f, new Color(0.98f, 0.96f, 1f, 1f), TextAlignmentOptions.Center, matNeonTitle);
            AutoSize(tmp, 22f, 42f);
            tmp.characterSpacing = 6f;
            Loc(tmp, key);

            var neon = root.AddComponent<CNENeonFlicker>();
            neon.tubes = new Graphic[] { tmp };
            neon.glows = new Graphic[] { haloImg };
            neon.onColor = new Color(0.98f, 0.96f, 1f, 1f);
            neon.offColor = new Color(0.36f, 0.34f, 0.46f, 0.35f);
            neon.glowAlpha = 0.45f;
            neon.playSound = false;
            neon.igniteOnEnable = true;
            neon.idleInterval = new Vector2(8f, 16f);
            return root;
        }

        GameObject BuildSlider(string name)
        {
            var root = UI(name, null);
            Rt(root).sizeDelta = new Vector2(250f, 44f);

            var railGo = UI("Rail", root.transform);
            var railRt = Rt(railGo);
            railRt.anchorMin = new Vector2(0f, 0.5f);
            railRt.anchorMax = new Vector2(1f, 0.5f);
            railRt.pivot = new Vector2(0.5f, 0.5f);
            railRt.anchoredPosition = Vector2.zero;
            railRt.sizeDelta = new Vector2(0f, 16f);
            Img(railGo, rail, Color.white, Image.Type.Sliced, true);

            var fillArea = UI("Fill Area", root.transform);
            var fillAreaRt = Rt(fillArea);
            fillAreaRt.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRt.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRt.pivot = new Vector2(0.5f, 0.5f);
            fillAreaRt.anchoredPosition = Vector2.zero;
            fillAreaRt.sizeDelta = new Vector2(-12f, 9f);

            var fillGo = UI("Fill", fillArea.transform);
            var fillRt = Rt(fillGo);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0.5f, 0.5f);
            fillRt.sizeDelta = new Vector2(12f, 0f);
            Img(fillGo, fill, CNEPalette.Turquoise, Image.Type.Sliced, false);

            var handleArea = UI("Handle Slide Area", root.transform);
            Stretch(handleArea, 14f, 14f, 0f, 0f);

            var handle = UI("Handle", handleArea.transform);
            var handleRt = Rt(handle);
            handleRt.anchorMin = new Vector2(0f, 0.5f);
            handleRt.anchorMax = new Vector2(0f, 0.5f);
            handleRt.pivot = new Vector2(0.5f, 0.5f);
            handleRt.anchoredPosition = Vector2.zero;
            handleRt.sizeDelta = new Vector2(40f, 40f);
            var knobImg = Img(handle, knob, Color.white, Image.Type.Simple, true);

            var slider = root.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = knobImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.None;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            var fb = root.AddComponent<CNESliderFeedback>();
            fb.knob = handleRt;
            return root;
        }

        TextMeshProUGUI BuildDigits(Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, string sample)
        {
            var plate = UI("Display", parent);
            Place(plate, anchor, pivot, position, size);
            Img(plate, shapeS, CNEPalette.Iron, Image.Type.Sliced, false);
            var outline = UI("Outline", plate.transform);
            Stretch(outline);
            Img(outline, outlineS, Color.white, Image.Type.Sliced, false);
            var value = UI("Value", plate.transform);
            Stretch(value, 6f, 6f, 2f, 4f);
            var t = Txt(value, sample, fontDigits, 28f, CNEPalette.Cyan, TextAlignmentOptions.Center, matDigits);
            AutoSize(t, 16f, 28f);
            return t;
        }

        GameObject BuildSwitch(string name)
        {
            var root = UI(name, null);
            Rt(root).sizeDelta = new Vector2(230f, 56f);

            var housingGo = UI("Housing", root.transform);
            Place(housingGo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(100f, 50f));
            var housingImg = Img(housingGo, housing, Color.white, Image.Type.Simple, true);

            var paddleGo = UI("Paddle", housingGo.transform);
            Place(paddleGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-22f, 0f), new Vector2(40f, 34f));
            Img(paddleGo, paddle, Color.white, Image.Type.Simple, false);

            var ledGlow = UI("LedGlow", root.transform);
            Place(ledGlow, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(124f, 0f), new Vector2(60f, 60f));
            var ledGlowImg = Img(ledGlow, glow, CNEPalette.Lettuce.WithAlpha(0f), Image.Type.Simple, false);

            var ledGo = UI("Led", root.transform);
            Place(ledGo, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(124f, 0f), new Vector2(20f, 20f));
            var ledImg = Img(ledGo, led, CNEPalette.ChromeDark, Image.Type.Simple, false);

            var state = UI("State", root.transform);
            Place(state, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(144f, 0f), new Vector2(86f, 40f));
            var stateText = Txt(state, "", fontHeading, 22f, CNEPalette.Navy, TextAlignmentOptions.MidlineLeft, null);
            AutoSize(stateText, 14f, 22f);
            var stateLoc = Loc(stateText, "common.off");

            var toggle = root.AddComponent<Toggle>();
            toggle.targetGraphic = housingImg;
            toggle.transition = Selectable.Transition.None;
            toggle.isOn = false;

            var sw = root.AddComponent<CNEToggleSwitch>();
            sw.paddle = Rt(paddleGo);
            sw.offX = -22f;
            sw.onX = 22f;
            sw.led = ledImg;
            sw.ledGlow = ledGlowImg;
            sw.stateLabel = stateLoc;
            return root;
        }

        /// <summary>
        /// Doner secici: kademeler pasif bir sablondan (Tick + Lamp + Label) kodla kurulur, boylece oyundaki
        /// gorunum sayisi degisirse dugme kendini yeniden dizer. Prefabda varsayilan uc kademe durur.
        /// </summary>
        GameObject BuildRotary(string name)
        {
            var root = UI(name, null);
            Rt(root).sizeDelta = new Vector2(330f, 160f);
            var center = new Vector2(0f, 50f);

            var slotsGo = UI("Slots", root.transform);
            Stretch(slotsGo);

            var template = UI("SlotTemplate", slotsGo.transform);
            Stretch(template);
            var tk = UI("Tick", template.transform);
            var tkRt = Place(tk, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), center, new Vector2(6f, 14f));
            Img(tk, tick, CNEPalette.Navy, Image.Type.Simple, false);
            var lampGo = UI("Lamp", template.transform);
            var lampRt = Place(lampGo, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), center, new Vector2(14f, 14f));
            var lampImg = Img(lampGo, led, CNEPalette.ChromeDark, Image.Type.Simple, false);
            var labelGo = UI("Label", template.transform);
            var labelRt = Place(labelGo, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), center, new Vector2(128f, 30f));
            var labelText = Txt(labelGo, "", fontHeading, 20f, CNEPalette.ChromeDark, TextAlignmentOptions.Center, null);
            AutoSize(labelText, 12f, 20f);
            labelText.raycastTarget = true;
            var labelLoc = Loc(labelText, "settings.view.off");
            var option = labelGo.AddComponent<CNERotaryOption>();

            var slot = template.AddComponent<CNERotarySlot>();
            slot.tick = tkRt;
            slot.lamp = lampRt;
            slot.lampGraphic = lampImg;
            slot.labelRect = labelRt;
            slot.label = labelText;
            slot.option = option;
            slot.localized = labelLoc;
            template.SetActive(false);

            var knobGo = UI("Knob", root.transform);
            var knobRt = Place(knobGo, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), center, new Vector2(86f, 86f));
            var knobImg = Img(knobGo, rotary, Color.white, Image.Type.Simple, true);

            var selector = root.AddComponent<CNERotarySelector>();
            selector.knob = knobRt;
            selector.targetGraphic = knobImg;
            selector.transition = Selectable.Transition.None;
            selector.slotTemplate = slot;
            selector.slotRoot = Rt(slotsGo);
            selector.center = center;
            option.owner = selector;
            selector.SetOptions(new List<CNEChoice>
            {
                new CNEChoice("", Text("settings.view.off"), "settings.view.off"),
                new CNEChoice("stylized", Text("settings.view.stylized"), "settings.view.stylized"),
                new CNEChoice("cne", Text("settings.view.toon"), "settings.view.toon")
            });
            selector.SetWithoutNotify(2);
            return root;
        }

        GameObject BuildLanguagePicker(string name)
        {
            var root = UI(name, null);
            Rt(root).sizeDelta = new Vector2(520f, 84f);
            var layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var chip = UI("ChipTemplate", root.transform);
            Rt(chip).sizeDelta = new Vector2(232f, 76f);

            var body = UI("Body", chip.transform);       // CNEButtonFeedback yuzu (hover/basma)
            Stretch(body);
            var pop = UI("Pop", body.transform);          // secim ziplamasi
            Stretch(pop);

            HardShadow(pop.transform, shapeS, 5f, 0.45f);
            var face = UI("Face", pop.transform);
            Stretch(face);
            var faceImg = Img(face, shapeS, CNEPalette.Cream, Image.Type.Sliced, true);

            var ring = UI("SelectedRing", pop.transform);
            Stretch(ring, -5f, -5f, -5f, -5f);
            var ringImg = Img(ring, ringS, CNEPalette.Lila.WithAlpha(0f), Image.Type.Sliced, false);

            var flagGo = UI("Flag", pop.transform);
            Place(flagGo, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(54f, 2f), new Vector2(76f, 55f));
            var flagImg = Img(flagGo, flagTr, Color.white, Image.Type.Simple, false);
            flagImg.preserveAspect = true;

            var labelGo = UI("Label", pop.transform);
            Place(labelGo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(100f, 0f), new Vector2(112f, 40f));
            var label = Txt(labelGo, "Türkçe", fontBody, 24f, CNEPalette.Navy, TextAlignmentOptions.MidlineLeft, null);
            AutoSize(label, 14f, 24f);

            var ledGo = UI("Led", pop.transform);
            Place(ledGo, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-15f, -15f), new Vector2(14f, 14f));
            var ledImg = Img(ledGo, led, CNEPalette.ChromeDark, Image.Type.Simple, false);

            var outline = UI("Outline", pop.transform);
            Stretch(outline);
            Img(outline, outlineS, Color.white, Image.Type.Sliced, false);

            var button = chip.AddComponent<Button>();
            button.targetGraphic = faceImg;
            button.transition = Selectable.Transition.None;
            var fb = chip.AddComponent<CNEButtonFeedback>();
            fb.face = Rt(body);
            fb.playClickSound = false; // bayrak sesi CNELanguageChip'ten

            var c = chip.AddComponent<CNELanguageChip>();
            c.button = button;
            c.body = Rt(pop);
            c.flag = flagImg;
            c.label = label;
            c.selectedRing = ringImg;
            c.led = ledImg;
            chip.SetActive(false);

            var picker = root.AddComponent<CNELanguagePicker>();
            picker.chipTemplate = c;
            picker.container = Rt(root);
            return root;
        }

        GameObject BuildJukebox(string name, Vector2 size)
        {
            var root = UI(name, null);
            Rt(root).sizeDelta = size;

            var stripGo = UI("Strip", root.transform);
            var stripRt = Rt(stripGo);
            stripRt.anchorMin = new Vector2(0f, 0.5f);
            stripRt.anchorMax = new Vector2(1f, 0.5f);
            stripRt.pivot = new Vector2(0.5f, 0.5f);
            stripRt.offsetMin = new Vector2(46f, -30f);
            stripRt.offsetMax = new Vector2(0f, 30f);
            Img(stripGo, strip, Color.white, Image.Type.Sliced, false);

            var recordGo = UI("Record", root.transform);
            var recordRt = Place(recordGo, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, 0f), new Vector2(78f, 78f));
            Img(recordGo, record, Color.white, Image.Type.Simple, false);

            var slot = UI("Slot", stripGo.transform);
            Place(slot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(48f, 36f));
            var slotText = Txt(slot, "A1", fontHeading, 24f, CNEPalette.Turquoise, TextAlignmentOptions.MidlineLeft, null);

            var clip = UI("TitleClip", stripGo.transform);
            Stretch(clip, 92f, 88f, 12f, 12f);
            clip.AddComponent<RectMask2D>();
            var titleGo = UI("Title", clip.transform);
            Stretch(titleGo);
            var title = Txt(titleGo, "", fontBody, 22f, CNEPalette.Navy, TextAlignmentOptions.MidlineLeft, null);
            AutoSize(title, 14f, 22f);
            title.text = Text("jukebox.no_track");

            var prev = Instance(pArrowButton, stripGo.transform);
            prev.name = "Previous";
            Place(prev, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-64f, 0f), new Vector2(30f, 30f));
            prev.transform.Find("Face").localRotation = Quaternion.Euler(0f, 0f, 180f);
            var next = Instance(pArrowButton, stripGo.transform);
            next.name = "Next";
            Place(next, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-26f, 0f), new Vector2(30f, 30f));

            var jb = root.AddComponent<CNEJukeboxStrip>();
            jb.record = recordRt;
            jb.slotCode = slotText;
            jb.title = title;
            jb.previousButton = prev.GetComponent<Button>();
            jb.nextButton = next.GetComponent<Button>();
            jb.noTrackText = new LocalizedString(CNELocalization.TableName, "jukebox.no_track");
            jb.noTrackFallback = Text("jukebox.no_track");
            return root;
        }

        GameObject BuildVUMeter(string name)
        {
            var root = UI(name, null);
            Rt(root).sizeDelta = new Vector2(440f, 112f);

            var face = UI("Face", root.transform);
            Place(face, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(160f, 104f));
            Img(face, vuFace, Color.white, Image.Type.Simple, false);

            var needle = UI("Needle", face.transform);
            var needleRt = Place(needle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 9f / 152f), new Vector2(0f, -39f), new Vector2(8f, 76f));
            needleRt.localRotation = Quaternion.Euler(0f, 0f, 46f);
            Img(needle, vuNeedle, Color.white, Image.Type.Simple, false);

            var sw = Instance(pSwitch, root.transform);
            sw.name = "TestSwitch";
            Place(sw, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(184f, 14f), new Vector2(230f, 56f));
            var swc = sw.GetComponent<CNEToggleSwitch>();
            swc.onKey = "settings.mic_test";
            swc.offKey = "settings.mic_test";
            SetKey(sw, "settings.mic_test");

            var status = UI("Status", root.transform);
            Place(status, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(186f, -30f), new Vector2(250f, 28f));
            var statusText = Txt(status, "", fontBody, 18f, CNEPalette.ChromeDark, TextAlignmentOptions.MidlineLeft, null);
            AutoSize(statusText, 12f, 18f);
            var statusLoc = Loc(statusText, "settings.mic_hint");

            var vu = root.AddComponent<CNEVUMeter>();
            vu.needle = needleRt;
            vu.testToggle = sw.GetComponent<Toggle>();
            vu.statusLabel = statusLoc;
            return root;
        }

        // ---- Ayarlar karti --------------------------------------------------------------
        GameObject BuildSettings(string name)
        {
            var root = UI(name, null);
            Stretch(root);
            root.AddComponent<CanvasGroup>();

            var dim = UI("Dim", root.transform);
            Stretch(dim);
            Img(dim, white, CNEPalette.Navy.WithAlpha(0.55f), Image.Type.Simple, true);

            var card = UI("Card", root.transform);
            var cardRt = Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1240f, 860f));
            HardShadow(card.transform, shapeL, 10f, 0.55f);
            var panelGo = UI("Panel", card.transform);
            Stretch(panelGo);
            Img(panelGo, panel, Color.white, Image.Type.Sliced, true);

            var title = Instance(pSign, card.transform);
            title.name = "Title";
            Place(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(380f, 88f));
            SetKey(title, "settings.title");

            var left = Column(card.transform, "LeftColumn", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(70f, -96f));
            var right = Column(card.transform, "RightColumn", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-70f, -96f));

            var settings = root.AddComponent<CNESettingsPanel>();

            // SES (degerler oyunun varsayilanlari: ana ses 100, muzik 50)
            var soundHeader = Section(left, "settings.section.sound");
            var master = SliderRow(left, "settings.master", CNEFloatSetting.MasterVolume, 0f, 1f, 1f, CNEValueFormat.Percent, settings);
            var music = SliderRow(left, "settings.music", CNEFloatSetting.MusicVolume, 0f, 1f, 0.5f, CNEValueFormat.Percent, settings);
            var track = JukeboxRow(left, "settings.music_track");
            // SOHBET (mikrofon kazanci 0-2, 1 = oldugu gibi -> 100)
            var chatHeader = Section(left, "settings.section.chat");
            var voice = SliderRow(left, "settings.voice", CNEFloatSetting.VoiceVolume, 0f, 1f, 1f, CNEValueFormat.Percent, settings);
            var mic = SliderRow(left, "settings.mic", CNEFloatSetting.MicGain, 0f, 2f, 1f, CNEValueFormat.Percent, settings);
            var vuRow = Row(left, "MicTest", 116f);
            var vu = Instance(pVUMeter, vuRow);
            Place(vu, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(206f, 0f), new Vector2(440f, 112f));
            Rt(vu).localScale = new Vector3(0.75f, 0.75f, 1f);

            // KONTROL VE GORUNTU
            var controlsHeader = Section(right, "settings.section.controls");
            var sensitivity = SliderRow(right, "settings.sensitivity", CNEFloatSetting.MouseSensitivity, 0.1f, 3f, 1f, CNEValueFormat.Decimal, settings);
            var fullscreenRow = Row(right, "Fullscreen", 64f);
            RowLabel(fullscreenRow, "settings.fullscreen");
            var fullscreen = Instance(pSwitch, fullscreenRow);
            Place(fullscreen, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(214f, 0f), new Vector2(230f, 56f));
            var viewRow = Row(right, "ViewMode", 168f);
            RowLabel(viewRow, "settings.view");
            var view = Instance(pRotary, viewRow);
            Place(view, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(206f, -4f), new Vector2(330f, 160f));

            // DIL (projede tek dil varken gizlenir)
            var languageHeader = Section(right, "settings.section.language");
            var langRow = Row(right, "Language", 92f);
            var picker = Instance(pLanguagePicker, langRow);
            Place(picker, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(520f, 84f));

            // Alt dugmeler
            var reset = Instance(pButtonNormal, card.transform);
            reset.name = "ResetButton";
            Place(reset, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(70f, 46f), new Vector2(320f, 72f));
            SetKey(reset, "settings.reset");
            var close = Instance(pButtonBack, card.transform);
            close.name = "CloseButton";
            Place(close, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-70f, 46f), new Vector2(260f, 72f));
            SetKey(close, "settings.close");
            close.GetComponent<CNEButtonFeedback>().playClickSound = false; // tabela kalkma sesi yeterli

            var transition = root.AddComponent<CNEPanelTransition>();
            transition.card = cardRt;
            transition.titleNeon = title.GetComponent<CNENeonFlicker>();
            transition.firstSelected = close.GetComponent<Button>();

            settings.transition = transition;
            settings.fullscreen = fullscreen.GetComponent<Toggle>();
            settings.fullscreenRow = fullscreenRow.gameObject;
            settings.look = view.GetComponent<CNERotarySelector>();
            settings.lookRow = viewRow.gameObject;
            settings.micTest = vu.GetComponent<CNEVUMeter>();
            settings.micTestRow = vuRow.gameObject;
            settings.languageRow = langRow.gameObject;
            settings.sections = new[]
            {
                NewSection(soundHeader, master, music, track),
                NewSection(chatHeader, voice, mic, vuRow.gameObject),
                NewSection(controlsHeader, sensitivity, fullscreenRow.gameObject, viewRow.gameObject),
                NewSection(languageHeader, langRow.gameObject)
            };
            settings.card = cardRt;
            settings.columns = new[] { Rt(left), Rt(right) };
            settings.resetButton = reset.GetComponent<Button>();
            settings.closeButton = close.GetComponent<Button>();
            return root;
        }

        static CNESettingsPanel.Section NewSection(GameObject header, params GameObject[] rows)
        {
            var section = new CNESettingsPanel.Section();
            section.header = header;
            section.rows = rows;
            return section;
        }

        static Transform Column(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position)
        {
            var col = UI(name, parent);
            Place(col, anchor, pivot, position, new Vector2(540f, 640f));
            var layout = col.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return col.transform;
        }

        static Transform Row(Transform column, string name, float height)
        {
            var row = UI(name, column);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return row.transform;
        }

        GameObject Section(Transform column, string key)
        {
            var row = Row(column, "Section_" + key, 50f);
            var labelGo = UI("Label", row);
            Stretch(labelGo, 0f, 0f, 4f, 10f);
            var t = Txt(labelGo, "", fontHeading, 26f, CNEPalette.Navy, TextAlignmentOptions.BottomLeft, null);
            t.characterSpacing = 4f;
            AutoSize(t, 16f, 26f);
            Loc(t, key);
            var bar = UI("Underline", row);
            Place(bar, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 2f), new Vector2(84f, 5f));
            Img(bar, shapeS, CNEPalette.Turquoise, Image.Type.Sliced, false);
            return row.gameObject;
        }

        void RowLabel(Transform row, string key)
        {
            var labelGo = UI("Label", row);
            Place(labelGo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(200f, 44f));
            var t = Txt(labelGo, "", fontBody, 25f, CNEPalette.Navy, TextAlignmentOptions.MidlineLeft, null);
            AutoSize(t, 15f, 25f);
            Loc(t, key);
        }

        /// <summary>Etiket + surgu + rakam ekrani. Satiri ayarlar kartinin listesine kaydeder (kaynaktaki ayara baglanir).</summary>
        GameObject SliderRow(Transform column, string key, CNEFloatSetting setting, float min, float max, float value, CNEValueFormat format, CNESettingsPanel panel)
        {
            var row = Row(column, "Row_" + key, 64f);
            RowLabel(row, key);
            var sliderGo = Instance(pSlider, row);
            Place(sliderGo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(206f, 0f), new Vector2(240f, 44f));
            string sample = format == CNEValueFormat.Percent
                ? Mathf.RoundToInt(value * 100f).ToString(System.Globalization.CultureInfo.InvariantCulture)
                : value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            var digits = BuildDigits(row, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(82f, 44f), sample);
            var slider = sliderGo.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            var fb = sliderGo.GetComponent<CNESliderFeedback>();
            fb.valueLabel = digits;
            fb.format = format;

            var entry = new CNESettingsPanel.SliderRow();
            entry.setting = setting;
            entry.row = row.gameObject;
            entry.slider = slider;
            panel.sliders.Add(entry);
            return row.gameObject;
        }

        GameObject JukeboxRow(Transform column, string key)
        {
            var row = Row(column, "Row_" + key, 84f);
            RowLabel(row, key);
            var jb = Instance(pJukebox, row);
            Place(jb, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(200f, 0f), new Vector2(340f, 76f));
            return row.gameObject;
        }

        // ---- Ana menu ---------------------------------------------------------------------
        GameObject BuildMainMenu(string name)
        {
            var root = UI(name, null);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0; // projede lobi pencerelerinin altinda kalacak sekilde sahnede ayarlanir
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            // Arka plan yok: menu, sahnenin kamerasinin cizdigi 3B gece salonunun ustunde durur.

            // Neon logo
            var logo = UI("Logo", root.transform);
            Place(logo, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(110f, -50f), new Vector2(780f, 310f));
            var halo = UI("Glow", logo.transform);
            Place(halo, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(340f, -150f), new Vector2(1000f, 430f));
            var haloImg = Img(halo, glow, CNEPalette.NeonLila.WithAlpha(0.45f), Image.Type.Simple, false);
            var cookNo = UI("CookNo", logo.transform);
            Place(cookNo, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -6f), new Vector2(720f, 170f));
            var cookNoText = Txt(cookNo, "Cook No", fontLogo, 124f, new Color(0.97f, 0.95f, 1f, 1f), TextAlignmentOptions.MidlineLeft, matLogoLila);
            var evil = UI("Evil", logo.transform);
            Place(evil, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, -122f), new Vector2(480f, 190f));
            var evilText = Txt(evil, "Evil!", fontLogo, 150f, new Color(0.92f, 1f, 1f, 1f), TextAlignmentOptions.MidlineLeft, matLogoCyan);

            var neonA = logo.AddComponent<CNENeonFlicker>();
            neonA.tubes = new Graphic[] { cookNoText };
            neonA.glows = new Graphic[] { haloImg };
            neonA.onColor = new Color(0.97f, 0.95f, 1f, 1f);
            neonA.offColor = new Color(0.33f, 0.30f, 0.45f, 0.35f);
            neonA.glowAlpha = 0.45f;
            neonA.igniteDelay = 0.25f;
            neonA.playSound = true;
            var neonB = evil.AddComponent<CNENeonFlicker>();
            neonB.tubes = new Graphic[] { evilText };
            neonB.onColor = new Color(0.92f, 1f, 1f, 1f);
            neonB.offColor = new Color(0.25f, 0.38f, 0.42f, 0.35f);
            neonB.igniteDelay = 0.42f;
            neonB.playSound = false;
            neonB.nervousness = 0.65f;
            neonB.idleInterval = new Vector2(3f, 8f);

            // Harfli menu panosu
            var board = UI("MenuBoard", root.transform);
            var boardRt = Place(board, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, -372f), new Vector2(640f, 476f));
            HardShadow(board.transform, shapeL, 10f, 0.55f);
            var boardFace = UI("Board", board.transform);
            Stretch(boardFace, 18f, 18f, 18f, 18f);
            Img(boardFace, boardTile, Color.white, Image.Type.Tiled, false);
            var frameGo = UI("Frame", board.transform);
            Stretch(frameGo);
            Img(frameGo, frame, Color.white, Image.Type.Sliced, false);

            var rows = UI("Rows", board.transform);
            Stretch(rows, 34f, 30f, 38f, 38f);
            var rowsLayout = rows.AddComponent<VerticalLayoutGroup>();
            rowsLayout.spacing = 0f;
            rowsLayout.childAlignment = TextAnchor.UpperLeft;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = true;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;

            var create = BoardRow(rows.transform, "menu.create_lobby", CNEPalette.Cyan, CNEPalette.Cream, CNEUISound.None);
            var browse = BoardRow(rows.transform, "menu.browse_lobbies", CNEPalette.Cyan, CNEPalette.Cream, CNEUISound.None);
            var howTo = BoardRow(rows.transform, "menu.how_to_play", CNEPalette.Cyan, CNEPalette.Cream, CNEUISound.None);
            var settingsRow = BoardRow(rows.transform, "menu.settings", CNEPalette.LilaLight, CNEPalette.Cream, CNEUISound.None);
            var quit = BoardRow(rows.transform, "menu.quit", CNEPalette.Cheese, CNEPalette.Cheese, CNEUISound.Back);

            // Jukebox (sol alt) ve surum (sag alt)
            var jukebox = Instance(pJukebox, root.transform);
            jukebox.name = "Jukebox";
            Place(jukebox, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(64f, 46f), new Vector2(460f, 84f));

            var version = UI("Version", root.transform);
            Place(version, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 26f), new Vector2(420f, 34f));
            var versionText = Txt(version, "", fontBody, 20f, CNEPalette.Cream.WithAlpha(0.55f), TextAlignmentOptions.MidlineRight, null);
            var versionLoc = Loc(versionText, "menu.version");

            // Ayarlar karti (gizli baslar)
            var settings = Instance(pSettings, root.transform);
            settings.name = "SettingsPanel";
            Stretch(settings);
            settings.SetActive(false);

            var menu = root.AddComponent<CNEMainMenu>();
            menu.createLobbyButton = create.GetComponent<Button>();
            menu.browseLobbiesButton = browse.GetComponent<Button>();
            menu.howToPlayButton = howTo.GetComponent<Button>();
            menu.settingsButton = settingsRow.GetComponent<Button>();
            menu.quitButton = quit.GetComponent<Button>();
            menu.settingsPanel = settings.GetComponent<CNESettingsPanel>();
            menu.versionLabel = versionLoc;
            menu.introRows = new RectTransform[] { Rt(create), Rt(browse), Rt(howTo), Rt(settingsRow), Rt(quit) };
            menu.board = boardRt;

            var cursorComp = root.AddComponent<CNECursor>();
            cursorComp.cursorTexture = cursor;
            return root;
        }

        GameObject BoardRow(Transform parent, string key, Color lampColor, Color textColor, CNEUISound clickSound)
        {
            var row = UI("Row_" + key, parent);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 80f;
            le.minHeight = 80f;

            var content = UI("Content", row.transform);   // ilk cocuk: acilis animasyonu bunu kaydirir
            Stretch(content);

            var lampGlow = UI("LampGlow", content.transform);
            Place(lampGlow, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(18f, 0f), new Vector2(84f, 84f));
            var lampGlowImg = Img(lampGlow, glow, lampColor.WithAlpha(0f), Image.Type.Simple, false);
            var lamp = UI("Lamp", content.transform);
            Place(lamp, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(18f, 0f), new Vector2(26f, 26f));
            var lampImg = Img(lamp, arrow, lampColor.WithAlpha(0f), Image.Type.Simple, false);

            var labelGo = UI("Label", content.transform);
            var labelRt = Stretch(labelGo, 48f, 6f, 4f, 4f);
            var label = Txt(labelGo, "", fontBoard, 42f, textColor, TextAlignmentOptions.MidlineLeft, matBoard);
            AutoSize(label, 24f, 42f);
            label.characterSpacing = 3f;
            Loc(label, key);
            var hop = labelGo.AddComponent<CNELetterHop>();

            var hit = UI("HitArea", row.transform);
            Stretch(hit);
            var hitImg = Img(hit, white, new Color(1f, 1f, 1f, 0f), Image.Type.Simple, true);

            var button = row.AddComponent<Button>();
            button.targetGraphic = hitImg;
            button.transition = Selectable.Transition.None;

            var boardRow = row.AddComponent<CNEMenuBoardRow>();
            boardRow.label = labelRt;
            boardRow.letters = hop;
            boardRow.lamp = lampImg;
            boardRow.lampGlow = lampGlowImg;
            boardRow.clickSound = clickSound;
            return row;
        }

        // ---- Demo sahnesi (yalnizca "Kur + demo sahnesi") ---------------------------------------
        public void BuildDemoScene(Scene scene)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = CNEPalette.Iron;
            camGo.AddComponent<AudioListener>();

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif

            // Demo arka plani: 3B salonun yerine resim (oyunda bu yok).
            var bgCanvasGo = new GameObject("DemoBackground", typeof(RectTransform));
            bgCanvasGo.layer = 5;
            var bgCanvas = bgCanvasGo.AddComponent<Canvas>();
            bgCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            bgCanvas.sortingOrder = -10;
            var bgScaler = bgCanvasGo.AddComponent<CanvasScaler>();
            bgScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            bgScaler.referenceResolution = new Vector2(1920f, 1080f);
            bgScaler.matchWidthOrHeight = 0.5f;
            var bg = UI("Image", bgCanvasGo.transform);
            Stretch(bg);
            Img(bg, background, Color.white, Image.Type.Simple, false);

            var menuGo = Instance(pMainMenu, null);
            var menu = menuGo.GetComponent<CNEMainMenu>();
            CNEUIKitBuilder.EnsureFolder(CNEUIKitBuilder.DemoDataFolder);
            menu.settingsSource = LoadOrCreateDemo<CNEPlayerPrefsSettingsSource>(CNEUIKitBuilder.DemoDataFolder + "/CNE_DemoSettingsSource.asset");
            menu.micLevelSource = LoadOrCreateDemo<CNEUnityMicrophoneSource>(CNEUIKitBuilder.DemoDataFolder + "/CNE_UnityMicrophone.asset");

            EditorSceneManager.SaveScene(scene, CNEUIKitBuilder.DemoScenePath);
        }

        static T LoadOrCreateDemo<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
