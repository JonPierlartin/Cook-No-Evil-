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
- **Local Test:** Unity 6 yerleşik Multiplayer Play Mode (ParrelSync gerekmez), Local Debug UDP
  (127.0.0.1:7777).
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
- `HoldOrPressInteractable` bir `MonoBehaviour`'dır ve `Update()` içindeki hold zamanlayıcısı
  `IsPressed` bayrağına bağlıdır. `BeginPress()`/`EndPress()` **yalnızca sunucudan çağrılır** —
  böylece zamanlayıcı da yalnızca sunucuda işler (K6). İstemci kodundan çağrılırsa hold süresi
  istemcide sayılır ve tamamlanma kararını istemci verir.
  *Hold tüketicileri (hepsi Faz 1): dondurma kolu (GDD §6.7.2), yangın tüpü (§5.2.3). İçecek
  makinesi hold DEĞİLDİR — düğmeye basılır, dolumu sunucu işletir (§6.7.1). Faz 0'da hiçbir
  `InteractionType.Hold` tüketicisi yoktur.*
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

**Klasörler:** `Assets/Scripts/{Core, Network, Player, Systems, UI}` — 30 .cs dosyası.

**Kurulu ve doğrulanmış:**
- Steam lobi/host/client (3 gerçek hesapla uçtan uca test edildi)
- `RoleManager` — rol atama, bağlantı onayı, disconnect/rejoin SteamId eşleştirmesi
- `GameLoopManager` — `CurrentRoundState` (Lobby/RoundActive/RoundEnded) **tek otorite**,
  `IsGamePaused` computed property
- `PlayerController` (CharacterController + mouse-look, owner-only), `PlayerInventory`
  (4 slot, `NetworkList<int>`), `PlayerSpawner`, `HotbarUI`
- `EmoteSystem` + `EmoteWheelUI` (tek katmanlı çark), `PlayerEmoteReactor`
- `VoIPController` (`IVoiceProvider` soyutlaması)
- `HoldOrPressInteractable` (headless Press/Hold primitive'i), `PlayerInteractor` (raycast)
- `BurgerAssemblyStation` + `BurgerRecipe` + `IngredientType` (ScriptableObject'ler)

**Sahne mimarisi:** İki ayrı kalıcı obje vardır — `GameSystems` (NetworkObject taşıyan:
RoleManager, VoIPController, EmoteSystem, PlayerSpawner, GameLoopManager) ve `NetworkBootstrap`
(NetworkObject taşımayan: SteamLobbyManager, NetworkTransportManager, NetworkManager,
transport'lar). Bu ayrım korunmalıdır.

---

## 🔧 Yeni GDD ile Çelişen / Elden Geçirilecek Mevcut Kod

Aşağıdakiler **eski tasarıma göre** yazılmıştır ve güncel `docs/GDD.md` ile çelişir. Bunlar üzerine
yeni özellik inşa edilmeden önce düzeltilmelidir. *(18 Eyl 2026'da gerçek kodla doğrulandı — 11
satırın tamamı hâlâ açık.)*

| Mevcut durum | GDD'nin gerektirdiği | Referans | Durum |
|---|---|---|---|
| `PlayerRole.Yamak` | `PlayerRole.Komi` (mekanik yeniden adlandırma) | §4.2 | Faz 0 |
| `EmoteSystem.selectionCooldown` (2.5 sn cooldown) | **Cooldown YOK** — emote bitmeden yenisi başlatılamaz | §3.6.0 | Faz 0 |
| `EmoteSystem.yamakEmoteLimit` (Yamak'a kısıtlı liste) | Kavram geçersiz — Kasiyer'de `R` sinyal çarkı, herkeste `E` genel çark | §3.6.0 | Faz 0 |
| `EmoteWheelUI` tek katmanlı, rol-kapılı (`IsWheelRole` Şef'i dışlıyor) | İç içe, **veri odaklı** sinyal çarkı (N kategori × M değer, `LevelConfig`'ten) + tüm rollerde `E` genel çark | §3.6.0, §11.9 | Faz 0 |
| `BurgerAssemblyStation`: ilk ekmek, sonrası **sıra kuralı yok** | Zorunlu kategori sırası: alt ekmek → protein → garnitür → sos → üst ekmek | §6.7.3 | Faz 0 |
| `BurgerRecipe.requiredIngredients` (Quantity'li liste) | Ekmek tek envanter öğesi, alt+üst olarak iki adımda konur | §6.7.3 | Faz 0 |
| `IngredientType` yalnızca `isBread` biliyor | Kategori bilgisi gerekli (protein/garnitür/sos) — sıra kuralı buna dayanıyor | §6.7.3 | Faz 0 |
| VoIP oda-bağımsız (`ReceiveVoiceClientRpc` hedefsiz broadcast; `GetOrCreateSpeakerPlayer` konuşmacı AudioSource'unu gerçek oyuncu pozisyonuna değil `VoIPController` transform'una parent ediyor) | Ses mekânsal olmalı (K4) | §10.4 | Faz 0 |
| `VoIPController.yamakLowPassCutoffHz` (low-pass) | Şef→Komi: **gibberish** (RMS ile sürülen maymun sesi), low-pass değil | §10.4 | **Faz 0.5'e ertelendi** (18 Eyl 2026) |
| `RoundEnded`'a geçiş mantığı yok | Kazanma/kaybetme koşulları bağlanmalı | §3.4 | Faz 0 |
| `SequentialRoleAssignmentStrategy` (katılma sırası) | Lobide rol seçimi (çakışma varsa hazır verilemez) | §8.1 | **Faz 0.5'e ertelendi** (18 Eyl 2026) |

---

## ⚠ Bilinen Açık Buglar

1. **Etkileşim istemci otoriteli ve sonucu herkese yayılıyor (ÖNCELİK 1).**
   - `PlayerInteractor.ReportInteractionAttemptServerRpc(bool succeeded)` — raycast ve karar
     istemcide veriliyor, sunucu yalnızca rapor alıyor ve hiçbir doğrulama yapmıyor. **K6 ihlali**,
     ayrıca hile açığı (değiştirilmiş istemci `succeeded: true` gönderebilir). Metot
     `ServerRpcParams` parametresi de almıyor, yani sunucu çağıranı okumuyor.
   - `PlayerInteractor.InteractionAttemptClientRpc(bool succeeded)` hedef parametresi almıyor;
     parametresiz `ClientRpc` olarak **tüm istemcilere** gidiyor. Bir oyuncunun etkileşimi üç
     ekranda birden görünüyor (gerçek 3 makineli testte doğrulandı).
   - Oyundaki **tek** etkileşim fiili bu olduğu için oyunun tamamı bu bug'la bozuk.
   - *Not: Önceki "LMB etkileşimi build'de başarısız" teşhisi **yanlıştı** — LMB çalışıyordu,
     görsel geri bildirim yoktu. 18 Eyl 2026'da kapandı.*
2. **"Rolün:" yazısı bazen boş kalıyor** — yalnızca gerçek çok-makineli testte görüldü,
   tekrar üretilemedi. Teşhis logu duruyor: `[LobbyUIController] Round baslama teshis:`.
   `LocalRole=None` ise sorun RoleManager senkronizasyonunda, doluysa UI/Localization katmanında.
3. **`Previous`/`Next` input action'ları `HotbarSlot1`/`HotbarSlot2` ile aynı tuşları (1/2)
   paylaşıyor.** Hiçbir script okumuyor gibi görünüyor — muhtemelen template kalıntısı,
   temizlenmeli.

## 🧹 Temizlik Borcu

- `Player.prefab` içinde stale `sprintMultiplier: 1.6` serileştirilmiş alanı (kodda karşılığı yok)
- `PlayerInteractor.HandleAttackStarted` içindeki teşhis `Debug.Log`
- `LobbyUIController.HandleRoundStateChanged` içindeki teşhis `Debug.Log`
- `InteractionToastUI` — geçici teşhis aracı, kalıcı oyun mekaniği DEĞİL. **Kalıcı highlight
  sistemi (GDD §4.1.2) devreye girdiğinde silinecek.**
- `VoIPController.cs:1-13` dosya başı yorumu arşivlenmiş eski spec'e referans veriyor
  ("GDD 2.2 / Red Line 2", "Hyper-Spatial Audio") — güncel `docs/GDD.md`'de böyle bir kavram yok,
  silinecek. *Kod yorumu da bayatlayabilir.*
- `BurgerAssemblyStation.PlacedIngredients.Clear()` tarif tamamlanınca otomatik sıfırlanıyor —
  "test edilebilirlik için" bilerek konmuş; gerçek sipariş sistemi bağlanırken paketleme akışını
  sessizce bozar (§6.7.3: tamamlanan hamburger envantere girer)
- `FindIngredientType` hem `BurgerAssemblyStation` hem `HotbarUI` içinde çiftlenmiş; iki ayrı
  Inspector'daki `registeredIngredients` dizileri birbirinden ayrışabilir → tek `IngredientRegistry`
- `GameLoopManager.StartRound()` yalnızca `AssignedRoleCount >= MaxPlayers` bakıyor, üç rolün
  gerçekten farklı ve geçerli olduğunu doğrulamıyor — lobide rol seçimiyle (Faz 0.5) kapanacak
- `Ekmek.asset` / `Kofte.asset` — gerçek ikonları yok. `Ekmek.asset`'e Adım 1'in test edilebilmesi
  için geçici olarak `EmoteA_Icon.png` atandı; gerçek malzeme ikonları gelince ikisi de
  düzeltilecek. *(İkon atanmamış bir `IngredientType` hotbar'da görünmez — envantere girse bile
  ekranda hiçbir şey değişmez ve mekanik test edilemez hale gelir.)*
- `steam_appid.txt` = `480` (Spacewar) ve sahnedeki `FacepunchTransport.steamAppId` = 480 —
  gerçek AppID alınınca ikisi de güncellenmeli
- `TestLevel` gri-kutu seviyesi (Floor + 4 duvar) — gerçek seviye geometrisi yok
- `BurgerAssemblyStation.activeRecipe` Inspector'dan sabit — gerçek sipariş kaynağına bağlı değil
- `NormalHamburger.asset` tarifi (`Ekmek × 2`, `Kofte × 1`) — placeholder, gerçek menüye bağlı değil
