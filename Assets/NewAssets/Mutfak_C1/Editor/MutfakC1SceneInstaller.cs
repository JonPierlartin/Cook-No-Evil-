using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// C1 mutfağını AÇIK sahneye kurar: CNE → Mutfak C1 → Install In Scene. Yeniden çalıştırılabilir (kendi kurduğu kökleri
// silip yeniden kurar; işlevsel kökleri yalnızca taşır).
//  1. Yerleşim prefab'ı mutfağın güneydoğu iç köşesine konur (dönüş yok).
//  2. Mimari (pakette yok): mutfak kuzeye büyütülür; pencere ve kapı açıklıkları yerleşimdeki yerlerine göre yer
//     tutucu kutularla yeniden kurulur. Eski duvar parçaları ve eski mutfak eşyaları KAPATILIR (silinmez).
//  3. İşlev taşınır: ızgara yuvaları, birleştirme tezgahları, ekmek kabı, malzeme kapları, buzdolabı, çöp kovaları,
//     pencere yuvaları ve hata paneli yeni modellerin soketlerine oturtulur. Kodları ve ağ kimlikleri değişmez.
// Sayılar yerleşimden ve modellerin sınırlarından okunur; elle girilenler aşağıdaki sabitlerdir (mevcut harita).
public static class MutfakC1SceneInstaller
{
    // Mevcut harita: mutfağın güneydoğu iç köşesi (doğu duvarının iç yüzü, güney duvarının iç yüzü, zemin üstü).
    private static readonly Vector3 Origin = new(4.5f, 0.30f, -3.58f);
    private const float WallThickness = 0.3f;
    private const float WallTop = 3.3f;
    private const float FloorThickness = 0.3f;
    // Oda: yerleşim 5,60 × 6,00 m. Batı duvarı yerinde kalır (iç yüzü x = −1,2).
    private const float RoomLength = 6f;
    private const float WestInnerFace = -1.2f;
    private const float OldNorthInnerFace = 1.2f;

    private const string LayoutRootName = "Mutfak_C1";
    private const string ArchitectureRootName = "Mutfak_C1_Mimari";
    private const string PanelPrefabPath = "Assets/Prefabs/Mutfak/HataPaneli_C1.prefab";
    private const string OldPanelPrefabPath = "Assets/Prefabs/HataPaneli.prefab";
    private const string LitMaterialPath = "Assets/NewAssets/Mutfak_C1/Materials/MI_XPanel_Yanan.mat";

    private static readonly string[] OldWallNames =
    {
        "PF_Wall_3x3", "PF_Wall_3x3 (1)", "PF_Wall_3x3 (23)", "Pf_StationWindow", "PF_Wall_3x3 (12)", "PF_DoorCase",
    };

    // Kimlik bileşeni taşıdığı için genel taramaya girmeyen eski dekor (adıyla kapatılır).
    private static readonly string[] OldDecorNames = { "PF_Frier" };

    private static StringBuilder _log;

    [MenuItem("CNE/Mutfak C1/Install In Scene")]
    public static void Install()
    {
        _log = new StringBuilder();
        var scene = SceneManager.GetActiveScene();

        var layout = PlaceLayout();
        BuildArchitecture(layout);
        DisableOldKitchen(layout);
        MoveFunction(layout);
        InstallErrorPanel(layout);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[CNE] Mutfak C1 sahneye kuruldu.\n" + _log);
    }

    public static string LastLog => _log != null ? _log.ToString() : string.Empty;

    private static Transform PlaceLayout()
    {
        var existing = GameObject.Find(LayoutRootName);
        if (existing != null)
            Object.DestroyImmediate(existing);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MutfakC1Builder.LayoutPrefabPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = LayoutRootName;
        instance.transform.SetPositionAndRotation(Origin, Quaternion.identity);
        return instance.transform;
    }

    // ---------- eski mutfak ----------

    private static void DisableOldKitchen(Transform layout)
    {
        var harita = GameObject.Find("Harita").transform;
        var walls = harita.Find("Walls");
        foreach (string wallName in OldWallNames)
        {
            var wall = walls.Find(wallName);
            if (wall == null)
            {
                _log.AppendLine($"UYARI: eski duvar bulunamadı: {wallName}");
                continue;
            }

            wall.gameObject.SetActive(false);
        }

        foreach (string decorName in OldDecorNames)
        {
            var decor = harita.Find(decorName);
            if (decor != null && decor.gameObject.activeSelf)
            {
                decor.gameObject.SetActive(false);
                _log.AppendLine($"kapatıldı (eski dekor): {decorName}");
            }
        }

        // Eski mutfak hacmindeki dekor: Harita'nın doğrudan çocukları ve sahne kökleri (ağ nesnesi olmayanlar).
        var volume = new Bounds();
        // Duvarların içinde duran parçalar da (eski kapı kanatları, duvara yaslı fritöz) alınsın diye hacim duvar
        // kalınlığı kadar genişletilir.
        volume.SetMinMax(
            new Vector3(WestInnerFace - WallThickness, Origin.y - FloorThickness, Origin.z - WallThickness),
            new Vector3(Origin.x, WallTop, OldNorthInnerFace + WallThickness));

        var candidates = new List<GameObject>();
        foreach (Transform child in harita)
        {
            if (child.name != "Floor" && child.name != "Walls")
                candidates.Add(child.gameObject);
        }

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.transform != harita && root.transform != layout && root.name != ArchitectureRootName)
                candidates.Add(root);
        }

        foreach (var candidate in candidates)
        {
            if (!candidate.activeSelf || candidate.GetComponentInChildren<NetworkObject>(true) != null
                || candidate.GetComponentInChildren<MonoBehaviour>(true) != null)
                continue;

            var renderers = candidate.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                continue;

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);

            if (!volume.Contains(bounds.center))
                continue;

            candidate.SetActive(false);
            _log.AppendLine($"kapatıldı (eski dekor): {candidate.name}");
        }
    }

    // ---------- mimari ----------

    private static void BuildArchitecture(Transform layout)
    {
        var existing = GameObject.Find(ArchitectureRootName);
        if (existing != null)
            Object.DestroyImmediate(existing);

        var root = new GameObject(ArchitectureRootName).transform;
        var harita = GameObject.Find("Harita").transform;
        var wallMaterial = harita.Find("Walls").GetComponentInChildren<Renderer>(true).sharedMaterial;
        var floorMaterial = harita.Find("Floor").GetComponentInChildren<Renderer>(true).sharedMaterial;

        float south = Origin.z;                         // güney duvarının iç yüzü
        float north = Origin.z + RoomLength;            // kuzey duvarının iç yüzü
        float east = Origin.x;                          // doğu duvarının iç yüzü
        float westOuter = WestInnerFace - WallThickness;
        float eastOuter = east + WallThickness;
        float floorTop = Origin.y;

        // Zemin: eski kuzey duvarının altından yeni kuzey duvarının dışına.
        Box(root, "Zemin_Uzanti", floorMaterial,
            new Vector3(westOuter, floorTop - FloorThickness, OldNorthInnerFace),
            new Vector3(eastOuter, floorTop, north + WallThickness));

        // Kuzey duvarı ve batı duvarının uzantısı.
        Box(root, "Duvar_Kuzey", wallMaterial,
            new Vector3(westOuter, floorTop, north), new Vector3(eastOuter, WallTop, north + WallThickness));
        Box(root, "Duvar_Bati_Uzanti", wallMaterial,
            new Vector3(westOuter, floorTop, OldNorthInnerFace + WallThickness), new Vector3(WestInnerFace, WallTop, north));

        // Doğu duvarı (İstasyon'la ortak): pencere açıklığı yerleşimdeki pencere modelinin sınırlarından.
        var window = WorldBounds(Asset(layout, "SM_StationWindow_23"));
        Box(root, "Duvar_Dogu_Guney", wallMaterial,
            new Vector3(east, floorTop, south), new Vector3(eastOuter, WallTop, window.min.z));
        Box(root, "Duvar_Dogu_Kuzey", wallMaterial,
            new Vector3(east, floorTop, window.max.z), new Vector3(eastOuter, WallTop, north));
        Box(root, "Duvar_Dogu_PencereAlti", wallMaterial,
            new Vector3(east, floorTop, window.min.z), new Vector3(eastOuter, window.min.y, window.max.z));
        Box(root, "Duvar_Dogu_PencereUstu", wallMaterial,
            new Vector3(east, window.max.y, window.min.z), new Vector3(eastOuter, WallTop, window.max.z));

        // Güney duvarı (Kasa'yla ortak): kapı açıklığı kasa modelinin sınırlarından. Doğudaki parça eski kapı
        // kasasının bittiği yere kadar (oradan sonrası mevcut duvar).
        var door = WorldBounds(Asset(layout, "SM_SwingDoorFrame_27"));
        float southOuter = south - WallThickness;
        var oldDoorCase = harita.Find("Walls/PF_DoorCase");
        float oldDoorCaseEast = oldDoorCase != null ? MaxWorldX(oldDoorCase.GetComponent<Renderer>()) : door.max.x;
        Box(root, "Duvar_Guney_Bati", wallMaterial,
            new Vector3(westOuter, floorTop, southOuter), new Vector3(door.min.x, WallTop, south));
        Box(root, "Duvar_Guney_Dogu", wallMaterial,
            new Vector3(door.max.x, floorTop, southOuter), new Vector3(oldDoorCaseEast, WallTop, south));
        Box(root, "Duvar_Guney_KapiUstu", wallMaterial,
            new Vector3(door.min.x, door.max.y, southOuter), new Vector3(door.max.x, WallTop, south));

        // Kapı kapalıdır (GDD 5.2.3: yalnızca yangında açılır — Faz 1): açıklık görünmez bir engelle kapatılır.
        Blocker(root, "Engel_Kapi", new Vector3(door.min.x, floorTop, southOuter), new Vector3(door.max.x, door.max.y, south));

        // Eşyaların çarpışması (dekor). Üstünde etkileşim hedefi duran eşyada engel o yüzeyin altında biter; yoksa
        // nişan ışını engele çarpar ve hedef bulunamaz.
        var solids = new (string asset, float topAboveFloor)[]
        {
            ("SM_Grill_01", 0.98f), ("SM_Grill_02", 0.98f), ("SM_Fryer_03", 0f), ("SM_Fryer_04", 0f),
            ("SM_FryStation_05", 0f), ("SM_AssemblyIsland_08", 0.98f), ("SM_PrepTable_13", 0.83f),
        };
        foreach (var (assetName, top) in solids)
        {
            var bounds = WorldBounds(Asset(layout, assetName));
            var max = bounds.max;
            if (top > 0f)
                max.y = floorTop + top;
            Blocker(root, "Engel_" + assetName, new Vector3(bounds.min.x, floorTop, bounds.min.z), max);
        }
    }

    private static void Box(Transform parent, string name, Material material, Vector3 min, Vector3 max)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.position = (min + max) * 0.5f;
        box.transform.localScale = max - min;
        box.GetComponent<Renderer>().sharedMaterial = material;
        GameObjectUtility.SetStaticEditorFlags(box, StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
    }

    private static void Blocker(Transform parent, string name, Vector3 min, Vector3 max)
    {
        var blocker = new GameObject(name);
        blocker.transform.SetParent(parent, false);
        blocker.transform.position = (min + max) * 0.5f;
        blocker.AddComponent<BoxCollider>().size = max - min;
    }

    // ---------- işlev ----------

    private static void MoveFunction(Transform layout)
    {
        // Izgara: iki ızgara, her birinde tek yuva (iki soketin ortası).
        var grill1 = Asset(layout, "SM_Grill_01");
        var grill2 = Asset(layout, "SM_Grill_02");
        PlaceSlot("Izgara_Yuva_Sol", SocketMidpoint(grill1), grill1.rotation);
        PlaceSlot("Izgara_Yuva_Sag", SocketMidpoint(grill2), grill2.rotation);
        var grillRoot = Find("Izgara");
        if (grillRoot != null)
        {
            grillRoot.SetPositionAndRotation((SocketMidpoint(grill1) + SocketMidpoint(grill2)) * 0.5f, grill1.rotation);
            HideOldVisuals(grillRoot);
        }

        // Birleştirme tezgahları: kesme tahtalarının üstü.
        PlaceStation("BurgerAssemblyStation", Asset(layout, "SM_CuttingBoard_09"));
        PlaceStation("BurgerAssemblyStation (2)", Asset(layout, "SM_CuttingBoard_10"));

        // Ekmek kabı: ekmek tepsisi.
        var tray = Asset(layout, "SM_BunTray_11");
        var bread = Find("EkmekContainer");
        if (bread != null)
        {
            var trayBounds = WorldBounds(tray);
            const float clickHeight = 0.12f;
            bread.SetPositionAndRotation(new Vector3(trayBounds.center.x, trayBounds.min.y + clickHeight * 0.5f, trayBounds.center.z), tray.rotation);
            bread.localScale = new Vector3(trayBounds.size.x, clickHeight, trayBounds.size.z);
            if (bread.TryGetComponent<MeshRenderer>(out var breadRenderer))
                breadRenderer.enabled = false;
        }

        // Malzeme kapları: malzeme alanının soketleri (adlarıyla eşleşir).
        var prep = Asset(layout, "SM_PrepTable_13");
        var binBounds = LocalBounds("SM_IngredientBin");
        foreach (var (containerName, socketName) in new[]
        {
            ("Kap_Marul", "Socket_Bin_1_Marul"), ("Kap_Domates", "Socket_Bin_2_Domates"), ("Kap_Tursu", "Socket_Bin_3_Tursu"),
            ("Kap_Sogan", "Socket_Bin_4_Sogan"), ("Kap_Peynir", "Socket_Bin_5_Peynir"),
        })
        {
            var container = Find(containerName);
            var socket = prep.Find(socketName);
            if (container == null || socket == null)
            {
                _log.AppendLine($"UYARI: {containerName} / {socketName} bulunamadı.");
                continue;
            }

            container.SetPositionAndRotation(socket.position, prep.rotation);
            HideOldVisuals(container);
            FitCollider(container, binBounds);
        }

        // Buzdolabı (köfte kabı).
        PlaceOnAsset("Buzdolabi_Et", Asset(layout, "SM_ReachInFridge_19"), LocalBounds("SM_ReachInFridge"));

        // Çöp kovaları: ikisi de çalışır (ikincisi ilkinin kopyası).
        var binLocal = LocalBounds("SM_TrashBin");
        PlaceOnAsset("Cop_Mutfak", Asset(layout, "SM_TrashBin_22"), binLocal);
        var secondBin = Find("Cop_Mutfak_2");
        if (secondBin == null)
        {
            var first = Find("Cop_Mutfak");
            secondBin = Object.Instantiate(first.gameObject).transform;
            secondBin.name = "Cop_Mutfak_2";
            RefreshNetworkHash(secondBin.GetComponent<NetworkObject>());
        }

        PlaceOnAsset("Cop_Mutfak_2", Asset(layout, "SM_TrashBin_21"), binLocal);

        // Pencere yuvaları: pervazdaki hamburger soketleri. Yuvanın tıklanan hacmi duvarı boydan boya geçer ki iki
        // taraftan da (Şef ve Komi) erişilsin.
        var window = Asset(layout, "SM_StationWindow_23");
        var windowBounds = WorldBounds(window);
        for (int i = 1; i <= 3; i++)
        {
            var socket = window.Find($"Socket_Burger_{i}");
            var slot = Find($"MutfakPencere_Yuva_{i}");
            if (socket == null || slot == null)
                continue;

            slot.SetPositionAndRotation(socket.position, Quaternion.identity);
            if (slot.TryGetComponent<BoxCollider>(out var collider))
            {
                const float slotWidth = 0.42f;
                float centerX = windowBounds.center.x - socket.position.x;
                collider.center = new Vector3(centerX, collider.size.y * 0.5f, 0f);
                collider.size = new Vector3(windowBounds.size.x, collider.size.y, slotWidth);
            }
        }

        // Doğma noktası: fritöz hattı ile yarımada arasındaki koridor.
        var island = WorldBounds(Asset(layout, "SM_AssemblyIsland_08"));
        var fryer = WorldBounds(Asset(layout, "SM_Fryer_03"));
        var spawn = Find("Dogma_Sef");
        if (spawn != null)
            spawn.SetPositionAndRotation(new Vector3(island.center.x, Origin.y, (fryer.max.z + island.min.z) * 0.5f), Quaternion.identity);

        var sound = Find("Ses_Mutfak");
        if (sound != null)
            sound.position = new Vector3(island.center.x, sound.position.y, island.center.z);

        VerifyNetworkHashes();
    }

    private static void PlaceSlot(string slotName, Vector3 position, Quaternion rotation)
    {
        var slot = Find(slotName);
        if (slot != null)
            slot.SetPositionAndRotation(position, rotation);
    }

    // Tezgahın yerleştirme noktası tahtanın üst yüzüne gelecek şekilde kök konumlanır.
    private static void PlaceStation(string stationName, Transform board)
    {
        var station = Find(stationName);
        if (station == null)
            return;

        var boardBounds = WorldBounds(board);
        var placement = station.Find("PlacementPoint");
        float placementOffset = placement != null ? placement.localPosition.y : 0f;
        station.SetPositionAndRotation(
            new Vector3(boardBounds.center.x, boardBounds.max.y - placementOffset, boardBounds.center.z), board.rotation);
        HideOldVisuals(station);

        if (station.TryGetComponent<BoxCollider>(out var collider))
        {
            const float clickHeight = 0.08f;
            collider.size = new Vector3(boardBounds.size.x, clickHeight, boardBounds.size.z);
            collider.center = new Vector3(0f, placementOffset + clickHeight * 0.5f - boardBounds.size.y, 0f);
        }
    }

    // Kök modelin pivotuna oturur; tıklanan hacim modelin kendi sınırıdır.
    private static void PlaceOnAsset(string rootName, Transform asset, Bounds localBounds)
    {
        var root = Find(rootName);
        if (root == null)
            return;

        root.SetPositionAndRotation(asset.position, asset.rotation);
        HideOldVisuals(root);
        FitCollider(root, localBounds);
    }

    private static void FitCollider(Transform root, Bounds localBounds)
    {
        if (!root.TryGetComponent<BoxCollider>(out var collider))
            collider = root.gameObject.AddComponent<BoxCollider>();

        collider.center = localBounds.center;
        collider.size = localBounds.size;
    }

    // Eski görsel çocuklar (artist'in önceki modelleri) kapatılır; yerleştirme / yığın düğümleri kalır.
    private static void HideOldVisuals(Transform root)
    {
        foreach (Transform child in root)
        {
            if (child.name.StartsWith("PF_") || child.name.StartsWith("SM_"))
                child.gameObject.SetActive(false);
        }
    }

    // ---------- hata paneli ----------

    private static void InstallErrorPanel(Transform layout)
    {
        var model = Asset(layout, "SM_XPanel_30");
        var prefab = BuildPanelPrefab();

        var old = GameObject.Find("HataPaneli_Mutfak");
        if (old != null)
            Object.DestroyImmediate(old);

        var panel = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        panel.name = "HataPaneli_Mutfak";
        panel.transform.SetPositionAndRotation(model.position, model.rotation);
        // Yerleşimdeki süs kopyası kapatılır: aynı yerde işlevli panel durur.
        model.gameObject.SetActive(false);
    }

    // Panel modeli + sönük işaretlerin Canvas'ı (eski panelden) + ErrorWallPanel (modelin X düğümlerine bağlı).
    private static GameObject BuildPanelPrefab()
    {
        var litMaterial = AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath);
        if (litMaterial == null)
        {
            litMaterial = new Material(Shader.Find("CNE/Toon"));
            litMaterial.SetColor("_BaseColor", new Color32(0xE0, 0x26, 0x2B, 0xFF));
            litMaterial.SetTexture("_PropMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/NewAssets/Mutfak_C1/Palettes/Mutfak_Beyaz_Prop.png"));
            litMaterial.EnableKeyword("_EMISSION_ON");
            litMaterial.SetFloat("_Emission", 1f);
            litMaterial.SetColor("_EmissionColor", Color.white * 2.5f);
            litMaterial.SetFloat("_EmissionBaseTint", 1f);
            AssetDatabase.CreateAsset(litMaterial, LitMaterialPath);
        }

        var root = new GameObject("HataPaneli_C1");
        try
        {
            var layoutPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MutfakC1Builder.LayoutPrefabPath);
            var source = Asset(layoutPrefab.transform, "SM_XPanel_30");
            var model = Object.Instantiate(source.gameObject, root.transform);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.SetActive(true);

            var slots = new List<Transform>();
            float faceZ = 0f;
            foreach (Transform child in model.transform)
            {
                if (!child.name.Contains(".X_"))
                    continue;

                slots.Add(child);
                child.GetComponent<Renderer>().sharedMaterial = litMaterial;
            }

            // Bakana göre soldan sağa: panelin önü +Z olduğu için bakanın solu +X'tir.
            slots.Sort((a, b) => b.localPosition.x.CompareTo(a.localPosition.x));
            faceZ = model.GetComponent<Renderer>().localBounds.max.z;

            var oldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OldPanelPrefabPath);
            var canvas = Object.Instantiate(oldPrefab.transform.Find("SonukIsaretler").gameObject, root.transform);
            canvas.name = "SonukIsaretler";
            // Eski panelin önü -Z idi; yeni modelinki +Z. Canvas panelin ön yüzünün hemen önüne, öne bakacak şekilde.
            canvas.transform.localPosition = new Vector3(0f, 0f, faceZ + 0.002f);
            canvas.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var panel = root.AddComponent<ErrorWallPanel>();
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("dimTemplate").objectReferenceValue = canvas.transform.GetChild(0);
            serialized.FindProperty("litTemplate").objectReferenceValue = null;
            var slotsProperty = serialized.FindProperty("modelSlots");
            slotsProperty.arraySize = slots.Count;
            for (int i = 0; i < slots.Count; i++)
                slotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return PrefabUtility.SaveAsPrefabAsset(root, PanelPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // ---------- yardımcılar ----------

    private static Transform Asset(Transform layout, string name)
    {
        var asset = layout.Find(name);
        if (asset == null)
            throw new System.InvalidOperationException($"Yerleşimde '{name}' yok.");

        return asset;
    }

    private static Transform Find(string name)
    {
        var found = GameObject.Find(name);
        if (found == null)
        {
            _log.AppendLine($"UYARI: sahnede '{name}' yok.");
            return null;
        }

        return found.transform;
    }

    private static Bounds WorldBounds(Transform asset)
    {
        var renderers = asset.GetComponentsInChildren<Renderer>(true);
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
            bounds.Encapsulate(renderer.bounds);

        return bounds;
    }

    // Renderer kapalıyken de doğru sonuç verir (Renderer.bounds kapalı nesnede güvenilmez): yerel sınırın köşeleri
    // dünyaya çevrilir.
    private static float MaxWorldX(Renderer renderer)
    {
        var local = renderer.localBounds;
        float max = float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            var corner = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            max = Mathf.Max(max, renderer.transform.TransformPoint(corner).x);
        }

        return max;
    }

    // Modelin kendi eksenindeki sınırı (model dosyası orijinde ve dönüşsüz durur).
    private static Bounds LocalBounds(string assetName)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/NewAssets/Mutfak_C1/Models/{assetName}.glb");
        return WorldBounds(model.transform);
    }

    private static Vector3 SocketMidpoint(Transform asset)
    {
        var sum = Vector3.zero;
        int count = 0;
        foreach (Transform child in asset)
        {
            if (!child.name.StartsWith("Socket_"))
                continue;

            sum += child.position;
            count++;
        }

        return count > 0 ? sum / count : asset.position;
    }

    // Sahnede kopyalanan NetworkObject kaynağın kimliğini taşır (CLAUDE.md): OnValidate yeniden üretir.
    private static void RefreshNetworkHash(NetworkObject networkObject)
    {
        if (networkObject == null)
            return;

        typeof(NetworkObject).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(networkObject, null);
        EditorUtility.SetDirty(networkObject);
    }

    private static void VerifyNetworkHashes()
    {
        var field = typeof(NetworkObject).GetField("GlobalObjectIdHash", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        var seen = new Dictionary<uint, string>();
        foreach (var networkObject in Object.FindObjectsByType<NetworkObject>(FindObjectsInactive.Include))
        {
            uint hash = (uint)field.GetValue(networkObject);
            if (seen.TryGetValue(hash, out string other))
                _log.AppendLine($"HATA: ağ kimliği çakışıyor: {networkObject.name} ↔ {other} ({hash})");
            else
                seen[hash] = networkObject.name;
        }

        _log.AppendLine($"ağ nesnesi: {seen.Count}, çakışma kontrolü tamam.");
    }
}
