# Cook No Evil! · İstasyon (Komi'nin odası) modelleri

*8 Ekim 2026 · 12 asset + yerleşim + mimari referans · GLB · kapsam: İstasyon planındaki nesneler (karakterler ve tekil item'lar hariç)*

## Paket: `CNE_Istasyon_Modeller.zip`

| Klasör / dosya | İçerik |
|---|---|
| `glb/` | Her asset ayrı `.glb`: hiyerarşi, pivot, hareketli parçalar, soketler, materyaller, gömülü dokular. `Istasyon_Yerlesim.glb`: 15 örnek plandaki yerinde + 4,00 × 4,45 referans zemin. `ARCH_Istasyon.glb`: zemin, tavan, 4 duvar (pencere kaba boşluklarıyla), süpürgelik; düz renk, UV yok. |
| `textures/` | Palet (256 px, 16 px hücre, S10·K3–K4 eklendi), meşe ve akçaağaç damar dokuları (1 tekrar = 0,5 m). |
| `manifest.json` | Ölçüler, soketler, hareketli parçalar, kontur/gölge bayrakları, yerleşim dönüşümleri (plan ve Unity). |
| `onizleme/` | Doğrudan bu GLB'lerden toon render: asset sayfası ve yerleşim. |
| `istasyon-sahne-kurulum-prompt.md` | Unity'de sahneyi kurdurmak için Claude Code prompt'u (projede `claude/istasyon-sahne-kurulum-prompt.md`). |

## Kullanım

1. **Unity (önerilen bu paket için):** glTFast (`com.unity.cloud.gltfast`) ile `glb/` klasörünü içe alın, sonra sahneyi `istasyon-sahne-kurulum-prompt.md` ile Claude Code'a kurdurun. Materyalleri projenin toon materyalleriyle değiştirin (eşleme tablosu prompt'ta).
2. **Blender:** File → Import → glTF 2.0; ölçek 1, Y yukarı otomatik dönüşür.

glTFast X eksenini ters çevirir: plan (x; y; z) → Unity (−x; y; z), Y dönüşleri işaret değiştirir; şekil aynı kalır. glTFast node `extras`'ını içe almayabilir; bayraklar ayrıca `manifest.json`'da ve aşağıdaki tabloda.

## Eksen, birim, pivot

- Birim metre. Y yukarı, asset'in önü +Z, X genişlik (C1 paketiyle aynı).
- Yerleşim ekseni: X doğu (0 = İstasyon batı duvarı iç yüzü), Z güney (0 = Kasa duvarı iç yüzü). C1 mutfak kökü (+4,20; 0; 0), Kasa kökü (0; 0; −3,20).
- Pivotlar: tezgahlar taban arka kenar ortası; tepsi, deste, kutu, çöp taban ortası; pano arka yüz ortası; pencereler net açıklığın alt kenarı ortası, yuva tarafındaki duvar yüzü (pivot = pervaz üstü, 1,00 m); tavan lambası üst yüz ortası.

## Uygulanan kurallar

- **Tek parça kasa + söve:** pencerelerde krom kasa ve söve kaplaması duvarı boydan geçen tek mesh (`Kasa_Sove`). Kasa önizlemesindeki kesik kontur bulgusunun çözümü. Söve keskin (r 0), bilinçli sert çizgi.
- **Tabla:** laminat nane üst + krom kenar bandı tek kutu, iki primitive (üst yüz `MI_Palette` laminat, yanlar `MI_Palette_Chrome`); bevel r 0,6 cm. Pervazlar da aynı yapıda.
- **Bevel:** gövdeler r 2 cm, kapaklar r 0,6 cm, plastikler 4 segment yumuşak; 3 mm altı bevel yalnızca küçük kulp ayaklarında. Normaller Harden + Weighted Normal eşdeğeri (düz yüz düz, bevel yumuşak).
- **Kontur:** pencereler, tezgahlar, tepsi, deste, kutular, çöp Evet; pano ve tavan lambası Hayır (Kasa setindeki karar: pano konturda değil).
- **Gölge:** zemine oturanlar Evet; duvara ve tavana monteler Hayır.
- **Renk:** yalnızca palet; UV'ler hücre ortalarında, boş hücre pembe. Ahşap dokulu parçalarda kutu izdüşümü UV (0,5 m tekrar).

**Palete eklenen hücreler (S10):** K3 #6F4E37 dana köfte pişmiş, K4 #F2B23A patates pişmiş (pano ikonları için). Tavuk pişmiş #D9A35B, S10·K0 (yağ) ile aynı renk; o hücre kullanıldı.

## Materyaller

| Materyal | Nerede |
|---|---|
| MI_Palette | Düz palet renkleri (laminat, plastik, lila, kraft, yiyecek ikonları, pano yüzü) |
| MI_Palette_Steel | Paslanmaz gövde, etek, yuva tepsileri |
| MI_Palette_Chrome | Kulp, tabla ve pervaz kenar bandı, pencere kasası + söve, lamba halkası |
| MI_Palette_Emission | Tavan lambası kubbesi (lamba beyazı S10·K2, emissive strength 2,5) |
| MI_Wood_Mese | Tezgah kapakları |
| MI_Wood_Akcaagac | Pano çerçevesi (yeni) |
| MI_Arch_* | Mimari referans: düz renk, UV yok |

## Hareketli parçalar ve soketler

| Asset | Node | Pivot | Hareket (Unity yerel) |
|---|---|---|---|
| SM_BurgerBox | Lid | Kapağın arka kenarı | X −110° açılır |
| SM_TrashBin | Lid | Kapağın arka kenarı | X −75° (pedal) |

Tezgah kapakları (`Door_1…`) ayrı node, menteşe (sol) kenarında pivotlu; C1'deki ada kapakları gibi statik. Pano kartları `Card_1_Ekmek` … `Card_9_Patates` ayrı node; içerik yer tutucu.

Soketler `Socket_` önekli boş node'lar, yönleri asset'le aynı: Tray_1/2 ve BagStack (paketleme), BoxStack_1–4 ve Work (kutulama), Pack (tepsi), Take (deste ve yığınlar), Content (kutu), Drop (çöp), Slot_1–3 (Kasa penceresi, iki yönlü), Burger_1–3 ve Side_1–3 (mutfak penceresi), Light (lamba).

## Asset'ler

| Dosya | Ad | G × D × Y (cm) | Üçgen | Kontur | Gölge | Soketler | Hareketli |
|---|---|---|---|---|---|---|---|
| SM_PackingStation | Paketleme istasyonu | 250 × 85 × 100 | 1.524 | Evet | Evet | Tray_1, Tray_2, BagStack | — |
| SM_BoxingStation | Burger kutulama | 125 × 85 × 100 | 1.092 | Evet | Evet | BoxStack_1–4, Work | — |
| SM_PackingTray | Paketleme tepsisi | 71 × 58 × 4 | 392 | Evet | Evet | Pack | — |
| SM_BagStack | Kese kağıdı destesi (kaynak) | 50,5 × 45,2 × 8,3 | 3.168 | Evet | Evet | Take | — |
| SM_BurgerBox | Burger kutusu (tekil) | 14,1 × 16,5 × 7,4 | 708 | Evet | Evet | Content | Lid |
| SM_BurgerBoxStack3 | Kutu yığını ×3 (kaynak) | 14,5 × 16,7 × 22,4 | 2.124 | Evet | Evet | Take | — |
| SM_BurgerBoxStack4 | Kutu yığını ×4 (kaynak) | 14,7 × 17,1 × 29,9 | 2.832 | Evet | Evet | Take | — |
| SM_TrashBin | Çöp kovası ø60 | 60 × 64 × 76,1 | 1.668 | Evet | Evet | Drop | Lid |
| SM_IngredientBoard | Malzeme panosu | 80 × 5 × 80 | 7.324 | Hayır | Hayır | — | — |
| SM_StationWindow_Kasa | Kasa penceresi | 262 × 65 × 116,1 | 348 | Evet | Hayır | Slot_1–3 | — |
| SM_StationWindow_Kitchen | Mutfak penceresi | 262 × 58 × 116,1 | 420 | Evet | Hayır | Burger_1–3, Side_1–3 | — |
| SM_CeilingLight | Tavan lambası | 39,2 × 39,2 × 8,5 | 1.392 | Hayır | Hayır | Light | — |

Yerleşim GLB'si 15 örnek, 29.026 üçgen (örneklenmiş mesh'ler bir kez saklanır).

## Kararlar ve varsayımlar

- **Mutfak penceresi:** C1 `SM_StationWindow`'un yerine geçer. Plan gereği İstasyon tarafındaki 10 cm pervaz kaldırıldı (duvar yüzünde 5 mm dudak). Pivot ve yuvalar aynı.
- **Kasa penceresi:** İstasyon planındaki gibi 3 iki yönlü yuva (61 × 19 cm). Kasa önizlemesi bu pencereyi yuvasız varsaymıştı; İstasyon planı esas alındı.
- **Pencere ölçüsü:** net açıklık 2,50 × 0,95, pervaz üstü 1,00 (C1 tanımı); kaba boşluk 2,53 × 1,011, alt kenar 0,954.
- **Tezgahlar:** C1 birleştirme adasıyla aynı aile. Kapaklar 76 cm (paketleme, 3 adet) ve 53 cm (kutulama, 2 adet); C1'deki 67/70/112 cm modüllere sığmadı.
- **Kese kağıdı ve kutu:** deste ve yığınlar statik kaynak; tekil kese kağıdı (3 hâl) item, pakette yok. SM_BurgerBox tekil kutu olarak da kullanılabilir.
- **Tavan lambası:** 2 adet (X 2,00; Z 1,30 ve 3,15); planda yok. SM_TrashBin ve SM_CeilingLight C1'dekilerle aynı ölçü ve pivot; projede varsa onlar kullanılabilir.
- **Malzeme panosu:** 3 × 3 kart (ekmek, dana köfte, tavuk köfte, peynir, marul, domates, soğan, turşu, patates); içerik yer tutucu.
- **Mimari:** ARCH yalnızca referans. Ortak duvarların komşu yüzleri yer tutucu renkte.

## Ne test edildi

- 14 GLB yapısal kontrolden geçti (0 sorun): başlık ve chunk'lar, accessor sınırları ve min/max, normal birimliği, indeks aralığı, üçgen sargısı (normallerle tutarlı), node ağacı, PNG'ler, palet UV'lerinin hiçbiri boş (pembe) hücreye düşmüyor.
- `onizleme/` render'ları bu GLB'ler yüklenerek alındı; yerleşimde pivot, yön ve konumlar plana oturuyor.
- Unity'ye içe alma denenmedi.
