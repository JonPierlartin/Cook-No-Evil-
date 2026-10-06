# Stilize görünüm — DENEME

Peak benzeri stilize (toon) görünümü denemek için. **Kalıcı değildir:** her şey bu klasördedir ve çalışma zamanında
uygulanır; hiçbir sahne, materyal dosyası ya da proje ayarı değişmez. **Kaldırmak için `Assets/StylizedTest`
klasörünü silmek yeterlidir.**

## Nasıl çalışır

- `StylizedLookController` oyun açılınca kendini kurar (sahnede nesnesi yoktur).
- Yerel oyuncunun rolü **Komi ya da Kasiyer** ise ve ayar açıksa: sahnedeki opak URP Lit materyallerini toon
  kopyalarıyla değiştirir, sis + gradient ortam ışığı + sıcak ışık tonu uygular. Şef'e uygulanmaz (kör görüş ayrı
  bir render'dır). Kapatılınca her şey eski hâline döner.
- **Aç/kapa:** ESC → Ayarlar → "GÖRÜNÜM" satırı (düğmeye bastıkça KAPALI → STİLİZE → CNE TOON). Tercih bu makinede saklanır.
- Sonradan doğan nesneler (öğeler, müşteriler, karakterler) yarım saniyede bir taranıp çevrilir; yeni doğan bir nesne
  en çok yarım saniye eski görünümle kalabilir.

## Dosyalar

| Dosya | Ne |
|---|---|
| `StylizedToon.shader` | Toon shader (HLSL, URP). Ana ışık + ek ışıklar + gölge alma/atma + sis. |
| `StylizedRamp.png` | Işık-gölge geçişinin ramp dokusu. |
| `Editor/StylizedRampGenerator.cs` | Ramp'i yeniden üretir: *Cook No Evil → Stilize Deneme → Ramp Dokusu Üret*. |
| `Resources/StylizedLookSettings.asset` | Tüm ayarlar (roller, renkler, sis). Değerler burada değiştirilir. |
| `StylizedOutline.shader` | Kontur çizgileri: derinlik + normal kenarlarına koyu çizgi (Şef'in kör görüşüyle aynı yöntem). |
| `StylizedOutlinePass.cs` | Kontur geçişi; görünüm açıkken her kare kameraya eklenir (renderer asset'ine feature eklenmez). |
| `StylizedLookController.cs` | Görünümü uygular ve geri alır. |

## Ayarlar (`StylizedLookSettings`)

| Ayar | Ne yapar | Başlangıç |
|---|---|---|
| `roles` | Görünümü hangi roller görür. | Komi, Kasiyer |
| `shadowColor` | Gölgede kalan yüzeylerin çarpıldığı renk. Siyah yerine mor/mavi ton stilize hava verir. | (0,45 / 0,40 / 0,62) |
| `ambientStrength` | Ortam ışığının gölge tarafına katkısı. Artınca gölgeler açılır ve gökyüzü rengini alır. | 0,6 |
| `rimColor` | Kenar parlamasının rengi. | sıcak krem |
| `rimIntensity` | Kenar parlamasının şiddeti. 0 = kapalı. | 0,35 |
| `rimPower` | Kenar parlamasının inceliği. Büyük = ince çizgi, küçük = geniş hale. | 3 |
| `outlineEnabled` | Kontur çizgileri açık mı. | açık |
| `outlineColor` | Çizgi rengi (alfa = saydamlık). | neredeyse siyah |
| `outlineThickness` | Çizgi kalınlığı (piksel). | 2 |
| `outlineDepthThreshold` | Derinlik farkı eşiği. Küçük = daha çok çizgi (nesne-arka plan sınırları). | 0,05 |
| `outlineNormalThreshold` | Yüzey yönü eşiği. Küçük = yumuşak kıvrımlar da çizilir; büyük = yalnız keskin köşeler. | 0,4 |
| `outlineThinDistance` | Bu mesafeden (m) uzaktaki çizgiler 1 piksele iner. | 4 |
| `simplifyCharacterOutlines` | Karakterlerde ve müşterilerde yalnızca dış hat çizilir; parmak, göz, tuş gibi küçük ayrıntıların iç çizgileri atlanır (yoksa birbirine girip modeli karartıyor). | açık |
| `overrideEnvironment` | Sis, ortam ışığı ve ışık tonu da değişsin mi (kapalıysa yalnızca shader). | açık |
| `skyColor / equatorColor / groundColor` | Gradient ortam ışığı: üstten, yandan ve alttan gelen renk. | açık mavi / şeftali / mor-gri |
| `fogColor` | Mesafe sisinin rengi. | pembe-şeftali |
| `fogStart / fogEnd` | Sisin başladığı ve tamamen kapattığı mesafe (m). Restoran küçük olduğu için uzak tutuldu. | 7 / 45 |
| `sunTint` | Ana ışığın rengi bununla çarpılır (sıcak ton). | (1 / 0,93 / 0,80) |
| `scanInterval` | Yeni nesnelerin taranma aralığı (sn). | 0,5 |

Shader'da ayrıca `_LitBrightness` (ışık tarafının parlaklığı, 1) vardır. Işığın **şiddeti** bilerek kullanılmaz,
yalnızca tonu alınır: sahnedeki güneş 2 şiddetinde olduğu için renkler yanıyordu.

**Ramp:** geçişin yeri ve yumuşaklığı `StylizedRampGenerator` içindeki `Threshold` (0,48) ve `Softness` (0,16)
sabitleridir. Küçük `Softness` = keskin, çizgi film gibi geçiş; büyük = yumuşak.

## Bilinen sınırlar

- Kontur çizgileri saydam yüzeylerin (cam) arkasındaki geometriye de çizilir; saydam nesnelerin kendisine çizilmez.
- Kontur değerleri (kalınlık, eşik) oyun açılırken okunur; değiştirince görünümü kapatıp açmak gerekir.

- Yalnızca **opak** URP Lit materyaller çevrilir; saydamlar (buzdolabı camı) ve arayüz olduğu gibi kalır.
- Normal map, metalik ve parlaklık kullanılmaz (toon'da düz boyama).
- Post-processing eklenmedi (bloom, vignette, renk ayarı): oyuncu kamerasında post-processing kapalı; açmak Şef'in
  kontur görüşünü de etkilediği için bu denemenin dışında bırakıldı.
- Ayrı bir test sahnesi kurulmadı; görünüm doğrudan oyunda denenir.
- Build'de sis çalışmazsa: Project Settings → Graphics → "Fog Modes" kullanılmayan varyantları ayıklıyor olabilir.
