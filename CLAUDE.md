# Cook No Evil! — Claude Code Proje Hafızası

Bu dosya **mühendislik gerçeğini** tanımlar: kodlama kuralları, öğrenilmiş teknik tuzaklar,
mevcut kod durumu ve GDD'den doğan bağlayıcı teknik kısıtlar.

## ⚠ Tek Doğruluk Kaynağı Kuralı

- **Tasarım gerçeği = `docs/GDD.md`.** Oyunun ne olduğu, mekaniklerin nasıl çalıştığı, dengeler,
  roller, akışlar — hepsi orada. **Bu dosya tasarımı ASLA tekrar etmez**, yalnızca referans verir.
- **Mühendislik gerçeği = bu dosya.** Kod konvansiyonları, paket sürümleri, bilinen tuzaklar,
  mevcut sınıf yapısı.
- **Çelişki durumunda `docs/GDD.md` esastır** ve kullanıcıya sorulur.
- **`docs/archive/` altındaki hiçbir dosya kaynak değildir.** Oradaki eski GDD/CLAUDE kopyaları ve
  tarihli kod envanteri dökümleri bayattır; kod gerçeği repodaki koddur.
- *Neden bu kural var:* Proje geçmişinde tasarım iki ayrı dosyada (`Cook_No_Evil_GDD_Spec.md` ve
  `CLAUDE.md`) tekrarlanmıştı; biri bayatladı ve ikisi çelişti. O dosyalar **arşivlendi**, artık
  kaynak değildir. Tasarımı buraya kopyalama.

---

## 📋 Çalışma Protokolü (bağlayıcı, her oturumda geçerli)

**Tek adım kuralı.** Kullanıcı tek seferde tek adım verir ve her adımın sonunda madde madde bir
"Kabul kriteri" listesi bulunur. Verilen adımın **dışına çıkılmaz**; sıradaki adım tahmin edilip
önden yazılmaz. "Hazır girmişken şunu da yapayım" yoktur.

**Raporlama.** Adım bitince durulur ve raporlanır: kabul kriterleri **tek tek** işaretlenir. Test
edilemeyen bir madde için "test edemedim, sebebi şu" yazılır — "muhtemelen çalışıyor" yazılmaz.
Bir adımın 1-2 saatten uzun süreceği anlaşılırsa kullanıcıya söylenir ve bölünür.

**Test iş bölümü.** Kabul kriterleri prompt'ta iki etiketle ayrı yazılır:
- **[Kod]** — derleme, `git diff`, kod taraması, Inspector alanlarının varlığı. **Claude Code doğrular.**
- **[Oyun]** — oyun içinde gözlemlenen her şey. **Kullanıcı** Multiplayer Play Mode'da doğrular.

Claude Code, açıkça istenmedikçe **MPPM'i çalıştırmaz, oyuncu sürmez, ekran görüntüsü almaz,
bilgisayar kontrolü kullanmaz.** [Oyun] kriterlerini raporda "⏳ kullanıcı testi bekliyor" diye
işaretler. *Gerekçe:* otomatik oyun içi test bir adımın token maliyetinin büyük kısmını oluşturuyor —
her tool sonucu bağlama ekleniyor, ekran görüntüleri en pahalı içerik türü — ve kullanıcının günlük
kullanım hakkını tüketiyor (20 Eyl 2026: 5 saatlik hak ~2 saatte bitti). Aynı test kullanıcı için
birkaç dakikadır.
**İstisna — teşhis:** kök nedeni bilinmeyen bir bug'da, kullanıcı **açıkça isterse**, Claude Code
ölçüm/probe çalıştırabilir (Adım 1.7'deki spawn yarışı kod okumayla bulunamazdı). Bu durumda da
ekran görüntüsü yerine log/kod tabanlı ölçüm tercih edilir.

**Boşluk doldurma yasağı.** `docs/GDD.md`'de cevabı olmayan bir tasarım kararıyla karşılaşılırsa
**durulur ve sorulur**; varsayım yapılmaz. Bu projede kodu Claude Code yazdığı için belirsiz kalan
her tanım doğrudan hatalı implementasyona dönüşür — **soru sormanın maliyeti, yanlış kod yazmanın
maliyetinden kat kat düşüktür.** Aşağıdaki "Zorunlu Karar Alma Protokolü" bağlayıcıdır.

**Parametreyi karara çevirme yasağı.** Bir şey `docs/GDD.md` §11.9'da `LevelConfig` alanı olarak
listelenmişse, o bir **seviye tasarımı parametresidir**, cevaplanacak bir tasarım sorusu değildir.
"Bu seviyede siparişler sabit mi randomize mi?", "kaç kanal açık olacak?", "garnitür numaraları
değişecek mi?" gibi sorularda doğru cevap bir değer seçmek değil, **ikisini de destekleyen veri
odaklı yapıyı kurmaktır.** Böyle bir soru sorulacaksa "hangi değer?" diye değil, "bu alan
`LevelConfig`'te şu isimle duruyor, doğru mu?" diye sorulur.

**Geri bildirim kuralı.** Yeni bir etkileşim veya mekanik yazılırken görsel/işitsel geri bildirimi
**aynı adımda** yazılır. Geri bildirimi olmayan mekanik test edilemez.

**Planlama dosyası.** `docs/PLAN.md` haftalık sıralama, saat tahminleri ve takvim içerir.
**Claude Code için bir talimat dosyası değildir** — oradaki sıraya bakılarak önden iş yapılmaz.
Bağlayıcı olan, kullanıcının o an verdiği adımdır.

---

## 🎯 Faz 0 Kapsam Sınırı (son tarih: 11 Ekim 2026)

Şu anki faz: **Faz 0 — DeepJam başvuru dikey dilimi.** Kapsamın tasarım tanımı `docs/GDD.md`
§11.2'dedir.

**Kritik ayrım — içerik kapsamı ≠ sistem kapsamı.** Faz 0 listesi hangi **içeriğin** üretileceğini
söyler; hangi **sistemin** yazılacağını değil. Bir içerik Faz 0 dışındaysa ama sistemi Faz 0
içindeyse (örn. sinyal çarkının 5 kategoriyi desteklemesi), sistem **veri odaklı yazılır ve içerik
`LevelConfig`'te kapalı gelir.** Sistemi de kapsam dışı olanlar hiç yazılmaz.

**Sistemi de kapsam dışı — hiç yazılmayacak:**
yangın olayı (tüp, kapı, alarm, söndürme, ızgara kilidi) · intercom · malzeme stok sistemi ·
yıldız sistemi · seviye seçim haritası · lobide rol seçimi · Şef→Komi gibberish sesi ·
final sanat ve ses geçişi.

**Yalnızca içeriği kapsam dışı — sistemi veri odaklı yazılır, içerik kapalı gelir:**
Şekil ve Vücut Bölgesi iletişim kanalları (animasyonlar ve değerler Faz 0'da üretilmez, çark
altyapısı 5 kategoriyi destekler) · Patates/Ekstra/İçecek/Dondurma kategorileri (üretim makineleri
Faz 0'da yapılmaz; `LevelConfig`'teki "aktif makineler" alanı tanımlı kalır) · tarif kitapçığının
tüm varyantları (kabuk ve şablon yazılır, sayfalar açık varyant listesinden türetilir).

Bir şeyin hangi tarafa düştüğünden emin değilsen **kod yazma**, kullanıcıya sor.

**Faz 0'da yanma var, yangın yok.** Et `Çiğ → Pişmiş → Yanmış` fazlarından geçer (§5.2.1). Faz 0'da
yanmış et **söndürme gerekmeden alınabilir** ve çöpe atılır; alarm/kapı/tüp/ızgara kilidi yoktur.
GDD §5.3.2'deki "yanmış et söndürülmeden alınamaz" kuralı Faz 1'den itibaren geçerlidir.

---

## Proje Bilgileri

- **Oyun Adı:** Cook No Evil!
- **Tür:** 3 Oyunculu Asimetrik Co-op / Cooking
- **Kamera:** First-person (tüm roller)
- **Unity Sürümü:** 6000.5.7f1 — projede KESİNLİKLE bu sürüm baz alınır.
- **Teknoloji Yığını:**
  - Networking: Netcode for GameObjects (NGO) **2.13.1**
  - Steam: Facepunch.Steamworks + `com.community.netcode.transport.facepunch`
  - Transport: Steam Datagram Relay (SDR); local testte Unity Transport/UDP fallback
  - Ses: Facepunch.Steamworks Voice (AudioSource üzerinden rol bazlı işleme)
  - Render Pipeline: URP 17.5.0
  - Input: Unity Input System (New) 1.20.0
  - Localization: com.unity.localization 1.5.12

---

## Zorunlu Karar Alma Protokolü (Kapsamı Sınırlandırılmış)

İki veya daha fazla makul seçenek arasında karar verilmesi gereken aşağıdaki konularda
**kendi başına seçim yapma** — seçenekleri özetleyip kullanıcıya sor, açık onay bekle:

- Paket/kütüphane seçimi veya sürümü (örn. transport paketi uyumsuzluğunda alternatif seçimi)
- Ana mimari yaklaşım (bir sistemin nasıl senkronize edileceği, hangi network deseni kullanılacağı)
- GDD'de tanımlanmamış yeni bir oyun mekaniği veya kural
- GDD'de belirtilenle çelişen ya da GDD'de hiç yer almayan bir tasarım kararı
- Geri dönüşü zor/maliyetli olan her türlü yapısal seçim

**Sormaya gerek olmayan (kendi mühendislik muhakemesini kullan):** değişken/fonksiyon/sınıf
isimlendirmesi, kod formatlama/stil, dosya/klasör içi küçük organizasyon detayları, yorum satırı
yazma şekli, GDD'de zaten net tanımlanmış bir kararın uygulanma detayı.

Belirsizlik durumunda kategoriden emin olunmasa bile sormak tercih edilir — ama kapsam dışı,
tersine çevrilebilir, önemsiz konularda ilerleme durdurulmaz.

---

## Genel Kodlama Kuralı

Yazılan tüm kodlar SOLID prensiplerine uygun olacak şekilde yazılır.

## Genel Geliştirme Disiplini

- **Seviyeden seviyeye değişebilecek hiçbir değer koda gömülmez.** Sipariş modu (sabit/randomize),
  açık kanal sayısı, kanal başına değer sayısı, açık varyant listesi, kitapçık sayfa sayısı,
  garnitür/protein eşleşmeleri, sos pompa dizilimi, stoklar, süre çarpanı — hepsi `LevelConfig`
  üzerinden gelir (K8, GDD §11.9). Kod bu değerlerin hiçbirini varsaymaz; N kategori, M değer,
  K sayfa mantığıyla yazılır. *Bu kural, "Faz 0'da nasılsa 3 kanal var" gibi geçici sadeleştirmeleri
  de kapsar — geçici sanılan sabitler sonradan saatlerce iş çıkarır.*
- Hiçbir script içinde over-engineering yapılmaz — ihtiyaç duyulmayan soyutlama, esneklik veya
  gelecekte-lazım-olabilir kod yazılmaz. *(Yukarıdaki veri odaklılık kuralıyla çelişmez: GDD §11.9
  bir alanı zaten tanımlıyorsa o esneklik "gelecekte lazım olabilir" değil, spesifikasyondur.)*
- Mümkün olduğunda hazır Unity Engine feature'ları kullanılır (Animator, Cinemachine, vb.) —
  bunların elle yeniden yazılmış karşılıkları üretilmez.
- Hiçbir problem için "play-around" (geçici, sorunun kök nedenini çözmeyen dolanma) yapılmaz;
  yazılan kod ölçeklenebilir, modüler ve Editor üzerinden (Inspector'dan) kullanılabilir olur.
  Bir bug UI katmanında gizlenerek "düzeltilmez".
- **Geri bildirimi olmayan mekanik test edilemez.** Yeni bir etkileşim fiili yazılırken
  görsel/işitsel geri bildirimi aynı adımda yazılır. *(18 Eyl 2026: "build'de çalışmıyor" diye
  raporlanan bir bug'ın aslında var olmadığı, yalnızca görünmediği anlaşıldı — bir haftalık
  teşhis kaybı.)*
- Veri/konfigürasyon saklamak için JSON yerine ScriptableObject kullanılır.
- Vertical Slice kapsamı GDD'de tanımlanandan fazla genişletilmez.

## 2.4 Güvenlik ve Ekip Çalışması Kuralları

- Hiçbir API Key/token/şifre C# scriptlerine hardcode edilmez.
- Hassas veriler `Assets/LocalSecrets` klasöründeki dosyalardan okunur.
- `.gitignore`'a `Assets/LocalSecrets/` ve `Assets/LocalSecrets.meta` eklenir; bu klasör git
  geçmişine girmişse `git rm -r --cached Assets/LocalSecrets/` ile temizlenir.
- Cloud/secret scriptleri ayar dosyasını bulamazsa `NullReferenceException` fırlatmaz; konsola
  uyarı basıp oyunu normal akışında devam ettirir.

---

## Sürüm Kontrolü ve Build

**Commit mesajı biçimi:**

```
<Kapsam>: <ne yapıldığı — Türkçe, emir kipi, küçük harfle başlar>

<gövde: ne değişti, hangi kabul kriterleri kapandı>
```

Kapsam etiketleri:

| Etiket | Ne zaman |
|---|---|
| `Faz 0 / Adım N` | `docs/PLAN.md`'deki numaralı adım |
| `Temizlik` | Temizlik Borcu maddesi |
| `Doküman` | `CLAUDE.md` / `docs/GDD.md` / `docs/PLAN.md` güncellemesi |
| `Düzeltme` | Plan dışı bug düzeltmesi |

**Kurallar:**
- Başlığa **dosya/sınıf adı değil, iş birimi** yazılır. Bir adım birden fazla dosyaya dokunur;
  dosya adına göre etiketlenirse aynı işin commit'leri geçmişte birbirinden kopar.
- Başlık 72 karakteri geçmez.
- Bir adım mümkünse tek commit'te toplanır.
- Gövdede hangi kabul kriterlerinin kapandığı, hangilerinin build testi beklediği yazılır.

Örnek:

```
Faz 0 / Adım 1: etkileşimi sunucu otoritesine al ve çağırana bağla

- İstemci yalnızca hedefin NetworkObjectReference'ını gönderiyor
- Sunucu mesafe + yatay yön doğruluyor; başarısızlıkta sessiz red
- Geri bildirim ClientRpcParams ile yalnızca çağırana
- Kabul kriteri 8 ve 9 kapandı; 1-7 ve 10 build testi bekliyor
```

**Commit zamanlaması:** Bir adımın kodu tamamlanıp **derlendiğinde** commit atılır — kabul
kriterlerinin build testinde doğrulanması beklenmez. Commit gövdesinde hangi kriterin kapandığı,
hangisinin test beklediği yazılır. Adım yarım bırakılıyorsa (derlenmiyor, tutarsız durumda)
commit atılmaz.

**Push:** Kullanıcı açıkça istemeden push yapılmaz.

**Git'e girmeyecekler:** `Builds/` (build çıktıları ve zip'ler), `Assets/LocalSecrets/` ve
`Assets/LocalSecrets.meta`, standart Unity klasörleri (`Library/`, `Temp/`, `Obj/`, `Logs/`,
`UserSettings/`). Bunlar `.gitignore`'da bulunmalıdır.

**Build çıktısı:** Build alındığında `C:\UnityProjects\Cook No Evil!\Builds` altına **hem klasör
hem `.zip`** olarak alınır. İkisi de aynı adı taşır: `CookNoEvil_<YYYY-AA-GG>_Adim<N>`
(örn. `CookNoEvil_2026-09-24_Adim1` ve `CookNoEvil_2026-09-24_Adim1.zip`).

---

## 🔴 GDD'den Doğan Bağlayıcı Teknik Kısıtlar

Bunlar tasarım kararlarının **implementasyon karşılıklarıdır**. Yanlış uygulanırsa mekanik
sessizce çöker ve fark edilmesi zor olur. Her biri GDD'deki ilgili bölüme referanslıdır.

### K1 — Round timer YOKTUR
Kodda round/seviye geri sayımı **olmamalıdır**. Zaman baskısı yalnızca **sipariş başına** işler.
Seviye, önceden belirlenmiş müşteri sayısı tamamlanınca biter. *(GDD §3.4)*

### K2 — Şef'in kontur render'ı geometri tabanlı olmalıdır
Kenarlar **depth + normal** tamponlarından üretilir, **renk tamponundan ASLA**. Renk tabanlı bir
Sobel filtresi kullanılırsa yüzeye basılı desenler/etiketler kenar olarak görünür ve Şef nesneleri
yüzey deseninden ayırt etmeye başlar — sos konum bağımlılığı çöker. *(GDD §4.1.1, §5.6)*

**Tek istisna — duvar hata sayacı (§7.1.2).** Yanan X, kontur pass'ine bir oyun durumuna göre dahil
edilir. Bu, kontur pass'ine dahil olma kararının bir oyun durumu tarafından sürüldüğü **tek yerdir**;
emsal değildir. Başka hiçbir nesne için "Şef bunu da görsün" diye bu yola başvurulmaz.

**Uygulama (Adım 9, 21 Eyl 2026):** URP'nin hazır `FullScreenPassRendererFeature`'ı + özel HLSL
(`BlindVisionOutline.shader`), Requirements = Depth + Normal, Fetch Color Buffer **kapalı**, Render
Graph uyumlu. Şef'e özel: `PC_RPAsset`'te ikinci renderer (`BlindVision_Renderer`, index 1);
`BlindVisionCamera` yerel rol Şef ise kameranın renderer'ını değiştirir. Desenli yüzey testi
(`KonturTest_DesenliYuzey`) geçti.

**Doğrulanmış risk — yarı saydam nesneler Şef'te görünmez.** Yarı saydam materyaller
derinlik/normal tamponuna yazmaz ve kontur geçişi ekranın tamamını yeniden basar; yeşil
yerleştirme önizlemesi Şef görüşünde **0 piksel** üretti (ölçüldü). Şef'in görmesi gereken yarı
saydam her şey için **ayrı bir çizim geçişi** gerekir.

**Şef önizlemesi (Adım 9b, 21 Eyl 2026):** `BlindVision_Renderer`'da iki hazır Render Objects
feature, `AfterRenderingPostProcessing`, katman filtresi `PlacementPreview` (katman 9): (1) maske
geçişi siluet stencil'e yazar, (2) halka geçişi (`BlindVisionPreviewOutline.shader`) geometriyi
ekran uzayında genişletip stencil'in dolu olduğu yeri atlar → dolgusuz, nabız gibi atan beyaz halka.
Önizleme katmanı (9) etkileşim raycast maskesinde (8) **değildir** ve olmamalıdır. Halka
`ZTest Always` çiziyordu — "önizleme yalnızca baktığın hedefte çıkıyor, sorun değil" diye kabul edilmişti; **yanlıştı** (21 Eyl 2026: Şef ile tezgah arasına giren oyuncunun üstünden görünüyordu). **Düzeltme `2a5690d`:** maske ve halka geçişleri artık `ZTest LEqual`; önündeki nesnenin arkasında kalan kısım çizilmez (kısmi örtme dahil). Derinlik payı `_DepthBias` (metre, geometriyi kameraya kaydırır) `BlindVisionPreviewOutline.mat`'ta, değer **0,25** — **K2d'de (`f241530`) pivot düzelince 0,02'ye indirildi** (0,01'den itibaren piksel sayısı sabit) — ölçümle seçildi: 0,1'de tezgah yüzeyi halkanın ~%19'unu kesiyordu, 1,0'da engelin üstünden sızma başlıyor. *Bu pay kısmen yer tutucu görsellerin tezgaha yarı gömülü durmasını (pivot borcu) telafi ediyor; pivot düzelince küçültülebilir.* Derinlik testi RenderTexture'lı kamerada doğrulandı; ekrana çizen oyun kamerasında sorun çıkarsa yedek yol derinlik dokusundan örneklemedir.

**Yuvadaki ince nesne (K2d, 22 Eyl 2026):** Şef'te tezgahtaki ekmeğin "kase" gibi yarım görünmesinin
tek sebebi **nesnenin yarısının tezgaha gömülü olmasıydı** (merkez pivot); kontur eşikleri
değiştirilmedi. **Kural:** öğe görseli yuvada yüzeyin *üstüne* oturmalıdır (görsel prefab kökü
tabanda) — gömülü nesnenin konturu kırpılır.

**Şef'in görebilmesi için nesne materyalinin `DepthNormals` geçişi olmalı.** URP Lit/Unlit'te var;
özel shader kullanılırsa ve bu geçiş yoksa nesne Şef'in dünyasında **hiç görünmez**. Artist'in
gerçek materyallerinde kontrol edilir.

**Post-processing:** kontur `BeforeRenderingPostProcessing`'de çiziliyor. Oyuncu kamerasında
post-processing açılırsa tonemapping beyazı griye çekebilir — o durumda enjeksiyon noktası
değiştirilir.

### K3 — Körlük/sağırlık ayarlarla telafi edilemez
Şef'in körlüğü **shader** olarak uygulanır, parlaklık/gamma filtresi olarak değil (yoksa oyuncu
parlaklığı açıp görür). Komi'nin sağırlığı ana ses seviyesinden bağımsız uygulanır. *(GDD §8.3)*

### K4 — Ses mekânsaldır
Ses mesafeye göre zayıflar. Kasa↔İstasyon ve İstasyon↔Mutfak arasında geçer; Kasa↔Mutfak
arasında pratikte anlaşılmaz (intercom gerekir). Global/oda-bağımsız VoIP **yanlıştır** — oyunun
temel kuralını deler. Rol bazlı ses işleme **konuşan × dinleyen** çifti üzerinden yapılır; yalnızca
dinleyicinin rolüne bakmak yetmez (GDD §10.4 matrisi). *(GDD §10.4, §5.1)*

### K5 — İlerleme verisi nesnenin üstünde durur, makinede değil
Pişme/dolum/söndürme ilerlemesi **pişen/dolan/sönen nesnede** tutulur. Yarıda alınıp geri konan
ürün kaldığı yerden devam eder. Tek bir ortak temel sınıf (sunucu sahipli durum enum'u + sunucu
sahipli progress float + opsiyonel decay) ızgara, fritöz, içecek, dondurma ve yangın söndürme
için ortaklaşa kullanılmalıdır. *(GDD §5.2.1, §6.7.1, §5.2.3)*

### K6 — Tüm etkileşim durumu sunucu sahiplidir
Doluluk/pişme miktarı `NetworkVariable`, yalnızca sunucu yazar. Basılı-tutma girdileri her frame
gönderilmez: istemci `Start...ServerRpc()` / `Stop...ServerRpc()` gönderir, **zamanlayıcıyı sunucu
işletir**. Tamamlanma/otomatik-bırakma kararını **sunucu** verir. Doğrulamalar (boş bardak
paketlenemez vb.) sunucu tarafında yapılır, UI kontrolüyle değil.

**Etkileşimin sonucuna sunucu karar verir, istemci değil.** İstemci yalnızca "etkileşmek istiyorum"
niyetini ve hedefini gönderir; geçerlilik (hedefe bakılıyor mu, menzil içinde mi, nesne müsait mi)
sunucuda doğrulanır. İstemcinin hesapladığı bir sonucu sunucuya rapor edip sunucunun bunu kabul
etmesi K6 ihlalidir ve hile açığıdır.

### K7 — NetworkObject parenting kuralları
Bardağı makineye takma gibi işlemler `NetworkObject.TrySetParent()` ile yapılır ve:
- **Sunucuda** çalıştırılmalıdır (istemci RPC gönderir)
- **Parent'ın kendisi de bir NetworkObject olmalıdır** (yuva noktası düz Transform olamaz)
- Nesne önce **spawn** edilmiş olmalıdır
- Geçersiz denemeler **sessizce geri alınır** — hata vermez, teşhisi zordur
- **`TrySetParent` `true` döndüğü hâlde parent geri alınmış olabilir** (NGO 2.13.1 kaynağı:
  `OnTransformParentChanged` geçersiz parent'ı geri alırken dönüş değeri `true` kalabiliyor).
  Bu yüzden `ItemMover` dönüş değerine ek olarak **gerçek `transform.parent`'ı da denetler.** Yeni
  bir parent işlemi yazılırsa aynı çift kontrol yapılır. *(Adım 4.2, 22 Eyl 2026)*

### K8 — Tek `LevelConfig` kaynağı
Seviyeden seviyeye değişen **her şey** tek bir ScriptableObject'te tanımlanır (müşteri sayısı,
sabır, yedek havuz, sipariş slotları ve sabit/randomize modu, açık varyantlar, aktif makineler,
sinyal çarkında açık kategoriler ve değerleri, kitapçıkta görünen açılımlar, garnitür numaraları,
protein yönleri, sos pompası konumları, stoklar, süre çarpanı, kontrol ipuçları). Ayrı ayrı
sistemler olarak yazılırsa proje dağılır. Oda yerleşimi sabit olduğu için makineler sahnede hep
bulunur; `LevelConfig` hangilerinin **aktif** olacağını belirler (spawn/despawn değil).
*(GDD §11.9)*

**Alan silme yasağı.** Bir faz o alanı kullanmıyor olabilir; bu, alanın tanımlanmayacağı anlamına
gelmez. Faz 0'da kullanılmayan alanlar (stok, fritöz/içecek/dondurma makineleri, Şekil ve Vücut
Bölgesi kanalları) **tanımlı ve boş/kapalı** bırakılır. Silinip sonradan geri eklenirse hem
asset'ler hem onları okuyan kod iki kez yazılır.

**Sabitleme yasağı.** Bu alanların hiçbiri koda gömülmez ve kod hiçbirinin değerini varsaymaz.
Her alan her seviye için bağımsız seçilebilir olmalıdır.

**Sabit/rastgele ikiliği.** Bu alanların çoğu yalnızca "hangi değer" değil, "**elle mi girilecek
yoksa bir aralıktan/havuzdan mı çekilecek**" sorusunu da taşır (GDD §7.3). Sayısal alanlar
(müşteri sayısı, aralık, sabır, stok, süre çarpanı) için *elle değer* **veya** *min–max aralık*;
seçim alanları (varyant, içecek, dondurma, patates/ekstra, eksik malzeme) için *sabit seçim*
**veya** *havuzdan rastgele*. İkisi aynı seviyede karışık kullanılabilir ve **her sipariş slotu
kendi modunu taşır** — bir seviyede 4 müşteriden ikisi sabit, ikisi rastgele olabilir.

**Alan başına ayrı mekanizma yazılmaz.** Tek bir ortak veri tipi kullanılır (sayısal alanlar için
"elle değer veya min–max aralık", seçim alanları için "sabit seçim veya havuzdan rastgele") ve tüm
alanlar bundan türetilir. Ayrı ayrı yazılırsa tek-kaynak ilkesi çöker ve her yeni alan yeniden iş
çıkarır.

**Uygulama (Adım 16b, 30 Eyl 2026):**
- Ortak tipler: `NumericValue` (Fixed değer / Random min–max; `ResolveInt`/`ResolveFloat`) ve
  `Selection<T>` (Fixed `fixedItems` / Random `pool` + `count`; havuzda **boş eleman = "yok"**; `takeAll` =
  havuzun tamamı rastgele sırayla — eşleşme karıştırma). Tek seçimli alan = count 1. **Başka "sabit mi rastgele
  mi" bayrağı yazılmaz** (kod taramasıyla doğrulandı).
- `LevelConfig` (SO): §11.9'un tamamı; `OrderSlot` (varyant, eksik, içecek, dondurma, yan — her biri
  `Selection`), `defaultOrderSlot` (slot listesinden fazla müşteri + yedek havuz; 30 Eyl kararı),
  `ChannelConfig` (kanal, açık mı, değer listesi, `Selection<ItemType>` eşleşme: `items[i] ↔ values[i]`),
  `activeStations` (`Selection<StationId>`), sos pompası dizilimi, stok + tetik, süre çarpanı, hazırlık, ipuçları,
  tohum. Tarif kitapçığı ayrı alan değil: açık varyantlardan türer.
- `BurgerVariant` (SO; eski `BurgerRecipe`, aynı script GUID'i): malzeme + **çıkarılabilir** işareti (eksik
  "komple randomize" kısayolu yalnızca işaretlileri alır — 30 Eyl kararı), görsel. Ekmek listede yok (sabit).
- `SignalChannel` + `SignalValue` (SO): kanal ve değer kataloğu (`Assets/Data/Channels/`). Yön (4) ve Sayı (5)
  dolu; Renk/Şekil/Vücut tanımlı, boş.
- `StationId` (SO, `Assets/Data/Stations/`) + `StationIdentity` (sahne): makine/kap kimliği. Açma/kapama yok.
- `LevelResolver.Resolve(config, seed)` — saf fonksiyon, `System.Random`, Edit modunda da çalışır.
  `LevelDirector` (GameSystems; **aktif LevelConfig'in tek seçim yeri**) round `RoundActive`'e geçince
  **yalnızca sunucuda, bir kez** çözer, `Current`'ta (`ResolvedLevel`) tutar ve konsola yazar. Replikasyon yok.
- **Sabır ve müşteriler arası bekleme her müşteri için ayrı çekilir** (`ResolvedLevel.Order.Patience`,
  `.IntervalBefore`; GDD §7.3.3, 3 Eki) — bölüm başında tek değer çekilip herkese verilmez. Müşteri sayısı, yedek
  havuz, süre çarpanı bölüm başına tektir.
- **Kanal eşleşmesinin sırası görseldir** (GDD §3.6.3, 3 Eki): `ChannelMapping.Items[i] ↔ Values[i]` aynı zamanda
  duvar panosundaki satır sırasıdır. Eşleşme `LevelDirector.SignalRows` ile replike edilir (Adım 13a); pano onu okur.
- Örnek: `Assets/Data/Levels/Seviye1_Taslak.asset` (içerik TASLAK). Property drawer yok (Inspector ham).

### K9 — ESC menüsü oyunu durdurmaz
`ESC` duraklatma menüsü **yereldir**; bir oyuncu ayarları açtığında diğerleri oynamaya devam eder.
`GameLoopManager`'daki pause mimarisi **yalnızca disconnect** içindir — ikisi karıştırılmamalıdır.
Kitap açıkken `ESC` yalnızca kitabı kapatır. *(GDD §8.3, §3.6.2)*

---

## Network Mimarisi ve Test Edilebilirlik

- Dedicated server YOK. Steam lobileri üzerinden bir oyuncu Host olur; bağlantılar SDR üzerinden P2P.
- **Server-Authoritative:** Host aynı zamanda Server'dır. Tüm oyun mantığı Host'ta hesaplanır.
- **Host Disconnect:** Host migration YOKTUR. Host koparsa oyun sona erer, client'lar
  "Sunucu Bağlantısı Koptu" ile Ana Menü'ye döner.
- **Client Disconnect:** Oyun donar ("DURDURULDU"); 5 dk içinde dönülmezse oturum kapanır,
  bölüm başarısız sayılır. 2 kişiyle devam YOK. *(GDD §8.2)*
- **Nişan doğrulaması ve pitch:** `NetworkTransform` oyuncu kökünde ve Owner otoriteli — **yaw
  senkronize, pitch değil** (pitch `PlayerController` içinde yerel olarak `CameraPivot`'a
  uygulanıyor). Sunucu istemcinin tam nişan vektörünü yeniden üretemez; etkileşim doğrulaması
  yatay yön + mesafe üzerinden yapılır. Pitch senkronizasyonu bir Faz 1 sertleştirme işidir.
- **Local Test (Adım 1.6'da kuruldu):** Unity 6 yerleşik Multiplayer Play Mode ile tek makinede 3
  oyuncu. `LocalDebugLobby` Steam'i tamamen atlayan ayrı bir giriş noktasıdır: lobi panelindeki
  **"Local Host"** / **"Local Join"** butonları `TransportMode.LocalUdp` (127.0.0.1:7777) ile
  `StartHost`/`StartClient` çağırır. Butonlar yalnızca `UNITY_EDITOR || DEVELOPMENT_BUILD` altında
  vardır. Bağlantı kimliği olarak süreç kimliği (PID) gider. **Steam yoluna dokunulmamıştır.**
  *Bilinen kısıtlar:* round sırasında kopan bir oyuncu UI'dan Local Join'e ulaşamaz (lobi paneli
  gizli); MPPM penceresi kapanıp açılırsa PID değişir ve rejoin eşleşmesi kırılır; "Ayrıl" butonu
  Steam olmadan `ClearRichPresence` çağırır.
- **VoIP/Local Test Çakışması:** `IVoiceProvider` ile Dependency Inversion. Production'da
  `SteamworksVoiceProvider`, Local Debug'da `MockVoiceProvider`.
- **Transport Geçiş Zamanlaması:** Transport bileşeni `StartHost()`/`StartClient()`/`StartServer()`
  çağrılmadan **ÖNCE** atanmış olmalıdır — ağ başladıktan sonra değiştirilemez.

---

## Öğrenilmiş Teknik Tuzaklar (gerçek testlerde bulunup çözülenler)

Bunlar tekrar karşılaşılmaması gereken, bedeli ödenmiş derslerdir.

**Facepunch / Steam:**
- Köprü paketinin `main` HEAD'i derleme hatası veriyordu (CS1028) — commit
  `0eda04fc2146a4f907a61de6403315bce705279e`'e sabitlendi. Paket kırılırsa buradan başla.
- `SteamMatchmaking.CreateLobbyAsync` varsayılan olarak **görünmez** lobi oluşturur —
  `SetFriendsOnly()` çağrılmazsa davet akışı çalışmaz.
- Rich Presence `connect` anahtarı yayınlanmazsa Steam Arkadaşlar listesinden "Katıl" çalışmaz.
- Tek davet kabulü hem `OnGameLobbyJoinRequested` hem `OnGameRichPresenceJoinRequested`
  tetikleyip `StartClient()`'ı iki kez çağırabilir — guard gerekir.
- Aynı SteamID ile aynı makinede relay socket'e kendine bağlanma **desteklenmez**
  (`ArgumentException: Invalid Connection`) — paket hatası değil, platform kısıtı.
- Steam overlay development build'lerde çalışmaz (AppID kayıtlı değil) — **kod hatası değil,
  ortam kısıtı**. `SteamUtils.IsOverlayEnabled = False`. Bu konuda kod değişikliği istenmiyor.

**NGO:**
- **Parametresiz `ClientRpc` TÜM istemcilere gider.** Tek bir oyuncuya ait geri bildirim
  `ClientRpcParams.Send.TargetClientIds` ile hedeflenmelidir; çağıran
  `ServerRpcParams.Receive.SenderClientId`'den okunur. *(18 Eyl 2026, gerçek 3 makineli testte
  bulundu: bir oyuncunun etkileşim toast'ı üç ekranda birden çıkıyordu.)*
  **İstisna:** emote broadcast'i (`EmoteSystem.EmoteTriggeredClientRpc`) **kasıtlıdır** — emote'ları
  herkes görür (GDD §3.6.0). Bu bir bug değildir, düzeltilmeyecektir.
- **Sunucu kodundan çağrılan bir `ServerRpc` yerel olarak çalışır ve `rpcParams` `default` olur** —
  yani `rpcParams.Receive.SenderClientId` sessizce **0 (host)** olur, gerçek etkileşen oyuncu değil.
  `SenderClientId` okuyan hiçbir `ServerRpc`, sunucu tarafındaki bir kod yolundan çağrılmamalıdır.
  *(18 Eyl 2026: Adım 1 `BeginPress()`'i sunucuya taşıyınca `BurgerAssemblyStation`'ın
  `OnInteractionCompleted` dinleyicisi de sunucuda çalışır oldu ve `PlaceIngredientServerRpc`'yi
  yerel olarak çağırmaya başladı; hangi client tıklarsa tıklasın **host'un** aktif envanter
  slotundaki malzeme siliniyordu. Kod derleniyor, hata vermiyor, "çalışıyor" görünüyor — teşhisi
  bu yüzden zor.)*
- **Etkileşim olayları etkileşen oyuncunun kimliğini taşımak zorundadır.**
  `HoldOrPressInteractable`'ın olayları `Action<ulong>`'dur ve etkileşen client id'sini geçirir.
  Bir dünya nesnesi üzerindeki olay sunucuda çalıştığı için "kim tetikledi" bilgisini kendi
  taşımalıdır; taşımazsa o nesneye abone olan her bileşen yanlış oyuncu üzerinde iş yapar. Yeni bir
  istasyon (ızgara, paketleme, teslim) yazılırken bu parametre kullanılır, `SenderClientId`
  yeniden okunmaya çalışılmaz.
- `NetworkManager.LocalClient.PlayerObject` rejoin sonrası güncellenmeyebilir. Yerel oyuncunun
  bileşenlerini bulmak için `IsOwner` kullanılır (`PlayerInventory.FindForClient` bu deseni izliyor;
  `HotbarUI` ise sahnede **etkin** `PlayerController`'ı arar — o da sahipsiz kalan nesnenin host'a
  devrine karşı korumalı). Sunucu tarafında ise `ConnectedClients[clientId].PlayerObject` kullanılır.
- **İstemci tahmin eder, sunucu karar verir.** Her karede sorulan arayüz durumları (crosshair
  gibi) replike veriden **yerel olarak** hesaplanır, RPC ile sorulmaz. Kural yine tek yerdedir:
  `IInteractionGate.CanInteract` hem istemcide gösterim için hem sunucuda karar için çağrılır.
  **İstemcinin "olur" dediği her şeyi sunucu kabul etmek zorundadır — tersi serbest değildir.**
  Crosshair veya önizleme "kullanılabilir" gösterip sunucu reddederse, geri bildirim yalan söylemiş
  olur. Bunu garanti etmek için:
  - Mesafe ve yön kontrolü **tek bir paylaşılan fonksiyondadır**; istemci (gösterim) ve sunucu
    (karar) aynısını çağırır. İki farklı formül yazılmaz.
  - Ölçü: **göz noktasından hedef collider'ının en yakın yüzey noktasına.** Merkeze (pivot)
    ölçülmez — büyük nesnelerde oyuncuyu nesnenin ortasına yapışmaya zorlar.
  - Sunucu, ağ gecikmesi için **küçük bir tolerans** ekler (`[SerializeField]`). İstemci her zaman
    daha katıdır.
  **Hedef görünür olmalı (Düzeltme `2a5690d`, GDD §4.1.2 ②):** etkileşim ışını
  `PlayerInteractor.aimMask` ile atılır ve **ilk katı çarpmada durur**; ilk çarpılan collider bir
  etkileşim hedefine ait değilse hedef yoktur. Maskede olmayanlar: Ignore Raycast (2), UI (5),
  PlacementPreview (9); trigger'lar yok sayılır. **Yeni bir katman eklenirse maskeye girip
  girmeyeceği bilinçli seçilir.** Yerel oyuncunun kendi kapsülü ışını durdurmaz çünkü ışın
  kapsülün içinden başlar (Unity kuralı); kamera kapsülün dışına taşınırsa bu bozulur — kodda
  `IsChildOf` koruması ve uyarı var. Sunucu engel kontrolü yapmaz; istemci daha katıdır.
  Uygulama: `HoldOrPressInteractable.CheckReach` (göz = `CameraPivot`, mesafe = `ClosestPoint`,
  yön = göze göre yatay). Menzil dışındaki hedef **nötr** görünür, "engelli" değil. Sunucu payları
  (`serverRangeTolerance`, `serverAimDotTolerance`) playtest parametresidir.
  *(21 Eyl 2026: istemci kameradan yüzeye, sunucu kökten merkeze ölçüyordu; crosshair ve önizleme
  "kullanılabilir" derken sunucu "menzil dışı 3,03 m" diye reddediyordu. Adım 2a'da "uç durum"
  sayılıp kabul edilmişti — yanlıştı.)*
- `HoldOrPressInteractable` bir `MonoBehaviour`'dır ve `Update()` içindeki hold zamanlayıcısı
  `IsPressed` bayrağına bağlıdır. `BeginPress()`/`EndPress()` **yalnızca sunucudan çağrılır** —
  böylece zamanlayıcı da yalnızca sunucuda işler (K6). İstemci kodundan çağrılırsa hold süresi
  istemcide sayılır ve tamamlanma kararını istemci verir.
  *Hold tüketicileri (hepsi Faz 1): dondurma kolu (GDD §6.7.2), yangın tüpü (§5.2.3). İçecek
  makinesi hold DEĞİLDİR — düğmeye basılır, dolumu sunucu işletir (§6.7.1). Faz 0'da hiçbir
  `InteractionType.Hold` tüketicisi yoktur.*
- **Envanter tip numarası tutuyordu (`NetworkList<int>`) — Adım 4.1b'de (`09a9a59`) gerçek öğelere taşındı.**
  Durum taşıyan öğeler (pişme ilerlemesi olan köfte, bileşimi olan hamburger, içeriği olan kese
  kağıdı, doluluğu olan bardak) bir tip numarasıyla temsil edilemez. K5 ("ilerleme nesnenin
  üstünde") ve K7 ("yuvaya `TrySetParent`, nesne önce spawn edilmiş") her öğenin sahnede bir
  `NetworkObject` örneği olmasını gerektiriyor. Izgara/birleştirme/paketleme bu mimari kurulmadan
  yazılmaz. *(21 Eyl 2026'da tespit edildi.)*
  **Hedef mimari (Adım 4.0 raporu, 21 Eyl 2026 — 4.1a/4.1b uygulandı; yuvaya yerleştirme 4.2'de):**
  - Her taşınabilir öğe tek `NetworkObject` kökü + `Item` bileşeni; **sunucu sahipli**, iç içe
    NetworkObject yok, **collider yok** (yuvaya tıklanır, öğeye değil). Her öğe prefab'ı NGO prefab
    listesine kayıtlı olmalı — değilse istemcide spawn olmaz.
  - Slot öğenin ağ kimliğini **+1 kaydırılmış** tutar (0 = boş). *Neden:* `default(NetworkObjectReference)`
    boş değildir, id 0'ı çözer (NGO kaynağından doğrulandı).
  - Elde tutulan öğe oyuncuya **parent edilmez**; dünya görseli kapalıdır, elde görünüm yerel
    `HeldItemVisual`'dır.
  - `TrySetParent` / `TryRemoveParent` / `Despawn` **yalnızca `ItemMover`'da** çağrılır; dönüş değeri
    denetlenir, başarısızlıkta öğe eski yerine alınır ve koşulsuz hata loglanır. *Neden:* `TrySetParent`
    başarısızlığı yalnızca `LogLevel.Developer`'da loglanıyor — varsayılan ayarda tamamen sessiz.
  - Öğe her zaman **elde doğar**; aynı karede spawn + parent yapılmaz.
  - Hamburger tek öğedir, katmanları kendi listesinde tutar; her katman malzeme türünü **ve
    pişmişlik fazını** taşır (çiğ köfte hamburgere konabilir — GDD §5.2.1).
  - Paketin içindekiler **gerçek öğe olarak paketin içinde kalır** ve kendi durumlarını taşır;
    "içindekileri yok edip listeye yaz" yolu **kullanılmaz** (teslimde birebir kontrol imkânsızlaşır,
    GDD §5.3.1). Yöntemi paketleme adımında belirlenir.
  - **Pişmişlik/doluluk engelleri yalnızca Komi'nin eylemlerine konur** (pakete koyma). Şef'in hiçbir
    gate'i öğenin durumuna bakmaz — bakarsa crosshair Şef'e durumu sızdırır (GDD §4.1.2 ①, §5.2.1).
- **Elle kurulan NetworkObject prefab'larının `GlobalObjectIdHash`'i çakışabilir.** NGO prefab'ları
  bu hash ile ayırt eder. Prefab, asset olarak kaydedilmeden önce sahnede kurulursa hash sahne
  nesnesi kimliğinden hesaplanır ve iki farklı prefab **aynı hash'i** alabilir (Adım 4.1a: iki öğe
  prefab'ı da 690666691). Sonuç: istemcide yanlış prefab spawn olur, hata vermez. **Kural:** her yeni
  NetworkObject prefab'ından sonra diskteki hash'ler karşılaştırılır; çakışma varsa asset
  üzerindeki `NetworkObject` yeniden doğrulanıp (`OnValidate`) kaydedilir. *(21 Eyl 2026)*
- **Sahibin yazdığı NetworkVariable ile ardından gönderilen ServerRpc arasında sıra garantisi YOK.**
  `NetworkVariable.Value` yalnızca yerel değeri yazıp kirli işaretler; delta bir sonraki ağ
  tick'inde gider. `ServerRpc` ise çağrıldığı anda gönderilir. Yani `ActiveSlotIndex` değiştirip
  aynı tick içinde etkileşirsen sunucu **eski slotu** okuyabilir. Faz 0'da kabul edildi (tuşa basıp
  bir tick içinde tıklamak nadir). **Düzeltildi (`8fd932a`, 22 Eyl 2026):** tıklama RPC'si slot
  numarasını da taşıyor, sunucu etkileşim yolunda `ActiveSlotIndex` okumuyor. `ActiveSlotIndex`
  yalnızca arayüz ve üçüncü şahıs görsel için kaldı. **Yeni bir etkileşim yazarken aktif öğe
  `InteractionContext.SlotIndex`'ten okunur.**
- **Öğe prefab'ı sahnede kurulursa `NetworkObject` kendini `InScenePlaced = true` işaretleyebilir**
  ve bu bayrak `OnValidate` ile geri dönmez (NGO 2.13.1: `CheckForInScenePlaced` yalnızca
  false→true yazar). Adım 7a'da `Kofte_Item`'da oldu; özel alan reflection ile düzeltildi. **Kural:**
  dinamik spawn edilecek prefab'lar sahnede kurulmaz (prefab modunda / doğrudan asset olarak
  kurulur); her yeni öğe prefab'ından sonra hem hash benzersizliği hem `InScenePlaced == false`
  kontrol edilir. *(22 Eyl 2026)*
- `NetworkManager.Shutdown()` asenkrondur — hemen yeniden bağlanma eski oturumun yarım durumuna
  çarpar. `WaitForNetworkShutdown` coroutine + busy guard gerekir.
- Sahne-içi kalıcı objeler (`GameSystems`) lobiler arası hayatta kalır — `OnNetworkSpawn`'da
  sunucu tarafından durum açıkça temizlenmelidir, `OnNetworkDespawn`'da üretilen objeler yok edilmelidir.
- Struct'lar NGO kaynak-üretici serileştirmesi için `INetworkSerializeByMemcpy` gerektirebilir.
- Singleton'lara (`NetworkManager.Singleton` vb.) `Awake()` içinde erişme — sıra garantisi yok,
  `Start()`'a ertele.
- Ses/etkileşim `ServerRpc`'leri varsayılan `RequireOwnership=true` ile client'lardan gelen çağrıyı
  reddeder — gerekiyorsa `false` yap.
- Play Mode çıkışında NGO'nun bilinen `OnDestroy()` sıralama hatası UDP soketini temiz kapatmaz;
  aynı Editor oturumunda ardışık `StartHost()` "address already in use" verir. **Kod hatası değil.**

**Unity / Editor:**
- **`CharacterController` + `Physics.autoSyncTransforms = 0` + ağ üzerinden konumlandırma = sessiz
  yarış koşulu.** NGO istemci tarafında prefab'ı **konumsuz** instantiate edip sonra
  `SetPositionAndRotation` ile taşıyor (`NetworkSpawnManager.InstantiateNetworkPrefab`).
  `CharacterController` prefab varsayılanında kayıt olduğu ve `autoSyncTransforms` kapalı olduğu
  için taşındığını bilmez; ilk `Move()` transform'u kendi bayat iç konumuna geri çeker.
  `NetworkTransform` (Owner otoriteli) sonra bu **bozuk** değeri yayar — hata `NetworkTransform`'da
  değil, taşıyıcısıdır.
  *Yarış:* spawn karesinde bir fizik adımı düşerse `CharacterController` senkron olur ve pozisyon
  tutar (~4'te 1). Bu yüzden bug bazen görünmez.
  **Kural:** `CharacterController` taşıyan bir nesne ağ üzerinden konumlandırıldığında, ilk
  `Move()`'dan önce controller transform'a senkronlanmalıdır — controller `enabled = false` →
  pozisyon ata → `enabled = true` (Unity'nin belgelenmiş teleport deseni), ya da
  `Physics.SyncTransforms()`. *(18 Eyl 2026, MPPM'de probe ile ölçülerek bulundu.)*
- **Multiplayer Play Mode sanal oyuncuları sahne değişikliğini kendiliğinden almaz.** Sahne
  diskte değiştiğinde ana Editor yeniden yükler, sanal Editor'ler **eski sahneyi bellekte tutmaya
  devam eder** — test sırasında "değişiklik uygulanmamış" gibi görünür. Sahne değiştikten sonra
  sanal oyuncular kapatılıp açılmalı (veya sahne onlarda da yeniden açılmalı). *(20 Eyl 2026:
  küplerin materyalleri değiştirildiğinde sanal oyuncularda hâlâ gri göründü.)*
- **Unity sahneyi yeniden kaydederken blok sırasını değiştirebilir.** İçerik birebir aynıyken
  `SampleScene.unity` yüzlerce satırlık diff üretir. Bu sıralama gürültüsü **commit edilmez**;
  sahne HEAD'e döndürülür ve yalnızca gerçek değişiklik uygulanır. *(21 Eyl 2026: 271/271 blok
  aynı, yalnızca sıra farklıydı.)*
- **`Collider.ClosestPoint` yalnızca Box, Sphere, Capsule ve *convex* MeshCollider ile çalışır.**
  Menzil kontrolü (`HoldOrPressInteractable.CheckReach`) buna dayanıyor. Etkileşilebilir bir
  nesneye **non-convex MeshCollider konmaz** — gerçek model geldiğinde ya primitif collider'lar ya da
  convex mesh collider kullanılır. Görsel mesh ile çarpışma hacmi ayrı tutulabilir.
- **`Destroy` kare sonuna ertelenir; o kareye kadar nesne hâlâ ebeveyninin çocuğudur.** Hemen
  ardından yapılan `GetComponentsInChildren` (özellikle `includeInactive: true`) silinmek üzere olan
  renderer'ları da toplar; kare sonunda liste ölü referans tutar ve `MissingReferenceException`
  fırlar. **Kural:** bir öğenin görsel çocuğu silinirken sıra `SetActive(false)` → `SetParent(null,
  false)` → `Destroy`'dur; bu desen tek yerde, `BurgerStackBuilder.Clear`'da durur, yeni görsel
  kaynakları (`IItemVisualSource`) onu kullanır. `Item.RefreshWorldVisual`'a null kontrolü eklenerek
  susturulmaz. *(27 Eyl 2026, `76da044`: hamburger yuvaya konunca kayboluyordu — veri kaybı değil,
  `Presence` değişiminde görünürlük döngüsü ölü renderer'da patlayıp yarıda kalıyordu.)*
- **Geometri ölçümü dünya AABB'siyle (`Renderer.bounds`) yapılmaz.** Dünya eksenine hizalı kutu,
  döndürülmüş bir nesnede gerçek boyuttan büyüktür; köşelerini yerel uzaya geri çevirmek şişmeyi
  geri almaz. Yığın/yerleşim hesapları `Renderer.localBounds` (veya mesh sınırları) ile, hedef
  kökün yerel uzayında yapılır. *(D5 `b11e568` → D6 `a99dfe7`, 30 Eyl 2026: birinci şahıs tutma
  noktası pitch aldığı için elde hamburger katmanları bakış açısına göre açılıyordu; toplam
  yükseklik 0,36 → 1,87'ye şişiyordu.)* **Doğrulama kuralı:** bir düzeltme, kullandığı fonksiyondan
  **bağımsız** bir ölçümle doğrulanır (örn. gerçek mesh köşeleri). D5'in "sonra 0" tablosu aynı
  fonksiyonla ölçüldüğü için döngüseldi ve yanlış çıktı.
- **Önizleme kopyası durum rengini taşımaz.** Faz rengi `MaterialPropertyBlock` ile yazılır ve
  materyal değişse de üstte kalır; `PlacementPreview` kopyadaki property block'ları temizler
  (GDD §4.1.2 ②: önizleme tamamen yeşil). *(`a99dfe7`)*
- **`Run In Background` (Player Settings) açık olmalıdır.** Kapalıyken odakta olmayan pencere
  güncellenmez; ağ alım kuyruğu taşar (`Receive queue is full ... (128)`), paketler düşer, hareket
  takılır ve bağlantı zaman aşımına düşüp oyun duraklatılır. MPPM'de aynı anda yalnızca bir pencere
  odakta olduğu için diğer iki oyuncu hep etkilenir; Steam build'inde host alt-tab yaparsa herkes
  donar. *(30 Eyl 2026, `fe38438`: "karakterler takılıyor, sonra donuyor" — kod hatası değildi.)*
  **Teşhis ipucu:** bu uyarı görülürse önce pencerenin güncellenip güncellenmediğine bakılır, kuyruk
  boyutu büyütülmez.
- **Sesli sohbet `AudioSource.mute` ile susturulamıyor.** Ses `VoiceStreamPlayer`'da
  `OnAudioFilterRead` ile sessiz taşıyıcı klibin üstüne enjekte ediliyor; `mute` klibin sinyaline
  uygulanıyor, sonradan enjekte edilen sesi kesmiyor (30 Eyl Steam testi: `fed5b33`'teki mute ile Komi
  oyuncuları net duydu). Birini susturmak için paket çözülmez (`1d4d1b2`). Aynı sebeple `volume` ve 3B
  ayarlarının da sohbet sesine uygulanmadığı varsayılmalı — K4 (mekânsal ses) adımında ölçülür.
- **"Yerel oyuncunun X'i" statik tek alanda tutulmaz.** Başka bir oyuncunun karakteri doğarken onun kamerası
  bir an etkinleşip kapanır (`ApplyNonOwnerState`); `OnEnable`'da "ben yereliim" yazan, `OnDisable`'da silen
  tek statik alan böylece silinir ve yerel olan hiç tanınmaz (30 Eyl: `DeafHearing` hiç devreye girmedi,
  Komi Şef'i ve cızırtıyı duydu). Etkin örnekler listesi tutulur.
- **Elde tutulan öğe duvardan geçmemeli, ama gövdenin içinde de kalmamalı.** Tutma noktası kapsül
  yarıçapından (0,5) uzaktaysa öğe duvara yaslanınca öbür tarafa geçer (30 Eyl: hamburger mutfak kapısından
  taşıyordu). Noktayı kapsülün içine almak **üçüncü şahısta çözüm değildir**: yer tutucu gövde görseli de 0,5
  yarıçaplı kapsül, öğe gövdenin içinde kalır ve kimse kimsenin elindekini göremez (aynı gün bu yüzden
  bozuldu). **Kural:** birinci şahıs noktası kapsülün içinde (yatayda ≤0,4; kamera `near` 0,05); üçüncü
  şahıs noktası gövdenin önünde (0,40/0,15/0,55) ve `HeldItemVisual.LateUpdate` gövde ekseninden öğe
  yarıçapında **SphereCast** atıp öğeyi engele değmeyecek kadar geri çeker (ince ışın çapraz açıda 1,9 cm
  taşıyordu). Final karakter sanatında da kollar collider dışına çıkacağı için bu desen kalır.
- **Oyuncu kökü kapsülün merkezindedir** (`CharacterController` center 0, height 2). Karakter zemin
  yüksekliğine konursa yarısı gömülür. Doğma yüksekliği prefab'ın kapsül ölçüsünden türetilir
  (`PlayerSpawner.GetFootOffset`); sabit y yazılmaz. *(30 Eyl 2026, `fe38438`)*
- **Kontur (K2) iki ayrı mesh'in aynı çizgide birleştiği ek yerinde pırıltı üretir.** Aynı yarıçaplı
  iki parça üst üste oturursa (alt + üst ekmek) kenarlar farklı dönüşümlerden geçtiği için piksel altı
  çatlaklar açılır; içerideki yatay yüzün normali görünür ve kamera döndükçe beyaz noktalar çıkar.
  **Kural:** birbirine değen parçalar aynı kenarda birleşmez — birinin ölçüsü farklı tutulur ki ek yeri
  gerçek bir basamak (gerçek kenar) olsun. Üst ekmek 0,19, alt 0,20. *(30 Eyl 2026, `4d76c5a`)*
- **Sahne kaydında layout'un yeniden hesapladığı RectTransform değerleri de gürültüdür.** Layout
  grubunun sürdüğü `m_AnchorMin/Max`, `m_SizeDelta` alanları kayıtta değişebilir. Sahne diff'i blok
  kimliğine (`&fileID`) göre karşılaştırılır; yalnızca amaçlanan bloklar alınır, HEAD sırası korunur,
  yeni bloklar sona eklenir. *(30 Eyl 2026, `fed5b33`)*
- **Sahnede kopyalanan NetworkObject kaynağın `GlobalObjectIdHash`'ini taşır** (30 Eyl: ikinci
  birleştirme tezgahı). Kopyadan sonra sahnedeki `NetworkObject`'lerin `OnValidate`'i çağrılıp sahne
  kaydedilir ve hash benzersizliği denetlenir (diskteki YAML'dan da okunabilir).
- **MCP komutları art arda zaman aşımına düşerse** (`Command TCS timed out`) editörde açık bir onay
  penceresi ana iş parçacığını bekletiyordur; kullanıcıya sorulur. *(30 Eyl: sahne kaydı + derleme sırasında.)*
- Canvas varsayılan WorldSpace açılır (Game view'da görünmez) — ScreenSpaceOverlay'e sabitle.
- `manage_gameobject create` + `save_as_prefab` her zaman **YENİ** obje yaratır; var olanı prefab'a
  çevirmez. Doğrusu: `manage_prefabs create_from_gameobject`, **isimle** hedefleyerek
  (instanceID hedeflemesi bu araç setinde güvenilir değil).
- `manage_components` kısa tip adıyla bulamayabilir — `PlayerController, Assembly-CSharp` gibi tam
  nitelenmiş isim kullan.

**Localization:**
- `Localization Settings.asset` → `m_StartupSelectors`: CommandLineLocaleSelector →
  SystemLocaleSelector → SpecificLocaleSelector (şu an fallback `tr`).
- **Nihai sürümde fallback `en` olacaktır** ve İngilizce Locale + String Table eklenecektir
  (GDD §"İleride Değişebilecekler" md.5). Şu an Türkçe geliştirme kolaylığı için birincil dildir.

---

## Mevcut Kod Durumu

Tam envanter (tüm sınıflar, imzalar, enum değerleri, sahne yapısı, prefab bileşenleri)
`docs/archive/KOD_ENVANTERI_<tarih>.md` altında tarihli olarak tutulur. **Bu döküm bayattır ve
kaynak değildir** — kod gerçeği repodaki koddur. Döküm yalnızca kod dışı danışmanlık için üretilir.

Özet:

**Klasörler:** `Assets/Scripts/{Core, Network, Player, Systems, UI}` — 89 .cs dosyası + `Assets/Editor/IconGenerator.cs` (3 Eki 2026).

**Kurulu ve doğrulanmış:**
- Steam lobi/host/client (3 gerçek hesapla uçtan uca test edildi)
- `RoleManager` — rol atama, bağlantı onayı, disconnect/rejoin SteamId eşleştirmesi
- `GameLoopManager` — `CurrentRoundState` (Lobby/RoundActive/RoundEnded) **tek otorite**,
  `IsGamePaused` computed property. **Kopma zaman aşımı (GDD §8.2, `fed5b33`):** duraklatma gerçek
  zamanla sayılır; `disconnectTimeoutSeconds` (Inspector, varsayılan 300) dolunca host bağlı
  istemcileri `error.session_timeout` sebebiyle koparır, sonra `OnServerSessionEnded` ile kendi ağını
  kapatır; herkes ilk ekrana "Bölüm başarısız" mesajıyla döner (`LobbyUIController.ShowErrorScreen`,
  Steam ve Local Debug yolu). Faz 0'da ayrı lobi sahnesi olmadığı için "lobiye dönüş" = ilk ekran.
  `PauseOverlayUI` (GameplayCanvas) duraklatmada "DURDURULDU" katmanını gösterir; yalnızca gösterir.
- `PlayerController` (CharacterController + mouse-look, owner-only), `PlayerInventory`
  (`NetworkList<ItemSlotEntry>`, slot sayısı Inspector'dan, varsayılan 4; slot seçimi
  `FindTargetSlot` — GDD §4.1; `HasFreeSlot` ve `ServerTryAddItem` aynı fonksiyonu kullanır),
  `PlayerSpawner` (**karakterler yalnızca round `RoundActive`'e geçince doğar**; bağlantı anında
  doğmaz — NetworkManager PlayerPrefab boş, `CreatePlayerObject = false`; rejoin'de var olan
  nesnenin sahipliği geri verilir), `HotbarUI`
- `Item` (öğe; `Type` prefab'a gömülü, `Presence` Carried/Placed, sunucu yazar; statik
  `NetworkSpawned`/`NetworkDespawned` olayları) · `ItemSlotEntry` (öğe id'si +1, 0 = boş) ·
  `ItemMover` (öğe spawn/despawn/parent işlemlerinin **tek** yeri: `SpawnCarried`, `Despawn`,
  `PlaceInSlot`, `TakeFromSlot`; her başarısızlıkta geri alma + koşulsuz `LogError`) ·
  `ItemSlot` (tek öğelik yuva, kendi NetworkObject'i olan sahne kökü nesnesinde; doluluk yuvanın
  doğrudan çocuğu olan `Item`'dan türetilir, ayrı bayrak yok; Inspector'dan **koyma rolleri
  (`placeRoles`, eski `allowedRoles` — `FormerlySerializedAs`) ve alma rolleri (`takeRoles`)** ayrı, ve
  kabul edilen türler; boş = hepsi; gate = GDD §4.1.2 ② "Yuva ile etkileşim", swap yok) ·
  `TrashBin` (GDD §5.3.2; bağlam slotundaki öğeyi alıp yok eder; rol kutu başına, durum sorgulamaz) ·
  `Ekmek_Item` / `TestItem_Item` / `Kofte_Item` / `Hamburger_Item` prefab'ları
- **Durumdan doğan görseller (6b `7b17d4a`):** `IItemVisualSource` — görseli `visualPrefab`'dan
  değil kendi durumundan üreten öğeler bu arayüzü uygular; elde görsel, dünya görseli ve önizleme
  aynı kaynaktan çizilir. Uygulayanlar: `BreadHalf` (ekmeğin "alt kondu" durumu; sunucu yazar;
  bütünken alt + üst, yarılanınca yalnızca üst ekmek — E1 `9ba8091`) ve `BurgerAssembly` (hamburgerin katman listesi; `BurgerStackBuilder` ile
  yerel çizilir, katmanlar pişmişlik fazını taşır). `Item.RefreshWorldVisual()` çalışma zamanında
  oluşan renderer'ları `Presence` mantığına katar.
- `ServerProgress` (K5 ortak ilerleme bileşeni; `Item`'dan türemez; `PhaseIndex` + `Progress`
  NetworkVariable, yalnızca sunucu yazar; `ServerAdvance(dt, isActive)`, `ServerReset()`; opsiyonel
  decay) + `ProgressProfile`/`ProgressPhase` (fazlar ve süreler SO'da — `KofteProgress.asset`:
  Çiğ 5 sn, Pişmiş 5 sn, Yanmış son faz; placeholder). Son fazda ilerleme durur (içecek dolumu
  "tepe noktada durur" kuralı da aynı yoldan).
- Faz rengi: `ItemType.phaseColors` → `ItemPhaseColoring` (tek kural) → `ItemPhaseVisual` (dünya
  görseli) ve `HeldItemVisual` (elde kopya, kaynağın `ServerProgress`'ine abone). Şef'te renk
  görünmez (K2).
- **Sinyal çarkı ve jest kuralları (Adım 13, 3 Eki 2026):**
  - `LevelDirector` artık `NetworkBehaviour`: çözülmüş kanal eşleşmesi `SignalRows`
    (`NetworkList<SignalRow>`: `LevelConfig.Channels` dizini, kanalın `values` dizini, eşleşen öğe id'si;
    yalnızca sunucu yazar, round başında bir kez). Asset referansı ağdan gitmez — her istemci aynı
    `LevelConfig`'i taşır, dizinle çözer. **Duvar panosu da bu listeyi okur; ayrı replikasyon yazılmaz.**
  - `SignalWheelModel.Build` (saf) çark ağacını yalnızca bu satırlardan kurar; kodda kategori/değer listesi
    yoktur. "Sipariş Bitti" kanala ait değildir (`OrderDoneChannel = -1`), asset'i `EmoteSystem.orderDoneSignal`.
  - `SignalWheelUI` (GameplayCanvas/`SignalWheelPanel`; `SignalWheel` action = R, basılı tut): sol tık seçer
    (kategori → içine gir, değer → gönder), sağ tık üst kata döner. Çark açıkken bakış ve etkileşim tıklaması
    kapalı (`PlayerController`, `PlayerInteractor` `IsWheelOpen` okur). Sinyal rolleri `EmoteSystem.signalRoles`.
  - `EmoteSystem` jestlerin tek sunucu otoritesi: `RequestSignalServerRpc` (rol → `CanPlayersAct` → sinyal bu
    bölümde açık → oynayan yok) ve `SelectEmoteServerRpc` (E; rol kısıtı yok). **Cooldown yok**; tek kayıt
    `_serverBusyUntil` (oyuncu başına bitiş anı), süre veriden (`SignalValue.Duration` klip varsa klip uzunluğu,
    yoksa `durationSeconds`; `EmoteDefinition.Duration`). İstemci `IsLocalBusy` ile tahmin eder — yayın gelince
    başladığı için sunucudan geç biter (istemci daha katı).
  - **Etkileşim jesti iptal eder:** `PlayerInteractor` sunucuda etkileşimi **kabul ettiği** anda
    `EmoteSystem.ServerCancelPlayback` çağırır; iptal herkese yayılır (`OnPlaybackCancelled`). Reddedilen
    tıklama iptal etmez. Yeni bir jest görseli yazılırsa bu olaya abone olur.
  - `PlayerSignalDisplay` (Player kökü; işaret `Visual/SignalAnchor`'da, yerel (0, 0,35, 0,55)): yer tutucu
    işaretler `Assets/Prefabs/Signals/` (ok, 1–5 çubuk, çerçeve), `SignalValue.visualPrefab`'dan. Final
    animasyon gelince yalnızca veri değişir. **Pencere açıklığı y 1,50–2,30** (ölçüldü); işaret 1,51–1,95
    arasında kalır. Yön okları Kasiyer'in yerel uzayındadır (Komi karşıdan aynalı görür — final animasyonda da
    böyle olacak; hangi tarafın "sağ" sayılacağı playtest konusu).
  - `GameLoopManager.CanPlayersAct` (round aktif **ve** duraklatılmamış): etkileşim, crosshair, sinyal ve emote
    çarkı — istemci ve sunucu — aynı koşulu buradan okur. Yeni bir oyuncu eylemi ayrı kontrol yazmaz.
- **Duvar malzeme panosu (Adım 15 + düzeltme, 3 Eki 2026):** `SignalMappingBoard` + `SignalMappingBoardEntry`
  (`Assets/Prefabs/MalzemePanosu.prefab`; gövde + dünya uzayı Canvas, collider yok, ağ nesnesi değil). İçerik
  yalnızca `LevelDirector.SignalRows`'tan: eşleşmesi olan her kanal için bir **bölüm** (yan yana), her eşleşme
  için bir giriş (resim + `ItemType.displayName`). **Kod panoya yazılmaz** (GDD §3.6.2): kanalın tüm eşleşen
  değerlerinde `useWheelAngle` varsa girişler o açılarda merkezin çevresine (yön), yoksa değer sırasıyla alt alta
  (sayı) dizilir. Yerleşim çapalarla, bölüm boyutuna oranla; giriş sayısına duyarsız. Sahnede
  `MalzemePanosu_Istasyon` (duvarın kuzey yüzü, z=−3,58) ve `MalzemePanosu_Kasa` (güney yüzü, z=−3,88), ikisi de
  x=6,22 / y=1,95, 1,7×1,0 m — pencerenin (x 7,3–9,0) batısında. Kökün +Z'si duvarın içine bakar. Pano Şef'in
  görüşünde yalnızca gövde konturu olarak görünür (UI derinliğe yazmaz).
- **Müşteri akışı (Adım 17a, 3 Eki 2026):**
  - `Customer` (`Assets/Prefabs/Musteri.prefab`; ağ prefab listesinde): sunucu sahipli ağ nesnesi, kökü ayakta
    (zemin y=0,30), `NetworkTransform` sunucu otoriteli. `State` (Arriving / WaitingToOrder / Ordered / Leaving),
    `PatienceRemaining`, `PatienceTotal` replike; yalnızca sunucu yazar. Sipariş (`ResolvedLevel.Order`) sunucuda
    müşterinin üstünde. Sipariş alma = mevcut etkileşim (gate: rol `orderTakerRoles` + durum WaitingToOrder).
    Sabır, müşteri sipariş yerine **vardığında** başlar; duraklatmada sayaç ve yürüyüş durur.
  - `CustomerDirector` (sahne: `MusteriYonetimi`; ağ durumu yok, yalnızca sunucuda çalışır):
    `LevelDirector.ServerLevelResolved` ile başlar. Müşteri sayısı, aralık (× `intervalCurve`), sabır, yedek havuz
    `ResolvedLevel`'den. **Eşzamanlı sınır `maxConcurrentCustomers` (3) — tek yer.** İlk müşteri hazırlık fazı
    biter bitmez gelir; aralık sonraki müşterilere uygulanır ve bir öncekinin **gelişinden** sayılır. Yedek, sabır
    hatasında yer olur olmaz gelir. Yer, müşteri ayrılmaya **başladığı** anda açılır. Teslim adımı
    `ServerCustomerServed(customer)` çağırır. `AllCustomersFinished` / `ServerAllCustomersFinished` bölüm sonu
    altyapısıdır (kazan/kaybet kararı yok).
  - Haritada ayrı sipariş/teslim penceresi yok, tek `PF_DeliveryCounter` (açıklık x 0…3,75, z −7,6…−6,7, üst y 1,4):
    doğu ucu sipariş (`SiparisYeri_1-3`, x 2,3/2,9/3,5), batısı teslim (`TeslimYeri_1-3`, x 0,3/0,9/1,5), hepsi
    z=−7,95; `Giris` (12, −9,5). **Sipariş bekleyenler de yan yana** (kuyruk olursa arkadakinin sabrı işlerken
    siparişi alınamaz) — GDD'de yazmıyor, Ersel'e soruldu.
  - `GameLoopManager.ErrorCount` (replike) + `ServerAddError(reason)`: bölümün **tek** hata sayacı; round başında
    sıfırlanır. 3 Hata kararı ve duvar göstergesi ayrı adımda.
  - `CustomerTimerDisplay` (17a'da `CustomerPatienceDisplay`): sabır çarkı, sipariş alınınca yerini sipariş süresi
    çarkına bırakır; "kim görür" kuralının tek yeri (`visibleToRoles`, Kasiyer). Çark müşterinin arkasına (kuzeye)
    bakar, billboard değil. `CustomerVisual`: duruma göre gövde rengi (yer tutucu).
  - Müşterinin collider'ı var (tıklanır); müşteri alanında oyuncu yok, hareket düz çizgi (engel bilmez).
- **Sipariş pop-up'ı ve sipariş süresi (Adım 17b, 3 Eki 2026):**
  - Süre = `(taban + sinyal × katsayı) × seviye çarpanı`. Taban/katsayı `OrderTimeSettings`
    (`Assets/Data/OrderTimeSettings.asset`: 32 / 4,3), çarpan `ResolvedLevel.TimeMultiplier`. **Çarpan toplamın
    tamamına uygulanır** (adım metni iki türlü okunabiliyordu; GDD "seviye bazında bir çarpan" diyor).
  - `OrderTimeCalculator` (saf): sinyal sayısı = siparişin içeriğinde (varyant malzemeleri − eksikler + içecek /
    dondurma / yan) olup **açık bir kanalda eşleşmesi bulunan** her öğe + 1 "Sipariş Bitti". Kanal adı/listesi kodda
    yok; yeni kanal veride açılınca kendiliğinden sayılır. Eşleşmesi olmayan öğe sayılmaz ve uyarı loglanır.
  - `Customer`: `OrderTimeRemaining/Total` (sipariş alınınca başlar, sunucu işletir), `OrderVariantIndex`,
    `OrderMissingItemIds` (pop-up içeriği). **Varyant ağda dizinle anılır:** `LevelConfig.CollectVariants()` sırası
    (`LevelDirector.GetVariantIndex` / `TryGetVariant`). Süre dolunca 1 Hata, müşteri ayrılır, yedek **gelmez**.
  - `CustomerOrderDisplay`: pop-up müşterinin üstünde (y 2,67–3,07; tezgah açıklığı y 1,40–2,90, Kasiyer'in
    gözünden ~3,1'e kadar görünür), yalnızca `Ordered` durumunda. Görsel `BurgerVariant.Image` — kitapçık da aynı
    alanı kullanacak. Eksik malzeme = ikon + üstünde X. Pop-up herkese çizilir (GDD: pratikte yalnızca Kasiyer görür).
  - Varyant görselleri `IconGenerator` ile üretildi (`Assets/Data/Icons/Varyant_*.png`). **İnce malzemeler ekmeğin
    altında neredeyse görünmüyor; iki taslak varyant birbirine çok benziyor** — varyantları ayırt edilebilir kılmak
    (açı, patlatılmış görünüm ya da artist çizimi) içerik işi.
- **Protein türleri `Tavuk`(8) `Balik`(9) `Veji`(10) (3 Eki):** tür + görsel + ikon var, **`itemPrefab` yok** —
  panoda görünürler ama oyunda alınamazlar (kap/buzdolabı ve öğe prefab'ı içerik işi). `Seviye1_Taslak` Yön
  eşleşmesi: Yukarı=Köfte, Sol=Tavuk, Sağ=Balık, Aşağı=Veji.
- **İkon üretici (Adım 15):** `IconGenerator` (menü: *Cook No Evil → İkon Üret*). `PreviewRenderUtility` ile ayrı
  önizleme sahnesinde, saydam arka planlı 256 px sprite üretir ve asset'e atar: `ItemType.visualPrefab` → `icon`
  (köfte ilk fazıyla), `SignalValue.visualPrefab` → `icon` (karşıdan; yalnızca seçili asset'ler için — Sayı
  değerlerine ikon üretilirse pano rakam yerine çubuk gösterir), `BurgerVariant` → `image` (yığın
  `BurgerStackBuilder` ile; düz listeden kuran overload eklendi). **Işık yalnızca `preview.Render` ile gelir;**
  `camera.Render` doğrudan çağrılırsa görüntü karanlık kalır. Hamburger türünün `visualPrefab`'ı yok, ikonu eski.
- **Çarkta sabit açı:** `SignalValue.useWheelAngle` + `wheelAngle` (0 = sağ, 90 = yukarı). Bir kattaki tüm
  seçenekler işaretliyse açılar veriden gelir (Yön değerleri kendi yönünde durur), değilse eşit aralık
  (`SignalWheelModel.ResolveAngles`).
- `EmoteWheelUI` (E, eski tek katmanlı çark; rol kısıtı yok) + `PlayerEmoteReactor` — GDD'deki genel emote

  çarkı henüz yazılmadı.
- `VoIPController` (`IVoiceProvider` soyutlaması). Round sırasında Kasiyer'in paketi sunucuda relay
  edilmez; **Komi hiçbir oyuncunun sesini duymaz** — gelen paket hiç çözülmez (`1d4d1b2`, K3).
- `DeafHearing` (oyuncu kamerası, AudioListener): "sağır mı" kuralının **tek yeri** (sağır rol + round
  aktif). Sağırken dinleyiciye low-pass (boğuk); `RoleAwareAudioRange` taşıyan efekt kaynakları menzilini
  ve seviyesini daraltır (GDD §4.1.3: dar yarıçap, kısık). Yarıçap 2 m, seviye 0,3, kesim 500 Hz —
  playtest parametresi. **Yeni makine/istasyon efekti yazılırken kaynağa `RoleAwareAudioRange` konur,
  rolloff Linear olur.**
- **Harita (30 Eyl 2026):** artist'in modüler haritası (`Assets/Scenes/Map.unity`, parçalar
  `Assets/NewAssets/`) ana sahneye `Harita` kökü altında taşındı; `Map.unity` kaynak olarak değişmeden
  duruyor. Eski gri-kutu harita **`SampleScene_EskiHarita.unity`**'de aynen saklanıyor (test için; build'de
  değil). **Zemin üst yüzeyi y=0,30.** Odalar (GDD §3.2): Mutfak x −1,35…4,65 / z −3,73…1,35 · İstasyon
  x 4,65…10,51 aynı z · Kasa kuzeyi z −3,73, güneyi teslim tezgahı (z −7,35). Pencereler: Mutfak↔İstasyon
  x=4,65; İstasyon↔Kasa z=−3,73 (x≈8); pervaz zeminden 1 m (geçilemez).
  - İşlevsel yerleşim: `Buzdolabi_Et` (PF_Fridge; köfte kabı) · iki `BurgerAssemblyStation` (iki
    PF_CuttingTable; GDD §6.7.3 "2 tezgah") · `Izgara` (PF_Grid; yuvalar ızgara üstünde) · `EkmekContainer`
    (kesme tahtalarının arasında, yer tutucu kutu) · doğu duvarına eklenen `PF_Table (Kaplar)` üstünde
    `Kap_Marul/Domates/Tursu/Sogan/Peynir` (kuzeyden güneye GDD numara sırası). Izgara yuvaları ızgara
    yüzeyinde (y=1,30; collider ızgara gövdesinin 1,42'lik collider'ının üstüne taşar — yoksa ışın gövdeye
    çarpar, hedef bulunmaz).
  - **Pencere yuvaları (GDD §5.1, 30 Eyl):** pervazda (y=1,30) 3'er `ItemSlot`, yalnızca Hamburger kabul
    eder. **İki yönlü:** `MutfakPencere_Yuva_1-3` Şef+Komi koyar/alır; `KasaPencere_Yuva_1-3`
    Komi+Kasiyer koyar/alır. TestTezgah kaldırıldı (eski sahnede duruyor).
  - **Çöp kutuları:** `Cop_Mutfak` (Şef), `Cop_Istasyon` (Komi), `Cop_Kasa` (Kasiyer) — aynı `TrashBin`,
    görsel PF_TrashBin.
  - **Mutfak kapısına (`PF_KitchenDoor`) BoxCollider eklendi** — modelde yoktu, Şef kasaya yürüyebiliyordu
    (GDD §5.2.3: kapı yalnızca yangında açılır; Faz 1'de kapı mekaniği bunu yönetir).
  - Doğma noktaları: `DogmaNoktalari/Dogma_<Rol>`, `PlayerSpawner.spawnPoints` (rol → Transform).
  - Erişim doğrulaması (editörde, göz 1,98): her hedefe **kullanacak rolün odasının** zemininden nişan ışını
    ulaşıyor ve menzil içinde (pencere yuvaları iki taraftan da); en zoru `Kap_Marul` (köşe, 1,60 m).
- **Malzemeler (30 Eyl 2026):** Garnitür türleri `Marul`(3) `Domates`(4) `Tursu`(5) `Sogan`(6) `Peynir`(7),
  kategori Garnitür; öğe prefab'ları `Assets/Prefabs/Items/`, görseller `ItemVisuals/`. **Kaplar sınırsız**
  malzeme verir — Ersel: "ileride malzeme editörden ayarlanabilir olacak; o sistem kurulunca değişecek"
  (stok/LevelConfig, K8). Et: şimdilik yalnızca dana (`Kofte`, SM_Meat_Raw); tavuk/balık/veji modelleri
  `NewAssets/BurgerMeats/`'te hazır, eklenmedi.
- **Artist FBX'leri (NewAssets) için kural:** FBX içindeki nesne kendi Blender sahnesindeki konumuyla gelir
  (ör. peynir 2 m ileride) — **doğrudan kullanılmaz**, görsel prefab'da ortalanır (sınır kutusu yatayda
  merkezde, taban y=0; K2d). Mesh orijinleri sınır kutusu merkezinde (halka kuralına uygun). Yiyecek
  FBX'lerinin **materyali yok** (URP varsayılan gri Lit): renkler `Assets/Materials/Food/*_Placeholder.mat`
  (URP Lit, DepthNormals var) — artist materyali gelince değişir. Köftenin faz rengi `_BaseColor`'ı
  **ezer** (doku yoksa mutlak renktir).
- **Ekmek modelleri (E1 `9ba8091`, harita):** `Ekmek_Visual` iki parçalı — `Alt` (SM_BurgerBun_Bottom) ve
  `Ust` (SM_BurgerBun_Top). **İki parçanın kenarı aynı yarıçapta (0,100) çakışıyordu → `Ust` yatayda 0,97
  ölçekli** (beyaz nokta kuralı, bkz. Unity tuzakları). `BreadVisualParts` ekmek modellerinin **tek
  kaynağı** (Bütün / Alt / Üst); yalnızca üst gösterilirken üst parça tabana iner. Yığında ekmek
  katmanının alt mı üst mü olduğu konumdan türetilir (ilk = alt, sonraki = üst; ağ alanı yok). Bağımsız
  ölçüm (8 katman, 0/35/−60°): boşluk 0.
- **Izgara (7b `0a79563`):** `Grill` — 2 `ItemSlot`, yuvadaki `ServerProgress`'i sunucu ilerletir; round
  dışında ve duraklatmada pişme durur. `Grill.IsCooking` pişirme ve ses için **tek kural**.
  `GrillSizzle` (GDD §10.5, `fed5b33`): köfte ızgaradayken sabit 3B cızırtı döngüsü
  (`Assets/Audio/Izgara_Cizirti.ogg`, BigSoundBank "Frying pan #2", CC0); faz değişiminde ses değişmez.
- Hotbar ikonları: `Assets/Data/Icons/` — `IconGenerator` ile üretilir (bkz. İkon üretici). Yeni türde menüden
  yeniden üretilir.
- **Hotbar görünümü (3 Eki):** slot zemini koyu yarı saydam (her renkten ikon okunur); seçili slot beyaz çerçeve
  (`HotbarUI.slotFrames`) + hafif büyüme (`activeScale`) ile belli olur, zemin rengiyle değil. Slot numarası köşede.
- **Envanter doluyken hamburger kapatılabilir (D3, `9ba8091`):** üst ekmek gate'i boş slot aramaz;
  ekmek slottan alınınca hamburger aynı slottan başlayan kuralla eklenir.
- `HoldOrPressInteractable` (Press/Hold primitive'i; olayları etkileşen clientId'yi taşır,
  `CanInteract` ile aynı nesnedeki tüm `IInteractionGate`'leri toplar)
- `PlayerInteractor` — istemci hedef gönderir, sunucu doğrular (mesafe + yatay yön + gate);
  her karede crosshair durumunu yerel olarak hesaplar
- `IInteractionGate` — "bu client şu an bununla etkileşebilir mi" tek sorgusu. **Sorgu bir
  `InteractionContext` alır: etkileşen client + o tıklamadaki slot numarası** (Düzeltme `8fd932a`).
  Sunucu etkileşim yolunda `ActiveSlotIndex` **okunmaz**; slot RPC ile gelir ve aralık doğrulanır.
  `ClientId` her zaman `SenderClientId`'den kurulur (istemciden gelmez). `HoldOrPressInteractable`
  olayları da bağlam taşır. *İzlenecek:* basılı-tutma sırasında bağlam **basış anında** donuyor;
  hold tüketicileri (Faz 1: dondurma kolu, yangın tüpü) yazılırken "tutarken slot değişirse iptal
  mi" sorusu cevaplanmalı.
- `IngredientContainer` (rol kapılı malzeme kabı) · `BurgerAssemblyStation` (rol kapılı, gate'li;
  **kategori sırası** GDD §6.7.3 — azalmayan sıra, geri dönmek yasak; **alt ekmekten sonra yalnızca
  protein** (et zorunlu, etsiz kapatılamaz — 30 Eyl); proteinden sonra garnitür/sos atlanabilir; **tarif
  doğrulaması yok**; `PlacedIngredients` artık `NetworkList<BurgerLayerEntry {TypeId, PhaseIndex}>`
  — katman köftenin pişmişlik fazını da taşır) · `BurgerStackVisual` (yığını replike listeden
  **yerel** çizer; katman yükseklikleri sınır kutusundan toplanır, ağ nesnesi spawn etmez;
  yerleştirme noktası yığının tepesine taşınır) · `ItemCategory` (Yok/Ekmek/Protein/Garnitur/Sos;
  `ItemType.category`, eski `isBread` kaldırıldı)
- `CrosshairUI` — üç durum (nötr / kullanılabilir / engelli), GDD §4.1.2 ①
- `ItemRegistry` (ScriptableObject) — tür **id'si** → `ItemType` çözümlemesinin tek kaynağı ve
  öğe prefab'larının editör doğrulaması. 4.1b'den beri envanter/arayüz tür bilgisini doğrudan
  `Item.Type`'tan alır; registry yalnızca id tutan yerler için (ör. `PlacedIngredients`) gerekir.
  Yeni bir tüketici kendi dizisini tutmaz.
- `PlacementTarget` (işaretçi bileşen; yerleşme noktası **kendi transform'u** — 4.2'de
  `placementPoint` alanı kaldırıldı. Birleştirme tezgahında `PlacementPoint` çocuğunda, `ItemSlot`'ta
  yuvanın kendisinde durur; `PlacementPreview` hedefin çocuklarında arar) + `PlacementPreview` (sahibe özel, elindeki
  öğenin görselinden yeşil yarı saydam kopya) — GDD §4.1.2 ②. Koşul `PlayerInteractor.CurrentTarget`
  + crosshair "kullanılabilir"; ayrı raycast veya mesafe yok.
- `HeldItemVisual` — elde tutulan öğenin görseli; replike `Slots` + `ActiveSlotIndex` + `Item`
  spawn/despawn olaylarından yerel olarak çizilir (öğe listeden geç gelirse kendiliğinden düzelir). Sahip birinci şahıs noktasında, diğerleri üçüncü şahıs noktasında görür.
  `ItemType.visualPrefab` alanını kullanır (aynı görsel yerleştirme önizlemesinde de kullanılır).
- `LocalDebugLobby` — Steam'siz MPPM testi
- `RoleManager.testRoleJoinOrder` — katılma sırasına göre rol listesi (Inspector; varsayılan
  Şef, Komi, Kasiyer). **Yalnızca test kolaylığı**, lobide rol seçimi değildir. Testte
  değiştirildiyse sahne değişikliği commit'lenmez.
- Etkileşim round dışında kapalı: `PlayerInteractor` crosshair ve sunucu kararı aynı
  `GameLoopManager.IsRoundActive` kontrolünü kullanır.
- `BlindVisionCamera` + `BlindVision_Renderer` + `BlindVisionOutline.shader` — Şef'in kör görüşü (K2)
- `BurgerVariant` (eski `BurgerRecipe`) + `ItemType` + `LevelConfig` (ScriptableObject'ler; bkz. K8 uygulama)

**Adlandırma notu (Temizlik `2675553`, 21 Eyl 2026):** `IngredientType`/`IngredientRegistry` →
`ItemType`/`ItemRegistry` oldu; öğe türü artık bardak, kese kağıdı, hamburger gibi malzeme dışı
öğeleri de kapsar. **Bilinçli olarak eski adla kalanlar:** serileştirilmiş alan adları
(`ItemRegistry.ingredients`, `IngredientContainer.ingredient`),
`IngredientContainer` ve `IngredientRequirement` sınıfları (gerçekten malzemeyle ilgililer),
`Assets/Data/Ingredients/` klasörü. Alan adı değiştirmek `FormerlySerializedAs` + asset yeniden
kaydı gerektirir; değeri yok.

**Sahne mimarisi:** İki ayrı kalıcı obje vardır — `GameSystems` (NetworkObject taşıyan:
RoleManager, VoIPController, EmoteSystem, PlayerSpawner, GameLoopManager) ve `NetworkBootstrap`
(NetworkObject taşımayan: SteamLobbyManager, NetworkTransportManager, NetworkManager,
transport'lar). Bu ayrım korunmalıdır.

---

## 🔧 Yeni GDD ile Çelişen / Elden Geçirilecek Mevcut Kod

Aşağıdakiler **eski tasarıma göre** yazılmıştır ve güncel `docs/GDD.md` ile çelişir. Bunlar üzerine
yeni özellik inşa edilmeden önce düzeltilmelidir. *(30 Eyl 2026'da gerçek kodla yeniden doğrulandı —
12 satırdan 5'i kapandı, 1'inin Faz 0 kısmı kapandı; açık kalanların 2'si Faz 0.5'e ertelendi.)*

| Mevcut durum | GDD'nin gerektirdiği | Referans | Durum |
|---|---|---|---|
| ~~`PlayerRole.Yamak`~~ | `PlayerRole.Komi` (mekanik yeniden adlandırma) | §4.2 | ✅ Adım 3 (`f6026b2`) |
| ~~`EmoteSystem.selectionCooldown` (2.5 sn cooldown)~~ | **Cooldown YOK** — emote bitmeden yenisi başlatılamaz | §3.6.0 | ✅ Adım 13b |
| ~~`EmoteSystem.komiEmoteLimit` (Komi'ye kısıtlı liste)~~ | Kavram geçersiz — Kasiyer'de `R` sinyal çarkı, herkeste `E` genel çark | §3.6.0 | ✅ Adım 13b |
| `EmoteWheelUI` tek katmanlı (rol kısıtı 13b'de kalktı; `R` sinyal çarkı 13a'da yazıldı) | Tüm rollerde iç içe `E` genel çark | §3.6.0 | `R` ✅ Adım 13a; **`E` genel çarkı açık** (PLAN 14) |
| ~~`BurgerAssemblyStation`: sıra kuralı yok~~ | Zorunlu kategori sırası | §6.7.3 | ✅ Adım 6a (`30855e2`) |
| ~~`BurgerRecipe.requiredIngredients`~~ (asset artık kullanılmıyor, Temizlik Borcu'nda) | Ekmek tek envanter öğesi, alt+üst iki adımda | §6.7.3 | ✅ Adım 6b (`7b17d4a`) |
| ~~`ItemType` yalnızca `isBread` biliyor~~ | Kategori bilgisi | §6.7.3 | ✅ Adım 6a (`30855e2`) |
| VoIP oda-bağımsız (`ReceiveVoiceClientRpc` hedefsiz broadcast; `GetOrCreateSpeakerPlayer` konuşmacı AudioSource'unu gerçek oyuncu pozisyonuna değil `VoIPController` transform'una parent ediyor) | Ses mekânsal olmalı (K4) | §10.4 | Faz 0 |
| ~~`VoIPController.komiLowPassCutoffHz` (Komi'de low-pass)~~ — Faz 0: Komi hiçbir oyuncuyu duymaz (`1d4d1b2`) | Şef→Komi: **gibberish** (RMS ile sürülen maymun sesi), low-pass değil | §10.4 | Faz 0 kısmı ✅; gibberish **Faz 0.5'e ertelendi** (18 Eyl 2026) |
| ~~Kopmada "DURDURULDU" arayüzü ve 5 dk zaman aşımı yok~~ | Duraklatma görünür; süre dolunca oturum kapanır, bölüm başarısız | §8.2 | ✅ `fed5b33` |
| `RoundEnded`'a geçiş mantığı yok | Kazanma/kaybetme koşulları bağlanmalı | §3.4 | Faz 0 |
| `SequentialRoleAssignmentStrategy` (katılma sırası) | Lobide rol seçimi (çakışma varsa hazır verilemez) | §8.1 | **Faz 0.5'e ertelendi** (18 Eyl 2026) |

---

## ⚠ Bilinen Açık Buglar

*(Önceki 1. madde — etkileşimin istemci otoriteli olması ve sonucunun herkese yayılması — Adım 1
ve 1.5'te kapandı. Ders olarak NGO tuzaklarında duruyor.)*

1. **"Rolün:" yazısı bazen boş kalıyor** — yalnızca gerçek çok-makineli testte görüldü,
   tekrar üretilemedi. Teşhis logu duruyor: `[LobbyUIController] Round baslama teshis:`.
   `LocalRole=None` ise sorun RoleManager senkronizasyonunda, doluysa UI/Localization katmanında.
2. **`Previous`/`Next` input action'ları `HotbarSlot1`/`HotbarSlot2` ile aynı tuşları (1/2)
   paylaşıyor.** Hiçbir script okumuyor gibi görünüyor — muhtemelen template kalıntısı,
   temizlenmeli.

3. *(Not, 30 Eyl: `fed5b33`'ten önce kodda "DURDURULDU" arayüzü hiç yoktu — duraklatma yalnızca
   hareketi kilitliyordu. Aşağıdaki gözlem bu bilgiyle yeniden test edilmeli.)*
   **MPPM/LocalUdp'de round sırasında kopma "DURDURULDU" üretmedi (doğrulanmadı).** 22 Eyl testinde
   round sırasında sanal oyuncu kapatıldı, oyun donmadı. Steam build'lerinde çalıştığı biliniyor.
   Olası sebepler: sanal oyuncu kapanınca transport kopmayı zaman aşımıyla geç fark ediyor, ya da
   duraklatma yolu Steam kimliğine bağlı. **Kök neden bilinmiyor; tahmin etme, ölçerek bul.**

4. **Şef'te ince ekmek önizlemesinin halkası tezgaha gömülü görünüyor.** E1'den (`9ba8091`) sonra
   da sürüyor, artık **alt ekmekte** (düz, 0,05 m): üst ekmeğin önizlemesi normal (30 Eyl oyun testi).
   Yuvaya konmuş ekmek her rolde normal; sorun yalnızca önizleme halkasında. K2 / Adım 9b halka
   geçişi, ince nesnede derinlik testi (`_DepthBias`). Kök neden ölçülmedi.
*(Kapananlar: hamburgerin yuvaya konunca kaybolması + `MissingReferenceException` — `76da044`;
elde hamburger katman boşluğu ve önizlemede kırmızı köfte — `a99dfe7` (30 Eyl, oyunda doğrulandı).
Dersler "Unity / Editor" tuzaklarında.)*

## 🧹 Temizlik Borcu

- `Player.prefab` içinde stale `sprintMultiplier: 1.6` serileştirilmiş alanı (kodda karşılığı yok)
- `LobbyUIController.HandleRoundStateChanged` içindeki teşhis `Debug.Log`
- LevelConfig/varyant Inspector'ı ham (property drawer yok; PLAN kesme sırası 5). Seviye yazarken
  `Selection`'da kullanılmayan liste de görünür — yalnızca `source`'a uyan alan okunur.
- `ProductCategory` bayrakları LevelConfig'te tanımlı ama henüz hiçbir tüketici okumuyor (sipariş adımı).
- `PlacementPreview.mat` (URP Unlit, yeşil, alfa 0.5, ZWrite kapalı) yer tutucu. *(Yarı gömülü
  görünme sorunu K2d'de pivot tabana alınarak çözüldü.)*
- `TestItem` elde ~0,10 m yüksek görünüyor (tutma noktaları Ekmek'e göre ayarlandı; K2d).
- `Mobile_RPAsset`'te kör görüş renderer'ı (index 1) yok — kalite seviyesi "Mobile" seçilirse Şef
  normal görüntü görür. Oyun yalnızca PC/Steam; Mobile kalite seviyesi kaldırılabilir.
- `KonturTest_DesenliYuzey` — K2 regresyon paneli; harita gelince ana sahneden çıkarıldı, yalnızca
  `SampleScene_EskiHarita`'da duruyor (K2 testi orada yapılır).
- **Şef önizleme halkası mesh orijininden genişliyor (K2d `f241530` sonrası).** Öğe görsel
  prefab deseni artık: **kök tabanda** (yuvaya oturma noktası), mesh `Mesh` alt nesnesinde yukarı
  kaydırılmış. Halka mesh'in kendi orijininden genişlediği için yer tutucu primitiflerde (orijini
  merkezde) doğru çalışıyor. **Gerçek modellerde mesh orijini tabandaysa halka yine kayar.**
  Karar bekliyor: ya artist brief'i "mesh orijini sınır kutusu merkezinde, prefab kökü tabanda"
  olur (kod yok), ya da shader sınır kutusu merkezini ayrıca alır.
- **İzlenecek:** `HoldOrPressInteractable._colliders` önbelleği Edit modu testinde boş kalıyor ve
  `CheckReach` her şeyi menzil dışı (`Infinity`) sayıyor — 30 Eyl'de tekrar görüldü. Olası sebep: editör
  yeniden derlemede özel alanları da serileştiriyor ve null diziyi **boş dizi** olarak geri yüklüyor;
  `??=` bir daha doldurmuyor. Oyunda görülmedi. **Editör ölçümünde alan reflection ile null'lanır.**
  Önbellek bir kez dolduruluyorsa **sonradan etkinleşen/eklenen collider'ları görmez** — `LevelConfig`
  ile makineler aktif/pasif yapılırken (K8) burası kontrol edilir.
- `Assets/Meshes/EkmekAlt_Mesh.asset`, `EkmekUst_Mesh.asset` — E1'in yer tutucu ekmek mesh'leri, artık
  hiçbir yerde kullanılmıyor (referans taraması 30 Eyl). Silinebilir.
- 4.1b'de `HotbarUI`, `HeldItemVisual`, `PlacementPreview`, `BurgerAssemblyStation`'dan `registry`
  alanı silindi; sahne ve `Player.prefab`'te serileştirilmiş kalıntısı duruyor (zararsız). Bir
  sonraki kayıtta Unity temizler — o diff'i gürültü sanıp atma.
- `TestTezgah_Gray.mat` — TestTezgah ana sahneden kaldırıldı (pencere yuvaları geldi); materyal yalnızca
  `SampleScene_EskiHarita`'da kullanılıyor.
- Çöp geri bildirimi yalnızca görsel (öğe elden/hotbar'dan kalkar); Şef için işitsel "çöpe gitti" sesi yok.
- **Bilinen sınır — aynı anda iki tıklama:** iki oyuncu aynı boş yuvaya aynı karede tıklarsa
  ikincisinin tıklaması "dolu yuva" kuralıyla ilkinin öğesini **alır**; istemcinin "koymak
  istiyordum" niyeti sunucuya taşınmıyor. Faz 0'da kabul edildi (yuvaları farklı roller sırayla
  kullanıyor). Playtest'te görülürse niyet RPC'ye eklenir.
- `CrosshairUI` görselleri yer tutucu (Knob sprite, beyaz/yeşil/kırmızı); "kullanılabilir" yeşili
  yeşil birleştirme tezgahı üzerinde düşük kontrastlı
- **Bayat kod yorumları (30 Eyl kod taraması; davranışa etkisi yok, okuyanı yanıltır):**
  `ItemRegistry` ("PlayerInventory
  int id taşır" — 4.1b'den beri yok) · `IngredientContainer:607` (Köfte'yi spawn edilemeyen tür diye
  anıyor) · `HoldOrPressInteractable:9,17` (paketleme/diyafon'u basılı tutma örneği veriyor; GDD'de ikisi
  de tıklama) · `GameLoopManager` başı ("skor hedefi" — GDD'de yok, K1) · `LobbyUIController:28,243` ve
  `RoundState` (kaldırılmış `RoleManager.IsRoundActive`'i güncel sanıyor) · `BurgerLayerEntry:5`
  ("hamburger tek öğe olacak") · arşiv spec'e atıflar ("GDD 2.1/2.2", "Bileşen 1/2", "Hyper-Spatial"):
  `MockVoiceProvider`, `NetworkTransportManager`, `RoleManager`, `VoIPController` Şef
  dalı · CLAUDE.md'de olmayan notlara atıflar: `RoleManager`, `SequentialRoleAssignmentStrategy`
  ("Test boşluğu").
- Öğe görselleri yer tutucu: `TestItem_Visual` (küre); yiyecek materyalleri `Food/*_Placeholder`; ekmek
  kabı sarı yer tutucu kutu (gerçek model yok); tutma noktası konumları (`Player.prefab`) yer tutucu.
- **Harita sonrası kalanlar (30 Eyl 2026):** ~~garnitür + kaplar~~ · ~~ikinci tezgah~~ · ~~doğma
  noktaları~~ · ~~cızırtı menzili (5 m)~~ — yapıldı. **Kalan:** protein çeşitleri (tavuk/balık/veji; Yön
  kanalı, Şef siluetten ayırır — modeller hazır) · menü / hamburger varyantı verisi (tarif kitapçığı +
  sipariş) · malzeme/stok'un editörden ayarlanması (kaplar şu an sınırsız).
- Kopma zaman aşımında (`fed5b33`) istemcinin kamerası: oyuncu nesnesi yok olunca sahne kamerası geri
  açılmıyor (`PlayerController` kapatmıştı); hata ekranı yine görünür (Overlay canvas). Host kopması
  yolunda da aynı durum var.
- Birden fazla oyuncu kopup biri dönerse `ServerResumeAfterReconnect` duraklatmayı kaldırıyor (diğeri
  hâlâ yokken). Zaman aşımı sayacı da sıfırlanır. Faz 0'da 3 oyuncu, nadir.
- `GameLoopManager.StartRound()` yalnızca `AssignedRoleCount >= MaxPlayers` bakıyor, üç rolün
  gerçekten farklı ve geçerli olduğunu doğrulamıyor — lobide rol seçimiyle (Faz 0.5) kapanacak
- Hotbar ikonları (`Assets/Data/Icons/`) modelden render edilmiş yer tutuculardır; final sanat
  geçişinde değişir. *(İkon atanmamış bir `ItemType` hotbar'da görünmez — envantere girse bile
  ekranda hiçbir şey değişmez ve mekanik test edilemez hale gelir. Her yeni türe ikon atanır.)*
- `steam_appid.txt` = `480` (Spacewar) ve sahnedeki `FacepunchTransport.steamAppId` = 480 —
  gerçek AppID alınınca ikisi de güncellenmeli
- Haritada collider'sız dekor: `PF_Frier`, sepetler, müşteri masa/sandalyeleri, kesme tahtaları,
  servis tepsileri — içinden geçilir. Fritöz Faz 0 dışı; müşteri alanı müşteri adımında ele alınır.
- Varyantlar (`Assets/Data/Variants/*_Taslak`) ve `Seviye1_Taslak` içerik taslağıdır; Ersel seviye yazarken değiştirir.
