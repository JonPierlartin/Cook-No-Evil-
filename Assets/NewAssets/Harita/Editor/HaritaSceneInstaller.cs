using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Haritanın tamamını AÇIK sahneye kurar: CNE → Harita → Install In Scene. Yeniden çalıştırılabilir (kendi kurduğu
// mimariyi silip yeniden kurar; yerleşim köklerini varsa yerinde bırakır; işlevsel kökleri yalnızca taşır).
//  1. Yerleşimler: C1 mutfak (Mutfak_C1), İstasyon, Kasa ve Salon prefab'ları paketlerdeki ortak eksene göre konur.
//     Paketler aynı planı paylaşır: İstasyon kökü = mutfak kökü + 4,20 m (x), Kasa kökü = İstasyon kökü − 3,20 m (z),
//     Salon kökü = Kasa kökü + 2,10 m (x) − 8,20 m (z).
//  2. Mimari (paketlerde yalnızca referans olarak var): duvarlar, zeminler ve tavanlar burada mesh olarak üretilir.
//     Desenler (fayans, lambri, dama) materyalde, dünya uzayında. Eski blockout harita ve yer tutucu mimari SİLİNİR.
//  3. İşlev taşınır: yuvalar, kaplar, çöp kovaları, panolar, hata panelleri, tarif kitapçığı, müşteri noktaları ve
//     doğma noktaları yeni modellerin soketlerine oturtulur. Kodları ve ağ kimlikleri değişmez.
// Sayılar yerleşimlerden ve modellerin sınırlarından okunur; elle girilenler aşağıdaki sabitlerdir (paket planları).
public static class HaritaSceneInstaller
{
    // C1 mutfak yerleşiminin kökü: mutfağın İstasyon duvarının iç yüzü (x) ve Kasa duvarının iç yüzü (z), zemin üstü.
    private static readonly Vector3 KitchenOrigin = new(4.5f, 0.30f, -3.58f);

    // Paket planları (metre).
    private const float WallThickness = 0.2f;
    private const float CeilingHeight = 2.75f;
    private const float KitchenWidth = 5.6f;
    private const float KitchenLength = 6f;
    private const float StationWidth = 4f;
    private const float StationLength = 4.45f;
    private const float CounterLength = 3f;
    // Pencerelerin duvardaki kaba boşluğu (kasa + söve bunu doldurur): pivot = net açıklığın alt kenarı ortası.
    private const float WindowHalfWidth = 1.265f;
    private const float WindowBottom = 0.954f;
    private const float WindowTop = 1.965f;
    private const float SkirtingHeight = 0.10f;
    private const float WainscotHeight = 1.0f;
    private const float TrimHeight = 0.035f;
    // Salon (müşteri alanı): Kasa'nın müşteri duvarının önünde, Kasa'dan iki yana 2,10 m taşar.
    private const float HallWidth = 14f;
    private const float HallLength = 8f;
    private const float HallOverhang = 2.1f;
    // Vitrin: cam bantlarının ve kapıların duvardaki boşluğu (zeminden).
    private const float StorefrontSill = 0.9f;
    private const float StorefrontTop = 2.3f;
    private const float FacadeBandHeight = 0.035f;
    // Sokak (paket planı, salon köküne göre Unity ekseninde): müşterinin doğduğu ve kaybolduğu noktalar yakın
    // kaldırımda; vitrinin önündeki iki saksı salon paketinin çalısıdır.
    private static readonly Vector3 StreetSpawn = new(5f, 0f, -1.5f);
    private static readonly Vector3 StreetDespawn = new(-20f, 0f, -1.5f);
    private static readonly Vector3[] StreetPlanters = { new(-2.4f, 0f, -0.55f), new(-5.2f, 0f, -0.55f) };
    // Kasiyer ve Komi kameralarının arka planı (paket: SY-10, S11·K12).
    private static readonly Color32 SkyColor = new(0xCF, 0xE3, 0xEE, 0xFF);
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const float FloorThickness = 0.3f;

    private const string KitchenRootName = "Mutfak_C1";
    private const string StationRootName = "Istasyon";
    private const string CounterRootName = "Kasa";
    private const string HallRootName = "Salon";
    private const string StreetRootName = "Sokak";
    private const string ArchitectureRootName = "Harita_Mimari";
    private const string MeshFolder = "Assets/NewAssets/Harita/Meshes";
    private const string PanelPrefabPath = "Assets/Prefabs/Mutfak/HataPaneli_C1.prefab";
    private const string OldPanelPrefabPath = "Assets/Prefabs/HataPaneli.prefab";
    private const string LitMaterialPath = "Assets/NewAssets/Mutfak_C1/Materials/MI_XPanel_Yanan.mat";
    private const string KitchenModelFolder = "Assets/NewAssets/Mutfak_C1/Models";

    // Eski harita: blockout kökü, mutfağın yer tutucu mimarisi ve daha önce kapatılmış eski eşyalar.
    private static readonly string[] ObsoleteRoots = { "Harita", "Mutfak_C1_Mimari", "SM_Table_02", "SM_KitchenDoor", "SM_KitchenDoor (1)" };

    private static StringBuilder _log;

    public static string LastLog => _log != null ? _log.ToString() : string.Empty;

    [MenuItem("CNE/Harita/Install In Scene")]
    public static void Install()
    {
        _log = new StringBuilder();
        var scene = SceneManager.GetActiveScene();

        var kitchen = PlaceLayout(KitchenRootName, MutfakC1Builder.LayoutPrefabPath, KitchenOrigin);
        var stationOrigin = KitchenOrigin + new Vector3(WallThickness + StationWidth, 0f, 0f);
        var counterOrigin = stationOrigin + new Vector3(0f, 0f, -WallThickness - CounterLength);
        var station = PlaceLayout(StationRootName, HaritaBuilder.StationPrefabPath, stationOrigin);
        var counter = PlaceLayout(CounterRootName, HaritaBuilder.CounterPrefabPath, counterOrigin);
        var hallOrigin = counterOrigin + new Vector3(HallOverhang, 0f, -WallThickness - HallLength);
        var hall = PlaceLayout(HallRootName, HaritaBuilder.HallPrefabPath, hallOrigin);
        // Sokak yerleşimi salon köküyle aynı yerde durur (paket planı).
        var street = PlaceLayout(StreetRootName, HaritaBuilder.StreetPrefabPath, hallOrigin);

        // Paketler arası sahiplik: mutfak penceresinin güncel modeli İstasyon paketinde; malzeme panolarının işlevli
        // (veriden dolan) hâli sahnede ayrıca durur.
        Disable(kitchen, "SM_StationWindow_23");
        Disable(station, "SM_IngredientBoard");
        Disable(counter, "SM_IngredientBoard");

        RemoveObsolete();
        BuildArchitecture(kitchen, station, counter, hall);
        MoveKitchenFunction(kitchen, station);
        MoveStationFunction(station);
        MoveCounterFunction(counter, station);
        PlaceCustomers(counter, hall, street);
        InstallStreetExtras(hall, street);
        InstallStreetLife(street);
        InstallChefVision(station);
        ApplySky();
        OpenDoors(hall);
        InstallAquarium(hall);
        InstallDoorSigns(hall);
        VerifyNetworkHashes();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[CNE] Harita sahneye kuruldu.\n" + _log);
    }

    // Kök varsa yerinde bırakılır (sahnede eklenen parçalar kaybolmasın), yoksa prefab'dan kurulur.
    private static Transform PlaceLayout(string rootName, string prefabPath, Vector3 origin)
    {
        var existing = GameObject.Find(rootName);
        if (existing == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new System.InvalidOperationException($"Prefab yok: {prefabPath} (önce CNE → Harita → Build).");

            existing = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            existing.name = rootName;
        }

        existing.transform.SetPositionAndRotation(origin, Quaternion.identity);
        return MutfakC1Builder.AssetContainer(existing.transform);
    }

    private static void Disable(Transform layout, string assetName)
    {
        var asset = layout.Find(assetName);
        if (asset != null)
            asset.gameObject.SetActive(false);
    }

    private static void RemoveObsolete()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (System.Array.IndexOf(ObsoleteRoots, root.name) < 0)
                continue;

            if (root.GetComponentInChildren<NetworkObject>(true) != null)
            {
                _log.AppendLine($"UYARI: '{root.name}' ağ nesnesi taşıyor, silinmedi.");
                continue;
            }

            _log.AppendLine($"silindi: {root.name}");
            Object.DestroyImmediate(root);
        }
    }

    // ---------- mimari ----------

    // Bir duvar yüzünün kaplaması: aşağıdan yukarı bantlar (üst sınır, materyal).
    private sealed class Finish
    {
        public readonly (float top, Material material)[] Bands;

        public Finish(params (float top, Material material)[] bands)
        {
            Bands = bands;
        }
    }

    private struct Opening
    {
        public float U0, U1, Y0, Y1;
    }

    // Eksenlere hizalı duvar: kalınlık ekseninde [slabMin, slabMax], uzunluk ekseninde [uMin, uMax]. Her yüzün
    // kaplaması uzunluk boyunca değişebilir (ortak duvarlar): (şu u'ya kadar, kaplama) parçaları; null = çizilmez.
    private sealed class Wall
    {
        public string Name;
        public bool AlongX;
        public float SlabMin, SlabMax, UMin, UMax;
        public (float uEnd, Finish finish)[] LowFace, HighFace;
        public readonly List<Opening> Openings = new();

        public Vector3 Point(float u, float y, float slab)
        {
            return AlongX ? new Vector3(u, y, slab) : new Vector3(slab, y, u);
        }
    }

    private sealed class MeshBuilder
    {
        private readonly List<Vector3> _vertices = new();
        private readonly List<Vector3> _normals = new();
        private readonly List<Material> _materials = new();
        private readonly List<List<int>> _triangles = new();

        public void Quad(Material material, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            int submesh = _materials.IndexOf(material);
            if (submesh < 0)
            {
                submesh = _materials.Count;
                _materials.Add(material);
                _triangles.Add(new List<int>());
            }

            int first = _vertices.Count;
            _vertices.AddRange(new[] { a, b, c, d });
            for (int i = 0; i < 4; i++)
                _normals.Add(normal);

            // Unity'de ön yüz: köşeler önden bakınca saat yönünde (cross(b−a, c−a) öne bakar).
            bool forward = Vector3.Dot(Vector3.Cross(b - a, c - a), normal) > 0f;
            var triangles = _triangles[submesh];
            if (forward)
                triangles.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
            else
                triangles.AddRange(new[] { first, first + 2, first + 1, first, first + 3, first + 2 });
        }

        public GameObject Build(Transform parent, string name)
        {
            // Mesh sahneye gömülmez, asset olarak durur. Yeniden kurulumda eski dosya silinip yenisi yazılır: var olan
            // mesh asset'ini yerinde güncellemek (CopySerialized ya da Clear + Set) çizilen veriyi yenilemiyordu —
            // sahne eski geometriyi yeni materyal sırasıyla çiziyordu.
            MutfakC1Builder.EnsureFolder(MeshFolder);
            string path = $"{MeshFolder}/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
                AssetDatabase.DeleteAsset(path);

            var asset = new Mesh { name = name };
            asset.SetVertices(_vertices);
            asset.SetNormals(_normals);
            asset.subMeshCount = _materials.Count;
            for (int i = 0; i < _materials.Count; i++)
                asset.SetTriangles(_triangles[i], i);
            asset.RecalculateBounds();
            AssetDatabase.CreateAsset(asset, path);

            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.AddComponent<MeshFilter>().sharedMesh = asset;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = _materials.ToArray();
            // Mimari ana ışığa gölge düşürmez (paket kuralı): odalar tavanlı, güneş içeriyi yine aydınlatır.
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return gameObject;
        }
    }

    private static void BuildArchitecture(Transform kitchen, Transform station, Transform counter, Transform hall)
    {
        var existing = GameObject.Find(ArchitectureRootName);
        if (existing != null)
            Object.DestroyImmediate(existing);

        var materials = HaritaBuilder.CreateArchitectureMaterials();
        float floorY = KitchenOrigin.y;
        var root = new GameObject(ArchitectureRootName).transform;
        root.position = new Vector3(0f, floorY, 0f);

        // Odaların iç yüzleri (dünya x / z).
        float kitchenX1 = KitchenOrigin.x, kitchenX0 = kitchenX1 - KitchenWidth;
        float kitchenZ0 = KitchenOrigin.z, kitchenZ1 = kitchenZ0 + KitchenLength;
        float stationX0 = kitchenX1 + WallThickness, stationX1 = stationX0 + StationWidth;
        float stationZ0 = kitchenZ0, stationZ1 = stationZ0 + StationLength;
        float counterX0 = kitchenX0, counterX1 = stationX1;
        float counterZ1 = kitchenZ0 - WallThickness, counterZ0 = counterZ1 - CounterLength;
        float westOuter = kitchenX0 - WallThickness, eastOuter = stationX1 + WallThickness;
        float hallX1 = counterX1 + HallOverhang, hallX0 = hallX1 - HallWidth;
        float hallZ1 = counterZ0 - WallThickness, hallZ0 = hallZ1 - HallLength;
        float hallWest = hallX0 - WallThickness, hallEast = hallX1 + WallThickness;

        var skirting = materials["MI_Arch_Supurgelik"];
        var kitchenFinish = new Finish((SkirtingHeight, skirting), (CeilingHeight, materials["MI_Arch_Mutfak_Duvar"]));
        var stationFinish = new Finish((SkirtingHeight, skirting), (CeilingHeight, materials["MI_Arch_Istasyon_Duvar"]));
        var counterFinish = new Finish(
            (SkirtingHeight, skirting), (WainscotHeight, materials["MI_Arch_Kasa_Lambri"]),
            (WainscotHeight + TrimHeight, materials["MI_Arch_Kasa_Bordur"]), (CeilingHeight, materials["MI_Arch_Kasa_Duvar"]));
        var outside = new Finish((CeilingHeight, materials["MI_Arch_Dis"]));
        // Salonun iç yüzü Kasa'yla aynı kaplama; dış cephe: plint, turkuaz kuşak, krom bant, krem.
        var hallFinish = counterFinish;
        var facade = new Finish(
            (SkirtingHeight, materials["MI_Arch_Dis_Plint"]), (StorefrontSill, materials["MI_Arch_Dis_Turkuaz"]),
            (StorefrontSill + FacadeBandHeight, materials["MI_Arch_Kasa_Bordur"]), (CeilingHeight, materials["MI_Arch_Dis_Krem"]));

        // Açıklıklar yerleşimdeki modellerin yerinden.
        float orderX = Asset(counter, "SM_StationWindow_Order").position.x;
        float deliveryX = Asset(counter, "SM_StationWindow_Delivery").position.x;
        float counterWindowX = Asset(station, "SM_StationWindow_Kasa").position.x;
        float kitchenWindowZ = Asset(station, "SM_StationWindow_Kitchen").position.z;
        var door = WorldBounds(Asset(kitchen, "SM_SwingDoorFrame_27"));
        const float frameOverlap = 0.03f;   // duvar kapı kasasının altına biraz girer (arada boşluk kalmasın)

        var walls = new List<Wall>();

        // Müşteri duvarı (Kasa ile salon arası; salon Kasa'dan iki yana taşar): sipariş ve teslimat pencereleri.
        var customerWall = new Wall
        {
            Name = "Musteri", AlongX = true, SlabMin = counterZ0 - WallThickness, SlabMax = counterZ0,
            UMin = hallWest, UMax = hallEast,
            LowFace = new[] { (hallEast, hallFinish) },
            HighFace = new[] { (westOuter, facade), (eastOuter, counterFinish), (hallEast, facade) },
        };
        customerWall.Openings.Add(Window(orderX));
        customerWall.Openings.Add(Window(deliveryX));
        walls.Add(customerWall);

        // Kasa ile mutfak + İstasyon arasındaki duvar: mutfak kapısı ve Kasa penceresi.
        var middleWall = new Wall
        {
            Name = "Orta", AlongX = true, SlabMin = counterZ1, SlabMax = kitchenZ0, UMin = westOuter, UMax = eastOuter,
            LowFace = new[] { (eastOuter, counterFinish) },
            HighFace = new[] { (kitchenX1, kitchenFinish), (stationX0, (Finish)null), (eastOuter, stationFinish) },
        };
        middleWall.Openings.Add(new Opening
        {
            U0 = door.min.x + frameOverlap, U1 = door.max.x - frameOverlap, Y0 = 0f, Y1 = door.max.y - floorY - frameOverlap,
        });
        middleWall.Openings.Add(Window(counterWindowX));
        walls.Add(middleWall);

        // Mutfak ile İstasyon arasındaki duvar: mutfak penceresi. İstasyon'un bittiği yerden sonrası dış yüz.
        var kitchenStationWall = new Wall
        {
            Name = "MutfakIstasyon", AlongX = false, SlabMin = kitchenX1, SlabMax = stationX0,
            UMin = kitchenZ0, UMax = kitchenZ1 + WallThickness,
            LowFace = new[] { (kitchenZ1 + WallThickness, kitchenFinish) },
            HighFace = new[] { (stationZ1, stationFinish), (stationZ1 + WallThickness, (Finish)null), (kitchenZ1 + WallThickness, outside) },
        };
        kitchenStationWall.Openings.Add(Window(kitchenWindowZ));
        walls.Add(kitchenStationWall);

        // Dış duvarlar.
        walls.Add(new Wall
        {
            Name = "Bati", AlongX = false, SlabMin = westOuter, SlabMax = kitchenX0,
            UMin = counterZ0 - WallThickness, UMax = kitchenZ1 + WallThickness,
            LowFace = new[] { (kitchenZ1 + WallThickness, outside) },
            HighFace = new[] { (counterZ1, counterFinish), (kitchenZ0, (Finish)null), (kitchenZ1 + WallThickness, kitchenFinish) },
        });
        walls.Add(new Wall
        {
            Name = "Dogu", AlongX = false, SlabMin = stationX1, SlabMax = eastOuter,
            UMin = counterZ0 - WallThickness, UMax = stationZ1 + WallThickness,
            LowFace = new[] { (counterZ1, counterFinish), (stationZ0, (Finish)null), (stationZ1 + WallThickness, stationFinish) },
            HighFace = new[] { (stationZ1 + WallThickness, outside) },
        });
        walls.Add(new Wall
        {
            Name = "MutfakArka", AlongX = true, SlabMin = kitchenZ1, SlabMax = kitchenZ1 + WallThickness,
            UMin = westOuter, UMax = stationX0,
            LowFace = new[] { (stationX0, kitchenFinish) }, HighFace = new[] { (stationX0, outside) },
        });
        walls.Add(new Wall
        {
            Name = "IstasyonArka", AlongX = true, SlabMin = stationZ1, SlabMax = stationZ1 + WallThickness,
            UMin = stationX0, UMax = eastOuter,
            LowFace = new[] { (eastOuter, stationFinish) }, HighFace = new[] { (eastOuter, outside) },
        });

        // Salonun yan duvarları ve vitrin duvarı (cam bantları ve iki kapı; boşluklar modellerin yerinden).
        walls.Add(new Wall
        {
            Name = "SalonBati", AlongX = false, SlabMin = hallWest, SlabMax = hallX0, UMin = hallZ0 - WallThickness, UMax = hallZ1,
            LowFace = new[] { (hallZ1, facade) }, HighFace = new[] { (hallZ1, hallFinish) },
        });
        walls.Add(new Wall
        {
            Name = "SalonDogu", AlongX = false, SlabMin = hallX1, SlabMax = hallEast, UMin = hallZ0 - WallThickness, UMax = hallZ1,
            LowFace = new[] { (hallZ1, hallFinish) }, HighFace = new[] { (hallZ1, facade) },
        });
        var storefront = new Wall
        {
            Name = "Vitrin", AlongX = true, SlabMin = hallZ0 - WallThickness, SlabMax = hallZ0, UMin = hallWest, UMax = hallEast,
            LowFace = new[] { (hallEast, facade) }, HighFace = new[] { (hallEast, hallFinish) },
        };
        var doors = new List<Bounds>();
        foreach (Transform asset in hall)
        {
            bool isDoor = asset.name.StartsWith("SM_StorefrontDoor");
            if (!isDoor && !asset.name.StartsWith("SM_StorefrontBay"))
                continue;

            var bounds = WorldBounds(asset);
            storefront.Openings.Add(new Opening
            {
                U0 = Snap(bounds.min.x), U1 = Snap(bounds.max.x), Y0 = isDoor ? 0f : StorefrontSill, Y1 = StorefrontTop,
            });
            if (isDoor)
                doors.Add(bounds);
        }

        walls.Add(storefront);

        var wallMesh = new MeshBuilder();
        var colliders = new GameObject("Carpisma").transform;
        colliders.SetParent(root, false);
        foreach (var wall in walls)
        {
            EmitFace(wallMesh, wall, low: true);
            EmitFace(wallMesh, wall, low: false);
            EmitReveals(wallMesh, wall, materials["MI_Arch_Dis"]);
            EmitColliders(colliders, wall);
        }

        wallMesh.Build(root, "Harita_Duvarlar");

        // Zeminler ve tavanlar. Kapı eşiği mutfak zeminiyle kaplanır; müşteri alanı şimdilik düz bir zemin.
        var floors = new MeshBuilder();
        Horizontal(floors, materials["MI_Arch_Mutfak_Zemin"], kitchenX0, kitchenX1, kitchenZ0, kitchenZ1, 0f, Vector3.up);
        Horizontal(floors, materials["MI_Arch_Mutfak_Zemin"], door.min.x, door.max.x, counterZ1, kitchenZ0, 0f, Vector3.up);
        Horizontal(floors, materials["MI_Arch_Istasyon_Zemin"], stationX0, stationX1, stationZ0, stationZ1, 0f, Vector3.up);
        Horizontal(floors, materials["MI_Arch_Kasa_Zemin"], counterX0, counterX1, counterZ0, counterZ1, 0f, Vector3.up);
        Horizontal(floors, materials["MI_Arch_Salon_Zemin"], hallX0, hallX1, hallZ0, hallZ1, 0f, Vector3.up);
        foreach (var doorBounds in doors)
            Horizontal(floors, materials["MI_Arch_Esik"], Snap(doorBounds.min.x), Snap(doorBounds.max.x), hallZ0 - WallThickness, hallZ0, 0f, Vector3.up);
        // Vitrinin dışı sokak paketinin kaldırımıdır (Sokak kökü); burada zemin üretilmez.
        floors.Build(root, "Harita_Zemin");

        // Dama karoları odanın köşesinden başlar (desen dünya uzayında; başlangıç noktası materyale yazılır).
        SetPatternOrigin(materials["MI_Arch_Kasa_Zemin"], counterX1, counterZ0);
        SetPatternOrigin(materials["MI_Arch_Salon_Zemin"], hallX1, hallZ0);

        var ceilings = new MeshBuilder();
        Horizontal(ceilings, materials["MI_Arch_Tavan"], kitchenX0, kitchenX1, kitchenZ0, kitchenZ1, CeilingHeight, Vector3.down);
        Horizontal(ceilings, materials["MI_Arch_Tavan"], stationX0, stationX1, stationZ0, stationZ1, CeilingHeight, Vector3.down);
        Horizontal(ceilings, materials["MI_Arch_Kasa_Tavan"], counterX0, counterX1, counterZ0, counterZ1, CeilingHeight, Vector3.down);
        Horizontal(ceilings, materials["MI_Arch_Kasa_Tavan"], hallX0, hallX1, hallZ0, hallZ1, CeilingHeight, Vector3.down);
        ceilings.Build(root, "Harita_Tavan");

        Blocker(colliders, "Zemin",
            new Vector3(hallWest, floorY - FloorThickness, hallZ0 - WallThickness),
            new Vector3(hallEast, floorY, kitchenZ1 + WallThickness));

        // Kapı kapalıdır (GDD 5.2.3: yalnızca yangında açılır — Faz 1): açıklık görünmez bir engelle kapatılır.
        Blocker(colliders, "Engel_Kapi", new Vector3(door.min.x, floorY, counterZ1), new Vector3(door.max.x, door.max.y, kitchenZ0));

        BuildChefScreens(root, materials["MI_Arch_Dis"], counterWindowX, counterZ1, door, floorY);
        BuildAcoustics(root,
            new Bounds(new Vector3((kitchenX0 + kitchenX1) * 0.5f, floorY + CeilingHeight * 0.5f, (kitchenZ0 + kitchenZ1) * 0.5f), new Vector3(KitchenWidth, CeilingHeight, KitchenLength)),
            new Bounds(new Vector3((stationX0 + stationX1) * 0.5f, floorY + CeilingHeight * 0.5f, (stationZ0 + stationZ1) * 0.5f), new Vector3(StationWidth, CeilingHeight, StationLength)),
            new Bounds(new Vector3((counterX0 + counterX1) * 0.5f, floorY + CeilingHeight * 0.5f, (counterZ0 + counterZ1) * 0.5f), new Vector3(counterX1 - counterX0, CeilingHeight, CounterLength)),
            new Bounds(new Vector3((hallX0 + hallX1) * 0.5f, floorY + CeilingHeight * 0.5f, (hallZ0 + hallZ1) * 0.5f), new Vector3(HallWidth, CeilingHeight, HallLength)),
            kitchenWindowZ, counterWindowX, orderX, deliveryX);

        // Eşyaların çarpışması (dekor). Üstünde etkileşim hedefi duran eşyada engel o yüzeyin altında biter; yoksa
        // nişan ışını engele çarpar ve hedef bulunamaz. Pencere pervazları da aynı kuralla (yuvaların altında).
        const float belowSurface = 0.98f;
        var solids = new (Transform layout, string asset, float topAboveFloor)[]
        {
            (kitchen, "SM_Grill_01", belowSurface), (kitchen, "SM_Grill_02", belowSurface), (kitchen, "SM_Fryer_03", 0f),
            (kitchen, "SM_Fryer_04", 0f), (kitchen, "SM_FryStation_05", 0f), (kitchen, "SM_AssemblyIsland_08", belowSurface),
            (kitchen, "SM_PrepTable_13", 0.83f),
            (station, "SM_PackingStation", belowSurface), (station, "SM_BoxingStation", 0f),
            (station, "SM_StationWindow_Kasa", belowSurface), (station, "SM_StationWindow_Kitchen", belowSurface),
            (counter, "SM_DrinkIceCounter", belowSurface), (counter, "SM_DrinkMachine", 0f), (counter, "SM_IceCreamMachine", 0f),
            (counter, "SM_ToppingBins", 0f), (counter, "SM_StationWindow_Order", belowSurface),
            (counter, "SM_StationWindow_Delivery", belowSurface),
        };
        foreach (var (layout, assetName, top) in solids)
        {
            var bounds = WorldBounds(Asset(layout, assetName));
            var min = bounds.min;
            var max = bounds.max;
            if (top > 0f)
            {
                min.y = floorY;
                max.y = floorY + top;
            }

            Blocker(colliders, "Engel_" + assetName, min, max);
        }
    }

    // Komşu boşlukların kenarları (cam bandı ile kapı) aynı sayıya otursun diye milimetreye yuvarlanır.
    private static float Snap(float value)
    {
        return Mathf.Round(value * 1000f) / 1000f;
    }

    private static void SetPatternOrigin(Material material, float x, float z)
    {
        var size = material.GetVector("_PatternSize");
        var updated = new Vector4(size.x, size.y, x, z);
        if (size == updated)
            return;

        material.SetVector("_PatternSize", updated);
        EditorUtility.SetDirty(material);
    }

    // ---------- Şef'in görüşü ----------

    // Şef yalnızca mutfağını, pencereden Komi'yi ve Komi'nin ODASINI görür (Ersel, 8 Eki): İstasyon'un eşyalarını,
    // Kasa'yı, Kasiyer'i ve ötesini görmez; mutfak kapısının camlarından da hiçbir şey görmez.
    //  - Perdeler (yalnızca Şef'in kamerasının çizdiği katman): Kasa penceresinin ve mutfak kapısının açıklığını
    //    Kasa tarafından kapatan düz yüzeyler. Arkalarındaki her şey (Kasa, Kasiyer, müşteriler, salon, sokak) Şef
    //    için görünmez; başka hiçbir rol perdeleri görmez.
    //  - Gizlenen eşyalar (Şef'in kamerasının çizmediği katman): İstasyon yerleşimi (pencereler hariç), İstasyon'daki
    //    pano ve hata paneli.
    // Katman kararını kamera verir (BlindVisionCamera); burada yalnızca nesneler katmanlara konur.
    private const string ChefHiddenLayer = "SefGormez";
    private const string ChefScreenLayer = "SefPerdesi";

    private static void BuildChefScreens(Transform root, Material material, float counterWindowX, float counterFaceZ, Bounds door, float floorY)
    {
        int layer = EnsureLayer(ChefScreenLayer);
        var screens = new GameObject("SefPerdeleri").transform;
        screens.SetParent(root, false);

        const float margin = 0.05f;     // perde açıklıktan biraz büyük (kenardan sızmasın)
        const float inset = 0.01f;
        // Kasa penceresi: açıklığın Kasa tarafındaki ağzı.
        Screen(screens, "Perde_KasaPenceresi", material, layer,
            new Vector3(counterWindowX, floorY + (WindowBottom + WindowTop) * 0.5f, counterFaceZ + inset),
            new Vector2(WindowHalfWidth * 2f + margin * 2f, WindowTop - WindowBottom + margin * 2f));
        // Mutfak kapısı: kapı kanatlarının Kasa tarafı (kanatlar Şef'e görünür kalır, camların ardı kapanır).
        float doorHeight = door.max.y - floorY;
        Screen(screens, "Perde_MutfakKapisi", material, layer,
            new Vector3(door.center.x, floorY + doorHeight * 0.5f, Mathf.Min(door.min.z, counterFaceZ) - inset),
            new Vector2(door.size.x + margin * 2f, doorHeight + margin * 2f));
    }

    // Kuzeye (+Z, mutfak ve İstasyon tarafına) bakan düz yüzey.
    private static void Screen(Transform parent, string name, Material material, int layer, Vector3 center, Vector2 size)
    {
        var screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(screen.GetComponent<Collider>());
        screen.name = name;
        screen.layer = layer;
        screen.transform.SetParent(parent, false);
        screen.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, 180f, 0f));
        screen.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = screen.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    private static void InstallChefVision(Transform station)
    {
        int hidden = EnsureLayer(ChefHiddenLayer);
        int screen = EnsureLayer(ChefScreenLayer);

        foreach (Transform asset in station)
        {
            // Pencereler odanın parçasıdır (ve mutfak penceresi iki odanın ortağıdır): görünür kalır.
            if (!asset.name.StartsWith("SM_StationWindow"))
                SetLayer(asset, hidden);
        }

        foreach (string name in new[] { "MalzemePanosu_Istasyon", "HataPaneli_Istasyon" })
        {
            var found = GameObject.Find(name);
            if (found != null)
                SetLayer(found.transform, hidden);
        }

        // Perdeleri Şef dışında hiçbir kamera çizmez: sahne kamerası ve oyuncu kamerasının kör olmayan hâli.
        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            if ((camera.cullingMask & (1 << screen)) == 0)
                continue;

            camera.cullingMask &= ~(1 << screen);
            EditorUtility.SetDirty(camera);
        }

        var player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            var blind = player.GetComponentInChildren<BlindVisionCamera>(true);
            var serialized = new SerializedObject(blind);
            serialized.FindProperty("hiddenWhenBlind").intValue = 1 << hidden;
            serialized.FindProperty("visibleOnlyWhenBlind").intValue = 1 << screen;
            if (serialized.ApplyModifiedPropertiesWithoutUndo())
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(player);
        }
    }

    private static void SetLayer(Transform root, int layer)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }

    // Katman yoksa ilk boş kullanıcı katmanına eklenir (Tags and Layers).
    private static int EnsureLayer(string name)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing >= 0)
            return existing;

        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        const int firstUserLayer = 8;
        for (int i = firstUserLayer; i < layers.arraySize; i++)
        {
            var slot = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(slot.stringValue))
                continue;

            slot.stringValue = name;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            _log.AppendLine($"katman eklendi: {name} ({i})");
            return i;
        }

        throw new System.InvalidOperationException($"'{name}' için boş katman yok.");
    }

    // ---------- akustik ----------

    // Ses yolu verisi (AcousticSpace): dört oda ve aralarındaki pencereler. Mutfak kapısı AÇIKLIK DEĞİLDİR (kapalı):
    // Kasa ile Mutfak arasında ses ancak İstasyon'dan, iki pencereden dolaşır.
    private static void BuildAcoustics(Transform root, Bounds kitchen, Bounds station, Bounds counter, Bounds hall,
        float kitchenWindowZ, float counterWindowX, float orderX, float deliveryX)
    {
        const int kitchenRoom = 0, stationRoom = 1, counterRoom = 2, hallRoom = 3;
        // Odalar duvar kalınlığının yarısı kadar genişletilir: pencerenin içindeki / duvara yaslı nokta da bir odaya düşsün.
        var grow = new Vector3(WallThickness, 0.5f, WallThickness);
        var space = new GameObject("SesAkustigi").AddComponent<AcousticSpace>();
        space.transform.SetParent(root, false);

        float earY = kitchen.center.y;
        var alongX = new Vector3(WindowHalfWidth, 0f, 0f);
        var alongZ = new Vector3(0f, 0f, WindowHalfWidth);
        space.Configure(
            new[]
            {
                Room("Mutfak", kitchen, grow), Room("Istasyon", station, grow), Room("Kasa", counter, grow), Room("Salon", hall, grow),
            },
            new[]
            {
                Portal("MutfakPenceresi", kitchenRoom, stationRoom, new Vector3(kitchen.max.x + WallThickness * 0.5f, earY, kitchenWindowZ), alongZ),
                Portal("KasaPenceresi", stationRoom, counterRoom, new Vector3(counterWindowX, earY, counter.max.z + WallThickness * 0.5f), alongX),
                Portal("SiparisPenceresi", counterRoom, hallRoom, new Vector3(orderX, earY, counter.min.z - WallThickness * 0.5f), alongX),
                Portal("TeslimatPenceresi", counterRoom, hallRoom, new Vector3(deliveryX, earY, counter.min.z - WallThickness * 0.5f), alongX),
            });
        EditorUtility.SetDirty(space);
    }

    private static AcousticSpace.Room Room(string name, Bounds bounds, Vector3 grow)
    {
        return new AcousticSpace.Room { name = name, center = bounds.center, size = bounds.size + grow };
    }

    private static AcousticSpace.Portal Portal(string name, int roomA, int roomB, Vector3 center, Vector3 halfSpan)
    {
        return new AcousticSpace.Portal { name = name, roomA = roomA, roomB = roomB, center = center, halfSpan = halfSpan };
    }

    private static Opening Window(float center)
    {
        return new Opening { U0 = center - WindowHalfWidth, U1 = center + WindowHalfWidth, Y0 = WindowBottom, Y1 = WindowTop };
    }

    private static List<float> Breaks(float min, float max, IEnumerable<float> candidates)
    {
        var breaks = new List<float> { min, max };
        foreach (float candidate in candidates)
        {
            if (candidate > min + 1e-4f && candidate < max - 1e-4f && !breaks.Exists(b => Mathf.Abs(b - candidate) < 1e-4f))
                breaks.Add(candidate);
        }

        breaks.Sort();
        return breaks;
    }

    private static IEnumerable<float> OpeningEdges(Wall wall)
    {
        foreach (var opening in wall.Openings)
        {
            yield return opening.U0;
            yield return opening.U1;
        }
    }

    private static bool InsideOpening(Wall wall, float u, float y)
    {
        foreach (var opening in wall.Openings)
        {
            if (u > opening.U0 && u < opening.U1 && y > opening.Y0 && y < opening.Y1)
                return true;
        }

        return false;
    }

    // Bir yüz: uzunluk ve yükseklik, açıklık kenarları ile kaplama bantlarından bölünür; açıklığa düşen hücre atlanır.
    private static void EmitFace(MeshBuilder mesh, Wall wall, bool low)
    {
        var segments = low ? wall.LowFace : wall.HighFace;
        float slab = low ? wall.SlabMin : wall.SlabMax;
        var normal = (wall.AlongX ? Vector3.forward : Vector3.right) * (low ? -1f : 1f);

        var edges = new List<float>(OpeningEdges(wall));
        foreach (var (uEnd, _) in segments)
            edges.Add(uEnd);
        var uBreaks = Breaks(wall.UMin, wall.UMax, edges);

        for (int i = 0; i < uBreaks.Count - 1; i++)
        {
            float u0 = uBreaks[i], u1 = uBreaks[i + 1], uMid = (u0 + u1) * 0.5f;
            Finish finish = null;
            foreach (var (uEnd, candidate) in segments)
            {
                if (uMid < uEnd)
                {
                    finish = candidate;
                    break;
                }
            }

            if (finish == null)
                continue;

            var heights = new List<float>();
            foreach (var opening in wall.Openings)
            {
                heights.Add(opening.Y0);
                heights.Add(opening.Y1);
            }

            foreach (var (top, _) in finish.Bands)
                heights.Add(top);
            var yBreaks = Breaks(0f, CeilingHeight, heights);

            for (int j = 0; j < yBreaks.Count - 1; j++)
            {
                float y0 = yBreaks[j], y1 = yBreaks[j + 1], yMid = (y0 + y1) * 0.5f;
                if (InsideOpening(wall, uMid, yMid))
                    continue;

                var material = finish.Bands[finish.Bands.Length - 1].material;
                foreach (var (top, candidate) in finish.Bands)
                {
                    if (yMid < top)
                    {
                        material = candidate;
                        break;
                    }
                }

                mesh.Quad(material, wall.Point(u0, y0, slab), wall.Point(u0, y1, slab), wall.Point(u1, y1, slab), wall.Point(u1, y0, slab), normal);
            }
        }
    }

    // Açıklığın iç yüzleri (pencere kasası ve sövesi bunları örter; arada boşluk görünmesin diye çizilir).
    private static void EmitReveals(MeshBuilder mesh, Wall wall, Material material)
    {
        var along = wall.AlongX ? Vector3.right : Vector3.forward;
        foreach (var opening in wall.Openings)
        {
            if (opening.Y0 > 0f)
            {
                mesh.Quad(material, wall.Point(opening.U0, opening.Y0, wall.SlabMin), wall.Point(opening.U0, opening.Y0, wall.SlabMax),
                    wall.Point(opening.U1, opening.Y0, wall.SlabMax), wall.Point(opening.U1, opening.Y0, wall.SlabMin), Vector3.up);
            }

            mesh.Quad(material, wall.Point(opening.U0, opening.Y1, wall.SlabMin), wall.Point(opening.U0, opening.Y1, wall.SlabMax),
                wall.Point(opening.U1, opening.Y1, wall.SlabMax), wall.Point(opening.U1, opening.Y1, wall.SlabMin), Vector3.down);
            mesh.Quad(material, wall.Point(opening.U0, opening.Y0, wall.SlabMin), wall.Point(opening.U0, opening.Y1, wall.SlabMin),
                wall.Point(opening.U0, opening.Y1, wall.SlabMax), wall.Point(opening.U0, opening.Y0, wall.SlabMax), along);
            mesh.Quad(material, wall.Point(opening.U1, opening.Y0, wall.SlabMin), wall.Point(opening.U1, opening.Y1, wall.SlabMin),
                wall.Point(opening.U1, opening.Y1, wall.SlabMax), wall.Point(opening.U1, opening.Y0, wall.SlabMax), -along);
        }
    }

    // Duvarın dolu kısımları kutu çarpışmalarıyla kaplanır; açıklıkların altı ve üstü ayrı kutulardır.
    private static void EmitColliders(Transform parent, Wall wall)
    {
        var uBreaks = Breaks(wall.UMin, wall.UMax, OpeningEdges(wall));
        float floorY = parent.position.y;
        for (int i = 0; i < uBreaks.Count - 1; i++)
        {
            float u0 = uBreaks[i], u1 = uBreaks[i + 1], uMid = (u0 + u1) * 0.5f;
            var spans = new List<(float y0, float y1)> { (0f, CeilingHeight) };
            foreach (var opening in wall.Openings)
            {
                if (uMid > opening.U0 && uMid < opening.U1)
                {
                    spans.Clear();
                    if (opening.Y0 > 0f)
                        spans.Add((0f, opening.Y0));
                    spans.Add((opening.Y1, CeilingHeight));
                }
            }

            foreach (var (y0, y1) in spans)
            {
                var min = wall.Point(u0, y0, wall.SlabMin) + Vector3.up * floorY;
                var max = wall.Point(u1, y1, wall.SlabMax) + Vector3.up * floorY;
                Blocker(parent, "Duvar_" + wall.Name, min, max);
            }
        }
    }

    private static void Horizontal(MeshBuilder mesh, Material material, float x0, float x1, float z0, float z1, float y, Vector3 normal)
    {
        mesh.Quad(material, new Vector3(x0, y, z0), new Vector3(x0, y, z1), new Vector3(x1, y, z1), new Vector3(x1, y, z0), normal);
    }

    private static void Blocker(Transform parent, string name, Vector3 min, Vector3 max)
    {
        var blocker = new GameObject(name);
        blocker.transform.SetParent(parent, false);
        blocker.transform.position = (min + max) * 0.5f;
        blocker.AddComponent<BoxCollider>().size = max - min;
    }

    // ---------- işlev: mutfak ----------

    private static void MoveKitchenFunction(Transform layout, Transform station)
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
        var binBounds = LocalBounds(KitchenModelFolder, "SM_IngredientBin");
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
        PlaceOnAsset("Buzdolabi_Et", Asset(layout, "SM_ReachInFridge_19"), LocalBounds(KitchenModelFolder, "SM_ReachInFridge"));

        // Çöp kovaları: ikisi de çalışır (ikincisi ilkinin kopyası).
        var binLocal = LocalBounds(KitchenModelFolder, "SM_TrashBin");
        PlaceOnAsset("Cop_Mutfak", Asset(layout, "SM_TrashBin_22"), binLocal);
        if (GameObject.Find("Cop_Mutfak_2") == null)
        {
            var first = Find("Cop_Mutfak");
            var second = Object.Instantiate(first.gameObject);
            second.name = "Cop_Mutfak_2";
            RefreshNetworkHash(second.GetComponent<NetworkObject>());
        }

        PlaceOnAsset("Cop_Mutfak_2", Asset(layout, "SM_TrashBin_21"), binLocal);

        // Pencere yuvaları: pervazdaki hamburger soketleri (pencerenin güncel modeli İstasyon paketinde).
        PlaceWindowSlots("MutfakPencere_Yuva_", Asset(station, "SM_StationWindow_Kitchen"), "Socket_Burger_", 0.42f);

        // Doğma noktası: fritöz hattı ile yarımada arasındaki koridor.
        var island = WorldBounds(Asset(layout, "SM_AssemblyIsland_08"));
        var fryer = WorldBounds(Asset(layout, "SM_Fryer_03"));
        var spawn = Find("Dogma_Sef");
        if (spawn != null)
            spawn.SetPositionAndRotation(new Vector3(island.center.x, KitchenOrigin.y, (fryer.max.z + island.min.z) * 0.5f), Quaternion.identity);

        var sound = Find("Ses_Mutfak");
        if (sound != null)
            sound.position = new Vector3(island.center.x, sound.position.y, island.center.z);

        var panelModel = Asset(layout, "SM_XPanel_30");
        InstallErrorPanel("HataPaneli_Mutfak", panelModel.position, panelModel.rotation);
        // Yerleşimdeki süs kopyası kapatılır: aynı yerde işlevli panel durur.
        panelModel.gameObject.SetActive(false);
    }

    // ---------- işlev: İstasyon ----------

    private static void MoveStationFunction(Transform layout)
    {
        // Paketleme alanları: tepsilerin üstü. Paketin fotoğraflı yüzü (−Z) tezgahın önüne, Komi'ye bakar.
        for (int i = 1; i <= 2; i++)
        {
            var tray = Asset(layout, $"SM_PackingTray_{i}");
            var area = Find($"PaketlemeAlani_{i}");
            if (area == null)
                continue;

            var socket = tray.Find("Socket_Pack");
            area.SetPositionAndRotation(socket != null ? socket.position : tray.position, tray.rotation * Quaternion.Euler(0f, 180f, 0f));
            if (area.TryGetComponent<BoxCollider>(out var collider))
            {
                var trayLocal = LocalBounds(HaritaBuilder.StationModelFolder, "SM_PackingTray");
                collider.size = new Vector3(trayLocal.size.x, collider.size.y, trayLocal.size.z);
                collider.center = new Vector3(0f, collider.size.y * 0.5f, 0f);
            }
        }

        // Kese kağıdı kabı: kese kağıdı destesi.
        var bags = Find("Kap_KeseKagidi");
        if (bags != null)
        {
            PlaceOnAsset("Kap_KeseKagidi", Asset(layout, "SM_BagStack"), LocalBounds(HaritaBuilder.StationModelFolder, "SM_BagStack"));
            HideChild(bags, "Gorsel");
        }

        PlaceOnAsset("Cop_Istasyon", Asset(layout, "SM_TrashBin"), LocalBounds(HaritaBuilder.StationModelFolder, "SM_TrashBin"));

        // Kasa penceresi yuvaları (iki yönlü: Komi koyar, Kasiyer alır).
        var window = Asset(layout, "SM_StationWindow_Kasa");
        PlaceWindowSlots("KasaPencere_Yuva_", window, "Socket_Slot_", 0.6f);

        // Malzeme panosu: pencerenin yanındaki duvar parçası (paketteki pano modelinin yeri). Pano oraya sığacak
        // kadar küçültülür; kökün +Z'si duvarın içine bakar.
        var boardModel = layout.Find("SM_IngredientBoard");
        var board = Find("MalzemePanosu_Istasyon");
        if (board != null && boardModel != null)
        {
            const float boardWidth = 1.7f;      // MalzemePanosu prefab'ının gövde eni
            const float wallGap = 0.03f;
            float windowEdge = WorldBounds(window).min.x;
            float wallFace = KitchenOrigin.x + WallThickness;
            float available = windowEdge - wallFace - wallGap * 2f;
            board.SetPositionAndRotation(
                new Vector3((windowEdge + wallFace) * 0.5f, boardModel.position.y, boardModel.position.z), Quaternion.Euler(0f, 180f, 0f));
            board.localScale = Vector3.one * Mathf.Min(1f, available / boardWidth);
        }

        // Hata paneli: Kasa penceresinin üstü (Komi sinyalleri bu pencereden izler).
        InstallErrorPanel("HataPaneli_Istasyon",
            new Vector3(window.position.x, PanelHeight(), KitchenOrigin.z), Quaternion.identity);

        var spawn = Find("Dogma_Komi");
        if (spawn != null)
        {
            float centerX = KitchenOrigin.x + WallThickness + StationWidth * 0.5f;
            spawn.SetPositionAndRotation(new Vector3(centerX, KitchenOrigin.y, KitchenOrigin.z + StationLength * 0.5f), Quaternion.Euler(0f, 270f, 0f));
        }
    }

    // ---------- işlev: Kasa ----------

    private static void MoveCounterFunction(Transform layout, Transform station)
    {
        PlaceOnAsset("Cop_Kasa", Asset(layout, "SM_TrashBin"), LocalBounds(HaritaBuilder.CounterModelFolder, "SM_TrashBin"));

        // Tarif kitapçığı: sipariş penceresinin pervazındaki kitap modeli. İnce model tıklanabilsin diye hacim yükseltilir.
        var book = Find("TarifKitapcigi");
        if (book != null)
        {
            var bookBounds = LocalBounds(HaritaBuilder.CounterModelFolder, "SM_RecipeBook");
            const float clickHeight = 0.08f;
            bookBounds.SetMinMax(bookBounds.min, new Vector3(bookBounds.max.x, Mathf.Max(bookBounds.max.y, clickHeight), bookBounds.max.z));
            PlaceOnAsset("TarifKitapcigi", Asset(layout, "SM_RecipeBook"), bookBounds);
            HideChild(book, "Gorsel");
        }

        // Malzeme panosu: Kasa–İstasyon duvarının Kasa yüzü, pencere ile mutfak kapısı arasında (tam boy).
        float wallZ = KitchenOrigin.z - WallThickness;
        var boardModel = layout.Find("SM_IngredientBoard");
        var board = Find("MalzemePanosu_Kasa");
        if (board != null && boardModel != null)
        {
            const float boardHalfWidth = 0.85f;
            const float gap = 0.08f;
            float windowEdge = WorldBounds(Asset(station, "SM_StationWindow_Kasa")).min.x;
            float x = Mathf.Min(boardModel.position.x, windowEdge - gap - boardHalfWidth);
            board.SetPositionAndRotation(new Vector3(x, boardModel.position.y, wallZ), Quaternion.identity);
            board.localScale = Vector3.one;
        }

        // Hata paneli: müşteri duvarında, iki pencerenin arası (Kasiyer müşterilere bakarken görür).
        float customerWallZ = wallZ - CounterLength;
        float betweenWindows = (Asset(layout, "SM_StationWindow_Order").position.x + Asset(layout, "SM_StationWindow_Delivery").position.x) * 0.5f;
        InstallErrorPanel("HataPaneli_Kasa", new Vector3(betweenWindows, PanelHeight(), customerWallZ), Quaternion.identity);

        var spawn = Find("Dogma_Kasiyer");
        if (spawn != null)
        {
            // Tezgahın önü, müşteri duvarına dönük.
            var counterBounds = WorldBounds(Asset(layout, "SM_DrinkIceCounter"));
            spawn.SetPositionAndRotation(
                new Vector3(counterBounds.max.x + 0.6f, KitchenOrigin.y, (counterBounds.max.z + wallZ) * 0.5f), Quaternion.Euler(0f, 180f, 0f));
        }

        var sound = Find("Ses_Kasa");
        if (sound != null)
        {
            var delivery = Asset(layout, "SM_StationWindow_Delivery").position;
            sound.position = new Vector3(delivery.x, sound.position.y, delivery.z);
        }
    }

    // Müşteri noktaları (salon; müşteriler pencereye, +Z'ye bakar). Sipariş penceresinin önünde arka arkaya sıra,
    // teslimat penceresinin önünde yuvaların hizasında yan yana. Giriş kapısı sipariş penceresinin, çıkış kapısı
    // teslimat penceresinin tam karşısındadır: müşteri girişten doğar, çıkıştan ayrılır.
    private static void PlaceCustomers(Transform counter, Transform hall, Transform street)
    {
        const float standOff = 0.55f;       // pervazın dış ucundan müşterinin merkezine
        const float queueSpacing = 0.8f;
        float floorY = KitchenOrigin.y;
        var facing = Quaternion.identity;

        var order = Asset(counter, "SM_StationWindow_Order");
        float frontZ = WorldBounds(order).min.z - standOff;
        for (int i = 1; i <= 3; i++)
        {
            var spot = Find($"SiparisYeri_{i}");
            if (spot != null)
                spot.SetPositionAndRotation(new Vector3(order.position.x, floorY, frontZ - (i - 1) * queueSpacing), facing);
        }

        var delivery = Asset(counter, "SM_StationWindow_Delivery");
        for (int i = 1; i <= 3; i++)
        {
            var spot = Find($"TeslimYeri_{i}");
            var socket = delivery.Find($"Socket_Slot_{i}");
            if (spot != null && socket != null)
                spot.SetPositionAndRotation(new Vector3(socket.position.x, floorY, frontZ), facing);
        }

        // Müşteri yakın kaldırımda doğar, giriş kapısının önüne yürüyüp içeri girer; ayrılırken çıkış kapısından
        // kaldırıma çıkar ve kaldırım boyunca yürüyüp kaybolur (noktalar sokak paketinden).
        var streetRoot = street.root;
        var entranceDoor = Asset(hall, "SM_StorefrontDoor_1").position;
        var exitDoor = Asset(hall, "SM_StorefrontDoor_2").position;
        float sidewalkZ = streetRoot.TransformPoint(StreetSpawn).z;
        var entrance = Find("Giris");
        if (entrance != null)
            entrance.SetPositionAndRotation(streetRoot.TransformPoint(StreetSpawn), Quaternion.Euler(0f, 270f, 0f));

        var director = Object.FindAnyObjectByType<CustomerDirector>();
        var entranceLane = EnsurePoint(director.transform, "GirisKapisi");
        entranceLane.SetPositionAndRotation(new Vector3(entranceDoor.x, floorY, sidewalkZ), facing);
        var exitLaneOutside = EnsurePoint(director.transform, "CikisKapisi");
        exitLaneOutside.SetPositionAndRotation(new Vector3(exitDoor.x, floorY, sidewalkZ), Quaternion.Euler(0f, 180f, 0f));
        var exit = EnsurePoint(director.transform, "Cikis");
        exit.SetPositionAndRotation(streetRoot.TransformPoint(StreetDespawn), Quaternion.Euler(0f, 270f, 0f));

        // Sipariş → teslimat yürüyüşü sıranın arkasından dolaşır; ayrılan müşteri önce çıkış kapısının hizasına gelir
        // (salonun ortasındaki akvaryumun içinden geçmesin).
        float aisleZ = frontZ - queueSpacing * 1.5f;
        var corner = Find("RotaKosesi");
        if (corner != null)
            corner.SetPositionAndRotation(new Vector3((order.position.x + delivery.position.x) * 0.5f, floorY, aisleZ), facing);

        var leaveLane = EnsurePoint(director.transform, "CikisYolu");
        leaveLane.SetPositionAndRotation(new Vector3(exitDoor.x, floorY, aisleZ), facing);

        var serialized = new SerializedObject(director);
        serialized.FindProperty("exitPoint").objectReferenceValue = exit;
        var lane = serialized.FindProperty("leaveWaypoints");
        lane.arraySize = 2;
        lane.GetArrayElementAtIndex(0).objectReferenceValue = leaveLane;
        lane.GetArrayElementAtIndex(1).objectReferenceValue = exitLaneOutside;
        var arrive = serialized.FindProperty("arriveWaypoints");
        arrive.arraySize = 1;
        arrive.GetArrayElementAtIndex(0).objectReferenceValue = entranceLane;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform EnsurePoint(Transform parent, string name)
    {
        var point = parent.Find(name);
        if (point == null)
        {
            point = new GameObject(name).transform;
            point.SetParent(parent, false);
        }

        return point;
    }

    // Vitrinin önündeki saksılar: salon paketindeki çalının kopyaları (sokak paketi yalnızca yerlerini verir).
    private const string StreetExtrasName = "Sokak_Ekler";

    private static void InstallStreetExtras(Transform hall, Transform street)
    {
        var old = GameObject.Find(StreetExtrasName);
        if (old != null)
            Object.DestroyImmediate(old);

        var extras = new GameObject(StreetExtrasName).transform;
        var bush = Asset(hall, "SM_PlantBush");
        for (int i = 0; i < StreetPlanters.Length; i++)
        {
            var copy = Object.Instantiate(bush.gameObject, extras);
            copy.name = $"SM_PlantBush_Sokak_{i + 1}";
            copy.transform.SetPositionAndRotation(street.root.TransformPoint(StreetPlanters[i]), Quaternion.identity);
            // Sokak fondur: çizgi ve gölge yok (paket kuralı).
            uint lineLayers = RenderingLayerMask.GetMask("Outline", "Outline Silhouette");
            foreach (var renderer in copy.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.renderingLayerMask &= ~lineLayers;
            }
        }
    }

    // Sokağın hareketi (paket manifest'i → bos_nodelar; salon köküne göre Unity ekseninde): iki araç şeridi, karşı
    // kaldırımda iki figüran hattı. Lambaların ışığı: lamba başına gölgesiz bir nokta ışık.
    private static readonly (string name, Vector3 start, Vector3 end, bool cars)[] StreetLanes =
    {
        ("Arac_Yakin", new Vector3(25f, -0.15f, -7.45f), new Vector3(-38f, -0.15f, -7.45f), true),
        ("Arac_Uzak", new Vector3(-38f, -0.15f, -10.95f), new Vector3(25f, -0.15f, -10.95f), true),
        ("Figuran_Dogu", new Vector3(25f, 0f, -13.6f), new Vector3(-38f, 0f, -13.6f), false),
        ("Figuran_Bati", new Vector3(-38f, 0f, -14.6f), new Vector3(25f, 0f, -14.6f), false),
    };

    private static readonly Vector2 CarInterval = new(5f, 13f);
    private static readonly Vector2 CarSpeed = new(6f, 8.5f);
    private static readonly Vector2 PedestrianInterval = new(7f, 18f);
    private static readonly Vector2 PedestrianSpeed = new(1.1f, 1.5f);
    private static readonly Color32 LampColor = new(0xFF, 0xE7, 0xC2, 0xFF);
    private const float LampIntensity = 0.6f;
    private const float LampRange = 6f;
    private const float LampHeadDrop = 0.35f;   // ışık, lambanın tepesinden bu kadar aşağıda (kürenin içi)

    private static void InstallStreetLife(Transform street)
    {
        var extras = GameObject.Find(StreetExtrasName).transform;
        var streetRoot = street.root;

        var traffic = new GameObject("Sokak_Trafik").AddComponent<StreetTraffic>();
        traffic.transform.SetParent(extras, false);
        var cars = new List<Object>();
        foreach (string prefabName in new[] { "Arac_Sedan", "Arac_Pikap", "Arac_Kamyonet" })
            cars.Add(AssetDatabase.LoadAssetAtPath<StreetTraveller>(HaritaBuilder.StreetActorPath(prefabName)));
        var pedestrians = new List<Object> { AssetDatabase.LoadAssetAtPath<StreetTraveller>(HaritaBuilder.StreetActorPath("Figuran")) };

        var serialized = new SerializedObject(traffic);
        var lanes = serialized.FindProperty("lanes");
        lanes.arraySize = StreetLanes.Length;
        for (int i = 0; i < StreetLanes.Length; i++)
        {
            var (name, start, end, isCar) = StreetLanes[i];
            var startPoint = new GameObject(name + "_Dogma").transform;
            var endPoint = new GameObject(name + "_Kaybolma").transform;
            startPoint.SetParent(traffic.transform, false);
            endPoint.SetParent(traffic.transform, false);
            startPoint.position = streetRoot.TransformPoint(start);
            endPoint.position = streetRoot.TransformPoint(end);

            var lane = lanes.GetArrayElementAtIndex(i);
            lane.FindPropertyRelative("name").stringValue = name;
            lane.FindPropertyRelative("start").objectReferenceValue = startPoint;
            lane.FindPropertyRelative("end").objectReferenceValue = endPoint;
            lane.FindPropertyRelative("interval").vector2Value = isCar ? CarInterval : PedestrianInterval;
            lane.FindPropertyRelative("speed").vector2Value = isCar ? CarSpeed : PedestrianSpeed;
            var prefabs = lane.FindPropertyRelative("prefabs");
            var source = isCar ? cars : pedestrians;
            prefabs.arraySize = source.Count;
            for (int k = 0; k < source.Count; k++)
                prefabs.GetArrayElementAtIndex(k).objectReferenceValue = source[k];
        }

        // Figüranlar yavaştır: aralarındaki en az mesafe araçlarınkinden kısa olmalı ki hat tıkanmasın; tek değer
        // araçlara göre seçildi, figüran aralığı zaten uzun.
        serialized.ApplyModifiedPropertiesWithoutUndo();

        var lights = new GameObject("Sokak_Isiklar").transform;
        lights.SetParent(extras, false);
        foreach (Transform asset in street)
        {
            if (!asset.name.StartsWith("SM_StreetLamp"))
                continue;

            var bounds = WorldBounds(asset);
            var lamp = new GameObject("Isik_" + asset.name).AddComponent<Light>();
            lamp.transform.SetParent(lights, false);
            lamp.transform.position = new Vector3(bounds.center.x, bounds.max.y - LampHeadDrop, bounds.center.z);
            lamp.type = LightType.Point;
            lamp.color = LampColor;
            lamp.intensity = LampIntensity;
            lamp.range = LampRange;
            lamp.shadows = LightShadows.None;
        }
    }

    // Gökyüzü: sahne kamerası ve oyuncu kamerası düz renk arka plan çizer (skybox yerine).
    private static void ApplySky()
    {
        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            SetSky(camera);

        var player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            bool changed = false;
            foreach (var camera in player.GetComponentsInChildren<Camera>(true))
                changed |= SetSky(camera);
            if (changed)
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(player);
        }
    }

    private static bool SetSky(Camera camera)
    {
        if (camera.clearFlags == CameraClearFlags.SolidColor && (Color32)camera.backgroundColor is var c
            && c.r == SkyColor.r && c.g == SkyColor.g && c.b == SkyColor.b)
            return false;

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = SkyColor;
        EditorUtility.SetDirty(camera);
        return true;
    }

    // Vitrin kapıları açık durur (müşteriler içinden yürür; kapı animasyonu yok): giriş salona doğru, çıkış sokağa
    // doğru açılır. Kanatların pivotu menteşe eksenidir.
    private static void OpenDoors(Transform hall)
    {
        SetLeaves(Asset(hall, "SM_StorefrontDoor_1"), inward: true);
        SetLeaves(Asset(hall, "SM_StorefrontDoor_2"), inward: false);
    }

    // Akvaryum: balıkları yüzdüren bileşen (yalnızca görsel, yerel).
    private static void InstallAquarium(Transform hall)
    {
        var aquarium = Asset(hall, "SM_Aquarium");
        if (!aquarium.TryGetComponent<AquariumFish>(out var fish))
            fish = aquarium.gameObject.AddComponent<AquariumFish>();

        var serialized = new SerializedObject(fish);
        serialized.FindProperty("water").objectReferenceValue = aquarium.Find("Water").GetComponent<Renderer>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // GİRİŞ / ÇIKIŞ tabelaları: paketteki yüz dokusunda yazı basılı (dile göre değişemez). Yüz düz zemine çevrilir,
    // yazı tabelanın önüne dünya uzayında bir Canvas olarak konur ve Localization tablosundan gelir.
    private const string StringTable = "UIStrings";
    // Arayüz kitinin (Assets/CNE_UI) tabela yazı tipi, ok görseli ve palet renkleri: tabelalar arayüzle aynı dili konuşur.
    private const string SignFontPath = "Assets/CNE_UI/Fonts/Righteous-Regular.ttf";
    private const string SignArrowPath = "Assets/CNE_UI/Art/Kit/ui_arrow.png";
    private static readonly Color32 SignEntranceColor = new(0x74, 0xBF, 0x4A, 0xFF);   // marul yeşili (devam / giriş)
    private static readonly Color32 SignExitColor = new(0xF2, 0x8C, 0x1E, 0xFF);       // peynir turuncusu (ayrıl / çıkış)
    private const string SignTextName = "Yazi";
    private const float SignCanvasScale = 0.001f;

    private static void InstallDoorSigns(Transform hall)
    {
        InstallDoorSign(Asset(hall, "SM_DoorSign_Giris"), "MI_Decal_Giris", "sign.entrance", "GİRİŞ", SignEntranceColor);
        InstallDoorSign(Asset(hall, "SM_DoorSign_Cikis"), "MI_Decal_Cikis", "sign.exit", "ÇIKIŞ", SignExitColor);
    }

    private static void InstallDoorSign(Transform sign, string faceMaterialName, string key, string turkish, Color color)
    {
        EnsureString(key, turkish);

        var renderer = sign.GetComponent<Renderer>();
        var blank = AssetDatabase.LoadAssetAtPath<Material>(HaritaBuilder.SignFaceMaterialPath);
        var shared = renderer.sharedMaterials;
        for (int i = 0; i < shared.Length; i++)
        {
            if (shared[i] != null && shared[i].name == faceMaterialName)
                shared[i] = blank;
        }

        renderer.sharedMaterials = shared;

        var old = sign.Find(SignTextName);
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        // Tabelanın önü +Z; Canvas ön yüzün hemen önünde, öne bakar.
        var bounds = renderer.localBounds;
        var canvasObject = new GameObject(SignTextName, typeof(RectTransform), typeof(Canvas));
        var rect = (RectTransform)canvasObject.transform;
        rect.SetParent(sign, false);
        rect.localPosition = new Vector3(bounds.center.x, bounds.center.y, bounds.max.z + 0.003f);
        rect.localRotation = Quaternion.Euler(0f, 180f, 0f);
        rect.localScale = Vector3.one * SignCanvasScale;
        rect.sizeDelta = new Vector2(bounds.size.x, bounds.size.y) / SignCanvasScale;
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

        float height = rect.sizeDelta.y;
        // Ok ve yazı birlikte ortalanır: ok kare, yazı kalan genişlikte.
        var arrowObject = new GameObject("Ok", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var arrowRect = (RectTransform)arrowObject.transform;
        arrowRect.SetParent(rect, false);
        arrowRect.anchorMin = arrowRect.anchorMax = new Vector2(0.17f, 0.5f);
        arrowRect.sizeDelta = Vector2.one * (height * 0.46f);
        var arrowImage = arrowObject.GetComponent<Image>();
        arrowImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SignArrowPath);
        arrowImage.color = color;
        arrowImage.preserveAspect = true;
        arrowImage.raycastTarget = false;

        var label = SignText(rect, "Metin", turkish, AssetDatabase.LoadAssetAtPath<Font>(SignFontPath), color, height * 0.58f);
        label.rectTransform.anchorMin = new Vector2(0.28f, 0f);
        label.rectTransform.anchorMax = new Vector2(0.96f, 1f);
        label.resizeTextForBestFit = true;
        label.resizeTextMaxSize = Mathf.RoundToInt(height * 0.58f);
        label.resizeTextMinSize = 20;

        var localize = label.gameObject.AddComponent<LocalizeStringEvent>();
        localize.StringReference.SetReference(StringTable, key);
        var setter = (UnityEngine.Events.UnityAction<string>)System.Delegate.CreateDelegate(
            typeof(UnityEngine.Events.UnityAction<string>), label, "set_text");
        UnityEventTools.AddPersistentListener(localize.OnUpdateString, setter);
    }

    private static Text SignText(RectTransform parent, string name, string content, Font font, Color color, float fontSize)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        var rect = (RectTransform)textObject.transform;
        rect.SetParent(parent, false);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var text = textObject.GetComponent<Text>();
        text.text = content;
        text.font = font;
        text.color = color;
        text.fontSize = Mathf.RoundToInt(fontSize);
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    // Anahtar tabloda yoksa eklenir; Türkçe tabloda değeri yoksa yazılır. Başka dil tablosu eklendiğinde çevirisi
    // oraya girilir, tabela kendiliğinden o dilde görünür.
    private static void EnsureString(string key, string turkish)
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection(StringTable);
        if (collection == null)
        {
            _log.AppendLine($"UYARI: '{StringTable}' tablosu bulunamadı; '{key}' eklenemedi.");
            return;
        }

        if (!collection.SharedData.Contains(key))
        {
            collection.SharedData.AddKey(key);
            EditorUtility.SetDirty(collection.SharedData);
        }

        foreach (var table in collection.StringTables)
        {
            if (table.LocaleIdentifier.Code != "tr" || table.GetEntry(key) != null)
                continue;

            table.AddEntry(key, turkish);
            EditorUtility.SetDirty(table);
        }
    }

    private static void SetLeaves(Transform door, bool inward)
    {
        // Paket: giriş Leaf_L Y −90°, Leaf_R Y +90° (plan ekseni); glTFast Y dönüşlerinin işaretini çevirir.
        float sign = inward ? 1f : -1f;
        var left = door.Find("Leaf_L");
        var right = door.Find("Leaf_R");
        if (left != null)
            left.localRotation = Quaternion.Euler(0f, 90f * sign, 0f);
        if (right != null)
            right.localRotation = Quaternion.Euler(0f, -90f * sign, 0f);
    }

    // ---------- yerleştirme yardımcıları ----------

    private static float PanelHeight()
    {
        // Mutfaktaki panelle aynı yükseklik (kapının üstü).
        var kitchenPanel = GameObject.Find("HataPaneli_Mutfak");
        return kitchenPanel != null ? kitchenPanel.transform.position.y : KitchenOrigin.y + 2.41f;
    }

    private static void PlaceSlot(string slotName, Vector3 position, Quaternion rotation)
    {
        var slot = Find(slotName);
        if (slot != null)
            slot.SetPositionAndRotation(position, rotation);
    }

    // Pervazdaki yuvalar: yuva soketin üstüne oturur; tıklanan hacim duvarı boydan boya geçer ki pencerenin iki
    // tarafından da erişilsin.
    private static void PlaceWindowSlots(string slotPrefix, Transform window, string socketPrefix, float slotWidth)
    {
        var windowBounds = WorldBounds(window);
        // Pencere duvarın içinden geçtiği eksen: pencerenin kendi +Z'si.
        bool throughX = Mathf.Abs(window.forward.x) > Mathf.Abs(window.forward.z);
        for (int i = 1; i <= 3; i++)
        {
            var socket = window.Find($"{socketPrefix}{i}");
            var slot = Find($"{slotPrefix}{i}");
            if (socket == null || slot == null)
                continue;

            slot.SetPositionAndRotation(socket.position, Quaternion.identity);
            if (!slot.TryGetComponent<BoxCollider>(out var collider))
                continue;

            float height = collider.size.y;
            if (throughX)
            {
                collider.center = new Vector3(windowBounds.center.x - socket.position.x, height * 0.5f, 0f);
                collider.size = new Vector3(windowBounds.size.x, height, slotWidth);
            }
            else
            {
                collider.center = new Vector3(0f, height * 0.5f, windowBounds.center.z - socket.position.z);
                collider.size = new Vector3(slotWidth, height, windowBounds.size.z);
            }
        }
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

    private static void HideChild(Transform root, string childName)
    {
        var child = root.Find(childName);
        if (child != null)
            child.gameObject.SetActive(false);
    }

    // ---------- hata paneli ----------

    private static void InstallErrorPanel(string panelName, Vector3 position, Quaternion rotation)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
        if (prefab == null)
            prefab = BuildPanelPrefab();

        var old = GameObject.Find(panelName);
        if (old != null)
            Object.DestroyImmediate(old);

        var panel = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        panel.name = panelName;
        panel.transform.SetPositionAndRotation(position, rotation);
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
            foreach (Transform child in model.transform)
            {
                if (!child.name.Contains(".X_"))
                    continue;

                slots.Add(child);
                child.GetComponent<Renderer>().sharedMaterial = litMaterial;
            }

            // Bakana göre soldan sağa: panelin önü +Z olduğu için bakanın solu +X'tir.
            slots.Sort((a, b) => b.localPosition.x.CompareTo(a.localPosition.x));
            float faceZ = model.GetComponent<Renderer>().localBounds.max.z;

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
            throw new System.InvalidOperationException($"Yerleşimde '{name}' yok ({layout.name}).");

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

    // Modelin kendi eksenindeki sınırı (model dosyası orijinde ve dönüşsüz durur).
    private static Bounds LocalBounds(string modelFolder, string assetName)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{modelFolder}/{assetName}.glb");
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
