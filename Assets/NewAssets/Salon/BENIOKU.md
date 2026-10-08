# Cook No Evil! · Salon (müşteri alanı) modelleri

*8 Ekim 2026 · 30 asset + yerleşim + mimari referans · GLB · kapsam: A2 salon önizlemesindeki müşteri alanı (oturma, dekor, çöpler, lambalar, tabelalar, vitrin ve kapılar; karakterler hariç)*

## Paket: `CNE_Salon_Modeller.zip`

| Klasör / dosya | İçerik |
|---|---|
| `glb/` | Her asset ayrı `.glb`: hiyerarşi, pivot, hareketli parçalar, soketler, materyaller, gömülü dokular. `Salon_Yerlesim.glb`: 55 örnek plandaki yerinde + 14,00 × 8,00 referans zemin + `Ref_`/`Ext_` boş node'ları. `ARCH_Salon.glb`: zemin (50 cm dama), tavan, 6 duvar parçası (kaba boşluklarla), süpürgelik, lambri, krom bordür, eşikler; düz renk, UV yok. |
| `textures/` | Palet (256 px, 16 px hücre, S10 dahil; salon için yeni hücre yok), `T_Wood_Mese` (1 tekrar = 0,5 m), 10 decal: 6 tabela yüzü (+ `_E` emission dokusu) ve 4 tablo. |
| `manifest.json` | Ölçüler, soketler, hareketli parçalar, kontur/gölge bayrakları, yerleşim dönüşümleri (salon planı, Unity ve Kasa planı), mimari boşluklar ve renkler. |
| `onizleme/` | Doğrudan bu GLB'lerden toon render: büyük ve küçük asset sayfaları, iki yerleşim kuşbakışı, girişten göz hizası, hareket + soket testi (pembe işaretler yalnız testte). |
| `salon-modeller.md` | Bu not. |

## Kullanım

1. **Unity:** glTFast (`com.unity.cloud.gltfast`) ile `glb/` klasörünü içe alın; sahneyi `istasyon-sahne-kurulum-prompt.md`'deki akışla kurun (yerleşim GLB'sinden okuma; `Ref_` ve `Ext_` node'ları yerleştirilmez). Materyalleri aşağıdaki tabloya göre projenin toon materyalleriyle değiştirin. Salona özgü farklar en altta.
2. **Blender:** File → Import → glTF 2.0; ölçek 1, Y yukarı otomatik dönüşür.

glTFast X eksenini ters çevirir: plan (x; y; z) → Unity (−x; y; z). Y dönüşleri işaret değiştirir, X dönüşleri (kapak menteşeleri) aynı kalır. glTFast node `extras`'ını içe almayabilir; bayraklar ayrıca `manifest.json`'da.

## Eksen, birim, pivot

- Birim metre. Y yukarı, asset'in önü +Z, X genişlik (C1, İstasyon ve Kasa paketleriyle aynı).
- Yerleşim ekseni: X doğu, Z güney; kök = salon iç kuzeybatı köşesi, zemin. Salonun iç ölçüsü 14,00 × 8,00, tavan 2,75, duvar 0,20.
- Komşu kökler (salon köküne göre): Kasa (+2,10; 0; +8,20), İstasyon (+2,10; 0; +11,40), C1 mutfak (+6,30; 0; +11,40). Kasa planı koordinatı = salon koordinatı − (2,10; 0; 8,20); manifest'te her örnek için `kasa_plani` olarak da var.
- Pivotlar: serbest duranlar (masa, sandalye, akvaryum, bitkiler, Ç2, Ç3, askılık, sakız makinesi, paspas, peçetelik) taban ortası; duvara yaslananlar (separe, jukebox, Ç1) taban arka kenar ortası, yani duvar çizgisi; duvara monteler (tablolar, tabelalar) arka yüz ortası; lambalar üst yüz ortası (tavan), OPEN neonu zincir bağlantısının ortası; vitrin bölmesi bant açıklığının alt kenarı ortası (0,90 m), kapı açıklığın alt kenarı ortası, ikisi de salon tarafı duvar yüzünde.

## Uygulanan kurallar

- **Bevel:** gövdeler r 1,5–2 cm (2 segment); tablalar ve kapaklar r 0,6–1,2 cm; vinil minder ve oturaklar r 3,5–4,5 cm, 4 segment yumuşak; vitrin kayıtları, kapı kasası ve tablo çerçevesi keskin. Düz yüzün normali düz, bevel'ın normali yumuşak geçişli (Harden + Weighted Normal eşdeğeri).
- **Tabla:** laminat nane üst + krom kenar bandı tek kutu, iki primitive (separe masası, yuvarlak masa, Ç1 üstü).
- **Kontur:** hiçbir salon asset'inde yok. Salon oyuncuların etkileşmediği "game feel" alanı; toon kuralı konturu oynanış nesnelerine ayırıyor. Salona açılan sipariş ve teslim pencereleri Kasa paketinde ve konturlu.
- **Gölge:** zemine ve masaya oturanlar Evet; duvara/tavana monteler, neon, paspas, vitrin ve kapılar Hayır. Saydam node'lar (`Water`, `Glass`, `Globe`) gölge düşürmemeli: Cast Shadows Off.
- **Renk:** yalnızca palet; UV'ler hücre ortalarında, boş (pembe) hücreye düşen yok. Meşe parçalarda kutu izdüşümü UV (0,5 m). Tabela ve tablo yüzleri decal; yalnızca palet renkleriyle çizildi, yazılar dokuya basılı (font bağımlılığı yok).
- **Sinyal renkleri** (ketçap kırmızısı, hardal sarısı, mayonez beyazı, barbekü kahvesi) salon dekorunda yok. Sinyal fonu #172A3A rezerve olduğu için tabela zeminleri dökme demir koyu #2C3237. Balıklar lila, cyan, turkuaz ve nane; desen olarak çizgi veya nokta kullanılmadı.

## Materyaller

| GLB materyali | Nerede | Proje karşılığı |
|---|---|---|
| MI_Palette | Vinil, laminat, plastik, saksı, yaprak, tabela gövdeleri | Düz palet (CNE/Toon, _BaseMap = palet) |
| MI_Palette_Steel | Kapı tekmeliği, Ç1 itme kapağı | Paslanmaz: parıltı yalnızca bevel'da |
| MI_Palette_Chrome | Masa bantları ve ayakları, separe kapakları, sandalye boruları, vitrin kayıtları ve kapı kasası, Ç2, peçetelik, lamba halkaları | Krom: matcap |
| MI_Palette_Emission | Lamba kubbeleri (S10·K2), jukebox ve OPEN neonları (lila neon S9·K1, cyan S9·K2) | Emission × HDR 2,5 (bloom eşiği 1,3) |
| MI_Palette_Glass | Vitrin ve kapı camları, akvaryum camı, sakız küresi | Şeffaf cam S7·K2, %25 opak (C1'deki cam) |
| MI_Palette_GlassStreak | Camlardaki iki beyaz parıltı çizgisi | Beyaz S6·K4, %60 opak. Proje camı kendi çizgisini çiziyorsa bu alt mesh'i kapatın |
| MI_Palette_Water | Akvaryum suyu | Yeni: turkuaz vurgu S1·K6, %45 opak, emission 0,3 |
| MI_Wood_Mese | Ç1 dolabı ve kapakları, tablo çerçeveleri | C1 meşe (T_Wood_Mese) |
| MI_Decal_Giris, _Cikis, _Teslim, _Menu, _Logo, _JukePanel | Tabela yüzleri ve jukebox paneli | Doku + `_E` emission dokusu; yoğunluk 1,6 (kapı), 2,0 (menü, TESLİM), 2,2 (logo), 1,0 (jukebox) |
| MI_Decal_Tablo_* | Tablo yüzleri | Doku, toon ışıkla (emission yok) |
| MI_Arch_* | Mimari referans | Projedeki mimari materyaller (renk adına göre) |

Doku ayarları: palet Filter Point, Compression None, Mip Maps kapalı, sRGB, Clamp. Meşe Bilinear, Mip Maps açık, Repeat. Decal'lar Bilinear, Mip Maps açık, Clamp.

## Hareketli parçalar

Her biri asset'in altında ayrı node; node'un orijini menteşe ya da merkez. Değerler glTF/plan ekseninde; Unity'de Y dönüşleri işaret değiştirir, X dönüşleri aynı kalır.

| Asset | Node | Pivot | Hareket |
|---|---|---|---|
| SM_StorefrontDoor | Leaf_L | Sol menteşe ekseni | Giriş: Y −90° içe (salona, +Z) · Çıkış: Y +90° dışa |
| | Leaf_R | Sağ menteşe ekseni | Giriş: Y +90° içe · Çıkış: Y −90° dışa |
| SM_BinCabinet | Flap | Kapağın üst kenarı | X +70° içe itilir |
| SM_BinDome | Flap | Kapağın üst kenarı (kubbe üstünde) | X +60° içe itilir |
| SM_BinSquare | Flap | Kapağın üst kenarı | X +70° içe itilir |
| SM_Aquarium | Fish_1–6 | Balığın merkezi, burun yerel +X | Yüzme animasyonu için serbest |

## Soketler

Boş node'lar; adları `Socket_` ile başlar, yönleri asset'le aynı (+Z ön).

| Asset | Soketler | Ne oturur |
|---|---|---|
| SM_Booth_2 | Seat_1, Seat_2 · Table_1, Table_2 · Napkin | Müşteri oturma noktası (masaya bakar) · her müşterinin tepsi yeri · peçetelik |
| SM_RoundTable | Table_1, Table_2 · Napkin | −Z ve +Z sandalyelerinin tepsi yerleri · peçetelik |
| SM_DinerChair | Seat | Oturma noktası (+Z'ye bakar) |
| SM_BinCabinet | Drop · TrayReturn | Atma noktası · tepsi bırakma |
| SM_BinDome, SM_BinSquare | Drop | Atma noktası |
| SM_PendantLight, SM_CeilingLight | Light | Point ışık (#FFE7C2, 0,30, gölgesiz) |
| SM_Jukebox | Light | İsteğe bağlı neon ışığı |

GLB'de bayraklar node `extras` alanında: kökte `cne_asset`, `cne_outline`, `cne_cast_shadow`; hareketli node'larda `cne_movable`; soketlerde `cne_socket`; yerleşim örneklerinde `cne_source_asset`.

## Asset'ler

Ölçüler sınırlayıcı kutu, cm, asset'in kendi ekseninde (G × D × Y). Toplam 30.268 üçgen (her asset bir kez); ARCH 1.966 üçgen.

| Dosya | Ad | G × D × Y (cm) | Üçgen | Kontur | Gölge | Soketler | Hareketli |
|---|---|---|---|---|---|---|---|
| SM_Booth_2 | Separe (2 kişilik) | 200,8 × 110 × 115 | 5.228 | Hayır | Evet | Seat_1, Seat_2, Table_1, Table_2, Napkin | — |
| SM_RoundTable | Yuvarlak masa ø100 | 100 × 100 × 76,5 | 768 | Hayır | Evet | Table_1, Table_2, Napkin | — |
| SM_DinerChair | Sandalye (krom ayaklı, vinil) | 44 × 48 × 90 | 2.628 | Hayır | Evet | Seat | — |
| SM_NapkinHolder | Peçetelik (krom) | 10 × 14 × 14,5 | 312 | Hayır | Evet | — | — |
| SM_Aquarium | Akvaryum (çift yüzlü, dolaplı) | 201 × 61,6 × 158 | 2.834 | Hayır | Evet | — | Fish_1–6 |
| SM_PlantBush | Saksı bitkisi (çalı) | 63,1 × 59,5 × 120,1 | 1.512 | Hayır | Evet | — | — |
| SM_PlantTall | Köşe bitkisi (uzun) | 74,9 × 59,3 × 183,5 | 1.560 | Hayır | Evet | — | — |
| SM_Jukebox | Jukebox | 80 × 58,1 × 158 | 2.078 | Hayır | Evet | Light | — |
| SM_Painting_Milkshake | Tablo · milkshake | 77 × 3 × 62 | 62 | Hayır | Hayır | — | — |
| SM_Painting_Plak | Tablo · plak | 87 × 3 × 67 | 62 | Hayır | Hayır | — | — |
| SM_Painting_Atom | Tablo · atom yıldızı | 57 × 3 × 72 | 62 | Hayır | Hayır | — | — |
| SM_Painting_Araba | Tablo · 50'ler arabası | 127 × 3 × 72 | 62 | Hayır | Hayır | — | — |
| SM_BinCabinet | Ç1 · Tepsi iade + çöp dolabı (meşe) | 102 × 58 × 123 | 3.624 | Hayır | Evet | Drop, TrayReturn | Flap |
| SM_BinDome | Ç2 · Kubbe kapaklı krom çöp | 47 × 47 × 89 | 880 | Hayır | Evet | Drop | Flap |
| SM_BinSquare | Ç3 · Kare itme kapaklı çöp (turkuaz) | 43 × 43,7 × 80 | 900 | Hayır | Evet | Drop | Flap |
| SM_CoatRack | Askılık | 42 × 42 × 181 | 664 | Hayır | Evet | — | — |
| SM_GumballMachine | Sakız makinesi | 30 × 30 × 114 | 1.696 | Hayır | Evet | — | — |
| SM_DoorMat | Paspas 150 × 55 | 150 × 55 × 1,2 | 300 | Hayır | Hayır | — | — |
| SM_PendantLight | Sarkıt lamba (lila şapka) | 39 × 39 × 72 | 600 | Hayır | Hayır | Light | — |
| SM_CeilingLight | Tavan lambası (Kasa/İstasyon ile aynı) | 39,2 × 39,2 × 8,5 | 288 | Hayır | Hayır | Light | — |
| SM_MenuBoard | Menü panosu (sipariş penceresinin üstü) | 250 × 4,7 × 55 | 302 | Hayır | Hayır | — | — |
| SM_LogoSign | Logo tabelası “Cook No Evil!” | 260 × 5,2 × 80 | 302 | Hayır | Hayır | — | — |
| SM_DeliverySign | TESLİM tabelası (teslim penceresinin üstü) | 150 × 4,2 × 30 | 302 | Hayır | Hayır | — | — |
| SM_DoorSign_Giris | GİRİŞ tabelası | 124 × 3,7 × 32 | 302 | Hayır | Hayır | — | — |
| SM_DoorSign_Cikis | ÇIKIŞ tabelası | 124 × 3,7 × 32 | 302 | Hayır | Hayır | — | — |
| SM_OpenNeon | OPEN neonu (camın içinde asılı) | 84,8 × 1,8 × 75,9 | 1.776 | Hayır | Hayır | — | — |
| SM_StorefrontBay_A | Vitrin bölmesi A (2 cam, 275 cm) | 275 × 26,5 × 140 | 84 | Hayır | Hayır | — | — |
| SM_StorefrontBay_B | Vitrin bölmesi B (4 cam, 490 cm) | 490 × 26,5 × 140 | 120 | Hayır | Hayır | — | — |
| SM_StorefrontBay_C | Vitrin bölmesi C (2 cam, 255 cm) | 255 × 26,5 × 140 | 84 | Hayır | Hayır | — | — |
| SM_StorefrontDoor | Vitrin çift kapısı 2 × 0,75 + vasistas | 170 × 16 × 230 | 574 | Hayır | Hayır | — | Leaf_L, Leaf_R |

## Yerleşim

`Salon_Yerlesim.glb` 55 örnek; örneklenen mesh'ler bir kez saklanır (30.270 üçgen). Örnek adı asset adı, birden fazlaysa `_1`, `_2` … ekli. Konum ve dönüşlerin tamamı `manifest.json` → `yerlesim` (salon planı, Unity ve Kasa planı değerleri).

| Grup | Örnek | Not |
|---|---|---|
| Oturma | 4 × SM_Booth_2, 2 × SM_RoundTable, 4 × SM_DinerChair, 6 × SM_NapkinHolder | Separeler batı ve doğu duvarında (12 kişi, 6 masa); peçetelikler masaların `Napkin` soketinde |
| Dekor | SM_Aquarium, SM_PlantBush, 2 × SM_PlantTall, SM_Jukebox, 4 tablo, SM_CoatRack, SM_GumballMachine, 2 × SM_DoorMat | Tablolar 1,76 m'ye ortalı |
| Çöpler | SM_BinCabinet, 2 × SM_BinDome, 2 × SM_BinSquare | Ç1 çıkışın doğusunda vitrin duvarına yaslı |
| Lambalar | 6 × SM_PendantLight, 4 × SM_CeilingLight | Sarkıtlar masaların, kubbeler dolaşım hattının üstünde |
| Tabelalar | SM_MenuBoard, SM_DeliverySign, SM_LogoSign, SM_DoorSign_Giris, SM_DoorSign_Cikis, SM_OpenNeon | Menü, TESLİM ve logo Kasa duvarının salon yüzünde; GİRİŞ/ÇIKIŞ vitrin kuşağının iç yüzünde; OPEN camın içinde, sokağa dönük |
| Cephe | SM_StorefrontBay_A/B/C, 2 × SM_StorefrontDoor | Giriş X 3,80 (içe açılır), çıkış X 10,40 (dışa açılır) |

## Paketler arası sahiplik

- **Kasa–salon duvarı:** `ARCH_Salon.glb` içinde `Duvar_Guney_Ortak_Kasa` (Z 8,00–8,20, X 1,90–12,10; Kasa yüzü `MI_Arch_Kasa_Duvar` yer tutucu). Kasa sahnesinde bu duvar zaten varsa yeniden yaratmayın; yalnızca salon yüzünü (krem, nane lambri, krom bordür, ceviz süpürgelik) uygulayın.
- **Sipariş ve teslim pencereleri** Kasa paketinde (`SM_StationWindow_Order`, `SM_StationWindow_Delivery`; salon tarafına 10 cm taşar). Yerleşimde yalnızca `Ext_` node'ları var: (3,80; 1,00; 8,20) ve (10,40; 1,00; 8,20). Kaba boşluklar ARCH'ta: X 2,535–5,065 ve 9,135–11,665, Y 0,954–1,965. Önizlemedeki koyu dikdörtgenler bu boşluklar.
- **SM_CeilingLight** Kasa/İstasyon paketindekiyle aynı ölçü ve pivot; projede varsa o kullanılabilir.

## Kararlar ve varsayımlar

- Tavan 2,75 m, Kasa ile aynı (A2 plan panelinde 2,70 yazıyordu).
- Vitrin: üç bölme (275 / 490 / 255 cm), 2 / 4 / 2 cam, 5 cm krom kayıt, krom denizlik; altında 0,90 m dolu duvar (içte nane lambri, dışta turkuaz vurgu). Cam parıltı çizgileri geometri.
- Kapı: çift kanat 2 × 0,75 m, cam, krom kasa, paslanmaz tekmelik, iki yüzde itme kolu, 2,10 + vasistas. Giriş ve çıkış aynı asset; yalnızca açılma yönü farklı.
- Tabelalar dökme demir koyu gövde + decal yüz. OPEN neonu sokağa dönük; içeriden ayna yazı okunur (önizleme bulgusu).
- TESLİM tabelası, lambalar, peçetelikler ve soketler planda yoktu; önizlemedeki gibi eklendi.
- Akvaryum suyu için yeni bir materyal (MI_Palette_Water) gerekti; renk paletteki turkuaz vurgu.
- İstasyon ve Mutfak bu pakette yok.

## Unity kurulumu: İstasyon akışından farklar

- **Kök:** `Salon` kökünü Kasa köküne göre yerleştirin: plan ekseninde (−2,10; 0; −8,20), Unity'de (+2,10; 0; −8,20). Ayna kontrolü: giriş kapısı sipariş penceresinin, çıkış kapısı teslim penceresinin tam karşısında olmalı; tablolar batı ve doğu duvarına, menü ve logo Kasa duvarının salon yüzüne düşmeli.
- **Işık:** her `SM_PendantLight` ve `SM_CeilingLight` örneğinin `Socket_Light` noktasına point ışık: #FFE7C2, Kasa'daki tavan lambasıyla aynı ayar (yoksa intensity 0,3, gölgesiz).
- **Saydamlar:** `Water`, `Glass`, `Globe` ve vitrin camı alt mesh'leri Cast Shadows Off; cam materyali derinliğe yazmaz (salonda Şef konturu yok).
- **Kapı ve kapaklar:** animasyon ya da kod yalnızca `Leaf_*`, `Flap` ve `Fish_*` node'larını döndürür; değerler yukarıdaki tabloda.

## Ne test edildi

- 32 GLB yapısal kontrolden geçti (0 sorun): başlık ve chunk'lar, buffer view ve accessor sınırları, POSITION min/max, normal birimliği, indeks aralığı, üçgen sargısı (normallerle tutarlı), node ağacı (tek ebeveyn, döngü yok, soketler mesh'siz), PNG imzaları, gömülü paletin paketteki paletle aynı olması, palet UV'lerinin hücre ortasında olması ve hiçbirinin boş (pembe) hücreye düşmemesi.
- `onizleme/` render'ları bu GLB'ler geri yüklenerek alındı: yerleşim ve ARCH plana oturuyor, decal'lar ve emission doğru kanalda, hareketli parçalar sınır konumunda, soketler pembe.
- Khronos glTF Validator bu ortamda kurulamadı (paket sunucusuna erişim yok). Unity'ye içe alma denenmedi.
