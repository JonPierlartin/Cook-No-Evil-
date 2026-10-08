# Cook No Evil! · Sokak (dış mekân) modelleri

*8 Ekim 2026 · 38 asset + 2 yerleşim · OBJ + MTL · kapsam: [sokak-ogeleri.md](sokak-ogeleri.md) v2.1'deki model kalemleri (MM, CK-01…07, UF-01 kartı, SE, AR, FX-02 ve FX-04 kartları); dokular taslak*

## Paket: `CNE_Sokak_Modeller.zip`

| Klasör / dosya | İçerik |
|---|---|
| `obj/` | 38 asset OBJ'si + 2 yerleşim OBJ'si, ortak `CNE_Sokak.mtl` ve 9 doku: palet (önizleme için yeniden kuruldu) ve 8 taslak doku (vitrin içi, tabela, pencere, uzak silüet, figüran atlası ve maskesi, egzoz pufu, oval gölge). OBJ, MTL ve PNG'ler aynı klasörde kalmalı. |
| `manifest.json` | Her asset'in ölçüsü, sınırları, pivotu, nesneleri, materyalleri, soketleri (asset ve Unity ekseninde) ve gölge bayrağı; yerleşimlerdeki her örneğin dönüşümü (plan ve Unity), materyal değişimi ve liste ID'si; referans noktaları; materyal tablosu. |
| `kontrol_raporu.json` | Kontrol betiğinin çıktısı: dosya başına sayımlar, pivot kuralları, kullanılan palet hücreleri, sorunlar (0). |
| `onizleme/` | 7 sayfa, doğrudan bu OBJ'ler geri yüklenerek toon render: araçlar (soketler pembe), cephe kiti, eşyalar ve kartlar, yerleşim kuşbakışı, sokak göz hizası, Kasiyer'in sipariş penceresinden bakış, 11 Ekim yerleşimi. |
| `sokak-modeller.md` | Bu not. |

## Kullanım

1. **Unity:** `obj/` klasörünü olduğu gibi `Assets/` altına kopyalayın. Model içe alma: Scale Factor 1 (birim metre), Normals **Import**, Generate Lightmap UVs kapalı, Read/Write kapalı. Materials sekmesinde *Search and Remap* (Project-Wide) ile projedeki aynı adlı materyallere bağlayın; olmayanları aşağıdaki tabloya göre oluşturun.
2. **Blender:** File → Import → Wavefront (.obj), varsayılan eksenlerle (Forward −Z, Up Y); n-gon'lar korunur.

Unity OBJ'yi içe alırken X eksenini ters çevirir: plan (x; y; z) → Unity (−x; y; z). Y dönüşleri işaret değiştirir. Manifest'teki bütün konumlar iki eksende de var (`unity` alanları).

OBJ boş node, hiyerarşi ya da özel alan taşımaz. Soketler, gölge bayrakları, yerleşim dönüşümleri ve referans noktaları yalnızca `manifest.json`'da; araç prefab'ı ve sahne yerleşimi bu bilgiyle kurulur (en altta).

## Eksen, birim, pivot

- Birim metre. Y yukarı, asset'in önü +Z, X genişlik (salon, Kasa, İstasyon ve C1 paketleriyle aynı). Araçlarda +X aracın solu; Unity'de X ters döndüğü için yine solu.
- Yerleşim ekseni salon planı: X doğu, Z güney, sokak negatif Z'de; kök salonun iç kuzeybatı köşesi, zemin Y 0. Yakın kaldırım ve bordürü Z −0,20…−3,20 (üst Y 0), yol Z −3,20…−12,70 (Y −0,15), karşı kaldırım Z −12,70…−15,70, karşı cephe ön yüzü Z −15,70, ara sokak X 22–28.
- Pivotlar:
  - Serbest duranlar (lamba, posta kutusu, gazete otomatı, ağaçlar, figüran ve silüet kartı): taban ortası.
  - Bank: taban, arka kenar ortası (cepheye yaslı, oturak +Z'de).
  - Cephe modülleri (dükkân cepheleri, dolgu, üst katlar): taban, ön duvar yüzü (z = 0), sol (batı) kenar. Pilaster ve silme +Z'ye taşar, vitrin girintisi −Z'de.
  - Tente: arka üst kenar ortası (`Socket_Awning`). Cephe tabelası: arka yüz ortası (`Socket_Sign`). Bıçak tabela: pilaster yüzü, tabelanın alt kenarı (`Socket_Blade`).
  - Araç gövdeleri: taban ortası (zemin), ön +Z; tekerlekler ayrı. Tekerlek: aks merkezi, aks X, jant yüzü +X.
  - Oval gölge: merkez, yukarı bakan 1 × 1 m kare.
  - `ARCH_Sokak` ve `ARCH_KarsiCephe_Yer` dünya koordinatında (kök = salon kökü).

## Uygulanan kurallar

- **Kontur yok, gerçek zamanlı gölge yok.** Sokak oyuncunun etkileşmediği fon; hareket edenler (araçlar, figüranlar) FX-04 oval gölgeyi taşır. Bütün sokak mesh'lerinde Cast Shadows Off.
- **Renk:** Mimari ve cephe düz renk (`MI_Arch_*`), doku yok; UV'leri sabit (0,5; 0,5). Eşyalar, tenteler, tekerlek ve araç detayları palet UV'si: her yüz tek hücrenin ortasında, boş (pembe) hücreye düşen yok. Kullanılan hücreler: S4·K3, S4·K6, S5·K0, S5·K6, S5·K7, S5·K8, S6·K6, S6·K8, S7·K0, S11·K7, S11·K8, S11·K9, S11·K10. Tabela, vitrin içi, pencere, silüet ve figüran yüzleri kendi dokularında.
- **Varyant = materyal:** Cephe modülleri tek mesh; cephe rengi örnek başına `MI_Arch_Cephe` → `_K0`, `_K4`, `_K5`, `_K6` değişimiyle. Araç gövdesi `MI_CarBody_*` slotuyla (AR-05); park eden kamyonet `MI_CarBody_Gri`.
- **Normaller:** Açıya göre yumuşatma: düz yüzler sert, yuvarlak formlar (lamba, ağaç tacı, araç gövdesi) yumuşak. Her köşe normali kendi yüzüyle aynı yönde.
- **Yüzler:** n-gon'lar düzlemsel ve dışbükey. Düzlemsel olmayan dörtgenler (ağaç tacı, araç gövdesi) dışa aktarımda kısa köşegenden üçgenlendi; içe alan program köşegen seçmez. Mimari ve kit parçalarında T-kavşak yok (çatlak ve kıvılcım olmasın).
- **Çakışma önlemi:** Tentenin alt yüzü 5 mm aşağıda; ara sokak duvarları cephe modüllerinin yan yüzleriyle çakışmasın diye sokağın içinde; bank sırtı taban bandının 1 cm önünde.
- **Sinyal renkleri** (ketçap kırmızısı, hardal sarısı, mayonez beyazı, barbekü kahvesi, sinyal fonu) hiçbir sokak öğesinde yok; arka lambalar lila, kırmızı fren lambası yok.

## Materyaller

| OBJ materyali | Nerede | Proje karşılığı |
|---|---|---|
| MI_Palette | Eşyalar, tenteler, ağaçlar, tabela gövdeleri, tekerlek, araç detayları | Düz palet (CNE/Toon, _BaseMap = ana palet) |
| MI_Palette_Chrome | Tamponlar, ızgaralar, jant göbeği, posta kutusunun ağız kapağı, gazete otomatının kulpu | Krom: matcap |
| MI_CarGlass | Araç camları (S5·K6, opak), ayrı alt mesh | Düz palet ya da proje camı |
| MI_CarBody_Nane, _Bebek, _Lila, _Gri | Araç gövdesi (AR-05) | CNE/Toon düz renk: #7DCECD (S11·K8), #84B9FF (S11·K9), #A58FD8 (S6·K6), #899BBB (S11·K0) |
| MI_Arch_Kaldirim, _Bordur, _Asfalt, _YanDuvar, _Kapi | Kaldırım; bordür, silme, taban bandı, denizlik ve orta çizgi; yol; ara sokak duvarları; dükkân kapıları | CNE/Toon düz renk: #9BAEC9 (S11·K2), #B7CAE8 (S11·K3), #54627A (S11·K1), #587098 (S11·K7), #4A535B (S5·K6) |
| MI_Arch_Cephe | Cephe modüllerindeki renk yuvası | Örnekte `_K0` #899BBB, `_K4` #688AC1, `_K5` #8B8DCA, `_K6` #7AA6AC ile değişir |
| MI_Ext_ShopInterior | Vitrin içi kartları | Unlit + DC_ShopInterior (taslak) |
| MI_Ext_Signs | Cephe ve bıçak tabela yüzleri | CNE/Toon + DC_Ext_Signs (taslak), emission yok |
| MI_Ext_Windows | Üst kat pencereleri | CNE/Toon + DC_Ext_Windows (taslak, opak) |
| MI_Ext_FarSilhouette | Uzak silüet kartı | Unlit, alpha clip 0,5 + T_Ext_FarSilhouette (taslak) |
| MI_Ped | Figüran kartı | SG_Ped_Flipbook (unlit, alpha clip 0,5, maskeyle renk) + T_Ped_Walk_Atlas, T_Ped_Walk_Mask (taslak) |
| MI_BlobShadow | Oval gölge | Unlit çarpma (multiply), opaklık 0,35 + T_BlobShadow |

Doku ayarları: palet Filter Point, Compression None, Mip Maps kapalı, sRGB, Clamp. Diğerleri Bilinear, Mip Maps açık, Clamp; silüet, figüran, puf ve gölge dokularında Alpha Is Transparency açık.

## Soketler

Boş node yerine manifest'te: `soketler` asset ekseninde, `soketler_unity` Unity ekseninde. Yönler asset'le aynı (+Z ön).

| Asset | Soket | Konum (asset ekseni, m) | Ne oturur |
|---|---|---|---|
| SM_ShopFront_6m_* | Socket_Sign | (3,00; 3,40; 0) | Cephe tabelası |
| | Socket_Awning | (2,30; 3,05; 0) | Tente |
| | Socket_Blade_L, _R | (0,20 ya da 5,80; 2,50; 0,10) | Bıçak tabela |
| SM_ShopFront_8m_* | Socket_Sign | (4,00; 3,40; 0) | Cephe tabelası |
| | Socket_Awning_1, _2 | (1,90 ya da 6,10; 3,05; 0) | 300'lük tente (vitrin başına bir) |
| | Socket_Blade_L, _R | (0,20 ya da 7,80; 2,50; 0,10) | Bıçak tabela |
| SM_Car_Sedan | Wheel_FL, _FR, _RL, _RR | (±0,80; 0,3188; ±1,45) | SM_Wheel; sağdakiler (−X) Y 180° |
| SM_Car_Pickup | Wheel_* | (±0,81; 0,3188; +1,60 ya da −1,55) | SM_Wheel |
| SM_Van | Wheel_* | (±0,82; 0,3188; ±1,70) | SM_Wheel |
| Üç araç | Socket_Exhaust | (−0,55; 0,30; −2,49 / −2,52 / −2,53), Y 180° | PS_Car_Exhaust |
| | Socket_Shadow | (0; 0,01; 0) | SM_BlobShadow, ölçek 2,3 × 1 × 5,2 |

## Asset'ler

Ölçüler sınırlayıcı kutu, cm, asset'in kendi ekseninde (G × D × Y). Toplam 4.764 üçgen (her asset bir kez). Hepsinde kontur Hayır; gölge yalnızca araçlarda ve figüran kartında oval (FX-04).

| Dosya | ID | Ad | G × D × Y (cm) | Üçgen / sınır | Soketler |
|---|---|---|---|---|---|
| ARCH_Sokak | MM-01…03, MM-05 | Sokak mimarisi: yakın kaldırım, yol, karşı kaldırım, ara sokak | 7000 × 3550 × 765 | 46 / 112 | — |
| ARCH_KarsiCephe_Yer | MM-04 | Karşı cephe yer tutucu blokları (11 Ekim) | 7000 × 320 × 750 | 168 / 216 | — |
| SM_ShopFront_6m_Plak | CK-01 | Dükkân cephesi 6 m · PLAK vitrini | 600 × 35 × 400 | 96 / 350 | Socket_Sign, Socket_Awning, Socket_Blade_L, Socket_Blade_R |
| SM_ShopFront_6m_Oyuncak | CK-01 | Dükkân cephesi 6 m · OYUNCAK vitrini | 600 × 35 × 400 | 96 / 350 | Socket_Sign, Socket_Awning, Socket_Blade_L, Socket_Blade_R |
| SM_ShopFront_6m_Radyo | CK-01 | Dükkân cephesi 6 m · RADYO vitrini | 600 × 35 × 400 | 96 / 350 | Socket_Sign, Socket_Awning, Socket_Blade_L, Socket_Blade_R |
| SM_ShopFront_8m_Cicek | CK-02 | Dükkân cephesi 8 m · ÇİÇEK vitrinleri | 800 × 35 × 400 | 110 / 450 | Socket_Sign, Socket_Awning_1, Socket_Awning_2, Socket_Blade_L, Socket_Blade_R |
| SM_ShopFront_8m_Kitap | CK-02 | Dükkân cephesi 8 m · KİTAP vitrinleri | 800 × 35 × 400 | 110 / 450 | Socket_Sign, Socket_Awning_1, Socket_Awning_2, Socket_Blade_L, Socket_Blade_R |
| SM_FacadeFill_3m | CK-03 | Dolgu duvar 3 m | 300 × 20 × 400 | 20 / 60 | — |
| SM_UpperFloor_3m | CK-04 | Üst kat 3 m | 300 × 10 × 360 | 14 / 80 | — |
| SM_UpperFloor_6m | CK-04 | Üst kat 6 m | 600 × 10 × 360 | 16 / 80 | — |
| SM_UpperFloor_8m | CK-04 | Üst kat 8 m | 800 × 10 × 360 | 18 / 80 | — |
| SM_Awning_300_Lila | CK-05 | Tente 300 · Lila | 300 × 120 × 65 | 20 / 60 | — |
| SM_Awning_300_Bebek | CK-05 | Tente 300 · Bebek | 300 × 120 × 65 | 20 / 60 | — |
| SM_Awning_300_Nane | CK-05 | Tente 300 · Nane | 300 × 120 × 65 | 20 / 60 | — |
| SM_Awning_450_Lila | CK-05 | Tente 450 · Lila | 450 × 120 × 65 | 20 / 60 | — |
| SM_Awning_450_Bebek | CK-05 | Tente 450 · Bebek | 450 × 120 × 65 | 20 / 60 | — |
| SM_Awning_450_Nane | CK-05 | Tente 450 · Nane | 450 × 120 × 65 | 20 / 60 | — |
| SM_FacadeSign_Plak | CK-06 | Cephe tabelası · PLAK | 300 × 6,2 × 50 | 12 / 30 | — |
| SM_FacadeSign_Cicek | CK-06 | Cephe tabelası · ÇİÇEK | 300 × 6,2 × 50 | 12 / 30 | — |
| SM_FacadeSign_Oyuncak | CK-06 | Cephe tabelası · OYUNCAK | 300 × 6,2 × 50 | 12 / 30 | — |
| SM_FacadeSign_Kitap | CK-06 | Cephe tabelası · KİTAP | 300 × 6,2 × 50 | 12 / 30 | — |
| SM_FacadeSign_Radyo | CK-06 | Cephe tabelası · RADYO | 300 × 6,2 × 50 | 12 / 30 | — |
| SM_BladeSign_Cicek | CK-07 | Bıçak tabela · ÇİÇEK | 10 × 86 × 138 | 96 / 120 | — |
| SM_BladeSign_Kitap | CK-07 | Bıçak tabela · KİTAP | 10 × 86 × 138 | 96 / 120 | — |
| SM_BladeSign_Radyo | CK-07 | Bıçak tabela · RADYO | 10 × 86 × 138 | 96 / 120 | — |
| SM_FarSilhouetteCard | UF-01 | Uzak silüet kartı | 2400 × 0 × 600 | 2 / 2 | — |
| SM_StreetLamp | SE-01 | Sokak lambası | 37 × 37 × 450 | 242 / 300 | — |
| SM_Mailbox | SE-02 | Posta kutusu | 50 × 47 × 110 | 82 / 250 | — |
| SM_Bench | SE-04 | Bank | 180 × 55 × 85 | 132 / 300 | — |
| SM_NewsBox | SE-05 | Gazete otomatı | 47 × 44 × 103 | 64 / 200 | — |
| SM_Tree_A | SE-06 | Ağaç A (büyük) | 312,5 × 247,5 × 549,7 | 376 / 500 | — |
| SM_Tree_B | SE-06 | Ağaç B (küçük) | 236,9 × 203,5 × 445,9 | 376 / 500 | — |
| SM_Car_Sedan | AR-01 | Sedan (gövde + cam) | 194 × 504 × 145* | 772 / 1.100 | Wheel_FL, Wheel_FR, Wheel_RL, Wheel_RR, Socket_Exhaust, Socket_Shadow |
| SM_Car_Pickup | AR-02 | Pikap (gövde + cam) | 198 × 512 × 162* | 740 / 1.100 | Wheel_FL, Wheel_FR, Wheel_RL, Wheel_RR, Socket_Exhaust, Socket_Shadow |
| SM_Van | AR-03 | Kamyonet (gövde + cam) | 198,8 × 513 × 222* | 656 / 1.300 | Wheel_FL, Wheel_FR, Wheel_RL, Wheel_RR, Socket_Exhaust, Socket_Shadow |
| SM_Wheel | AR-04 | Tekerlek | 22,5 × 63,8 × 63,8 | 60 / 64 | — |
| SM_Ped_Card | FX-02 | Figüran kartı | 90 × 0 × 180 | 2 / 2 | — |
| SM_BlobShadow | FX-04 | Oval gölge kartı 1 × 1 m | 100 × 100 × 0 | 2 / 2 | — |

\* Araçlarda yükseklik zeminden, tekerlekle; gövde mesh'i Y 30–32 cm'den başlar.

## Yerleşim

`Sokak_Yerlesim.obj` (YT-02, Faz 0.5): 63 örnek (ARCH dört nesne olduğu için 66 nesne), 5.296 üçgen. Örnek adı asset adı, birden fazlaysa `_1`, `_2` …; ARCH nesneleri `ARCH_Sokak__Kaldirim_Yakin` gibi. Konum, dönüş, ölçek ve materyal değişimlerinin tamamı `manifest.json` → `yerlesim.faz05`.

| Grup | Örnek | Not |
|---|---|---|
| Mimari | ARCH_Sokak: Kaldirim_Yakin, Yol, Kaldirim_Karsi, AraSokak | Kaldirim_Yakin Exterior_Gameplay'de (NavMesh kaynağı), diğerleri Exterior_Visual'da |
| Karşı cephe | 10 dolgu, 3 × 6 m ve 2 × 8 m dükkân, 15 üst kat, 5 cephe tabelası, 4 tente, 3 bıçak tabela | Ön yüz Z −15,70; dizilim aşağıda |
| Fon | SM_FarSilhouetteCard | (25; 0; −35,60), ara sokağın sonunda |
| Eşyalar | 6 lamba, posta kutusu, 2 bank, gazete otomatı, 3 ağaç | Yakın lambalar Z −3,00, karşıdakiler Z −12,90; banklar Z −15,64; ağaçlar Z −13,20 |
| Park eden kamyonet (AR-06) | SM_Van + MI_CarBody_Gri, 4 SM_Wheel, oval gölge | (3,80; −0,15; −4,45), Y 90° (önü doğuya) |

Karşı cephe dizilimi, batıdan doğuya (sol kenar X):

| X | Modül | Cephe | Tabela, tente, bıçak |
|---|---|---|---|
| −30, −27, −24, −21 | 4 dolgu | K5, K4, K5, K4 | — |
| −18 | 6 m PLAK | K4 | Tente 450 lila |
| −12 | 8 m ÇİÇEK | K6 | Tente 300 bebek mavisi (sol vitrin), bıçak sağda |
| −4 | Dolgu | K5 | — |
| −1, +2, +5 | 3 dolgu | K0 | GİRİŞ konisinin karşısı: tabela ve tente yok |
| +8 | 6 m OYUNCAK | K5 | Tente 300 nane |
| +14 | 8 m KİTAP | K4 | Tente 300 lila (sol vitrin), bıçak sağda |
| +22…+28 | Ara sokak | — | — |
| +28 | 6 m RADYO | K6 | Bıçak solda, tente yok |
| +34, +37 | 2 dolgu | K4, K5 | — |

`Sokak_Yerlesim_11Ekim.obj` (YT-01): yakın kaldırım, yol, karşı kaldırım ve MM-04'ün 9 bloğu; 196 üçgen. Ara sokak yok, X 22–28 boş.

Referans noktaları yerleştirilmez; Unity'de boş GameObject olarak kurulur (`manifest.json` → `bos_nodelar`):

| Nokta | Plan | Unity | Ne için |
|---|---|---|---|
| Ext_Spawn_W | (−5; 0; −1,5) | (5; 0; −1,5) | Müşteri doğma noktası (SY-04) |
| Ext_Despawn_E | (20; 0; −1,5) | (−20; 0; −1,5) | Müşteri kaybolma noktası (SY-04) |
| Ext_SM_PlantBush_1 | (2,4; 0; −0,55) | (−2,4; 0; −0,55) | Salon paketinden saksı (SE-03) |
| Ext_SM_PlantBush_2 | (5,2; 0; −0,55) | (−5,2; 0; −0,55) | Salon paketinden saksı (SE-03) |
| Ref_Arac_Yakin_Dogma | (−25; −0,15; −7,45) | (25; −0,15; −7,45) | Yakın şerit, doğuya (SY-06) |
| Ref_Arac_Yakin_Kaybolma | (38; −0,15; −7,45) | (−38; −0,15; −7,45) | Yakın şerit, doğuya |
| Ref_Arac_Uzak_Dogma | (38; −0,15; −10,95) | (−38; −0,15; −10,95) | Uzak şerit, batıya |
| Ref_Arac_Uzak_Kaybolma | (−25; −0,15; −10,95) | (25; −0,15; −10,95) | Uzak şerit, batıya |
| Ref_Figuran_Dogu_Dogma | (−25; 0; −13,6) | (25; 0; −13,6) | Figüran hattı, doğuya |
| Ref_Figuran_Dogu_Kaybolma | (38; 0; −13,6) | (−38; 0; −13,6) | Figüran hattı, doğuya |
| Ref_Figuran_Bati_Dogma | (38; 0; −14,6) | (−38; 0; −14,6) | Figüran hattı, batıya |
| Ref_Figuran_Bati_Kaybolma | (−25; 0; −14,6) | (25; 0; −14,6) | Figüran hattı, batıya |

## Paketler arası sahiplik

- **Restoranın dış cephesi, vitrin ve otomatik kapılar** salon paketinde (`SM_StorefrontBay_*`, `SM_StorefrontDoor`, ARCH_Salon'un kuzey duvarı). Bu paket duvarın dış yüzünden (Z −0,20) dışarısını kapsar; kapı ve kapı sesi yok.
- **SE-03 saksılar** salon paketinin `SM_PlantBush`'u; yerleşimde yalnızca `Ext_SM_PlantBush_1`, `_2` noktaları.
- **Müşteriler** karakter paketinde (FX-01 atlası onlardan render edilecek); önizlemedeki konturlu şişeler vekil.
- **Palet:** Paketteki `cook_no_evil_palet_256.png` renk-paleti.md'den yeniden kuruldu (S10·K2 ve S11 dahil, boş hücreler pembe); yalnızca önizleme ve kontrol için. Projede ana palet kullanılır; içinde HZ-01'in S11 satırı olmalı.

## Kararlar ve listeden (v2.1) farklar

- **Biçim OBJ** (liste GLB diyordu): dosya adları `.obj`; soketler, bayraklar ve yerleşim dönüşümleri manifestte.
- **Dükkân başına modül:** `SM_ShopFront_6m_Plak`, `_Oyuncak`, `_Radyo`; `SM_ShopFront_8m_Cicek`, `_Kitap`. Geometri aynı, yalnızca vitrin içi panelinin UV'si farklı (OBJ'de UV örnek başına değişmez).
- **Tente (CK-05):** 2 boy × 3 renk = 6 dosya, renk palet UV'sinde. 8 m dükkânlarda vitrin 3 m olduğu için 300'lük tente; 450 hem modülün sol kenarından hem kapının üstüne taşıyordu.
- **Orta çizgi (MM-02)** bordür materyalinde (MI_Arch_Bordur, S11·K3 #B7CAE8); listede #CFC9BD (S6·K5) yazıyordu. Ayrı renk istenirse çizgi yüzü ayrı materyale alınır.
- **MM-04:** 9 blok (liste 8), X −30…+22 ve +28…+40 boşluksuz; hepsi tek OBJ'de dünya koordinatında (`Blok_01…09`).
- **MM-05:** yan duvarlar X 22,0–22,2 ve 27,8–28,0, yani ara sokağın içinde.
- **CK-04 üst kat:** tek düzlem duvar + 10 cm çıkıntılı başlık; derinlik ve ayrı 30 cm parapet yok (dünyada Y 4,00–7,60). Pencere decal'ı 1,00 × 2,00 m, 3D denizlik yok.
- **CK-06, CK-07 ölçüleri:** cephe tabelası 300 × 6,2 × 50; bıçak tabela 10 × 86 × 138 (levha 8 × 70 × 120 + kol, askılar, duvar plakası).
- **SE-04 bank** Z −15,64 (liste −15,40), sırtı cepheye yaslı.
- **Araçlar** listedekinden biraz büyük: sedan 194 × 504 × 145, pikap 198 × 512 × 162, kamyonet 199 × 513 × 222 (liste uzunlukları 480 / 500 / 500). Şeritlere ve park şeridine sığıyor.
- **AR-04 tekerlek** ø66 × 20, 12 kenar; aks yüksekliği 0,3188 (12-genin alt kenarı zemine değer). Tekerlek açısal hızı = hız / 0,33.
- **UF-01** materyal adı `MI_Ext_FarSilhouette` (liste MI_Ext_Unlit diyordu).
- **Tabela yazıları** taslak dokuda Poppins Bold; UI rehberi tabela için Righteous ya da Rammetto One öneriyor.
- **Dokular** (CK-08, CK-09, CK-10, UF-01, FX-01, FX-03, FX-04) taslak: UV bölgeleri sabit, içerik değişebilir.

## Unity kurulumu

- **Kök:** `Exterior` kökü salon köküyle aynı yerde; altında `Exterior_Visual` (rol Şef ise kapatılır) ve `Exterior_Gameplay` (yakın kaldırım, NavMesh). `Sokak_Yerlesim.obj` (0; 0; 0), dönüş 0, ölçek 1 ile konur; X dönüşümünü içe alıcı yapar. Ayna kontrolü: park eden kamyonet giriş kapısının önünde (Unity X −3,80), RADYO dükkânı en doğuda (Unity X −28…−34), ara sokak Unity X −22…−28.
- **Hızlı yol ve üretim yolu:** Birleşik yerleşim OBJ'si hızlı kurulum için (tek model, 66 alt nesne). Üretimde her asset bir kez içe alınıp prefab yapılır, örnekler manifest dönüşümleriyle yerleştirilir (GPU instancing, tek tek düzenleme).
- **Static:** mimari, cephe ve eşyalar Batching Static; Contribute GI kapalı; Cast Shadows Off.
- **Araç prefab'ı:** kök (taban ortası) → `Body` (SM_Car_*) + 4 SM_Wheel (`soketler_unity` konumlarında) + `Socket_Exhaust` (PS_Car_Exhaust) + `Socket_Shadow` (SM_BlobShadow, ölçek 2,3 × 1 × 5,2). SY-07 yalnızca `Body`'yi yaylandırır, tekerlekleri yerel X'te döndürür (sağ tekerlekler 180° dönük olduğu için işaret ters).
- **Figüran:** SM_Ped_Card + MI_Ped; altında SM_BlobShadow 0,70 × 0,35.
- **Gökyüzü:** Kasiyer ve Komi kameralarında arka plan Solid Color #CFE3EE (S11·K12) (SY-10).

## Ne test edildi

- Kontrol betiği 40 OBJ'yi ve MTL'yi diskten geri okudu: **0 sorun**. Kontroller:
  - Satır biçimi: yalnızca v, vt, vn, f, o, g, usemtl, mtllib; yorum dışında ASCII; BOM ve CRLF yok. v/vt/vn indeksleri aralıkta.
  - MTL: tanımsız materyal yok; dokular var, PNG imzaları sağlam.
  - Geometri: normaller birim; her köşe normali yüzle aynı yönde (sarım tutarlı); bozuk yüz yok; n-gon'lar düzlemsel ve dışbükey; asset içinde yinelenen yüz yok; mimari ve kit parçalarında T-kavşak yok.
  - UV: hepsi 0–1 içinde; palet UV'leri hücre ortasında ve dolu hücrede; atlas UV'leri panel sınırını aşmıyor.
  - Sayılar: üçgen sayısı bütçe içinde ve manifestle aynı; sınırlar manifestle aynı; pivot kuralları tutuyor; tekerlekler zemine değiyor.
  - Yerleşim: her örnek, kaynak asset'in dönüştürülmüş hâliyle 0,02 mm içinde aynı.
- Yerleşimde komşu cephe modüllerinin uç kapakları sırt sırta çakışıyor (31 çift, birbirinin içinde kaldığı için görünmez). Modüller tek başına da kapalı dursun diye bırakıldı.
- `onizleme/` render'ları bu OBJ'ler geri yüklenerek alındı (look-dev rasterizer'ı; Unity değil).
- Unity'ye ve Blender'a içe alma denenmedi (bu ortamda yoklar).
