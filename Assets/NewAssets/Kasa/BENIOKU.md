# Cook No Evil! · Kasa (Kasiyer'in odası) modelleri

*8 Ekim 2026 · 17 asset + yerleşim + mimari referans · GLB · kapsam: Kasa planındaki nesneler + tavan lambaları (karakterler hariç)*

## Paket: `CNE_Kasa_Modeller.zip`

| Klasör / dosya | İçerik |
|---|---|
| `glb/` | Her asset ayrı `.glb`: hiyerarşi, pivot, hareketli parçalar, soketler, materyaller, gömülü dokular. `Kasa_Yerlesim.glb`: 18 örnek plandaki yerinde + 3 dış referans node'u + 9,80 × 3,00 referans zemin. `ARCH_Kasa.glb`: zemin (50 cm dama), tavan, 4 duvar (kaba boşluklarla), süpürgelik, lambri, krom bordür; düz renk, UV yok. |
| `textures/` | Palet (256 px, 16 px hücre, S10 dahil; Kasa için yeni hücre yok), meşe ve akçaağaç damar dokuları (1 tekrar = 0,5 m). |
| `manifest.json` | Ölçüler, soketler, hareketli parçalar, kontur/gölge bayrakları, yerleşim dönüşümleri (plan ve Unity), dış referanslar. |
| `onizleme/` | Doğrudan bu GLB'lerden toon render: büyük ve küçük asset sayfaları, hareket + soket testi (pembe işaretler yalnız testte), yerleşim kuşbakışı ve göz hizası. |

## Kullanım

1. **Unity:** glTFast (`com.unity.cloud.gltfast`) ile `glb/` klasörünü içe alın; sahneyi `istasyon-sahne-kurulum-prompt.md`'deki akışla kurun (yerleşim GLB'sinden okuma, `Ref_` ve `Ext_` node'ları yerleştirilmez). Materyalleri projenin toon materyalleriyle değiştirin.
2. **Blender:** File → Import → glTF 2.0; ölçek 1, Y yukarı otomatik dönüşür.

glTFast X eksenini ters çevirir: plan (x; y; z) → Unity (−x; y; z). Y ve Z dönüşleri işaret değiştirir, X dönüşleri aynı kalır. glTFast node `extras`'ını içe almayabilir; bayraklar ayrıca `manifest.json`'da.

## Eksen, birim, pivot

- Birim metre. Y yukarı, asset'in önü +Z, X genişlik (C1 ve İstasyon paketleriyle aynı).
- Yerleşim ekseni: X doğu (0 = Kasa batı duvarı iç yüzü), Z güney (0 = müşteri duvarı iç yüzü). İstasyon kökü (0; 0; 3,20), C1 mutfak kökü (4,20; 0; 3,20); İstasyon paketindeki "Kasa kökü (0; 0; −3,20)" ile tutarlı.
- Pivotlar: tezgah ve makineler taban arka kenar ortası; yığın, kutu, çöp ve item'lar taban ortası; pano, askı ve stok kapağı arka yüz ortası (duvar yüzü); pencereler net açıklığın alt kenarı ortası, Kasa tarafı duvar yüzü (pivot = pervaz üstü, 1,00 m); tavan lambası üst yüz ortası.

## Uygulanan kurallar (İstasyon paketiyle aynı)

- **Tek parça kasa + söve:** pencerelerde krom kasa ve söve duvarı boydan geçen tek mesh (`Kasa_Sove`), keskin.
- **Tabla ve pervaz:** laminat nane üst + krom kenar bandı tek kutu, iki primitive; bevel r 0,6 cm.
- **Bevel:** gövdeler r 1,2–2 cm, kapaklar r 0,6 cm, plastikler ve dondurma makinesi gövdesi 4 segment yumuşak, söve ve ızgara çubukları keskin. Normaller Harden + Weighted Normal eşdeğeri.
- **Kontur:** tezgah, makineler, yığınlar, kutular, pencereler, stok kapağı, çöp ve item'lar Evet; pano, tüp askısı ve tavan lambası Hayır.
- **Gölge:** zemine ve tezgaha oturanlar ve item'lar Evet; duvara ve tavana monteler Hayır.
- **Renk:** yalnızca palet; UV'ler hücre ortalarında, boş (pembe) hücreye düşen yok. Ahşapta kutu izdüşümü UV.

## Materyaller

| Materyal | Nerede |
|---|---|
| MI_Palette | Düz palet renkleri (laminat, plastik, lila, rozetler, tüp, kitap, pano kartları, parçacıklar) |
| MI_Palette_Steel | Paslanmaz gövdeler, damlama tepsileri, yuva tepsileri, stok kapağı |
| MI_Palette_Chrome | Kulplar, tabla ve pervaz kenar bandı, `Kasa_Sove`, musluklar, dağıtıcı baş, lamba halkası |
| MI_Palette_Emission | Lamba kubbesi (S10·K2) ve dondurma makinesi ekranı (cyan S9·K2), emissive strength 2,5 |
| MI_Wood_Mese | Tezgah kapakları |
| MI_Wood_Akcaagac | Pano çerçevesi |
| MI_Arch_* | Mimari referans: düz renk, UV yok |

## Hareketli parçalar

| Asset | Node | Pivot | Hareket (Unity yerel) |
|---|---|---|---|
| SM_DrinkMachine | Lever_1–4 | Kolun üst kenarı | X +12° (geriye itilir) |
| SM_IceCreamMachine | Lever | Dağıtıcı başın üstü | X +40° (aşağı çekilir) |
| | Lid | Hazne kapağının arka kenarı | X −80° açılır |
| SM_StockHatch_Kasa | Drawer | Çerçeve arka yüzü | Z +0,30 m Kasa'ya kayar |
| SM_FireExtinguisher | Handle | Kolun arka ucu | Z +12° (sıkılır; glTF'te −12°) |
| SM_RecipeBook | Cover | Sırt üst kenarı | Z −180°'ye kadar açılır (glTF'te +180°) |
| SM_TrashBin | Lid | Kapağın arka kenarı | X −75° (pedal) |

Tezgah kapakları (`Door_1…5`) ayrı node, menteşe (sol) kenarında pivotlu, statik. Parçacık içerikleri (`Content_1_Cikolata`, `Content_2_Draje`, `Content_3_Biskuvi`) ve pano kartları (`Card_1_Ekmek` … `Card_9_Patates`) ayrı node.

## Soketler

`Socket_` önekli boş node'lar, yönleri asset'le aynı: DrinkMachine, CupStack, LidStack, IceCream, Toppings (tezgah üstü yerleri); Slot_1–4 (içecek makinesi), Slot (dondurma); Take (yığınlar); Fill ve Lid (tekil bardak); Scoop_1–3 (parçacık kutuları); RecipeBook (sipariş penceresi pervazı); Slot_1–3 (teslimat penceresi); Load (stok çekmecesi); Extinguisher (askı); Hand ve Nozzle (tüp); Drop (çöp); Light (lamba, aşağı bakar).

## Asset'ler

| Dosya | Ad | G × D × Y (cm) | Üçgen | Kontur | Gölge | Soketler | Hareketli |
|---|---|---|---|---|---|---|---|
| SM_DrinkIceCounter | İçecek + dondurma tezgahı | 375 × 86,6 × 100 | 1.092 | Evet | Evet | DrinkMachine, CupStack, LidStack, IceCream, Toppings | — |
| SM_DrinkMachine | İçecek makinesi (4 slot) | 125,4 × 59 × 78 | 4.888 | Evet | Evet | Slot_1–4 | Lever_1–4 |
| SM_CupStack | Karton bardak yığını (kaynak) | 38 × 38 × 33,9 | 6.112 | Evet | Evet | Take | — |
| SM_LidStack | Plastik kapak yığını (kaynak) | 38 × 38 × 13,4 | 4.528 | Evet | Evet | Take | — |
| SM_Cup | Karton bardak (tekil) | 9,4 × 9,4 × 12,3 | 560 | Evet | Evet | Fill, Lid | — |
| SM_CupLid | Plastik kapak (tekil) | 9,5 × 9,5 × 1,4 | 400 | Evet | Evet | — | — |
| SM_IceCreamMachine | Dondurma makinesi (tek slot) | 62 × 60,8 × 85 | 2.376 | Evet | Evet | Slot | Lever, Lid |
| SM_ToppingBins | Parçacık kutuları ×3 | 43 × 66 × 24,9 | 4.356 | Evet | Evet | Scoop_1–3 | — |
| SM_StationWindow_Order | Sipariş penceresi | 262 × 65 × 116,1 | 252 | Evet | Hayır | RecipeBook | — |
| SM_StationWindow_Delivery | Teslimat penceresi | 262 × 65 × 116,1 | 612 | Evet | Hayır | Slot_1–3 | — |
| SM_StockHatch_Kasa | Stok kapağı, Kasa yüzü | 75 × 28,8 × 50 | 960 | Evet | Hayır | Load | Drawer |
| SM_ExtinguisherBracket | Yangın tüpü askısı | 17,8 × 20 × 37 | 324 | Hayır | Hayır | Extinguisher | — |
| SM_FireExtinguisher | Yangın tüpü (item) | 23,1 × 17,7 × 50,2 | 2.184 | Evet | Evet | Hand, Nozzle | Handle |
| SM_RecipeBook | Tarif kitabı (item) | 30,6 × 25 × 4,1 | 456 | Evet | Evet | — | Cover |
| SM_IngredientBoard | Malzeme panosu | 80 × 5 × 80 | 2.382 | Hayır | Hayır | — | — |
| SM_TrashBin | Çöp kovası ø60 | 60 × 65,5 × 76 | 1.356 | Evet | Evet | Drop | Lid |
| SM_CeilingLight | Tavan lambası | 39,2 × 39,2 × 8,5 | 1.688 | Hayır | Hayır | Light | — |

Yerleşim GLB'si 18 örnek, 33.568 üçgen (örneklenmiş mesh'ler bir kez saklanır). ARCH 1.586 üçgen.

## Paketler arası sahiplik

- **Kasa–İstasyon penceresi** (`SM_StationWindow_Kasa`) İstasyon paketinde. Kasa yerleşiminde yalnızca `Ext_SM_StationWindow_Kasa` boş node'u var: (1,70; 1,00; 3,20).
- **Mutfak çift kapısı** C1 paketinde (`SM_SwingDoorFrame` + 2 × `SM_SwingDoor`); `Ext_` node'u açıklık ortasında.
- **Stok çekmecesi:** C1 `SM_StockHatch` mutfak yüzü (kaldıraçlı). Bu paketteki `SM_StockHatch_Kasa` aynı geçişin Kasa yüzü: çerçeve, çekmece ön yüzü ve gövdesi. İki yüzün tek çekmece olarak bağlanması kurulumda/kodda.
- **SM_TrashBin, SM_CeilingLight** C1 ile aynı ölçü ve pivot; **SM_IngredientBoard** İstasyon paketindeki tanımla aynı (80 × 5 × 80, akçaağaç çerçeve, 3 × 3 kart). Projede varsa onlar kullanılabilir.

## Kararlar ve varsayımlar

- **Tezgah:** 3,10–6,85 m (plan çizimi), kapaklar C1 modülleri 4 × 70 + 1 × 67 cm.
- **İçecek makinesi:** 3 ince bölme ile 4 bardak yuvası. Rozetler kola #42352E, su #A7CFE6, portakal #F28C1E, misket #74BF4A (plandaki sarı hardal sinyaline çok yakın).
- **Pencereler:** sipariş penceresinde yuva yok, pervazında tarif kitabı soketi. Teslimat penceresinde planındaki gibi 3 paket yuvası (26 × 18 cm).
- **Stok çekmecesi:** Kasa'ya 0,30 m çekilir (plandaki açılma alanı ~0,55 m); Kasa yüzünde kaldıraç yok.
- **Yangın tüpü:** gövde #E8553E (ketçap kırmızısı rezerve); askıda gövde 0,30–0,70 m, pervazın altında.
- **Yığınlar ve tekil parçalar:** bardak ve kapak yığınları statik kaynak; tekil bardak ve kapak yığının birimi olarak eklendi. Dondurma kabı ve içecek dolumu pakette yok.
- **Parçacıklar:** draje renkleri lila, turkuaz, marul yeşili, peynir turuncusu, açık lila; sinyal renkleri (kırmızı, sarı, beyaz, kahve) kullanılmadı.
- **Tavan lambası:** 4 adet (x 1,225 / 3,675 / 6,125 / 8,575; z 1,50); planda yok.

## Açık soru

- **Malzeme panosu iki yerde:** Kasa planı panoyu güney duvarın Kasa yüzüne (3,10–3,90 m) koyuyor, İstasyon paketi aynı duvarın İstasyon yüzüne (3,05–3,85 m). Ya iki pano var ya biri yanlış odada. Kasa yerleşimi Kasa planını izliyor; yükseklik İstasyon'la aynı (merkez 1,70 m).

## Ne test edildi

- 19 GLB yapısal kontrolden geçti (0 sorun): başlık ve chunk'lar, buffer view sınırları ve hizası, accessor sınırları ve min/max, normal birimliği, indeks aralığı, üçgen sargısı (normallerle tutarlı), node ağacı, PNG'ler, palet UV'lerinin hücre ortasında olması ve hiçbirinin boş (pembe) hücreye düşmemesi.
- `onizleme/` render'ları GLB'ler geri yüklenerek alındı: hareketli parçalar sınır konumunda, soketler işaretli; yerleşimde pivot, yön ve konumlar plana oturuyor.
- Khronos glTF Validator bu ortamda kurulamadı (paket sunucusuna erişim yok). Unity'ye içe alma denenmedi.
