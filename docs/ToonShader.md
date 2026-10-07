# CNE Toon — shader, outline ve post-process

Bu doküman `Assets/CNEToon/` altındaki toon görünümünü anlatır: `CNE/Toon` shader'ı, `CNEOutlineFeature` kontur
geçişi, post-process profilleri ve `ToonLookdev` test sahnesi. Eski stilize denemeden (`Assets/StylizedTest/`)
bağımsızdır.

## Sanatçı için: nasıl kullanılır (10 satır)

1. Materyal oluştur, shader olarak **CNE/Toon** seç.
2. **Palet dokusu**na palet PNG'sini ata; modelin UV'leri palette kullanacağı renk karesine oturmalı.
3. Palet dosyalarını yolu `/Palettes/` içeren bir klasöre koy (öneri: `Assets/Art/Palettes`); içe aktarma ayarları
   kendiliğinden yapılır. Maske dokusunun adı `_Prop` ile bitmeli (ör. `Mutfak_Prop.png`).
4. Parlama, emission ya da desen istiyorsan **Özellik maskesi** ata: R = parlama, G = emission, B = desen. Maske
   yoksa bu üçü hiç çalışmaz.
5. Paslanmaz çelik: **Parıltı**'yı aç. Krom: **Matcap**'i aç ve matcap dokusu ata. Neon: **Emission**'ı aç, rengin
   şiddetini 1'in üstüne çıkar (bloom yalnızca bunu parlatır).
6. Karakterlerde **Kenar ışığı**nı aç.
7. AO'yu Blender'da vertex renginin **R** kanalına pişir; shader onu okur (`Vertex AO gücü`).
8. Normal map, metalik, pürüzlülük, AO dokusu **yoktur ve eklenmez** (aşağıda "Değişmez kurallar").
9. Çizgi alacak nesnenin Renderer'ında **Rendering Layer Mask**'e `Outline` ekle (oynanış nesneleri); karakterlerde
   `Outline Silhouette` kullan (yalnızca dış hat). Duvar, zemin, tezgah gibi mimariye ekleme.
10. Denemek için `Assets/Scenes/ToonLookdev.unity` sahnesini aç ve Play'e bas (ya da oyunda ana menü → TOON TEST).

## Değişmez kurallar

Bunlar bozulursa Şef'in kontur görüşü (GDD §4.1.1) ve sos şişelerinin kasıtlı olarak aynı olan silueti (§5.6)
bozulur.

- Shader'ın `DepthOnly` ve `DepthNormals` geçişleri vardır; Şef'in görüşü bunları okur.
- `DepthNormals` **yalnızca geometrik (vertex) normal** yazar. Normal map desteği eklenmez: yüzey ayrıntısı Şef'in
  konturuna sızar.
- Bütün geçişler aynı vertex konumunu kullanır; vertex kaydırma yoktur.
- Metalik, pürüzlülük, normal ve AO **doku** kanalı yoktur.
- Oyun durumu (pişmişlik, sinyal rengi) yalnızca **renkle** anlatılır: `_BaseColor`, renderer başına
  `MaterialPropertyBlock` ile yazılır (`ItemPhaseColoring` bugün böyle yazıyor; shader'la uyumludur).
- Outline sabit bir stildir; oyun durumuna göre nesne nesne açılıp kapanmaz (GDD §4.1.2). Şef'in kamerasında
  çalışmaz ve Şef'in kontur geçişine dokunmaz.

## `CNE/Toon` parametreleri

### Renk

| Parametre | Ne yapar | Varsayılan |
|---|---|---|
| Palet dokusu (`_BaseMap`) | Yüzey renginin okunduğu doku (sRGB). | beyaz |
| Renk (`_BaseColor`) | Palet rengiyle çarpılır. Oyun kodu renderer başına değiştirir. | beyaz |
| Özellik maskesi (`_PropMap`) | Paletle aynı düzende, linear. R parlama, G emission, B desen. | siyah (hiçbiri) |
| Alpha clip / Alpha eşiği | Palet alfasına göre piksel atar. | kapalı / 0,5 |

### Işık

| Parametre | Ne yapar | Varsayılan |
|---|---|---|
| Işık sarması (`_Wrap`) | 0,5 = half-Lambert. Büyüdükçe ışık yüzeyin arkasına sarar. | 0,5 |
| Gölge eşiği (`_ShadowThreshold`) | Işık ile gölge bandının ayrıldığı yer. | 0,5 |
| Geçiş yumuşaklığı (`_ShadowSoftness`) | Bant kenarının genişliği. Küçük = keskin. | 0,03 |
| Düşen gölge kenarı (`_CastShadowSoftness`) | Başka nesnenin düşürdüğü gölgenin kenar yumuşaklığı. Küçük = keskin ama gölge haritasının pikselleri görünür. | 0,15 |
| Ara bant, eşiği, koyuluğu | Üçüncü ton (ışık ile gölge arası). | kapalı / 0,75 / 0,35 |
| Gölge rengi (`_ShadowTint`) | Gölgedeki yüzey bu renge doğru çarpılır. | `#3E4A7A` |
| Gölge gücü (`_ShadowStrength`) | 0 = gölge yok, 1 = tamamen gölge rengi. | 0,55 |
| Dolaylı ışık gücü (`_GIStrength`) | Lightmap, light probe ve ortam ışığının katkısı; basamaksız eklenir. | 0,5 |
| Işık üst sınırı (`_LightClamp`) | Toplam ışık çarpanı bunu geçemez. | 1,2 |

**Gölge kalitesi.** Yüzeyin yönü ve düşen gölge ayrı basamaklanır. Düşen gölgenin temizliği URP asset'indeki gölge
ayarlarına bağlıdır (`PC_RPAsset`: mesafe 18 m, çözünürlük 4096, 2 kademe, normal bias 1) — oyun iç mekân olduğu için
mesafe kısa tutulur, böylece gölge haritasının pikselleri küçülür. Şablon materyallerde gölge eşiği 0,56'dır: bant
sınırı ışığa biraz daha dönük tarafa kayar ve gölge haritasının yüzeyin kendi üstündeki pürüzünü örter.

**Formül.** Gölgedeki renk = `renk × lerp(1, gölge rengi, gölge gücü)`. Gerçek zamanlı gölge aynı banda katılır:
gölgede kalan yüzey ile ışığa dönük olmayan yüzey aynı tonu alır. Ek ışıklar (lamba, neon) aynı basamakla, mesafeyle
zayıflayarak eklenir. Toplam = ana ışık + ek ışıklar + dolaylı ışık × güç, üst sınırla kırpılır, vertex AO ile çarpılır.

**Dikkat — bantlar kaybolursa.** Ana ışık çok güçlüyse ya da ortam ışığı açıksa gölge tarafı da üst sınıra çarpar ve
bantlar görünmez. Önce ışığı düzelt (ana ışık şiddeti 1, koyu ortam ışığı), sonra gerekirse dolaylı ışık gücünü düşür.

**Renk doğruluğu.** Beyaz ana ışık, şiddet 1, dolaylı ışık yok, Tonemapping None iken tam aydınlık yüzeyin ekrandaki
rengi palet hex'iyle aynıdır (ölçüldü: ketçap `#E0262B`, hardal `#F5C518`, mayonez `#FAF6EA`, barbekü `#8C4A22`).

### Parlama

| Parametre | Ne yapar | Varsayılan |
|---|---|---|
| Parıltı | Keskin kenarlı küçük leke; yalnızca aydınlık bantta, maskenin R kanalında. | kapalı |
| Parıltı rengi / boyutu / kenarı | Lekenin rengi, büyüklüğü, kenar yumuşaklığı. | beyaz / 0,05 / 0,005 |
| Matcap, dokusu, gücü | Kameraya göre sabit yansıma dokusu; maskeli yüzeyin rengi yerine geçer. | kapalı / gri / 1 |

### Rim (kenar ışığı)

| Parametre | Ne yapar | Varsayılan |
|---|---|---|
| Kenar ışığı | Siluete yakın yüzeyde basamaklı parlama. | kapalı |
| Renk / Eşik / Yumuşaklık / Güç | Rengi, inceliği (büyük eşik = ince), kenarı, parlaklığı. | beyaz / 0,6 / 0,03 / 0,5 |
| Yukarı ağırlığı | 1 = yalnızca yukarı bakan yüzeylerde güçlü; 0 = her yönde aynı. | 0,5 |

### Emission

| Parametre | Ne yapar | Varsayılan |
|---|---|---|
| Emission / rengi | Kendi ışığını veren yüzey; maskenin G kanalında. HDR şiddet > 1 bloom alır. | kapalı / siyah |
| Palet rengini al (`_EmissionBaseTint`) | 1 = yüzey kendi palet renginde ışır, emission rengi yalnızca şiddeti verir. | 0 |

### Desen (tarama)

| Parametre | Ne yapar | Varsayılan |
|---|---|---|
| Tarama deseni | Gölge bandına düşük kontrastlı çizgiler; maskenin B kanalında. | kapalı |
| Dokusu / Güç / Ölçek | Döşenebilir gri desen, kontrastı, metre başına tekrar (obje uzayında, triplanar). | beyaz / 0,2 / 4 |

### AO

| Parametre | Ne yapar | Varsayılan |
|---|---|---|
| Vertex AO gücü | Vertex renginin R kanalındaki AO'nun etkisi. Vertex rengi yoksa etkisiz. | 1 |

## Palet dokuları

Yolu `/Palettes/` içeren dokular içe aktarılırken (`CNEPaletteImporter`): mipmap kapalı, filtre Bilinear, sıkıştırma
yok, wrap Clamp, sRGB açık. Adı `_Prop` ile biten dosyada sRGB kapalıdır (maske).

## Outline (`CNEOutlineFeature`)

`PC_Renderer`'a eklidir; kör görüş renderer'ında yoktur. Ayarları `Assets/Settings/PC_Renderer.asset` → CNEOutline.

**Nasıl çalışır.** (a) `Outline` ve `Outline Silhouette` rendering layer'larındaki opak nesneler kameranın derinliğine
karşı iki kanallı bir maskeye çizilir (R = tam çizgi, G = yalnızca siluet). (b) Derinlik + normal dokularında kenar bulunur; dış hat ayrıca maskenin kendi sınırından alınır (yalnızca derinliğe
bakılınca kıvrımlı nesnenin çizgisi uzaktan nokta nokta kopuyordu). (c) Çizgi yalnızca maskenin dokunduğu yerde
çizilir: etiketli nesnenin silueti ve iç kırımları çizgi alır, yalnızca mimariye ait kenarlar almaz.

| Ayar | Ne yapar | Varsayılan |
|---|---|---|
| Outline Layer | Tam çizgi alacak rendering layer: kalın dış hat + ince, soluk iç çizgi (eşyalar, öğeler). | Outline |
| Silhouette Layer | Yalnızca dış hat alacak rendering layer (karakterler, müşteriler). Parmak, göz, tuş gibi küçük ayrıntıların iç çizgileri birbirine girip modeli karartır; bu katmanda çizilmezler. | Outline Silhouette |
| Pass Event | Geçişin sırası (saydamlardan ve post-process'ten önce). | AfterRenderingSkybox |
| Outline Color | Çizgi rengi. | `#24233A` |
| Color Blend | 1 = düz çizgi rengi; 0 = nesnenin kendi renginin koyu tonu. | 1 |
| Darken | Color Blend 0'a yaklaşırken sahne renginin çarpıldığı koyuluk. | 0,35 |
| Width Px | Kalınlık, referans yükseklikte piksel. | 2 |
| Reference Height | Kalınlığın tanımlandığı ekran yüksekliği. | 1080 |
| Depth Threshold | Derinlik farkı eşiği (mesafeye oranla). Küçük = daha çok siluet çizgisi. | 0,05 |
| Normal Threshold | Yüzey yönü eşiği. Büyük = yalnız keskin kırımlar. | 0,5 |
| Edge Softness | Kenar geçişinin yumuşaklığı. | 0,5 |
| Fade Start / End | Çizginin solmaya başladığı ve kaybolduğu mesafe (m). | 12 / 25 |
| Inner Width Px | İç çizginin kalınlığı (referans yükseklikte piksel). | 1 |
| Inner Opacity | İç çizginin koyuluğu. 0 = yalnızca dış hat, 1 = dış hat kadar koyu. | 0,5 |
| Inner Fade Start / End | İç çizgilerin solmaya başladığı ve kaybolduğu mesafe (m). | 3 / 7 |

**İki çizgi ağırlığı.** Dış hat (kalın, tam koyu): nesnenin silueti, komşu nesneyle ya da kendi ayrı parçasıyla
(kapak, sepet, kapı kanadı) sınırı, derinlik kademeleri. İç çizgi (ince, soluk, yakında): yüzeyin yön değiştirdiği
kırımlar. İç çizgiler dış hat kadar ağır çizilince sık ayrıntı yüzeyi karartır; hiç çizilmeyince eşya boş kalır.
Yeni asset için yapılacak tek şey Renderer'a `Outline` katmanını vermektir.

**Katman atama.** Renderer → Rendering Layer Mask → `Outline`. Koddan atanıyorsa diğer bitler korunur:
`renderer.renderingLayerMask |= RenderingLayerMask.GetMask("Outline")`.

## Post-process

Profiller *CNE → Post → Create Profiles* menüsüyle üretilir (`Assets/CNEToon/Post/`); var olanın üzerine yazılmaz,
elle düzenlenebilir.

| Profil | İçerik |
|---|---|
| `CNE_Global` | Tonemapping None; Color Adjustments 0 / 0 / 0 (sanatçı ±10 içinde oynatır); Bloom eşik 1,3, şiddet 0,4, saçılma 0,6; diğer efektler etkisiz değerle ezili. |
| `CNE_Global_Neutral` | Aynısı, Tonemapping Neutral (karşılaştırma). |
| `CNE_Debug_Grayscale` | Saturation −100 (gri tonlama testi). |

Görünüm açıkken (`CNELook.Active`) `CNEPostProcessController` global Volume'u kurar ve **varsayılan renderer'la çizen**
oyun kameralarında post-process + SMAA High açar; kapanınca kameralar eski değerlerine döner. Şef'in kamerası kör
görüş renderer'ını kullandığı için dokunulmaz; ayrıca `BlindVisionCamera` kör görüşte post-process'i kapalı tutar.
Odalara göre local volume yoktur; ruh hâli ışıkla kurulur.

Ayarlar: `Assets/CNEToon/Resources/CNELookSettings.asset` (profil, öncelik, AA, debug tuşları).

URP asset (`PC_RPAsset`): HDR açık, Depth Texture açık, MSAA kapalı.

## Debug (yalnızca Editor ve Development Build, görünüm açıkken)

- **F9** — gri tonlama testi (değerlerin renk olmadan okunup okunmadığına bakmak için).
- **F10** — outline aç / kapa.

## Işık kurulumu

- Ana ışık: Directional, **Mixed**, rengi `#FFE9C7`, şiddet 1.
- Lighting Settings: Mixed Lighting → **Baked Indirect**. Direkt ışık realtime ve basamaklı kalır; lightmap yalnızca
  sekme ışığını ve AO'yu taşır.
- Ortam ışığı koyu tutulur (test sahnesinde düz, koyu mavi); açık ortam ışığı bantları yok eder.
- Hareketli nesneler (öğeler, karakterler) light probe'dan beslenir; sahnede Light Probe Group bulunmalı.
- Emission'lı yüzey bake'e katılacaksa materyalde Global Illumination → Baked.

## Test sahnesi (`Assets/Scenes/ToonLookdev.unity`)

*CNE → Lookdev → Build Scene* menüsüyle kodla üretilir (materyaller ve dokular `Assets/CNEToon/Lookdev/`). İçerik:
koyu fon (`#172A3A`) önünde dört sinyal renginde küre ve beyaz küre, tarama, paslanmaz çelik, krom, neon; `Outline`
katmanında olan ve olmayan aynı nesne; bevel'lı ve bevel'sız küp; lamba; Şef kamerası.

Açma: sahneyi editörde açıp Play, ya da oyunda ana menü → **TOON TEST** (çıkış: ESC).

| Tuş | Ne yapar |
|---|---|
| 1 | Ana ışık: sıcak (`#FFE9C7`) ↔ beyaz |
| 2 | Şef kamerası (kontur görüşü) aç / kapa |
| 3 | Tonemapping None ↔ Neutral |
| A / D, W / S | Kamerayı döndür, yaklaştır / uzaklaştır |
| F9 / F10 | Gri test / outline |

## Bilinen sınırlamalar

- Çizgi kalınlığı tam piksel adımlarıyla değişir: 1080p ve 1440p'de 2 piksel, 4K'da 4 piksel.
- Outline maskesi alpha clip'i bilmez: alpha clip'li nesnenin atılan pikselleri de maskeye girer.
- Outline yalnızca opak nesnelere çizilir; saydam nesneler çizginin üstünde kalır.
- `_BaseColor`'ı `MaterialPropertyBlock` ile değişen renderer SRP Batcher'dan düşer (pişen köfte gibi).
- Tarama deseni obje uzayındadır; static batching uygulanan nesnede obje uzayı değiştiği için desen kayar.
- Shader SSAO'yu okumaz (AO vertex renginden gelir).
- Deferred rendering desteklenmez (GBuffer geçişi yok); proje Forward+ kullanır.
