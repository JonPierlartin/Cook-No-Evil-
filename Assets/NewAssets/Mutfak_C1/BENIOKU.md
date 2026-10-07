# Cook No Evil! · C1 mutfak modelleri

*7 Ekim 2026 · 34 asset + C1 yerleşim sahnesi · kapsam asset listesiyle aynı (item'lar ve mimari hariç)*

## Paket

| Klasör / dosya | İçerik |
|---|---|
| `glb/` | Her asset ayrı `.glb`: hiyerarşi, pivot, hareketli parçalar, soketler, materyaller, gömülü dokular. `C1_Mutfak_Yerlesim.glb`: tüm asset'ler C1 planındaki yerlerinde + 5,60 × 6,00 m referans zemin. |
| `obj/` | Aynı geometri `.obj` + `CNE_Materyaller.mtl` + dokular. Statik: pivot ve hiyerarşi yok, hareketli parçalar kapalı konumda ayrı nesne. |
| `textures/` | Palet (256 px, 16 px hücre), meşe damar dokusu, 3 decal. |
| `CNE_C1_Blender_Kurulum.py` | Blender'da tüm asset'leri canlı Bevel + Weighted Normal modifier'larıyla kurar, istenirse her asset'i FBX yazar. |
| `onizleme/` | Bu dosyalardan toon önizleme render'ları: asset sayfası, yerleşim, hareketli parça ve soket testi (pembe işaretler yalnız testte). |

## Hangi dosyayı kullanmalı

1. **Blender → FBX (Unity için önerilen).** Paketi bir klasöre açın, Blender'da (3.6 LTS, 4.x veya 5.x) Scripting → Open → `CNE_C1_Blender_Kurulum.py` → Run Script. `CNE_C1_Mutfak` koleksiyonu kurulur: asset'ler bir sayfada, C1 yerleşimi 12 m sağda (koleksiyon örnekleri; asset'i düzenleyince yerleşim de değişir). FBX için scriptin başındaki `EXPORT_FBX_DIR`'e klasör yazıp tekrar çalıştırın; kurulum varsa atlanır, yalnızca her asset ayrı FBX olarak yazılır (−Z Forward, Y Up, FBX All, Apply Transform, modifier'lar uygulanmış).
2. **GLB doğrudan Unity'ye.** glTFast paketiyle (`com.unity.cloud.gltfast`) `glb/` klasörünü içe alın. Hiyerarşi ve pivotlar aynen gelir; materyalleri projenin toon materyalleriyle değiştirin.
3. **OBJ** hızlı bakış ve başka araçlar için.

Unity FBX ayarı: Scale Factor 1, Convert Units açık, Normals: Import (bevel normalleri dosyadan gelsin), Read/Write kapalı.

## Eksen, birim, pivot

- Birim metre. Y yukarı, asset'in önü +Z (Unity'de de +Z, Blender'da −Y), X genişlik.
- Pivotlar asset listesindeki gibi: zemindekiler taban arka kenar ortası, duvara monteler arka yüz, kapı kanadı menteşe ekseni.
- Unity içe alırken X eksenini ters çevirir (sağ elden sol ele); dosyadaki X değerleri Unity'de işaret değiştirir, şekil aynı kalır.

## Bevel ve outline: modellere nasıl uygulandı

Bevel testindeki kural: **çizgi istemediğin yere bevel, istediğin yere kademe.**

| Ne | Bevel | Sonuç |
|---|---|---|
| Gövde köşeleri (ızgara, fritöz, tezgah, ada, buzdolabı, davlumbaz, ısı lambası) | r 2 cm (dar gövdelerde 1,5), 2 segment | Yalnız siluet; köşede iç çizgi yok |
| Tablalar, üst çerçeveler, raflar | r 0,8–1,2 cm, 2 segment | Kenar yumuşar, çizgiyi taşıntı verir |
| Kapaklar, kontrol panelleri, çekmece önü, ada kapakları | r 0,6–0,8 cm + gövdeden 2 cm çıkıntı | İç çizgi derinlik kademesinden; ~4 m'ye kadar kesintisiz |
| Krom kulp | çubuk r 0,5, ayak r 0,3 cm | Krom matcap bevel'da parlar |
| Plastikler (kesme tahtası, ekmek tepsisi, malzeme kabı) | r 0,5–0,9 cm, 4 segment (Yumuşak) | Yumuşak siluet |
| Izgara çubukları, pencere ve kapı söveleri, süpürgelik boşluğu | keskin (r 0) | Bilinçli sert çizgi |
| Torna parçalar (düğme, kova, kubbe, kol) | bevel profilde, 0,3–0,6 cm | Mesh'e pişmiş |
| Decal'lar | düz quad | — |

Blender'da script her kutu parçaya şunu kurar: **Bevel** (Width = tablodaki r, Segments 2 ya da 4, Limit Method Angle 30°, Clamp Overlap, Harden Normals) + **Weighted Normal** (Face Area, Keep Sharp, Weight 50). GLB ve OBJ'de aynı bevel geometriye pişmiştir: düz yüzün normali düz, bevel'ın normali yumuşak geçişli.

## Materyaller ve doku ayarları (Unity)

| Materyal | Nerede | Ayar |
|---|---|---|
| MI_Palette | Opak düz renkler | Palet: Filter Point, Compression None, sRGB, Mip Maps kapalı |
| MI_Palette_Steel | Paslanmaz | Toon parıltı yalnız bevel'da |
| MI_Palette_Chrome | Kulp, çerçeve, halka | Matcap |
| MI_Palette_Glass | Buzdolabı camı, kapı lumbuzu | %25 opak; Şef konturu için derinliğe yazmalı |
| MI_Palette_Emission | Lensler, ısı lambası, buzdolabı ışığı, LED | HDR yoğunluk 2,5 (bloom eşiği 1,3) |
| MI_Wood_Mese | Ada kapakları, salınım kapısı | Bilinear, Repeat, Mip Maps açık; 1 tekrar = 0,5 m |
| MI_Decal_FireWarning | Yangın levhası | Alpha Is Transparency, alfa kesme 0,5, Clamp |
| MI_Decal_FryerDisplay / FridgeDisplay | Ekranlar | Emission 2,0, Clamp |

Her parçanın UV'si kendi palet hücresinin ortasında. Boş hücreler parlak pembe: yanlış UV hemen görünür.

**Palete eklenen satır (S10).** Renk paletinde karşılığı olmayan üç renk 10. satıra kondu; `renk-paleti.md`'ye eklenmesi önerilir: S10·K0 #D9A35B yağ · S10·K1 #FF8A4A ısı lambası (emission) · S10·K2 #FFE7C2 lamba beyazı (davlumbaz lensi, tavan lambası; emission).

## Hareketli parçalar

Her biri asset'in altında ayrı node; node'un orijini menteşe ya da kızak noktası. Değerler Unity yerel dönüşü. Blender'da: Unity Y ekseni → Blender Z (işaret ters), X aynı, Unity +Z öteleme → Blender −Y.

| Asset | Node | Pivot | Hareket (Unity) |
|---|---|---|---|
| SM_ReachInFridge | Door_L | Sol menteşe | Y +105° dışa açılır |
| | Door_R | Sağ menteşe | Y −105° |
| SM_Fryer | Basket_1, Basket_2 | Sepetin arka üst kenarı (askı) | Yağa indirme: Y −0,12 m · kaldırma: X −40°'ye kadar |
| SM_IngredientBin | Lid | Kapağın arka kenarı | X −100°'ye kadar |
| SM_TrashBin | Lid | Kapağın arka kenarı | X −75° (pedal) |
| SM_StockHatch | Drawer | Çerçeve ön yüzü | Z +0,25 m dışarı kayar |
| | Lever | Kol mafsalı | X +75° öne/aşağı çekilir |
| SM_XPanel | X_1, X_2, X_3 | X'in merkezi | Yanan X için materyal değişimi veya ölçek nabzı |
| SM_SwingDoor | kök node | Menteşe ekseni, taban | Y ±90° (çift yönlü) |

## Soketler

Boş node'lar; adları `Socket_` ile başlar, item ya da ışık buraya oturur. Yönleri asset'le aynı (+Z ön).

| Asset | Soketler | Ne oturur |
|---|---|---|
| SM_Grill | Slot_1, Slot_2 | Köfte: halka merkezi, çubukların üstü |
| SM_Fryer | Vat | Yağ yüzeyinin ortası |
| SM_FryStation | Holding, Cartons | Tutma haznesi tabanı · karton rafı |
| SM_ExhaustHood | Light_1, Light_2 | Lens yüzeyi; spot ışığı buraya koyup aşağı çevirin (X +90°) |
| SM_AssemblyIsland | Board_1, Board_2, BunTray, SauceCaddy | Tabla üstündeki C1 yerleri (90° dönük) |
| SM_SauceCaddy | Bottle_1–4 | Şişe tabanları |
| SM_PrepTable | Bin_1_Marul … Bin_5_Peynir | Malzeme kapları, raydaki sırayla |
| SM_IngredientBin | Content | Malzeme item'ı |
| SM_ReachInFridge | Shelf_0–3 | Taban + 3 raf yüzeyi |
| SM_TrashBin | Drop | Atma noktası |
| SM_StationWindow | Burger_1–3, Side_1–3 | Pervazdaki yuvalar |
| SM_HeatLampRail | Light | Isı lambası ışığı |
| SM_SwingDoorFrame | Hinge_L, Hinge_R | Kanatların pivotu (sağ kanat 180° dönük) |
| SM_StockHatch | PassThrough | Arkadan gelen stok |
| SM_CeilingLight | Light | Point ışık |

FBX'te sabit/hareketli/soket bilgisi custom property olarak da gelir (`cne_outline`, `cne_cast_shadow` kök node'da; `cne_movable`, `cne_socket` ilgili node'larda); GLB'de aynı bilgiler node `extras` alanında. `cne_outline` ve `cne_cast_shadow` asset listesindeki Outline ve Gölge sütunlarıyla aynı.

## Asset listesinden farklar

- Kapaklar ve kontrol panelleri (ızgara, fritöz, patates istasyonu, malzeme alanı) gövdeden 1,2 değil 2 cm çıkıntılı; iç çizgi kademeden gelsin diye. Kulp ve düğme dahil derinlik ~90 cm.
- Krom kulp bevel'ı r 0,8 değil 0,5: çubuk 1,2 cm kalın.
- Yağ, ısı lambası ve lamba beyazı palette S10 satırında (yukarıda).

## Asset'ler

Ölçüler sınırlayıcı kutu, cm, asset'in kendi ekseninde (G × D × Y). Toplam 33.958 üçgen (her asset bir kez); C1 yerleşimi 45.024 üçgen.

| Dosya | Ad | G × D × Y | Üçgen | Hareketli | Soket |
|---|---|---|---|---|---|
| SM_Grill | Izgara | 90 × 90,4 × 125 | 3.096 | — | 2 |
| SM_Fryer | Fritöz | 90 × 90,5 × 134 | 2.274 | Basket_1, Basket_2 | 1 |
| SM_FryStation | Patates kutusu istasyonu | 49 × 83,6 × 136 | 1.308 | — | 2 |
| SM_ExhaustHood | Davlumbaz | 190 × 95 × 85 | 768 | — | 2 |
| SM_AssemblyIsland | Birleştirme yarımadası | 133,2 × 376,6 × 100,1 | 5.088 | — | 4 |
| SM_CuttingBoard | Kesme tahtası | 80 × 60 × 2,6 | 300 | — | — |
| SM_BunTray | Ekmek tepsisi | 60 × 65 × 4 | 1.500 | — | — |
| SM_SauceCaddy | Sos şişesi yuvası | 100 × 22 × 6 | 1.452 | — | 4 |
| SM_PrepTable | Malzeme alanı | 200 × 89,6 × 100,3 | 1.200 | — | 5 |
| SM_IngredientBin | Malzeme kabı + kapak | 29 × 31,2 × 18 | 900 | Lid | 1 |
| SM_ReachInFridge | Buzdolabı (cam kapılı) | 110 × 90,6 × 200 | 2.786 | Door_L, Door_R | 4 |
| SM_WallShelf | Duvar rafı | 190 × 32 × 21 | 324 | — | — |
| SM_TrashBin | Çöp kovası | 60 × 62,5 × 76,4 | 812 | Lid | 1 |
| SM_StationWindow | İstasyon penceresi + pervaz | 262 × 66 × 116 | 2.304 | — | 6 |
| SM_HeatLampRail | Isı lambası + fiş rayı | 240 × 38,5 × 19,8 | 648 | — | 1 |
| SM_SwingDoor | Çift salınım kapısı (kanat) | 73 × 7,7 × 206 | 2.136 | — | — |
| SM_SwingDoorFrame | Çift kapı kasası | 162 × 25 × 216 | 684 | — | 2 |
| SM_StockHatch | Stok kapağı + kaldıraç | 86 × 19 × 51,3 | 1.280 | Drawer, Lever | 1 |
| SM_Intercom | İnterkom | 29 × 4,9 × 24 | 968 | — | — |
| SM_XPanel | X paneli | 110 × 6,2 × 36 | 864 | X_1, X_2, X_3 | — |
| SM_CeilingLight | Tavan lambası | 39,2 × 39,2 × 9,1 | 672 | — | 1 |
| DC_FireWarning | Yangın uyarı levhası | 30 × — × 30 | 2 | — | — |
| DC_FryerDisplay | Fritöz ekranı | 17 × — × 6,4 | 2 | — | — |
| DC_FridgeDisplay | Buzdolabı ısı ekranı | 19 × — × 6,6 | 2 | — | — |
| SM_Knob | Düğme | 5,5 × 3,5 × 5,5 | 320 | — | — |
| SM_BarHandle_12 | Krom çubuk kulp 12 cm | 12 × 2,6 × 2 | 324 | — | — |
| SM_BarHandle_18 | Krom çubuk kulp 18 cm | 18 × 2,6 × 2 | 324 | — | — |
| SM_BarHandle_28 | Krom çubuk kulp 28 cm | 28 × 2,6 × 2 | 324 | — | — |
| SM_BarHandle_29 | Krom çubuk kulp 29 cm | 29 × 2,6 × 2 | 324 | — | — |
| SM_BarHandle_30 | Krom çubuk kulp 30 cm | 30 × 2,6 × 2 | 324 | — | — |
| SM_BarHandle_60 | Krom çubuk kulp 60 cm | 60 × 2,6 × 2 | 324 | — | — |
| SM_CabinetDoor_Oak_67 | Meşe dolap kapağı 67 cm | 67 × 2 × 74 | 108 | — | — |
| SM_CabinetDoor_Oak_70 | Meşe dolap kapağı 70 cm | 70 × 2 × 74 | 108 | — | — |
| SM_CabinetDoor_Oak_112 | Meşe dolap kapağı 112 cm | 112 × 2 × 74 | 108 | — | — |

## Ne test edildi

- GLB: 35 dosya yapısal kontrolden geçti (accessor sınırları, normal birimliği, üçgen sargısı, node ağacı). `onizleme/` render'ları doğrudan bu dosyalardan.
- OBJ: 34 dosya, indeks ve materyal referansları.
- Blender scripti: bu ortamda Blender yok. Script, Blender API'sini taklit eden katı bir test ortamında 3.6, 4.2, 4.5 ve 5.0 sürüm yollarıyla çalıştırıldı: hiyerarşi, pivot, soket, FBX seçimi, ikinci çalıştırma. Gerçek Blender'da hata görürseniz konsoldaki mesajı gönderin.
- Unity'ye içe alma denenmedi.
