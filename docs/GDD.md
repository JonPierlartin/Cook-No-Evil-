# Cook No Evil! — Game Design Document

**Sürüm:** 1.1 · **Tarih:** 18 Eylül 2026 · **Durum:** Faz 0 kapsam revizyonu uygulandı ✓

Tüm tasarım boşlukları kapalı; Faz 0 kodlamasına başlamak için yeterli. Yol haritası DeepJam
takvimine göre kuruldu (§11).

> ### Bu doküman nedir, nasıl kullanılır
>
> - **Bu dosya oyunun tek tasarım doğruluk kaynağıdır.** Oyunun ne olduğu, mekaniklerin nasıl
>   çalıştığı, sayısal dengeler, roller ve akışlar burada tanımlanır.
> - **Mühendislik gerçeği `CLAUDE.md`'dedir** (kod konvansiyonları, paket sürümleri, öğrenilmiş
>   teknik tuzaklar, mevcut kod durumu). O dosya tasarımı tekrar etmez, buraya referans verir.
> - **Çelişki durumunda bu dosya esastır.**
> - **Arşivlenen dosyalar** — `Cook_No_Evil_GDD_Spec.md`, eski `CLAUDE.md`,
>   `Bilesen2_PlayerController_Plan.md`, `Cook_No_Evil_Calistirma_Adimlari.md` artık kaynak
>   değildir; `docs/archive/` altına taşınmalıdır. Aynı tasarımın iki dosyada tutulması bu
>   projede bir kez bayatlamaya ve çelişkiye yol açtı; tekrarlanmamalıdır.
> - **Uygulama sırasında belirsizlik çıkarsa varsayım yapılmaz** — GDD'ye dönülür, karar
>   netleştirilir, buraya yazılır, sonra kodlanır.
>
> **Yan dosyalar:** `Cook_No_Evil_Menu.xlsx` (malzeme listesi, 12 hamburger varyantı, sinyal
> yükü analizi, seviye giriş sırası) — §6.6 ve §6.7'nin sayısal eki.

---

## İçindekiler

1. Konsept & Vizyon
2. Hedef Kitle & Platform
3. Çekirdek Döngü — *kazanma/kaybetme, sipariş süresi formülü, müşteri akışı, iletişim kod sistemi*
4. Roller & Asimetri Tasarımı — *duyusal kısıtlar, kontur render, highlight, işitme yarıçapı*
5. Sistemler — *pencereler, yangın, müşteri noktaları, paketleme, çöp, stok, round state, sos şişeleri*
6. Seviye / Mutfak Tasarımı — *üretim sistemi, birleştirme, kademeli açılım*
7. İlerleme & Skor — *yıldız sistemi, teslimat geri bildirimi, sipariş belirleme*
8. Multiplayer / Netcode Gereksinimleri — *lobi, disconnect, ayarlar*
9. Sanat Yönü
10. Ses Tasarımı — *VoIP matrisi, rol bazlı ses işleme*
11. Kapsam & Yol Haritası — *DeepJam takvimi, fazlar, LevelConfig mimarisi*
· İleride Değişebilecekler · Açık Sorular

---

## 1. Konsept & Vizyon

Cook No Evil!, 3 oyunculu asimetrik bir kooperatif mutfak oyunu. Her oyuncu, tek bir duyudan (görme, işitme veya konuşma) yoksun bir rol üstleniyor ve müşteri siparişlerini zamanında, eksiksiz ve doğru şekilde hazırlayıp teslim etmek için birbirine tamamen bağımlı hale geliyor.

**İsim kökeni:** Oyunun adı, üç bilge maymun deyiminden (Speak No Evil, Hear No Evil, See No Evil) geliyor — Kasiyer (dilsiz), Komi (sağır) ve Şef (kör) bu üçlünün mutfağa uyarlanmış hali.

Oyunun merkezinde **bilgi asimetrisi** var: siparişi sadece bir oyuncu görebiliyor, tarifi sadece bir diğeri duyabiliyor, yemeği sadece üçüncüsü hazırlayabiliyor. Başarı; iletişimin kısıtlı kanallardan (emote, ses, görsel pencereler) ne kadar hızlı ve doğru aktarıldığına bağlı.

**Ton:** Bombanana tarzı kaotik/mizahi/troll bir enerji, Overcooked'ın aile dostu çizgisiyle birleşiyor — karanlık mizah veya rahatsız edici içerik hedeflenmiyor; kör/sağır/dilsiz kısıtları gerilim değil, komik-kaotik yanlış anlaşılmaların kaynağı olarak kurgulanıyor.

Referans oyunlar: Overcooked (kaos + zaman baskısı + aile dostu ton), Bombanana (kademeli zorlaşan, mizahi/troll parti-oyunu temposu), Keep Talking and Nobody Explodes (asimetrik bilgi + kısıtlı iletişim kanalı), Moving Out (koop fizik/koordinasyon).

## 2. Hedef Kitle & Platform

- **Platform:** Sadece PC, sadece Steam (Facepunch Steamworks + Steam lobisi üzerinden). LAN/yerel çok oyunculu şu an kapsam dışı.
- **Kamera:** **First-person.** Tüm roller birinci şahıs bakışla oynanır; §3.6'daki jest sistemi buna dayanır (Komi, Kasiyer'in gövde/kol hareketlerini pencereden karşıdan görür).
- **Oturum yapısı:** 3 kişilik online koop, sabit.
- **Round süresi:** Sabit değil, seviyeye göre kademeli artıyor (bkz. §3.5) — referans model Bombanana: 1. seviye ort. 30-45 sn (tepe nokta 1 dk), 2. seviye ~1:30 + yeni bir mekanik/modül.
- **Hedef kitle:** Hem eğlence/troll-mizah arayan hem de zorlanmak isteyen oyuncular — ikisi bir arada hedefleniyor. Bombanana'nın kaotik-komik enerjisiyle Overcooked'ın zorlayıcı-ama-erişilebilir zorluk eğrisi arası bir nokta.
- **Ton/yaş uygunluğu:** Aile dostu, Overcooked çizgisinde; karanlık/rahatsız edici içerik yok.

## 3. Çekirdek Döngü

### 3.1 Üst Seviye Döngü
```
Karakterini Seç → Sipariş Al & İletişim Zinciriyle Hazırla → Siparişi Müşteriye Teslim Et → Yeni Seviyeye Geç → (tekrar) Karakterini Seç
```

### 3.2 Round Başlangıcı
- Lobide oyuncular rol seçer: **Kasiyer**, **Komi**, **Şef**.
- Roller sabit oda ataması ile başlar: Kasiyer → Kasa, Komi → Orta Oda / İstasyon, Şef → Mutfak.

### 3.3 Sipariş → Hazırlama → Teslim Zinciri
Aşağıdaki tam zincir **Hamburger** için geçerli — menüdeki tek ürün türü bu üç istasyonun tamamından geçiyor. Diğer ürün kategorilerinin (İçecek, Patates/Ekstra, Dondurma) kendi, daha kısa yolları var — bkz. §6.7 (Üretim Sistemi).

1. Müşteri kasaya gelir; kafasının üzerinde bir pop-up ile siparişini gösterir (istenen/istenmeyen malzemeler dahil). Bu pop-up **sadece Kasiyer** tarafından görülebilir.
2. Kasiyer, emote wheel (delta-bazlı el-kol emote sistemi) kullanarak siparişi Komi'ye aktarır.
3. Komi, emote'ları yorumlayıp tarifi **sesli olarak** Şef'e anlatır.
4. Şef, malzemeleri pişirip/birleştirerek yemeği hazırlar.
5. Şef, hazırladığı yemeği İstasyon/Mutfak penceresi üzerinden Komi'ye teslim eder (bkz. §5.1.2).
6. Komi, yemeği paketleyip Kasa/İstasyon penceresi üzerinden Kasiyer'e (dilsize) teslim eder (bkz. §5.1.1).
7. Kasiyer, paketlenmiş yemeği müşteriye teslim eder — zincir tamamlanır.

**Paket mantığı:** Bir siparişte birden fazla kategori varsa (örn. Hamburger + Patates + İçecek), hepsi tek bir paket olarak değerlendirilir. Paketteki herhangi bir bileşen eksik, yanlış veya hatalıysa (örn. peynirsiz istenen hamburgere şefin yanlışlıkla peynir koyup domatesi unutması) **paketin tamamı** 1 Hata sayılır — bkz. §3.4, §7.4.

### 3.4 Kazanma & Kaybetme Koşulları (netleşti ✓)

**Kazanma:** Her seviyenin önceden belirlenmiş bir **müşteri sayısı** vardır. Bu sayı yalnızca tasarımcı tarafından bilinir — **oyunculara gösterilmez**. O seviyedeki tüm siparişler 3 hata yapılmadan eksiksiz tamamlanırsa seviye kazanılır ve sonraki seviyeye geçilir.

**Kaybetme — 1 Hata sayılan durumlar:**
- Sipariş kendi süresi içinde teslim edilemezse.
- Sipariş yanlış veya eksik malzemeyle teslim edilirse.

**3 Hata**'da seviye kaybedilir.

**Son müşteri telegrafı (netleşti ✓):** Müşteri sayısı oyunculara **sayı olarak gösterilmez**, ama bölümün **son müşterisi** geldiğinde ortam bunu belli eder — tabela döner, müzik değişir, ışık kısılır gibi atmosferik bir sinyal. Sayı bilgisi vermeden bir doruk noktası ("bu sonuncu") yaratır; aksi halde bölüm hiçbir hazırlık olmadan aniden bitiyordu.

**Önemli — round geri sayımı YOK:** Seviyenin ayrı bir geri sayım sayacı yoktur. Zaman baskısı yalnızca **sipariş başına** işler (§3.4.1). §2 ve §3.5'te geçen "round süresi" değerleri bir geri sayım değil, seviyenin **beklenen toplam süresidir** (müşteri sayısı × sipariş süreleri). Bu ayrım implementasyon için kritiktir: kodda round timer diye bir şey olmamalı, yalnızca sipariş timer'ları olmalı.

### 3.4.1 Sipariş Süresi Formülü (yeniden kalibre edildi ✓)
Her siparişin kendi zaman çarkı vardır ve süresi **siparişin yüküne göre ölçeklenir**.

```
Sipariş Süresi = taban + (toplam sinyal sayısı × sinyal katsayısı)
```

**"Toplam sinyal sayısı" tanımı (netleşti ✓):** İletilen her **değer** bir sinyaldir:

```
Sinyal sayısı = 1 (protein) + garnitür adedi + sos adedi
                + [o seviyede açık olan diğer kanalların değerleri]
                + 1 ("Sipariş Bitti")
```

İç içe çarktaki **kategori seçimi tıklaması sayılmaz** — kategori bir sinyal değil, sinyale ulaşma adımıdır. Bir siparişin **tık sayısı**, sinyal sayısının 2 katıdır (kategori + değer); süre formülü tıkla değil, sinyalle ölçülür.

*Örnek:* et + 3 garnitür + 1 sos → **6 sinyal**, 12 tık → 32 + (6 × 4,3) = **57,8 sn**.

Formül kanal sayısından bağımsızdır: o seviyede hangi kanallar açıksa (§11.9) onların ürettiği değerler toplanır.

**Kalibrasyon dayanağı — iki referans nokta:**
| Sipariş | Sinyal sayısı | Hedef süre |
|---|---|---|
| Basit (tenders + kola) | 3 (ekstra + içecek + "bitti") | **45 sn** |
| Tam menü (burger + patates + içecek + dondurma) | 10 | **75 sn (1:15)** |

Bu iki noktadan türetilen değerler: **taban ≈ 32 sn**, **sinyal katsayısı ≈ 4,3 sn/sinyal**.

*Not: Bu değerler, emote animasyonlarının birbirini bloklaması (§3.6.0) hesaba katılarak belirlendi ve önceki taslaktaki (15 sn + 2,5 sn/sinyal) rakamların yerini aldı — o değerler animasyon süresi bilinmeden yazılmıştı ve çok dardı.*

**Hepsi tunable kalmalıdır** (ScriptableObject üzerinden). Gerçek emote animasyon süresi ölçüldükten sonra ilk playtest'te tekrar gözden geçirilecektir. Seviye bazında ayrıca bir çarpan uygulanabilir (erken seviyelerde cömert, geç seviyelerde sıkı).

### 3.4.2 Müşteri Akışı ve Eşzamanlılık (netleşti ✓)

- **Eşzamanlı üst sınır: 3 müşteri.** Restoranda aynı anda en fazla 3 müşteri bulunur.
- Seviyenin toplam müşteri sayısı bundan bağımsızdır (örn. 10. seviyede 5 müşteri). İlk 3'ü sırayla gelir ve teslim noktasında bekler; **bir siparişin teslimi tamamlandığında** kalan havuzdan bir sonraki müşteri içeri alınır.
- **Müşteriler arası aralık:** 10-15 sn *(başlangıç değeri, test edilecek)*.
- **Sabır süresi:** 20-30 sn *(başlangıç değeri, test edilecek)*.
- **Geliş sıklığı: örtüşmeli/artan model.** Seviye ilerledikçe müşteriler arası aralık kısalır ve üst üste binme kasıtlı olarak artar. 3'lük üst sınıra ulaşıldığında yukarıdaki kural devreye girer (yeni müşteri ancak bir teslimatla yer açılınca girer).
- Bu iki kural birlikte, geç seviyelerde "sürekli 3 müşteri dolu" durumunu doğal olarak üretir; erken seviyelerde ise aralık uzun olduğu için restoran çoğu zaman tek müşteriyle çalışır.

**Seviye parametreleri (level data):** toplam müşteri sayısı · müşteriler arası temel aralık · aralık kısalma eğrisi · yedek müşteri sayısı. Eşzamanlı üst sınır (3) global sabittir.

#### 3.4.3 Hazırlık Fazı (netleşti ✓)
Bölüm başladığında **ilk müşteri gelmeden önce 15 saniyelik bir hazırlık penceresi** vardır ve bu süre oyunculara görünür şekilde sayılır.

*Gerekçe:* Sos pompalarının yeri her bölümde değişiyor (§5.6) ve Komi'nin bunu okuyup Şef'e bildirmesi gerekiyor. Ayrıca garnitür numaraları da değişebiliyor (§3.6.3). İlk müşteri t=0'da gelseydi brifing için hiç zaman kalmaz, bu mekaniklerin tamamı işlevsiz olurdu.

#### 3.4.4 Müşteri Sabır Süresi ve Yedek Havuz (netleşti ✓)

Her müşterinin **iki ayrı sayacı** vardır:
1. **Sabır süresi** — müşteri geldiği andan Kasiyer siparişi alana (sol tık) kadar işler.
2. **Sipariş süresi** — sipariş alındıktan teslimata kadar işler (§3.4.1).

**Sabır çarkı yalnızca Kasiyer'e görünür** — müşterinin kafasında, sipariş alınmadan önce. (Sipariş alındıktan sonra yerini sipariş süresi çarkına bırakır, §3.6.1.) Komi ve Şef bunu göremez.

**Sipariş süresi çarkı da pratikte yalnızca Kasiyer'e görünür (netleşti ✓).** Çark müşterinin kafasında durur, müşteriler ise Kasa'da ve Kasa/İstasyon penceresinin solunda kalır (§5.1.1) — Komi onları göremez, Şef zaten göremez. **Komi ve Şef zaman baskısını yalnızca Kasiyer'den öğrenir.** Bu, oda yerleşiminin doğal sonucudur ve kasıtlıdır: zaman bilgisi de bilgi asimetrisinin bir parçasıdır ve iletişim zincirinden geçer.

**Sabır dolarsa:** müşteri **1 Hata** bırakıp dükkandan ayrılır. Bu, Kasiyer'in siparişleri sonsuza kadar almayıp oyunu kilitlemesini engeller.

**Yedek müşteri havuzu — bölüm başına 2 kişi.** Sabır hatası yüzünden bir müşteri giderse, bölümün kısalmaması için havuzdan yeni bir müşteri gelir. Bu havuz **yalnızca sabır hatasında** tetiklenir — yanlış/eksik teslim hatasında tetiklenmez (o müşteri zaten siparişini almış ve ayrılmıştır).

> **Havuz boyutunun doğrulaması:** 2 sayısı matematiksel olarak tam yeterlidir. Sabır hatası = 1 Hata; 3 Hata'da bölüm kaybedilir. Dolayısıyla bir oyuncu **en fazla 2 sabır hatası** yapıp oyunda kalabilir — 3.'sü zaten bölümü bitirir. Havuz hiçbir senaryoda tükenmeden oyun sona erer.

### 3.5 Round / Oturum İlişkisi
- Round ve oturum eşdeğer — ayrı bir "maç" katmanı yok.
- Oyun bir puzzle-oyunu gibi bölüm/seviye yapısında ilerliyor; seviye ilerledikçe tarif çeşitliliği genişliyor (aynı yemeğin varyantları, içecekler, patates kızartması vb. eklenerek zorluk artıyor).
- **Zorluk eğrisi referansı — Bombanana modeli:** Seviyenin beklenen süresi kademeli artıyor (1. seviye ~30-45 sn, 2. seviye ~1:30). Bu bir geri sayım değil, müşteri sayısı ve sipariş yüklerinin doğal sonucudur (§3.4, §3.4.1). Her yeni seviyede süre uzamasının yanında **yeni bir modül/mekanik** de ekleniyor — tam liste §6'da.

### 3.6 İletişim Kod Sistemi (Kasiyer → Komi Kanalı) — netleşti ✓

Kasiyer'in siparişi Komi'ye aktarma şekli (§3.3, adım 2), isimlendirilmiş bir tarifi ("Cheeseburger") göstermek **değil** — siparişin **ham bileşenlerini** ayrı ayrı kodlanmış fiziksel kanallardan iletmek. Bu sayede "hangi tarif" sorusu hiç ortaya çıkmıyor: Komi hiçbir zaman bir tarif adı öğrenmiyor, sadece bileşenleri tek tek çözüp Şef'e sözlü aktarıyor — tarif kimliği iletişimin hiçbir noktasında yer almıyor.

Her menü kategorisi, ayrı ve birbirinden ayrışık bir kanalla kodlanıyor:

| Kategori | Kanal | Nasıl Gösteriliyor |
|---|---|---|
| Ana Protein (Et/Tavuk/Veji/Balık) | **Yön** | Kasiyer 4 yönden birini işaret ediyor (yukarı/aşağı/sağ/sol) — tek seçim |
| Garnitür (çoklu seçim) | **Sayı** | Komi'nin önündeki numaralı listeye göre, Kasiyer dahil olan her garnitürü parmak sayısıyla **sırayla** gösteriyor. *(Mesafeden okunabilirlik Bombanana'da doğrulanmış kabul edildi; ek efekt eklenmeyecek.)* |
| Sos | **Renk** | Kasiyer'in eli renk değiştirip sallanıyor *(ilk versiyon — daha iyi bir fikir çıkarsa değişebilir)* |
| İçecek | **Şekil** | Kasiyer elini/kolunu kullanarak geometrik şeklin **kendisini vücuduyla oluşturuyor** (havada çizmiyor) — Daire/Kare/Üçgen/Zigzag, her biri bir içeceğe karşılık geliyor |
| Dondurma | **Vücut Bölgesi** | Kasiyer kendi vücudunun farklı bir yerine dokunuyor (Simon Says tarzı) — Baş/Göğüs/Karın/Bacak, her biri bir dondurma çeşidine karşılık geliyor |

**Önemli tasarım ilkesi:** Kod **oyun tarafından sabit tanımlı** — oyuncular arasında sıfırdan icat edilmiyor. Komi'nin önünde bu kodların hangi ham malzemeye karşılık geldiğini gösteren sabit bir referans tablosu (menü panosu) var. Zorluk, kodu icat etmekte değil, bu çok-kanallı sistemi **zaman baskısı altında hızlı ve hatasız icra etmek/okumakta**.

#### 3.6.0 Emote Giriş Sistemi — Kasiyer'in Sinyal Gönderme Arayüzü (netleşti ✓)

İki ayrı çark vardır:

**1. Sinyal çarkı — `R` tuşu, yalnızca Kasiyer.** 5 kategoriyi içeren **iç içe (nested)** bir çark: önce kategori (Yön / Sayı / Renk / Şekil / Vücut Bölgesi), sonra o kategorinin değeri seçilir.

**Çarkın içeriği tamamen veri odaklıdır (netleşti ✓).** Çarkta hangi kategorilerin görüneceği ve her kategoride hangi değerlerin bulunacağı `LevelConfig`'ten okunur (§11.9, §6.7.5) — koda gömülü bir kategori veya değer listesi **bulunmaz**. Kategori sayısı seviyeden seviyeye değişebilir: örneğin ilk seviyelerde yalnızca Yön/Sayı açıkken, sosun tanıtıldığı seviyede Renk, içeceğin tanıtıldığı seviyede Şekil kategorisi belirir ve çark büyür.

Çark kodu **kategori sayısına ve değer sayısına duyarsız** yazılır: N kategori, her kategoride M değer. N ve M yalnızca veriden gelir. Bu, §6.7.5'teki kademeli açılımın uygulama karşılığıdır.

Sinyal çarkında kategorilere ek olarak bir **"Sipariş Bitti"** jesti bulunur. Kasiyer her siparişin son sinyalinden sonra bunu gönderir.
*Gerekçe (kritik):* Aynı anda 3 müşteri olabildiği için Kasiyer bir siparişin sinyallerini gönderirken araya girip başka bir müşteriden sipariş almak zorunda kalabilir. Sınır işareti olmazsa Komi, gelen sinyal akışının nerede bitip nerede başladığını **bilemez** ve siparişler karışır. Bu jest hem sınırı çizer hem Komi'ye "aktarabilirsin" onayı verir.

**2. Genel emote çarkı — `E` tuşu, tüm roller.** Oynanışa etki etmeyen sosyal emote'lar (orta parmak, gözüm üstünde, tekrar et, kafam karıştı, esneme vb.). Her rolde vardır.

**Spam engeli — cooldown YOK:** Bir emote animasyonu **bitmeden** yeni bir emote başlatılamaz. Ne aynısı ne farklısı. Bu, cooldown eklemeden hem aynı emote'un hem farklı emote'ların art arda spam'lenmesini engeller.

**Animasyon iptali:** Emote animasyonu sırasında oyuncu bir nesneyle etkileşime girmek isterse (örn. eline bir şey almak) **animasyon anında iptal olur** ve etkileşim gerçekleşir. Emote, etkileşimi bloklamaz.

**Animasyon süresi — üretim hedefi (netleşti ✓):** Sinyal animasyonlarının hedef süresi **1,2–1,5 saniyedir**. §3.4.1'deki taban ve katsayı bu aralık varsayılarak kalibre edilmiştir.

| Animasyon süresi | 6 sinyallik aktarım | 57,8 sn'den geriye kalan |
|---|---|---|
| 1,2 sn | ~13 sn | 45 sn ✔ |
| 1,5 sn | ~15 sn | 43 sn ✔ |
| 2,5 sn | ~21 sn | 37 sn ⚠ |
| 3,5 sn | ~27 sn | 31 sn ✘ |

Kalan süreye Komi'nin sözlü aktarımı, Şef'in pişirme + birleştirme süresi, Komi'nin paketlemesi ve Kasiyer'in teslimi sığmak zorundadır. Animasyonlar **2,5 sn'yi aşarsa §3.4.1'deki katsayılar baştan türetilir.** Bu, animasyoncuya verilen bir üretim hedefidir; kodda süre animasyon klibinden okunur, sabit yazılmaz.

> ⚠ **DENGE NOTU:** §3.4.1 bu kısıt hesaba katılarak kalibre edilmiştir; yine de gerçek animasyon süresi ölçüldükten sonra tüm katsayılar playtest ile doğrulanmalıdır.
> Sipariş süresi formülünün **eski taslağı** (Hamburger tabanı 15 sn + 2,5 sn/sinyal — §3.4.1'deki güncel değerlerle **değiştirilmiştir**, artık geçerli değildir), emote animasyonlarının birbirini bloklayacağı bilinmeden yazıldı. 6 sinyallik bir siparişte Kasiyer'in yapması gerekenler: pop-up'ı oku → kitabı eline al → doğru sayfaya git → malzemeleri oku → kitabı bırak → 6 sinyali sırayla gönder (her biri: çark aç → kategori seç → değer seç → animasyon bitene kadar bekle). Sadece sinyal gönderimi, animasyon süresine bağlı olarak 15 saniyeyi bulabilir — üstüne Komi'nin aktarımı ve Şef'in pişirme+birleştirme süresi biner.
> **Bu yüzden §3.4.1'deki hiçbir sayı sabit kabul edilmemelidir.** Gerçek emote animasyon süresi ölçüldükten sonra, ilk playtest'te tüm katsayılar yeniden türetilecektir. Kodda hepsi ScriptableObject üzerinden ayarlanabilir olmalıdır.

#### 3.6.3 Kod Eşleşmelerinin Bölüm Başında Değişmesi (netleşti ✓)

**Garnitür numaraları belli seviyelerde değişir** (hangi seviyelerde değişeceği bir level parametresidir). Örneğin bir bölümde Marul=1 iken başka bir bölümde Marul=4 olabilir. Duvardaki malzeme listesi (§3.6.2) güncel eşleşmeyi gösterir; hem Kasiyer hem Komi listeden okur, yani sistem kendi içinde tutarlıdır. Zorluk, ezberine güvenip listeye bakmayan oyuncuyu cezalandırmaktan gelir.

**Protein yönleri de değişir.** Garnitür numaraları gibi, yön→protein eşleşmesi de belli seviyelerde kayar (bir bölümde Yukarı=Et iken başkasında Yukarı=Balık olabilir). Duvardaki liste güncel eşleşmeyi gösterir. Şef bundan etkilenmez — köfteleri zaten siluetinden tanır (§4.1.1), yön kodu yalnızca Kasiyer→Komi kanalında yaşar.

**Renkler değişmez.** Sos renkleri sabittir (Ketçap kırmızı, Hardal sarı, Mayonez beyaz, Barbekü kahverengi).

*Gerekçe:* Sayı keyfi bir etikettir — Marul'un 1 ya da 4 olması eşit derecede anlamsızdır, yeniden atanması temizdir. Renk ise anlam taşır: ketçabın kırmızı olması sezgiseldir, ayrıca sos pompaları da fiziksel olarak o renktedir (§5.6). Rengi yeniden atamak oyuncunun sezgisiyle savaşır ve beceri değil keyfilik hissi yaratır. Ayrıca her kanalın **tek bir değişkenlik kaynağı** olması gerekir: garnitürde bu numara kaymasıdır, sosta ise pompaların **konum** değişimidir (§5.6). İkisi üst üste binerse kafa karışıklığı becerinin önüne geçer.

#### 3.6.1 Sipariş Alma Akışı (netleşti ✓)

1. Müşteri Sipariş Penceresi'ne gelir. Kasiyer **sol tık** ile siparişi alır.
2. Müşterinin kafasında bir pop-up belirir: istenen ürünün **fotoğrafı**. İstenmeyen malzeme varsa yanında **çarpı (X)** işareti çıkar.
3. Ardından müşterinin kafasında bir **zaman çarkı** belirir ve müşteri Teslim Penceresi'ne geçer. Sonraki müşteriler orada **yan yana** dizilir (kuyruk değil) — böylece sonradan gelen bir siparişin önce bitmesi durumunda teslimat tıkanmaz.
4. **Pop-up müşterinin üstünde kalıcıdır** — Kasiyer, siparişi aktarırken dönüp tekrar bakabilir. Ayrı bir "fiş/sipariş listesi" arayüzü yok.
5. Kasiyer elindeki **menüden** o ürünün içeriğini bulur, ardından **malzeme listesinden** her malzemenin numarasını/kodunu bulup §3.6'daki kanallardan Komi'ye gösterir.

**Eksik malzeme asla ayrı bir sinyale dönüşmez.** Kasiyer her zaman "içinde ne VAR"ı iletir; domatessiz bir ürün, sadece daha kısa bir sayı dizisi demektir. Olumsuzlama/çarpı jesti yoktur — çarpı işareti yalnızca Kasiyer'in ekranındaki bir gösterim biçimidir. Komi menüyü göremediği için ürünün "standart" içeriğini zaten bilmez; dolayısıyla bir eksikliği fark etmesi de gerekmez.

#### 3.6.2 Referans Materyalleri (netleşti ✓)

**Tarif kitapçığı — Kasiyer'de, fiziksel nesne.** Bombanana'daki kılavuz gibi: tezgahta durur, **eline alınır ama envantere girmez**. Eline alındığında oyuncu kitaba kilitlenir; sol tık ile sayfa çevrilir.
- **Sayfa düzeni:** solda hamburgerin resmi (müşterinin kafasında çıkanla **aynı** görsel), sağda içerdiği malzemeler.
- **Kitaptan çıkış `ESC` iledir.** Kitap açıkken `ESC` duraklatma menüsünü açmaz, yalnızca kitabı kapatır — yani `ESC` bağlama duyarlıdır (kitap açık → kitabı kapat; değilse → duraklatma menüsü).
- **İlk sayfa içindekiler tablosudur.** Kategoriye (Et / Tavuk / Balık / Veji burgerleri) tıklanınca doğrudan o bölümün ilk sayfasına atlar — tek tek sayfa çevirmek gerekmez.

**Sayfa düzeni (netleşti ✓):** **Bir açılım = bir hamburger.** Solda o hamburgerin resmi (müşterinin kafasında çıkanla aynı görsel), sağda içerdiği malzemeler. 12 varyant + içindekiler açılımı = **26 sayfa**.

**Kitapçık veri odaklıdır (bağlayıcı).** Kodda sabit bir sayfa sayısı, sabit bir varyant listesi veya sabit bir sayfa içeriği **bulunmaz**. Kod yalnızca şunları sağlar:
- kitabın dış kabuğu ve açılım şablonu (sol görsel alanı / sağ malzeme listesi alanı)
- içindekiler açılımı, o anda açık olan varyantların kategorilerinden üretilir
- kategoriye tıklandığında o kategorinin ilk açılımına atlama
- sayfa çevirme ve `ESC` ile çıkış

Sayfaların **içeriği** ve **sayısı**, `LevelConfig`'te açık olan hamburger varyantlarından (§11.9, §6.7.5) türetilir. Varyant eklendiğinde/çıkarıldığında kitap kendini yeniden üretir; koda dokunulmaz. Böylece demo sürümünde az sayıda varyantla, tam sürümde 12 varyantla aynı kod çalışır.
- *Tasarım gerekçesi (Bombanana'dan doğrulandı):* kılavuz baştan sona okunan bir kitap değil, **"kapılar dizisi"**dir. Arama hızı, kategoriyi ne kadar hızlı tanımlayabildiğine bağlıdır.
- **Malzeme seti birebir aynı olan varyantlar** (örn. Deluxe Burger ↔ Veji Deluxe) **görsel olarak farklı çizilir** — pop-up ve kitaptaki resim aynı olduğu için Kasiyer ayırt edebilir.

**Malzeme listesi — duvarda, iki odada birden.** Hem İstasyon'da hem Kasa'da asılı durur. UI değil, fiziksel pano.
- *Gerekçe:* Komi hem listeyi hem Kasiyer'in jestlerini **aynı anda** görebilmeli. TAB gibi bir tuşa bağlansaydı panel açıkken jest kaçardı ve iletişim zayıflardı.
- **Yerleşim kısıtı:** Liste panoları, Komi'nin hem panoyu hem Kasa/İstasyon penceresini; Kasiyer'in de hem panoyu hem Komi'yi görebileceği şekilde konumlandırılmalıdır.

**Kasiyer'in mekânsal döngüsü (dikkat):** Kasiyer'in jestlerini Komi'nin görebilmesi için Kasiyer'in **Kasa/İstasyon penceresinin önünde** olması gerekir. Yani Kasiyer sürekli dört nokta arasında gidip gelir: sipariş penceresi → tarif kitabı → sinyal penceresi → teslim penceresi. Bu yürüyüş süresi, sipariş süresi hesabının (§3.4.1) görünmeyen bir parçasıdır; Kasa odasının yerleşimi doğrudan Kasiyer'in temposunu belirler ve bu dört nokta birbirine makul mesafede olmalıdır.

**Seviye tasarımı kısıtı:** Pop-up'lar müşterinin üstünde kaldığı ve ayrı bir fiş arayüzü olmadığı için, **Teslim Penceresi'ndeki kuyruk, Kasa/İstasyon penceresinden görülebilecek şekilde konumlandırılmalıdır** — aksi halde Kasiyer, Komi'ye sinyal verirken siparişi tekrar kontrol edemez ve tamamen ezbere kalır. Bu, oda yerleşimi için bağlayıcı bir gerekliliktir (bkz. §6.8).

**Peynir ve Ekmek (netleşti ✓):** Peynir ayrı bir kanal değil — **garnitür listesinin 5. maddesi** (sayı kanalı). Ekmek türü yok; her üründe aynı ekmek kullanılır ve hiçbir sinyal gerektirmez *(şimdilik — ileride değişebilir)*.

*Not: Bu bölümde gösterilen örnek menü (Main/Garnitür/Soslar: Et-Tavuk-Veji-Balık, Domates-Marul-Turşu, Barbekü-Ketçap-Mayonez) yalnızca kod mantığını açıklamak için çizilmişti — gerçek/tam menü daha kapsamlı olacak, bkz. §6.6. Referans görsel: `assets/menu-kod-sistemi-taslak.png`.*

## 4. Roller & Asimetri Tasarımı

### 4.1 Roller ve Duyusal Kısıtlar

| Rol | Oda | Duyusal Kısıt | Kendine Özgü Bilgi Erişimi | Üretim Sorumluluğu |
|---|---|---|---|---|
| Kasiyer | Kasa | Dilsiz (konuşamaz) | Sipariş pop-up'ını görebilen **tek** rol | *(yok — sadece sipariş alma/teslim, bkz. §3.3)* |
| Komi | Orta Oda / İstasyon | Sağır (duyamaz) | Etin pişme durumunu İstasyon/Mutfak penceresinden görebilen tek rol | İçecek + Dondurma |
| Şef | Mutfak | Kör (siyah-beyaz / düşük görüş render) | Hiçbir görsel bilgiye erişemez; sadece sözlü yönlendirmeyle çalışır | Hamburger + Patates + Ekstra (Tenders/Nuggets/Soğan Halkası) |

Hareket kısıtı yok — kısıtlar tamamen duyusal ve bilgiye erişimle ilgili.

**Envanter:** Her rol aynı anda **4 öğe** taşıyabilir (4 slotlu hotbar, 1-4 tuşlarıyla seçim). Şef bu sayede ekmeği ve köfteyi aynı anda taşıyıp hamburgeri kesintisiz birleştirebilir.

#### 4.1.1 Şef'in Görüşü — Kontur Render (netleşti ✓)
Şef dünyayı yalnızca **siyah zemin üzerinde beyaz kontur çizgileri** olarak görür (referans: Bombanana). Dolgu yok, renk yok, gölge yok, doku yok — sadece nesnelerin kenar çizgileri ve silueti.

**Bundan doğan tek kural:** Şef için **şekille** anlatılan her şey görünür; **renkle veya dokuyla** anlatılan her şey görünmezdir.

> **Bağlayıcı teknik kısıt — kenarlar geometriden çıkarılmalıdır.** Kontur shader'ı kenarları **depth + normal** tamponlarından (yani 3B biçimden) üretmelidir; **renk tamponundan ASLA**. Klasik bir Sobel/renk-kenarı filtresi kullanılırsa düz yüzeye basılı her şey — sos şişelerindeki desenler, etiketler, renk geçişleri — kenar olarak görünür hale gelir ve Şef nesneleri yüzey deseninden ayırt etmeye başlar. Bu, §5.6'daki sos konum bağımlılığını ve genel olarak "durum görünmez" kuralını sessizce yok eder. Bu kısıt, §4.1.1'in shader düzeyindeki karşılığıdır.

Doğrudan sonuçları:
- **Etin pişme derecesi renkle anlatıldığı için Şef göremez.** Komi'nin İstasyon/Mutfak penceresinden sözlü yönlendirmesi zorunlu hale gelir (§5.1.2). Bu bir yan etki değil, tasarımın amacıdır.
- Yangın da renk/parlaklıkla anlatıldığı için Şef için belirsizdir — §5.2.3'teki "Şef kendi yangınını söndüremez, Kasiyer müdahale eder" kuralıyla tutarlı.
**Temel kural — Kimlik görünür, durum görünmez (netleşti ✓):**
- **Kimlik siluetten okunur.** Bir nesnenin *ne olduğu* Şef için görünürdür: dana köfte / tavuk köfte / balık köfte birbirinden şekille ayrılır, malzeme kapları birbirinden ayrılır. Şef bunları Komi'ye sormadan bulabilir.
- **Durum asla okunmaz.** Bir nesnenin *hangi durumda olduğu* Şef için görünmezdir: çiğ mi pişmiş mi, ne kadar dolmuş — hiçbiri. Bu bilgiyi yalnızca Komi verebilir (§5.1.2).
- **Tek istisna — sos şişeleri:** Burada kimlik de kasıtlı olarak gizlenir (§5.6). Tüm şişeler aynı siluete sahiptir, yalnızca renkle ayrılır; Şef hangisinin hangisi olduğunu Komi'den öğrenmek zorundadır.

- **Bağlayıcı sanat kısıtı (rafine edilmiş):** Kural "her şey siluetle ayrışsın" değil, şudur — **Şef'in bağımsız bulabilmesi gereken nesneler siluetleriyle ayrışır; Komi'ye bağımlı olmasını istediğimiz nesneler kasıtlı olarak ayrışmaz.**
  - **Siluetle ayrışmalı:** malzeme kapları (marul/domates/turşu/soğan/peynir), ızgara, fritöz, paketleme alanı. Şef bunları yardımsız bulabilmeli, yoksa rol oynanamaz hale gelir. Her birinin farklı bir form/gövde şekli olmalı.
  - **Kasıtlı olarak ayrışmamalı:** **sos şişeleri** — bkz. §5.6. Bunlar birbirinin aynı siluete sahiptir ve yalnızca renkle ayrılır; Şef renk göremediği için hangisinin hangisi olduğunu Komi'den öğrenmek zorundadır.

#### 4.1.3 Komi'nin Sağırlığının Kapsamı (netleşti ✓)
Komi tamamen sağır değildir — **çok dar bir yarıçapta**, aşırı boğuk ve kısık şekilde duyar.
- **Duyar:** yanındaki makinelerin sesi (örn. dondurma makinesini kullanırken onun sesi) — boğuk ve zayıf.
- **Duymaz:** uzaktaki hiçbir şey. Mutfaktaki ızgarada pişen etin cızırtısı Komi'ye ulaşmaz (mesafe yeter).
- **Şef'in sesi:** duyar ama anlamaz — bkz. §10.4.

#### 4.1.2 Etkileşim Vurgusu (Highlight) — Çift Görsel (netleşti ✓)
Oyuncu bir nesneye bakıp etkileşime girebildiğinde nesne vurgulanır. **Tek bir "vurgulu" durumu vardır ama iki farklı görsel karşılığı olur:**
- **Kasiyer / Komi (normal render):** standart renk/emissive vurgu (örn. Komi dondurmaya parçacık eklerken hedefin yeşil parlaması).
- **Şef (kontur render):** renk işe yaramaz — hedefin **kontur çizgisi kalınlaşır / parlar / nabız gibi atar**. Şef'in dünyası zaten çizgilerden oluştuğu için bu mükemmel okunur.

Bu sistem mevcut kodda **hiç yok** — sıfırdan kurulacak (mevcut `PlayerInteractor` yalnızca ham raycast etkileşimi yapıyor, görsel geri bildirim üretmiyor).

**Etkileşim menzili bir playtest parametresidir (netleşti ✓).** GDD sabit bir değer tanımlamaz; Inspector'dan ayarlanabilir kalır (şu anki değer 2,5 m) ve ilk playtest'te belirlenir. Menzil değeri tasarım kararı olarak değil, denge parametresi olarak ele alınır.


Üretim sorumluluğu dağılımı §6.7'de detaylandırılıyor — Komi'nin kendi kategorileri için Şef'e ihtiyaç duymadan tek başına üretim yapabilmesi, çekirdek döngüye (§3.3) paralel, daha kısa bir üretim yolu ekliyor.

### 4.2 İsimlendirme
"Yamak" ismi yerine **Komi** onaylandı — gerçek mutfak hiyerarşisinde şef ile servis arasında koşuşturan, malzeme/tabak taşıyan yardımcı personeli tanımlıyor; rolün fonksiyonuna (bilgi + malzeme taşıyıcısı, şef ile kasiyer arasındaki köprü) birebir uyuyor.

### 4.3 Rol Sabitliği ve İstisna (netleşti ✓)
- Roller birbirinin yerine geçemez — kesin görev ayrımı, esneklik yok.
- **Tek istisna:** yangın acil durumu (bkz. §5.2) — Kasiyer, Kasa/Mutfak arasındaki özel kapıdan fiziksel olarak Mutfağa girebiliyor. Bu, normal iş akışının değil, acil durum protokolünün parçası; Şef kör olduğu için kendi yangınını söndüremiyor, dışarıdan müdahale gerekiyor.
- Bu istisna yalnızca Et'in yanmasıyla tetiklenen gerçek yangın için geçerli. Mutfak'taki Patates/Ekstra (fritöz) yanması bir yangın değil, sessiz bir ürün israfı — dolayısıyla ayrı bir müdahale/istisna gerektirmiyor (bkz. §5.2.1, §6.7).

## 5. Sistemler

Not: Oyunda ayrı bir **Dumbwaiter** mekanizması yok. Oda-arası birincil etkileşim, üç farklı pencere/panel tipi üzerinden yürüyor.

### 5.1 Oda Arası Etkileşim: Pencere / Panel Sistemi

#### 5.1.1 Kasa/İstasyon Penceresi (Kasiyer ↔ Komi)
- Karşılıklı görüş: **var**. Ses geçişi: **var**.
- Müşteriler pencerenin solunda kaldığı için Komi müşterileri göremez — dolayısıyla sipariş pop-up'ını da göremez. **Bilgi asimetrisinin fiziksel kaynağı burası.**
- Sadece Komi bu pencereden malzeme/paket koyabiliyor.
- Komi'nin paketlediği yemek bu pencereden Kasiyer'e (dilsize) geçer.

#### 5.1.2 İstasyon/Mutfak Penceresi (Komi ↔ Şef)
- Karşılıklı görüş: **var**. Ses geçişi: **var**.
- Komi, etin pişip pişmediği gibi görsel durumları buradan takip edip Şef'i yönlendirebilir — Şef'in kendi göremediği bilgiyi dışarıdan tamamlayan tek kanal.
- Şef, tarifi Komi'nin sözlü yönlendirmesiyle hazırlar ve bitmiş yemeği bu pencereden teslim eder; sadece Şef bu pencereden malzeme/paket koyabiliyor.
- **Kapasite:** aynı anda **3 hamburger + 3 yan ürün** durabilir. Komi almadan da Şef üretmeye devam edebilir.

#### 5.1.3 Kasa/Mutfak Paneli — Intercom (Kasiyer ↔ Şef, Komi'yi atlar)
- Karşılıklı görüş **yok**, ses geçişi **yok** (normal koşullarda).
- Sadece Mutfak tarafındaki bir buton üzerinden Şef, diyafon aracılığıyla Kasa'dan malzeme talep edebilir.
- Kasiyer, Şef'in istediği malzemeleri bu panelden Mutfak'a geçirebilir.
- **Kullanım amacı:** "malzeme bitme" durumu — normal iletişim zincirinin (Kasiyer→Komi→Şef) dışına çıkan, Komi'yi devre dışı bırakan acil/doğrudan bir tedarik kanalı.

### 5.2 Acil Durum Sistemi — Yangın

#### 5.2.1 Pişirme Fazları
Ateşle temas eden her ürün (Et, Patates, Ekstra — hepsi Mutfak'ta, bkz. §6.7) aynı faz ilerlemesinden geçiyor:

```
Çiğ → Pişmiş → Yanmış
```

**Tek pişirme modeli (netleşti ✓):** Et, Patates ve Ekstra kalemlerinin hepsi aynı üç fazdan geçer. Ara fazlar (Az/Orta/İyi Pişmiş) kaldırıldı — hem kod hem iletişim tek bir modele indi. Doğru hedef **Pişmiş**'tir; erken alınan ürün çiğ kalır ve hatalıdır, geç kalınırsa yanar.

*[Faz süreleri test/placeholder — henüz final değil]* Her faz geçişi 3-10 sn aralığında, ürün bazında ayarlanabilir olmalıdır.

**İlerleme verisi nesnenin üstünde durur (netleşti ✓):** Pişme ilerlemesi makinede değil, **pişen nesnenin kendisinde** tutulur. Yani yarıda alınıp geri konan bir ürün **kaldığı yerden devam eder**, sıfırdan başlamaz.
- Gerekçe (tutarlılık): dondurma dolumu (§6.7.2) ve içecek dolumu (§6.7.1) zaten bu deseni kullanıyor — fritözde sıfırlama yapmak aynı oyunda iki zıt kural yaratırdı.
- Yan fayda: yarım pişmiş bir eti bir ızgara slotundan diğerine taşımak da ilerlemeyi sıfırlamaz; kural bedava geliyor.
- Adalet gerekçesi: Şef zaten pişmişliği göremiyor; sıfırlama cezası, en duyu-kısıtlı role en sert yükü bindirirdi.

**Önemli fark (netleşti ✓):** Sadece **Et**'in yanması gerçek bir yangın olayı tetikliyor (§5.2.3). **Patates/Ekstra** yanınca yangın çıkmıyor — ürün sadece israf olup çöpe gidiyor, Şef yeniden başlatıyor. Aynı görsel faz dizisini (Çiğ→...→Yanmış) paylaşsalar da sonuçları farklı: Et = yangın olayı, Patates/Ekstra = sessiz ürün kaybı.

#### 5.2.1.1 Izgara Mekaniği (netleşti ✓)

- Izgara **küçüktür ve aynı anda yalnızca 2 et alır** — 3D model bu kapasiteye göre tasarlanır.
- Şef eti eline alır; ızgarada **yerleştirilecek yuva belirginleşir** ve sol tık ile et o yuvaya sabitlenir (attach).
- Et pişerken §5.2.1'deki fazlardan geçer. Şef, Komi'den gelen sözlü bilgiye göre sol tık ile eti tekrar eline alır.

**Kapasite kısıtı bir darboğazdır:** Aynı anda 3 müşteri olabildiği için (§3.4.2) Şef siparişleri ızgara sırasına sokmak zorunda kalır. Bu bilinçli bir baskı kaynağıdır.

**Şef'in göremedikleri ve doğan bağımlılık:**
- Yuvanın kendisi **şekille** okunabilir olmalıdır (ızgarada girintili bir yatak gibi) — kontur render'da renkli bir highlight görünmez (§4.1.1).
- Izgaradaki etlerin **türü** siluetten okunur (dana/tavuk/balık köfte birbirinden ayrılır, §4.1.1) — Şef hangisinin hangi protein olduğunu Komi'siz bilir.
- Ama **pişmişliği** okunmaz. Komi **konum üzerinden** yönlendirmek zorundadır ("soldaki hazır, sağdaki değil").
- Bu, §5.6'daki sos şişeleri mekaniğiyle aynı desendir: Şef'in dünyası konumlardan ibarettir, anlamı Komi verir.

**Doğru pişmişlik:** Yalnızca **Pişmiş** kabul edilir. Erken alınan et çiğ kalır ve ürün hatalıdır; geç kalınırsa yanar ve yangın çıkar (§5.2.2). Müşteri siparişlerinde pişmişlik tercihi yoktur — tek doğru hedef vardır.

#### 5.2.2 Yangının Sonuçları (netleşti ✓)
- Et'in yanması **sadece ızgarayı** kilitliyor — yangın çıkar, ızgara söndürülene kadar yeni et konulamaz, ızgarada pişmekte olan diğer etler de yanar. Fritöz (Patates/Ekstra) bundan etkilenmiyor, Şef ateş sırasında da kızartmaya devam edebilir.
- Bu bir global Mutfak kilidi değil — yalnızca yanan ekipman (ızgara) kilitleniyor.
- Yangın çıktığı anda alarm çalar.
- İlgili siparişler zamanında yetiştirilemezse normal kurallar işler: **1 Hata** (§3.4), 3 Hata'da round kaybedilir. (Patates/Ekstra'nın israf olması da aynı şekilde gecikmeye ve dolayısıyla Hata'ya yol açabilir — ama yangın/alarm/kapı olmadan.)
- Bu risk **rastgele değil, tamamen önlenebilir**: Komi'nin İstasyon/Mutfak penceresinden pişme durumunu görebilmesi (§5.1.2) yeterli bir erken uyarı sistemi olacak şekilde tasarlandı — dikkatli oynanırsa yangın hiç çıkmaz.

#### 5.2.3 Mutfak Yangını — Söndürme Protokolü (netleşti ✓)
- Et yandığı anda yangın çıkar; bu andan itibaren yangın tüpü **Kasiyer (dilsiz)** tarafından alınabilir hale gelir. Tüp **Kasa'da** duruyor.
- Kasiyer tüpü eline aldığı anda Kasa/Mutfak kapısı açılır (tüp elde olmadan kapı açılmaz).
- Kasiyer Mutfağa girip yangını söndürür; tüp yerine konduğunda kapı otomatik kapanır.
- Rol sabitliğinin (§4.3) istisnası — Şef kör olduğu için kendi yangınını göremiyor/söndüremiyor.

**Söndürme mekaniği (netleşti ✓):** Kasiyer **sol tık basılı tutarak** püskürtür. Söndürme, birikimli bir ilerleme değeri üzerinden işler ve **bırakıldığında geri sayar**:
- Alev, biriken ilerlemeyle görsel olarak küçülür (hedefe yaklaştıkça kıvılcım seviyesine iner).
- Tuş bırakılırsa ilerleme azalmaya başlar ve alev yeniden büyür.
- *Örnek:* 5 sn gerekiyorsa ve Kasiyer 4 sn tuttuysa alev kıvılcıma iner; 1 sn bırakırsa ilerleme 3 sn'ye geriler; 2 sn daha tutarsa yangın tamamen söner.
- Gerekli süre ve geri sayma hızı ayrı tunable parametrelerdir.

*Mimari not:* Bu, §5.2.1'deki pişme ilerlemesi ve §6.7.1'deki dolum ilerlemesiyle **aynı desendir** — sunucu sahipli bir ilerleme değeri. Tek farkı, bırakıldığında geri sayması. Ortak temel sınıfın opsiyonel bir "decay" parametresi olmalıdır.

### 5.3 Müşteri Etkileşim Noktaları
- **Sipariş Penceresi:** Kasiyer müşterinin siparişini burada alır (§3.6.1).
- **Teslim/Kasa Penceresi:** Müşteri, hazırlanan siparişini burada teslim alır. Müşteriler burada **yan yana** dizilir — kuyruk/FIFO yok, herkes her an servis edilebilir.
- **Sipariş pop-up'ı teslim noktasında da görünmeye devam eder** (numara değil, istenen siparişin kendisi). Kasiyer, paketin üzerindeki içerik fotoğrafını müşterilerin üstündeki sipariş pop-up'larıyla karşılaştırarak doğru kişiyi bulur.

### 5.3.1 Paketleme ve Teslim (netleşti ✓)

**Paketleme — Komi:**
- Komi, Şef'ten gelen ürünleri kese kağıdına koyar. **Numara/etiket yok** — Komi zaten siparişleri Şef'e kendisi aktardığı için hangi ürünün hangi siparişe ait olduğunu **içeriğinden tanır**.
- Paketin üzerinde **içeriğin fotoğrafı** görünür (hamburger, patates vb. + varsa eksik malzeme işaretleri). Bu fotoğraf paketin **gerçek içeriğini** gösterir — sipariş fişini değil. Dolayısıyla Şef yanlış yaptıysa fotoğraf da yanlış görünür ve hiçbir müşteriyle eşleşmez.
- Komi'nin aynı anda uçuşta olan siparişleri akılda tutması gerekir — bu, Komi'ye aktarım görevinin ötesinde gerçek bir bilişsel yük verir.

**Paketleme mekaniği (netleşti ✓):**
1. Komi sol tık ile bir **kese kağıdı** alır — elindeyken katlı görünür.
2. Paketleme alanına sol tık ile bırakır; kağıt orada **ağzı açık** durur (fast-food zincirlerindeki gibi).
3. Şef pencereye bir ürün bıraktığında Komi sol tık ile onu eline alır, ardından ağzı açık kese kağıdına sol tık yapar — ürün paketin içine girer.
4. Komi kendi ürettiği içecek ve dondurmayı da aynı şekilde pakete koyar.
5. Hazır paket **Kasa/İstasyon penceresine** bırakılır (§5.1.1) ve Kasiyer oradan alır.

**Paketleme alanı sayısı: 2.** Aynı anda 3 müşteri olabildiği için tek alan ciddi bir jonglörlük yaratırdı. *(Test sonrası artırılabilir.)*

**Kese kağıdının görsel durumları:**
- Boş ve elde → **katlanmış** görünür.
- Paketleme alanına konmuş → **ağzı açık** durur, ürün eklenebilir.
- İçi dolu ve elde → **ağzı kapalı** görünür.
- **Kapalı bir paket yeniden açılabilir:** Komi paketi tekrar paketleme alanına sol tık ile koyarsa ağzı yeniden açılır ve eksik kalan ürün eklenebilir.

**Paket, siparişle birebir eşleşmelidir.** Sadece hamburger isteyen bir müşterinin paketine kola da konursa bu **1 Hata**'dır — fazlalık da eksiklik kadar hatalıdır.

**Hata kuralı — geri alma yok:**
- Komi bir ürünü yanlış pakete koyarsa, **o paketin tamamı çöp olur**. Ürünü geri alıp doğrusuna koyma yok *(şimdilik — test sonrası değişebilir)*.
- Bu, Komi'nin paketleme adımını gerçek bir risk noktası yapar: Komi yalnızca bir aktarım katmanı değil, kendi başına hata üretebilen bir istasyondur.

**Teslim — Kasiyer:**
- Kasiyer, paketin içerik fotoğrafını teslim noktasındaki müşterilerin sipariş pop-up'larıyla eşleştirip doğru müşteriye verir. Müşteriler yan yana durduğu için sıralama kısıtı yoktur.

**Tasarım notu — paket fotoğrafının etkisi:** Fotoğraf hatayı *önlemez*, **açığa çıkarır**. Dikkatli bir Kasiyer yanlış paketi teslim etmeden yakalar; ama paket çöpe gittiği için ürün baştan yapılır ve bu süre kaybı çoğu zaman gecikmeye, dolayısıyla yine **1 Hata**'ya yol açar. Sonuç olarak §3.4'teki "yanlış malzemeyle teslim" koşulu, sistematik bir hata olmaktan çıkıp yalnızca dikkatsiz oyuncunun düştüğü bir duruma dönüşür; hataların çoğu "geç teslim" kanalından işler.

**Üç rolün kendi hata kaynağı (tasarım dengesi):** Kasiyer yanlış kodlar → Şef yanlış hatırlar/uygular → Komi yanlış paketler. Her rol, kendi başına tüm siparişi çöpe gönderebilir; hiçbiri sadece aktarım katmanı değildir.

### 5.3.2 Çöp / İmha Mekaniği (netleşti ✓)
- Herhangi bir öğe çöpe atılabilir (yanmış et, çiğ kalmış ürün, yanlış paket).
- Şef öğeyi sol tık ile eline alır, çöp kutusuna sol tık ile atar. **Atılan öğe imha edilir, geri alınamaz.**
- Yangın akışı: yangın çıkar → Kasiyer söndürür → **yanmış et alınabilir hale gelir** → Şef eline alıp çöpe atar. Yanmış et söndürülmeden alınamaz.

**Faz kısıtı (netleşti ✓):** Yukarıdaki kural **Faz 1'den itibaren** geçerlidir. Faz 0'da yangın sistemi kapsam dışıdır (§11.2): et yanar, "Yanmış" durumuna geçer ve **söndürme gerekmeden alınabilir**, doğrudan çöpe atılır. Alarm, kapı, yangın tüpü ve ızgara kilidi Faz 0'da yoktur. Bedel korunur (ürün kaybı → gecikme → 1 Hata), altyapı ertelenir. Bu bir faz kısıtıdır, tasarım değişikliği değildir.

### 5.4 Malzeme Tedarik ve Stok Sistemi

Ayrı bir dumbwaiter/asansör yok; malzeme aktarımı §5.1.3'teki Intercom paneli üzerinden yapılır. Şef'in malzemesi bittiğinde intercom'la Kasiyer'den ister, Kasiyer panelden gönderir. Gönderim başına **1 porsiyon**.

#### 5.4.1 Stok Modeli — Seviye Parametresi (netleşti ✓)

**Çözülen problem:** Stok miktarı global bir sabit olursa denge kurulamaz. Bir seviyede 5 müşteri varsa toplam tüketim malzeme başına yalnızca 2-3 porsiyondur; kaplarda 10-15 porsiyon durursa hiçbir zaman bitmez, 1-2 porsiyon durursa Şef sürekli intercom'da kalır. İki uç arasında işe yarayan bir global sayı yoktur.

**Kök neden:** Tüketim zaten tasarımcı tarafından biliniyor. Seviyedeki siparişler yazılıyken kaç soğan harcanacağı da belli — yani "bitecek mi?" emergent bir soru değil, bir simülasyonun içine gizlenmiş bir tasarımcı kararıdır.

**Karar:** Başlangıç stoğu global sabit değil, **seviye başına + malzeme başına tasarımcı parametresidir**. Tasarımcı, krizin tam olarak hangi müşteride patlamasını istiyorsa stoğu ona göre ayarlar (örn. 10. seviyede soğan kriziinin 3. müşteride çıkması isteniyorsa, soğan stoğu ilk 2 müşterinin tükettiği kadar verilir).

İki tanım biçimi desteklenmelidir:
- **Stok bazlı:** `malzeme → başlangıç porsiyon sayısı`. Sabit (deterministik) siparişli seviyelerde tüketim öngörülebilir olduğu için kriz anı kesin olarak konumlandırılır.
- **Tetik bazlı:** `N. teslimattan sonra X malzemesi biter`. Randomize siparişli seviyelerde (§7.3) tüketim öngörülemez olduğundan bu biçim kullanılır.

Stoğun sınırsız olması da geçerli bir ayardır — **ilk seviyelerde bitme mekaniği hiç devreye girmez** (bkz. §6.4, kademeli tanıtım ilkesi).

#### 5.4.2 Stoğun Görünürlüğü ve İnandırıcılık (netleşti ✓)

**Problem:** Stok mekaniğinin çalışması için kapların az sayıda porsiyon tutması gerekir (bir seviyede malzeme başına tüketim 2-3 porsiyondur). Ama "peynir kabı" denen bir nesnenin içinde 2 peynir olması oyuncuya **saçma ve komik** görünür. Kabı dolu göstermek için miktarı artırırsak bu sefer malzeme hiç bitmez. Bu bir denge değil, **inandırıcılık** problemidir.

**Kök neden:** Nesne, taşıdığından fazlasını vaat ediyor. "Kap" toplu depolama çağrıştırır; içinde 2 şey olunca beklenti ihlal edilir. Aynı miktar, başka bir nesne biçiminde tamamen normal görünür — gerçek mutfaklarda şef arkadaki depodan değil, **hattaki küçük servis kabından** alır ve orada zaten 3-4 porsiyon durur.

**Üç katmanlı çözüm:**

**(a) Biten malzemeler, azlığı inandırıcı olan kaplarda durur:**

| Malzeme | Kap biçimi | Neden inandırıcı |
|---|---|---|
| Et | Küçük köfte tepsisi | Hat üstünde 3-4 köfte normaldir |
| Peynir | Dilim yığını / küçük tepsi | Açılmış paketten birkaç dilim kalması normaldir |
| Sos | **Şişe / pompa** | Bir sos şişesinin bitmesi mutfaktaki en inandırıcı olaydır |
| Patates | Porsiyonluk sepet | Porsiyonlanmış olması beklenir |

**(b) Azaldığında saçma duracak malzemeler hiç bitmez.** Marul, domates, turşu, soğan açık hazırlık kaplarında durur ve gerçek mutfaklarda sürekli dolu tutulur — bunlar **hiçbir zaman tükenmez**. Bu hem inandırıcılığı korur hem de biten malzeme setini 3-4 kaleme indirir; §5.4.3'te savunulan "intercom nadir olmalı" ilkesini otomatik olarak sağlar.

**(c) Kaplar dolu başlar, "az kalmış" hali geçicidir.** Yalnızca birkaç malzeme ve yalnızca geç seviyelerde tükendiği için oyuncu yarı boş bir kaba uzun süre bakmaz; rahatsız edici durum saniyeler sürer, ardından takviye gelir.

**Yan fayda — Şef'in okunabilirliği:** Kontur render'da (§4.1.1) bir şişenin boşalması bir **silüet değişimidir**, net görülür. Kaptaki dilim sayısının 4'ten 3'e düşmesi çok daha zayıf bir sinyaldir. İnandırıcılık düzeltmesi ile Şef'in stoğu önceden fark edebilmesi aynı yöne çıkar: biten malzemeler için **şişe/yığın gibi silüeti değişen** formlar tercih edilmelidir.

#### 5.4.3 Tasarım Değerlendirmesi — Intercom'un Maliyeti
Intercom, oyundaki **tek** Kasiyer↔Şef doğrudan kanalıdır ve bu yönüyle oyunun temel kısıtını (her bilgi Komi'den geçer) zayıflatır. Ayrıca zaten en yüklü rol olan Kasiyer'in dikkatini böler. Buna karşılık kötü zamanlanmış kesinti, hedeflenen kaotik tonla (§2) uyumludur.

**Sonuç:** Mekanik korunuyor ama **nadir ve geç** olmalı. §5.4.1'deki seviye-bazlı stok parametresi bu nadirliği zaten otomatik sağlar.

### 5.5 GameLoopManager — Round State Mimarisi *(Uygulandı ✓)*

**Mevcut implementasyon (Claude Code'dan doğrulandı):** `GameLoopManager` şu an bilerek minimal bırakılmış bir iskelet. Tek görevi `IsGamePaused` bayrağını tutmak ve bir oyuncu bağlantısı koptuğunda/geri döndüğünde diğer oyuncuların hareket/etkileşimini kilitleyip çözmek. Round akışı (`IsRoundActive`) hâlâ ayrı bir sınıfta, `RoleManager` içinde tutuluyor ve "Oyunu Başlat" butonuyla tetikleniyor. Sipariş üretimi/zamanlaması, hata sayacı (3 hak) ve zorluk ölçekleme — GDD §3 ve §6-7'de tanımlanan Bileşen 2 kapsamı — hiçbirinin kodu henüz yazılmadı.

**Değerlendirme:** Bileşen 2'yi (round/sipariş/strike) askıya alıp önce networking temelini (Player Controller, disconnect/reconnect/pause) sağlamlaştırmak doğru bir sıralamaydı — temel olmadan oyun mantığı kurmak daha kırılgan olurdu. Ancak şu anki durumda iki sorun var:

1. **İsim/sorumluluk uyumsuzluğu:** "GameLoopManager" ismi round/sipariş/skor akışını yönetecekmiş izlenimi veriyor, ama şu an yalnızca pause bayrağı tutuyor.
2. **Dağılmış round durumu (kritik):** `RoleManager.IsRoundActive` ve `GameLoopManager.IsGamePaused` birbirinden habersiz, bağımsız iki bayrak. Bileşen 2 eklendiğinde bu somut hatalara yol açar — örn. bir oyuncu bağlantıyı kaybettiğinde sipariş süresi işlemeye devam ederse, dönen oyuncu anlamsız bir "1 Hata" ile karşılaşır; ya da pause sırasında round bitişi tetiklenebilir.

**Mimari karar (Bileşen 2'ye başlamadan önce yapılmalı):** Tek bir "Round State" otoritesi tanımlanmalı — sipariş üretimi/timer/strike de bu sınıfta toplanacağı için mantıklı yer `GameLoopManager`; `RoleManager` sadece rol atamasında kalmalı. Önerilen model:

- State machine: `Lobby → RoundActive → RoundEnded (Win/Loss) → Results`
- `Paused` ayrı bir state değil, sadece `RoundActive` iken anlamlı olan bir overlay/flag.
- Davranış kuralları: pause sırasında sipariş timer'ı donar, yeni hata sayılmaz, round bitişi tetiklenemez; resume'da timer kaldığı yerden devam eder.

Bu birleştirme yapılmadan sipariş üretimi/strike/zorluk sistemi üstüne inşa edilirse, aynı "iki otorite" problemi büyüyerek devam eder. Claude Code'a Bileşen 2'yi yazdırmadan önce bu state konsolidasyonunu netleştirmeni öneririm (bkz. §8 — bu bayraklar muhtemelen `NetworkVariable`, senkronizasyon açısından da tek otorite olması gerekiyor).

**Uygulama sonucu:** `RoleManager.IsRoundActive`/`StartRound()` kaldırıldı, tek otorite `GameLoopManager.CurrentRoundState`/`StartRound()` oldu. Play Mode doğrulaması yapıldı: Lobby'de pause no-op, round aktifken disconnect pause'u doğru tetikliyor, reconnect pause'u kaldırıp rolü geri veriyor, RoundEnded'da pause no-op. Gerçek sipariş timer'ı/hata sayacı henüz yok — bu, Bileşen 2 ile birlikte gelecek ve o zaman uçtan uca (disconnect → timer donuyor → reconnect'te kaldığı yerden devam) test edilecek. Bu GDD'nin tasarım kararları açısından konu kapandı; kalan iş implementasyon tarafında.

### 5.6 Sos Şişeleri — Konum Bağımlılığı (netleşti ✓)

Sos şişeleri, Şef'in körlüğünü oynanışta gerçekten ısırtan tek sistemdir.

**Kural:**
- Tüm sos şişeleri **aynı siluete** sahiptir; birbirlerinden yalnızca **renk + desen** ile ayrılırlar (Ketçap kırmızı, Hardal sarı, Mayonez beyaz, Barbekü kahverengi).

**Çift kodlama — renk + desen (netleşti ✓):** Her sos rengine bir **desen** eşlik eder. Desen **iki yerde** görünür: **sos pompasının üstünde** ve **duvardaki malzeme listesinde** (§3.6.2), böylece hangi desenin hangi sos olduğu okunabilir.
- *Amaç:* Mutfağı pencereden okuyan Komi (§5.1.2), pompaları renk ayrımı yapamasa bile desenlerinden tanıyıp Şef'i yönlendirebilir.
- **Desenler kaba olmalı** — ince desen pencere arkasından okunmaz. Örn. düz şerit / tırtıklı / düz (desensiz) / noktalı.
- **Şef bu desenleri göremez** — kontur shader geometri tabanlı olduğu için düz yüzeye basılı desen ona ulaşmaz (§4.1.1). Yani desen eklemek §5.6'daki bağımlılığı bozmaz.
- **Bilinen sınır (kayda geçirildi):** Kasiyer'in el sinyali yalnızca **renk** taşır, desen taşımaz. Dolayısıyla renk ayrımı yapamayan bir oyuncu Komi rolünde sos kanalını okuyamaz. Bilinçli bir karardır; ileride el sinyaline de desen eklenerek kapatılabilir.
- Şef kontur render ile gördüğü için (§4.1.1) **renk göremez** — yani şişeleri birbirinden ayırt etmesi fiziksel olarak imkânsızdır. Şef için tek ayırt edici bilgi **konumdur**.
- Dolayısıyla hangi şişenin hangi sos olduğunu Şef'e yalnızca **Komi** söyleyebilir (İstasyon/Mutfak penceresinden, §5.1.2).

**Şişe konumları bölüm başında değişir** (hangi seviyelerde değişeceği bir level parametresidir). Her bölümün başında Komi'nin şişe düzenini okuyup Şef'e bildirmesi gerekir — örn. *"soldan ikinci mayonez"*.

**Doğan çeviri zinciri:** Kasiyer sosu **renkle** gönderir → Komi rengi kendi tablosundan **sos adına** çevirir → ama Şef sos adını bilmez, yalnızca konum bilir → Komi bir çeviri daha yapıp **konuma** çevirir. Üç aşamalı bu zincir, sos kanalını oyunun en yüksek iletişim yüklü öğesi yapar.

**Hedeflenen hata:** Komi bölüm başında bildirmeyi unutur veya yanlış bildirirse, Şef kendinden emin biçimde yanlış şişeye basar — ketçap sandığı yerden mayonez çıkar ve hamburger yanlış yapılır. Şef bunu fark edemez (kör); ancak Komi pencereden görüp sözlü olarak düzeltebilir. Yani kurtarma yolu vardır ama dikkat gerektirir.

**Tasarım gerekçesi:** Bu mekanik, Komi'ye *siparişi aktarmanın* ötesinde **mutfağın durumunu aktarma** görevi veriyor — yani tamamen yeni bir bağımlılık kanalı açıyor. Bölüm başında olması bunu bir sürpriz değil, öğrenilebilir bir **disiplin** haline getirir: iyi takımlar "bölüm başında sosları kontrol et" ritüelini kendileri geliştirir.

**Seviye tasarımı kısıtı:** Sos şişelerinin düzeni, Komi'nin İstasyon/Mutfak penceresinden **bölüm başında görülebilir** olmalıdır. Görülemezse Komi brifingi yapamaz ve mekanik adaletsiz hale gelir.

## 6. Seviye / Mutfak Tasarımı

### 6.1 Mutfak Fiziksel Tasarımı
- Fiziksel layout tüm seviyelerde **aynı kalıyor**: 3 sabit oda — Kasa, İstasyon (Orta Oda), Mutfak (bkz. §3.2, §5.1).
- Zorluk artışı yeni bir harita/kat değil, **mevcut odalara eklenen yeni istasyonlar** üzerinden geliyor (örn. İstasyon'a ileride eklenecek içecek standı). Bu istasyonların tam mekaniği malzeme sistemiyle birlikte ileride ayrıca derinleştirilecek — bkz. §6.6.

### 6.2 Seviye İlerleme Yapısı — Yapısal Şablon (Örnek)
Aşağıdaki sıralama, tarif/malzeme özelinde **kesinleşmiş bir liste değil**, ilerleme mantığını gösteren bir şablon:

| Seviye | Yenilik | Öğrettiği/Test Ettiği Şey |
|---|---|---|
| 1 | Tek tarif (Classic Burger), 1 müşteri, tam malzemeli sipariş | Tutorial — temel iletişim zincirinin (Kasiyer→Komi→Şef→Komi→Kasiyer) oturması |
| 2 | 2 müşteri — biri tam malzemeli, diğeri eksiltilmiş (örn. domatessiz) | Aynı ürünün daha kısa malzeme listesiyle gelebilmesi — Kasiyer'in çarpı işaretlerini okuyup daha az sayı göstermesi |
| 3 | Yeni tarif: Cheeseburger (+ yeni malzeme kabı: peynir) | Menü çeşitliliği + mutfağa yeni malzeme kabı eklenmesi |
| 4 | Yeni tarif varyantı (örn. özel/gurme burger) | Tarif karmaşıklığının artması |
| ... | ... | ... |
| 7 | İçecek kategorisi + yan ürünler eklenir (İstasyon'a yeni istasyon) | Yeni ürün kategorisi + yeni fiziksel istasyon |

### 6.3 Malzeme Kapları — Kademeli Genişleme
- Mutfaktaki malzeme kapları (domates, soğan, turşu vb.) başlangıçta minimal: yalnızca ilk tarifin ihtiyaç duyduğu kaplar mevcut.
- Her yeni tarif eklendiğinde, o tarifin gerektirdiği yeni malzeme kabı da mutfağa ekleniyor (örn. Cheeseburger gelince peynir kabı ekleniyor, Şef oradan alıp hazırlıyor).
- Bu, hem görsel karmaşıklığı hem de Şef'in (kör olduğu için) mekânsal ezber yükünü kademeli artırıyor — zorluk eğrisinin bir parçası.

### 6.4 Menü Büyüme İlkesi — Dengeli Dağılım
Yeni içerik (tarif, malzeme kabı, istasyon, mekanik) toplam seviye sayısına **eşit ve dengeli** dağıtılmalı; erken ya da geç yığılma istenmiyor. §6.2'deki şablon bu ilkenin bir örneği: her birkaç seviyede bir tek bir yeni katman ekleniyor (önce zincirin temel işleyişi, sonra istisna iletişimi, sonra yeni tarif+malzeme, sonra yeni ürün kategorisi+istasyon).

### 6.5 Toplam Seviye Sayısı & Seviye Seçim Arayüzü
- MVP hedefi: **20 seviye**.
- Seviye seçim arayüzü, Candy Crush / Instagram Reels tarzı bir **"sonsuzluk algısı"** ile tasarlanacak — içerik sınırlı (20 seviye) olsa da oyuncuya bitmeyecekmiş hissi veren bir harita/akış arayüzü. Bu karar §9 (Sanat Yönü) ve UI tasarımını da etkileyecek.

### 6.6 Kapsam Notu — 20 Seviyenin Tam Dökümü
20 seviyenin tam tarif/malzeme/mekanik dökümü **bilinçli olarak GDD dışında** tutuluyor — GDD'nin okunabilirliğini bozacağı için ayrı bir içerik/level-design tablosu olarak tutulacak. GDD burada yalnızca yapısal ilkeyi (§6.4) ve üretim sistemini (§6.7) sabitliyor.

**Yardımcı doküman:** `Cook_No_Evil_Menu.xlsx` — tam malzeme listesi, kanal kod ataması, hamburger × malzeme matrisi, sinyal yükü analizi ve önerilen menü açılış sırası bu tabloda tutuluyor.

### 6.7 Üretim Sistemi (Menü Ağacı)

Menü dört üretim kategorisine ayrılıyor, her biri farklı bir role bağlı (bkz. §4.1):

**Hamburger** *(Şef — Mutfak, tam zincir gerektirir, bkz. §3.3)*
Yapı: **Ekmek (sabit)** + **Ana Protein** (Et/Tavuk/Balık/Veji — tek seçim, yön kanalı) + **Garnitür** (Marul-1 / Domates-2 / Turşu-3 / Soğan-4 / Peynir-5 — çoklu seçim, sayı kanalı) + **Sos** (Ketçap-Kırmızı / Hardal-Sarı / Mayonez-Beyaz / Barbekü-Kahve — maks 2, renk kanalı). Et §5.2.1'deki pişirme fazlarından geçer.

Menüde **12 hazır varyant** var (4 Et / 3 Tavuk / 2 Balık / 3 Veji) — bunlar müşterinin sipariş edebileceği hazır kombinasyonlardır. Varyant isimleri yalnızca level tasarımı etiketi; oyun içinde oyuncuya isim olarak hiç gösterilmez (§3.6). Tam liste `Cook_No_Evil_Menu.xlsx` dosyasındadır.

*Not: Varyant sayısı mekanik derinlik eklemez — bir siparişin zorluğu yalnızca malzeme sayısına bağlıdır. Varyant sayısının tek etkisi **Kasiyer'in menüde arama/ezberleme yükünü** artırmasıdır.*

**Patates & Ekstra** *(Şef — Mutfak, tam zincir gerektirir)*
*Sepet mekaniği:* Şef çiğ ürünün sepetini eline alır, fritöze sol tık ile koyar; makine pişirmeye başlar. Şef pişme durumunu göremez (§4.1.1). Erken alırsa ürün çiğ haliyle envantere döner. **Bir sepet = bir porsiyon.** Pişince pişmiş haliyle envantere döner ve pencereye bırakılabilir hale gelir.
- Patates (kızartma)
- Ekstra: Tenders, Nuggets, Soğan Halkası
- Fritöz işi Şef'e verildi (Komi'de değil) — Mutfak'taki tüm ateş/kızartma ekipmanı (ızgara + fritöz) tek elde toplanıyor.
- Çiğ→Pişmiş→Yanmış fazlarından geçiyor (§5.2.1) ama **yangın tetiklemiyor** — sadece Et'in yanması gerçek yangına (alarm+kapı+söndürme, §5.2.3) yol açıyor; Patates/Ekstra yanarsa ürün israf olur/çöpe gider, ekstra bir olay tetiklenmez (bkz. §5.2.4, netleşti ✓).

**İçecek & Dondurma** *(Komi — İstasyon, tek başına üretilir, "ekstra" görev)*
- İçecek: Kola, Gazoz, Portakallı Gazoz, Limonata.
- Dondurma: Sade, Çikolata Parçacıklı, Renkli Draje Parçacıklı, Bisküvi Parçacıklı.
- Pişirme/yanma riski yok.

**Adet sınırı — kategori başına en fazla 1 (netleşti ✓):** Bir müşteri aynı kategoriden birden fazla ürün isteyemez (2 hamburger, 2 içecek vb.). *Gerekçe:* İletişim kod sisteminde (§3.6) **adet taşıyan bir kanal yoktur** — sayı kanalı garnitür kimliği için kullanılıyor, miktar için değil. Kasiyer "2 tane" diyebileceği bir sinyale sahip değildir. Dolayısıyla sipariş üretimi, kategori başına en fazla bir ürünle sınırlandırılmalıdır.

**Servis (Final Paket):** Bir siparişteki tüm kategoriler (yalnızca sipariş edilenler — Hamburger/Patates/Ekstra/İçecek/Dondurma'nın herhangi bir alt kümesi) Kasiyer'de tek bir pakette birleşiyor ve müşteriye öyle teslim ediliyor (bkz. §3.3 — paket mantığı).

**Tasarım notu:** Bu yapı, çekirdek zinciri (Kasiyer→Komi→Şef→Komi→Kasiyer) yalnızca Hamburger, Patates ve Ekstra için zorunlu kılıyor (hepsi Mutfak'ta üretiliyor); İçecek ve Dondurma Komi tarafından bağımsız üretiliyor. Kasiyer artık kendi başına üretim yapmıyor — rolü tamamen sipariş alma, zinciri yönetme (emote ile Komi'yi yönlendirme) ve teslimat üzerine kurulu. Komi ise hem zincirdeki iletişim/paketleme görevini hem de İçecek+Dondurma üretimini üstleniyor — çoklu-görev baskısı Kasiyer'den çok Komi üzerinde yoğunlaşıyor.

Referans görseller: `assets/uretim-agaci.png` (tam üretim/menü ağacı), `assets/et-fazlari.png` (pişirme faz diyagramı), `assets/sure-testing.png` (faz süreleri, test değerleri).

### 6.7.1 İçecek Üretim Mekaniği (netleşti ✓)

Tek boyut bardak vardır. Akış:

1. Komi boş bardağı eline alır.
2. Bir içecek makinesinin **yakınına girdiğinde** makinenin yuvası vurgulanır (§4.1.2'deki highlight sistemi — hamburger birleştirme tezgahındakiyle aynı desen).
3. Sol tık ile bardak yuvaya **sabitlenir** (attach).
4. İstenen içeceğin **düğmesine basılır** (tek sol tık). Bardak **otomatik olarak** dolmaya başlar — **basılı tutma değildir.** Dolumu sunucu işletir.
5. Dolum **tepe noktaya** ulaştığında otomatik durur; taşma yoktur.

**Yarıda alma (netleşti ✓):** Komi bardağı dolum bitmeden sol tık ile eline alabilir. Bardak, **o andaki dolum yüzdesiyle** envantere girer — %32'de alındıysa %32 dolu kalır — ve elde **görsel olarak o seviyede görünür.** İlerleme verisi bardağın kendisinde durur (§5.2.1'deki aynı ilke), makinede değil; bardak geri konup doldurulmaya devam edilebilir ve kaldığı yerden devam eder.

**Tamamlama — kapak ve pipet (netleşti ✓):** Tam dolu bardak henüz hazır ürün değildir. Sırayla:
1. **Kapak** eline alınır (sol tık) ve bardağa sol tık ile takılır.
2. **Pipet** eline alınır (sol tık) ve bardağa sol tık ile takılır.

**Sıra zorunludur — pipet kapaktan önce takılamaz.** Kapak ve pipet **sınırsızdır**, stok sistemine dahil değildir.

**Kural:** Yarım bardak ve kapak/pipet takılmamış bardak **paketlenemez**. Bu bir arayüz kontrolü değil, paketleme anında **sunucu tarafında** yapılan bir doğrulamadır (§8).

*Mimari not:* Dolum ilerlemesi §5.2.1'deki pişme ve §5.2.3'teki söndürme ile **aynı desendir** — sunucu sahipli, nesnenin üstünde duran bir ilerleme değeri. Farkı: bırakıldığında geri saymaz (decay yok) ve tamamlanınca otomatik durur.

### 6.7.2 Dondurma Üretim Mekaniği (netleşti ✓)

Tek makine, tek slot, tek aroma (vanilya). Bir **kol** ile çalışır:
- Komi kolu **çekili tuttuğu sürece** kaba dondurma dolar.
- Kabın bir **tepe noktası** vardır; dolum oraya ulaştığında kol, oyuncu bırakmasa bile **otomatik olarak bırakılır** ve sade dondurma hazır olur (taşma yok).
- Kol erken bırakılırsa dolum **yarıda kalır ve dolan miktar görsel olarak kalır** (bir indirmeyi duraklatmak gibi). Yarım dondurma tamamlanmamış üründür.
- Makinenin yanında **üç ayrı topping kutusu** durur: renkli draje, çikolata parçacığı, bisküvi parçacığı. İstenen çeşit için ilgili kutudan kaşıkla **tek seferde** dondurmanın üstüne serpiştirilir.

**Topping stoğu (netleşti ✓):** Topping kutuları **sınırsızdır** ve stok sistemine dahil değildir. *(İleride bitebilir/azalabilir hale getirilebilir — bkz. "İleride Değişebilecekler".)*

**Kol basılı tutmadır (teyit edildi ✓):** Dondurma makinesinin kolu, GDD'deki iki gerçek "basılı tutma" etkileşiminden biridir (diğeri yangın tüpüdür, §5.2.3). İçecek makinesi basılı tutma **değildir** (§6.7.1 — düğmeye basılır, dolumu sunucu işletir). Bu ayrım ortak ilerleme temel sınıfının tasarımını doğrudan etkiler: temel sınıf hem "tek tetikle başlayıp sunucunun yürüttüğü" hem "basılı tutuldukça ilerleyen" modu desteklemelidir.
- Dört varyant bu şekilde üretilir: **Sade** (topping yok) + üç topping çeşidi. Bu, §3.6'daki Vücut Bölgesi kanalının 4 slotuyla birebir örtüşür.

### 6.7.3 Hamburger Birleştirme Mekaniği (netleşti ✓)

Birleştirme ayrı bir alanda yapılır (tezgah üstü kesme tahtası vb.). **Birleştirme tezgahı sayısı: 2** — Şef aynı anda iki hamburger üzerinde çalışabilir. **Fritöz sayısı da 2**'dir. *(Her ikisi de test sonrası değişebilir.)* Şef eline bir malzeme aldığında tahtada **highlight belirir**, sol tık ile malzeme konur ve envanterden düşer. Diğer oyuncular (Komi, pencereden) bu adımları görebilir.

**Zorunlu sıra (kategori bazlı):**
```
Alt ekmek → Köfte/Protein → Garnitür → Sos → Üst ekmek
```
- **Kategori sırası zorunludur** — sostan önce garnitür, garnitürden önce protein konmalıdır.
- **Kategori içinde sıra serbesttir** — peynir/domates/soğan hangi sırayla konursa konsun fark etmez; ketçap ve mayonez de öyle.

**Ekmek iki parçadır, tek envanter öğesidir:** Şef ekmeği eline aldığında alt+üst birlikte gelir. Alt kısım konduğunda elindeki ekmek görsel olarak yarılanır (diğer oyuncular bunu görür). Üst kısım da konduğunda ekmek envanterden tamamen çıkar ve hamburger tamamlanır.

**İstasyon tarif doğrulaması YAPMAZ.** Yalnızca **kategori sırasını** denetler. Yanlış malzeme (marul yerine domates) konabilmelidir — hatanın kendisi oyunun konusudur. *(Bu açıkça yazılmazsa tarif doğrulaması eklenir ve yanlış yapma imkânı ortadan kalkar.)*

**Tamamlanan hamburger envantere girer.** Şef onu eline alıp pencerede beliren highlight'a sol tık yapar; hamburger pencereye sabitlenir ve Komi alır.

### 6.7.4 Sos Uygulama — Envantere Alınmaz (netleşti ✓)

Sos şişeleri **hiçbir zaman eline alınmaz / envantere girmez**. Birleştirme tezgahının yanında **pompa biçiminde** sabit dururlar. Sos sırası geldiğinde Şef doğru şişeye sol tık yapar; sos doğrudan tezgahtaki hamburgere uygulanır.

**Gerekçe (kritik):** Şişeler envantere alınabilseydi Şef "3. slot ketçap" diye ezberlerdi ve §5.6'daki konum bağımlılığının tamamı çökerdi — çünkü şişenin kimliği, Şef'in görebildiği bir envanter slotuna bağlanmış olurdu. Envanter devre dışı kalınca ezberlenecek slot da kalmaz.

Şişelerin yeri bölüm başında değişmeye devam eder (§5.6) — Komi'nin brifingi olmadan Şef hangi pompanın hangi sos olduğunu bilemez.

### 6.7.5 Kademeli İçerik Açılımı — Öğretme Mekanizması (netleşti ✓)

Oyun, mekanikleri ayrı bir tutorial bölümüyle değil, **içeriği kademeli açarak** öğretir:

- Bir ürün kategorisi hangi bölümde tanıtılıyorsa, **emote çarkındaki karşılığı da tam o bölümde belirir** — öncesinde çarkta hiç görünmez. Örn. içecek şekilleri 6. bölümde eklenir.
- Aynı şekilde **fiziksel makine de o bölümde ortaya çıkar**: içecek makinesi, fritöz ve dondurma makinesi tanıtıldıkları bölümden önce sahnede yoktur.
- Böylece çarka yeni bir kategori eklenmesi, oyuncu için doğal bir "yeni bir şey öğreneceğim" sinyali olur; ayrı bir öğretici ekrana gerek kalmaz.

**Seviye 1'e özel kontrol ipuçları (netleşti ✓):** Kademeli açılım *neyin ne zaman geleceğini* çözer ama ilk iki dakikayı çözmez — oyuncu `R`'nin sinyal çarkını açtığını, kitabın sol tıkla alındığını, duvarda bir liste olduğunu bilmez. Bu yüzden **yalnızca 1. bölümde** ekranda bağlama duyarlı ipuçları görünür ("Siparişi almak için sol tık", "Komi'ye anlatmak için R"). Sonraki bölümlerde hiç çıkmaz. Ayrı bir tutorial bölümü yapılmaz.

**Bu yapı tek bir sistemle sağlanır — bkz. §11.9 (LevelConfig).**

### 6.8 Açık Nokta — İçecek/Kızartma Makinelerinin Fiziksel Yerleşimi (kapsam dışı bırakıldı ✓)
Oyuncular odalarında sabit bir noktada durmuyor, serbest hareket ediyorlar — dolayısıyla "pencerenin yanında durma" gibi bir kısıt yok. İçecek standı ve Patates/Ekstra/Dondurma istasyonlarının Kasa/İstasyon odaları içinde tam olarak nereye konacağı (gerekirse pencerelerden biraz uzağa da konabilir) bir **GDD kararı değil, level design kararı** — makineler prefab haline getirildikten sonra sahneye yerleştirme aşamasında belirlenecek. GDD bu detayı sabitlemiyor.

## 7. İlerleme & Skor

### 7.1 Yıldız Sistemi
Seviye performansı, o rounddaki hata sayısına (§3.4) göre yıldıza çevriliyor:

| Hata Sayısı | Sonuç |
|---|---|
| 0 | ★★★ (3 yıldız) |
| 1 | ★★☆ (2 yıldız) |
| 2 | ★☆☆ (1 yıldız) |
| 3 | Round kaybedilir — yıldız yok, seviye geçilmez |

**Canlı gösterge (netleşti ✓):** Yıldızlar yalnızca bölüm sonunda hesaplanıp gösterilmez — oyun boyunca **ekranda sürekli görünür**. İlk hata yapıldığı anda 3 yıldız gözlerinin önünde 2'ye düşer. Bu, her siparişe görünür bir bedel yükler ve win/lose ikiliğini sürekli bir gerilime çevirir.

**Tasarım gerekçesi:** Mevcut sistemde her geri bildirim ceza biçimindeydi (hata, çöp, zaman aşımı); doğru bir teslimatın karşılığı yalnızca "hatanın olmaması"ydı. Canlı yıldız göstergesi, başarıyı da anlık olarak görünür kılar.

### 7.1.1 Teslimat Geri Bildirimi — Üç Role Üç Ayrı Sunum (netleşti ✓)

Teslimatın doğru mu yanlış mı olduğu **her üç oyuncuya da** ulaşmalıdır. Ama Komi sağır, Şef kör — tek bir sunum üçüne birden çalışmaz. Bu, §4.1.2'deki highlight sistemiyle aynı desendir: **tek olay, üç ayrı sunum.**

| Rol | Doğru teslimat | Yanlış teslimat |
|---|---|---|
| **Kasiyer** (müşteriyi görür) | Müşteri sevinç animasyonu + yazar kasa "ching" sesi | Müşteri öfke animasyonu + hata sesi |
| **Komi** (sağır → görsel olmalı) | Yıldız göstergesinde olumlu bir parlama/atım | Yıldızlardan biri görünür şekilde kırılır/söner |
| **Şef** (kör → işitsel olmalı) | Mutfağa ulaşan belirgin bir başarı sesi | Belirgin bir hata sesi |

**Yazar kasa sesi** özellikle yerinde: Kasiyer karakterin kendisi bir yazar kasa (§9.2) — başarı sesi karakterin kendi gövdesinden çıkmış olur.

**Ayrı bir hata sayacı YOK.** Canlı yıldız göstergesi (§7.1) zaten hata sayısını birebir kodluyor: 3 yıldız = 0 hata, 2 yıldız = 1 hata, 1 yıldız = 2 hata. İkinci bir sayaç eklemek ekranı gereksiz kalabalıklaştırır ve aynı bilgiyi iki yerde tutar. Bunun yerine **yıldız kaybı anı dramatize edilir**: yıldız görünür şekilde kırılır ve hata sesi çalar.

*Değerlendirildi, şimdilik alınmadı:* hızlı teslimat ödülü (süre çarkında bol vakit kalmışken teslim edilen siparişe kutlama efekti/bahşiş). Yıldız sistemini etkilemeyeceği için düşük riskli bir ekleme; test sonrası tekrar değerlendirilebilir.

### 7.1.2 Hata Göstergesi — Duvar Paneli (netleşti ✓)

Yıldız sistemi (§7.1) Faz 0 kapsamı dışında olduğu için hata sayısının görünürlüğü ayrı bir fiziksel göstergeden gelir. Faz 1'de yıldız sistemiyle birlikte yeniden değerlendirilir.

**Biçim:** Her odada (Kasa, İstasyon, Mutfak) tavana yakın, duvara monte, dijital saat görünümlü bir panel. Üzerinde yan yana **3 adet X**. UI değil, dünyada duran fiziksel bir nesne — duvar malzeme listesiyle (§3.6.2) aynı dili konuşur.

**Davranış:** Panel başlangıçta sönüktür; X'ler görülebilir ama aydınlatılmamıştır. Her hatada soldan başlayarak bir X **aydınlanır**. 3 X aydınlandığında seviye kaybedilir (§3.4).

**Şef için (bağlayıcı):** Kontur render'da renk ve parlaklık okunmaz (§4.1.1). Bu yüzden:
- **Sönük X, Şef'e hiç görünmez.**
- **Aydınlanan X, Şef'in ekranında görünür hale gelir.**

Şef hata sayısını, odanın üst tarafına baktığında **görünen X sayısını sayarak** okur; 0 hatada hiçbir şey görmez. Tek bir nesne, üç rol için üç ayrı okunuş üretir — §7.1.1'deki "tek olay, üç ayrı sunum" deseninin aynısıdır.

> **Uygulama notu (bağlayıcı):** Bu, kontur pass'ine dahil olma kararının bir **oyun durumu** tarafından sürüldüğü **tek yerdir.** Emsal değildir; başka hiçbir nesne için "Şef bunu da görsün" diye bu yola başvurulmaz, aksi halde §4.1.1 sistematik olarak delinir.
>
> *Yöntem:* GameObject layer'ı değil, Renderer'ın **rendering layer mask**'ı değiştirilir; kontur Renderer Feature'ı bunu `FilteringSettings.renderingLayerMask` ile filtreler. Normal kamera rendering layer'ları yok saydığı için Kasiyer ve Komi X'i her iki durumda da görür (sönük/parlak farkıyla). Panelin durumu sunucu sahiplidir; rendering layer değişimi istemcide o `NetworkVariable` dinlenerek yapılır.
> *Yedek plan:* URP 17.5'te bu filtreleme beklendiği gibi çalışmazsa geometri tabanlı çözüme düşülür — aydınlanan X duvardan fiziksel olarak öne çıkar; kontur bu değişimi zaten yakalar.

**Ses zorunluluğu korunur:** §7.1.1'deki işitsel hata geri bildirimi Şef için ayrıca zorunludur. Duvar paneli yalnızca bakıldığında okunur; ses anındadır.

### 7.2 Tekrar Oynanabilirlik
- Geçilmiş bir seviye istenildiği kadar tekrar oynanabilir (daha iyi yıldız derecesi için).

### 7.3 Sipariş Belirleme Sistemi — Sabit / Rastgele Karması (netleşti ✓)

**Temel ilke:** Seviye tasarımcısı, bir seviyedeki **her şeyi** ya elle girer ya da bir havuzdan/aralıktan rastgele çektirir; ikisi aynı seviyede karışık kullanılabilir. Kod hiçbir alanın sabit ya da rastgele olacağını varsaymaz. Bunların tamamı `LevelConfig` üzerinden ayarlanır (§11.9).

#### 7.3.1 Sipariş slotu — bağımsız mod seçimi

Bir seviyedeki her sipariş slotu **kendi başına** sabit veya rastgele olabilir. Aynı seviyede 4 müşteri varsa ikisinin siparişi elle yazılıp diğer ikisi rastgele bırakılabilir. Slotlar birbirinden bağımsızdır.

**Sabit slot:** varyant, eksik malzemeler ve yan ürünler (varsa) tasarımcı tarafından tek tek belirlenir.

**Rastgele slot:** aşağıdaki alanların her biri **ayrı ayrı** sabit veya rastgele seçilebilir:

| Alan | Sabit | Rastgele |
|---|---|---|
| Hamburger varyantı | Belirli bir varyant | Tasarımcının belirlediği havuzdan çekilir |
| Eksik malzeme | Hangi malzemeler eksik, elle | İzinli havuz + adet aralığı (aşağıda) |
| İçecek | Belirli bir içecek / yok | Havuzdan çekilir (havuza "yok" da konabilir) |
| Dondurma | Belirli bir çeşit / yok | Havuzdan çekilir |
| Patates / Ekstra | Belirli bir ürün / yok | Havuzdan çekilir |

#### 7.3.2 Eksik malzeme randomizasyonu

İki parametreyle tanımlanır:
- **İzinli havuz:** hangi malzemelerin çıkarılabileceği. Tasarımcı tek tek işaretler.
- **Adet aralığı:** min–max. `min = 0` ise müşterinin **tam malzemeli** sipariş verme ihtimali vardır.

*Örnek:* Classic Burger'de izinli havuz = {soğan, turşu, domates}, adet aralığı = 0–1. Müşteri ya tam malzemeli ister, ya bu üçünden **rastgele birini** istemez.

**"Komple randomize" kısayolu:** İzinli havuz elle doldurulmak yerine tek bir işaretle "o varyantın çıkarılabilir malzemelerinin tamamı" olarak bırakılabilir. Mekanizma aynıdır — yalnızca havuzu elle doldurma zahmetini kaldırır. Adet aralığı yine ayrı ayarlanır.

**Neyin çıkarılabilir olduğu da bir parametredir.** Ekmeğin veya proteinin çıkarılabilir sayılıp sayılmayacağı kodun kararı değildir; izinli havuz tasarımcının doldurduğu serbest bir listedir.

#### 7.3.3 Sayısal seviye parametreleri

Toplam müşteri sayısı, müşteriler arası aralık, sabır süresi, yedek havuz boyutu, malzeme başlangıç stokları ve süre çarpanı — bunların **her biri** ya elle bir değerle ya da bir **min–max aralıkla** verilebilir. Aralık verildiğinde değer bölüm başında çekilir.

*Gerekçe:* "Birinci bölümde 1 müşteri olsun" dedikten sonra "çok hızlı bitti, 2 yapayım" demek bir kod değişikliği olmamalıdır. Bu değerlerin tamamı Inspector'dan ayarlanır.

#### 7.3.4 Uygulama ilkesi (bağlayıcı)

Her alan için ayrı bir "sabit mi rastgele mi" mekanizması yazılmaz. **Tek bir ortak veri tipi** kullanılır (sayısal alanlar için "elle değer veya min–max aralık", seçim alanları için "sabit seçim veya havuzdan rastgele") ve tüm alanlar bu tipten türetilir. Ayrı ayrı yazılırsa §11.9'un "tek kaynak" ilkesi çöker.

### 7.4 Takım Performansı
- Skor tamamen **ortak/takım** seviyesinde tutuluyor; bireysel oyuncu bazında ayrı bir performans ölçümü (kim kaç hata yaptı gibi) yok.

---

## 8. Multiplayer / Netcode Gereksinimleri
*[KISMEN DOLU]*
- Ağ altyapısı: Netcode for GameObjects + Facepunch Steamworks (Steam lobisi üzerinden).
- Player Controller tamamlandı ve gerçek 3 kişilik Steam oturumunda doğrulandı.
- Disconnect/reconnect/pause mimarisi kuruldu (bkz. §5.5 — Round State tek otoriteye konsolide edildi).
- Emote sistemi Rust-tarzı delta-bazlı bir çark olarak tasarlandı; karakter animasyonları tüm oyunculara görünür.
- §5.5'teki round-state/pause-state birleştirme kararı burayı da etkiliyor: bu bayraklar `NetworkVariable` olduğundan senkronizasyon tek otoriteden yönetilmeli.
- Sipariş üretimi, timer, hata sayacı gibi Bileşen 2 unsurlarının network senkronizasyonu henüz tasarlanmadı — implementasyon safhasında ele alınacak.

### 8.1 Lobi, Rol Seçimi ve Bölüm Akışı (netleşti ✓)

**Rol seçimi lobide yapılır.** İki oyuncu aynı rolü seçemez — çakışma varsa hazır verilemez ve oyun başlatılamaz. Hazır verildikten sonra rol değiştirilemez.
*(Mevcut kodda roller test amacıyla katılma sırasına göre otomatik atanıyor — `SequentialRoleAssignmentStrategy`. Seçim ekranı bunun yerini alacak; `IRoleAssignmentStrategy` soyutlaması zaten bu geçiş için kurulmuştu.)*

**Bölümler arası akış:** Bölüm biter → yıldızlar gösterilir → oyuncular **lobiye döner** → sonraki bölüm seçilir → herkes hazır verir → **Başla butonu aktifleşir ve host başlatır.** (Bombanana modeli.)

**İlerleme kaydı (netleşti ✓):** Hangi bölümlerin açık olduğu ve her bölümden kaç yıldız alındığı kaydedilir; **Steam Cloud** ile senkronlanır.
- **Oturum host'un ilerlemesiyle oynanır** — hangi bölümlerin seçilebilir olduğunu host'un kaydı belirler (pazarlık/uzlaşma gerekmez).
- **Ama her oyuncu kendi kaydına da işler:** bölüm tamamlandığında üç oyuncunun da kendi kaydı o bölümü tamamlanmış olarak işaretler.
- *Gerekçe:* Oyun 3 kişi zorunlu. Yalnızca host ilerleseydi, host bir akşam müsait olmadığında ya da gruptan ayrıldığında diğer ikisinin ilerlemesi fiilen yok olurdu. Bu çözümün maliyeti neredeyse sıfır (bölüm sonunda her client kendi kaydına yazar), kazancı ise üçünden herhangi birinin bir sonraki sefer host olabilmesi.

### 8.2 Oturum İçi Disconnect (netleşti ✓)
- Bir oyuncunun bağlantısı koparsa oyun **donar** ve ekranda **"DURDURULDU"** yazısı görünür (§5.5'teki pause mimarisi).
- Kopan oyuncu **5 dakika içinde** dönmezse oturum kapatılır, **bölüm başarısız sayılır** ve lobiye dönülür.
- **2 kişiyle devam etmek mümkün değildir** — üç rol de zorunludur.

**Kod tarafı yeniden adlandırma görevi:** Mevcut kodda rol enum'u hâlâ `PlayerRole.Yamak` olarak duruyor. GDD §4.2'de isim **Komi** olarak değiştirildi. Kod da yeniden adlandırılmalıdır (`Yamak` → `Komi`), aksi halde dokümanı okuyan herkes kodda başka bir isimle karşılaşır. Bu, davranış değiştirmeyen mekanik bir yeniden adlandırmadır ve Faz 0'ın ilk adımlarında yapılmalıdır.

### 8.3 Ayarlar ve Duyusal Kısıt Bütünlüğü (netleşti ✓)

Ayarlar menüsünün içeriği (ses seviyeleri, fare hassasiyeti, çözünürlük, **tuş yeniden atama**) standart bir yapım işidir ve tasarım kararı gerektirmez. Ancak **bağlayıcı bir kural** vardır:

> **Hiçbir ayar, bir rolün duyusal kısıtını zayıflatamaz.**

- Şef'in körlüğü bir **shader** (kontur render, §4.1.1) olarak uygulanmalıdır — parlaklık/gamma filtresi olarak DEĞİL. Karartma filtresi olarak yazılırsa oyuncu parlaklık ayarını yükseltip görmeye başlar.
- Komi'nin sağırlığı (§4.1.3) ses seviyesi ayarıyla telafi edilebilir olmamalıdır — zayıflatma, ana ses seviyesinden bağımsız uygulanmalıdır.
- Kasiyer'in dilsizliği sunucu tarafında uygulanır; istemci ayarıyla geri alınamaz.

**Duraklatma menüsü oyunu durdurmaz.** `ESC` menüsü **yereldir** — bir oyuncu ayarları açtığında diğer ikisi oynamaya devam eder. §5.5'teki pause mimarisi **yalnızca disconnect** içindir; ikisi karıştırılmamalıdır. *(Kitap açıkken `ESC` yalnızca kitabı kapatır, menüyü açmaz — §3.6.2.)*

## 9. Sanat Yönü

### 9.1 Görsel Stil
**Low poly**, stilize/karikatürize — fast-food restoranı teması.

### 9.2 Karakter Tasarımı — Nesne-Kafalı Antropomorfizm
Her rol, işiyle özdeşleşen bir nesnenin antropomorfik hali olarak tasarlanıyor:

| Rol | Görsel Kimlik |
|---|---|
| Kasiyer | Eski tip kasa makinesi karakteri |
| Komi | Ketçap şişesi karakteri (etiketinde "Kitchen Apprentice" yazıyor — role birebir uyuyor) |
| Şef | Hamburger karakteri (aşçı şapkası, önlük, spatula) |

Ortak tasarım dili: beyaz eldivenli eller, küçük ayakkabılar, karikatürize gözler. Bu görsel ayrım rol kimliğini (kim kim olduğunu) taşıyor; duyusal kısıtlar (kör/sağır/dilsiz) ayrı bir katman olarak deneyimleniyor (örn. Şef'in ekranının siyah-beyaz render edilmesi, bkz. §4.1) — yani karakter tasarımı ile duyusal kısıt görselleştirmesi birbirinden bağımsız iki katman.

Referans görseller: `assets/karakter-lineup.png` (oyun-içi low-poly stildeki üçlü sıralama), `assets/karakter-moodboard.png` (konsept/mood-board örnekleri, ŞEF etiketli mutfak sahnesi dahil).

### 9.3 Restoran Teması
Fast-food.

### 9.4 Maskot / Logo
Üç maymun temasına (bkz. §1 — isim kökeni) görsel bir gönderme (logo/maskot) **planlanmıyor**. İsim kavramsal bir referans olarak kalıyor; görsel kimlik nesne-karakterler üzerinden kuruluyor.

## 10. Ses Tasarımı

### 10.1 Müzik Tonu
Bombanana tarzı komik/enerjik — onaylandı (bkz. §1, §2 — genel ton).

### 10.2 Ses Efektleri
Genel liste onaylandı — kapsam genişletildi:
- Yeni müşteri/sipariş zili
- Hata sesi (geç/yanlış teslim)
- Yangın alarmı
- Intercom/diyafon sesi (malzeme talebi)
- Emote/iletişim onay sesi
- Seviye tamamlama + yıldız sesi
- **Pişirme/kızartma sesleri** — her üretim kalemi için ayrı, faz bazlı (§5.2.1): çiğ/pişme/kızarma cızırtısı, yanma sesi
- **Pencereler arası teslim sesi** — bir ürün bir pencereden diğerine geçerken ufak bir onay/geçiş sesi (§5.1)
- **Karakter sesleri (netleşti ✓)** — konuşma/gibberish yok; her karakter (ketçap şişesi/Komi dahil) için yalnızca hareket/tepki efektleri (zıplama, çarpma, memnuniyet/hata sesi gibi).

### 10.3 Sesli İletişim (VoIP) — Karara Bağlandı ✓
Discord/VoIP üzerinden oyun-dışı konuşarak kısıtları by-pass etme ihtimali teknik olarak engellenmiyor; **kasıtlı olarak engellenmeyecek**. Gerekçe: oyunun tüm amacı zaten kör/sağır/dilsiz kısıtları içinde iletişim kurmaya çalışmak — bunu by-pass etmek oyunun kendi eğlencesini ortadan kaldırıyor. Bu, birçok parti oyununda (ör. kart oyunlarında elini göstermeme kuralı) görülen kendi kendini dengeleyen bir tasarım prensibi: kuralı bozmak teknik olarak mümkün ama oyuncunun kendi deneyimini bozduğu için pratikte tercih edilmiyor.

*Not (küçük öneri, blocker değil):* Bu varsayımın oyunculara açıkça anlatılması faydalı olabilir — Overcooked'daki fiziksel kısıtların aksine ("aynı odada aynı kontrolcüyü paylaşamazsınız" gibi kendiliğinden anlaşılan bir kısıt değil), buradaki kısıt sosyal/gönüllü bir sözleşmeye dayanıyor. Tutorial veya lobi ekranında tek satırlık bir hatırlatma ("Bu oyunun amacı kısıtlı iletişim — Discord'da konuşmadan oynayın!") yeni oyuncuların bunu anlamasını hızlandırabilir. İstersen bunu §9 (UI/Sanat) ya da §11'e küçük bir madde olarak ekleyebiliriz.

### 10.4 Oyun-içi VoIP ve Rol Bazlı Ses (netleşti ✓)

Oyunun **kendi sesli sohbeti vardır** ve role göre çalışır (§10.3'teki Discord kararından bağımsız — o, oyun-dışı konuşmayı engellememe kararıydı).

**Ses mekânsaldır — oda sınırlarına tabidir.** Sesin hangi odalar arasında geçtiği §5.1'deki pencere/panel tanımlarıyla belirlenir:
- **Kasa ↔ İstasyon:** ses geçer (§5.1.1)
- **İstasyon ↔ Mutfak:** ses geçer (§5.1.2)
- **Kasa ↔ Mutfak:** ses **geçmez** (§5.1.3) — yalnızca Şef intercom butonuna bastığında açılır

Buna göre gerçek ses matrisi:

| Konuşan | Kasiyer duyar | Komi duyar | Şef duyar |
|---|---|---|---|
| Kasiyer | — (mikrofonu kapalı) | — | — |
| Komi | normal | — | **normal** |
| Şef | **çok zayıf (mesafe)** — pratikte anlaşılmaz | **anlamsız maymun sesi** | — |

**Mesafe zayıflaması, sert kesme değil.** Kasiyer ile Şef arasında yapay bir "ses geçmez" kuralı yoktur — ses **mesafeye göre doğal olarak zayıflar**. Kasa ile Mutfak yeterince uzak olduğu için Şef'in sesi Kasiyer'e anlaşılır şekilde ulaşmaz; ikisi anlaşmak için **intercom'u kullanmak zorunda kalır**. Bu, keyfi bir kısıt değil, mekânın kendi sonucudur.

> **Yerleşim kısıtı (bağlayıcı):** Kasa ile Mutfak arasındaki mesafe, normal konuşmanın anlaşılamayacağı kadar uzak olmalıdır. Odalar birbirine fazla yakın konumlandırılırsa Şef ve Kasiyer bağırarak anlaşır, intercom mekaniği (§5.1.3, §5.4) işlevsizleşir ve oyunun temel kuralı (her bilgi Komi'den geçer) delinir.

**"Agucubugucu" mekaniği:** Şef konuştuğunda Komi kelimeleri duymaz; yerine anlamsız maymun/gıdıklama sesleri duyar. Uygulama: Şef'in gelen ses paketi Komi tarafında **çalınmaz**, yalnızca **ses şiddeti (RMS) ölçülür** ve bu şiddet bir gibberish ses örneğinin volümünü/pitch'ini sürer. Şef sakin konuşursa kısık, bağırırsa bağıran gibberish çıkar; sustuğunda kesilir.

*Tasarım gerekçesi:* Önceki "boğuk duyma" çözümünde Komi bir şey söylendiğini anlar ama kelimeleri kaçırır — sinir bozucu. Gibberish'te Komi **bir şey olduğunu net duyar, ne olduğunu hiç anlamaz** — hem daha komik hem daha okunaklı. Teknik olarak da low-pass filtreden kolaydır (ses işlenmez, atılıp yerine başka bir şey konur).

### 10.5 Şef'in İşitsel Telafisi (netleşti ✓)
Şef bir şeylerin piştiğini duyar ama **pişme durumunu ayırt edemez**: etten sabit bir cızırtı gelir, faz değişiminde (Çiğ→Pişmiş) **hiçbir ek ses çıkmaz**. Ses, Şef'e "burada bir şey pişiyor" der; "hazır oldu" demez. Bu bilinçlidir — aksi halde Komi'ye olan bağımlılık (§5.1.2) zayıflardı.

## 11. Kapsam & Yol Haritası

### 11.1 Takvim — DeepJam Bağlamı
Proje, GameDev.ist'in düzenlediği **DeepJam** hızlandırma programına başvuru hedefiyle planlanıyor.

| Kilometre | Tarih |
|---|---|
| **Başvuru son tarihi** | **11 Ekim 2026** (24 gün) |
| Program başlangıcı | 26 Ekim 2026 (39 gün ≈ 5,5 hafta) |
| Program bitişi | 20 Aralık 2026 |
| Final etkinliği (İstanbul, Indie Core) | 15-16 Ocak 2027 |

Program 8 hafta, tamamen ücretsiz, IP %100 geliştiricide kalıyor. İçerik: 72 saat birebir mentorluk, 64 saat oyun tasarımı eğitimi, 32 saat girişimcilik, 24 saat DevLog, 8 saat influencer networking.

**Kritik ayrım:** Gerçek son tarih program başlangıcı (26 Ekim) değil, **başvuru tarihidir (11 Ekim)** — 8 haftalık kilometre taşı planı o gün teslim ediliyor. Ayrıca bu oyunun konsepti kâğıt üzerinde iyi anlatılamıyor (asimetrik iletişim komedisi ancak izlenince anlaşılıyor); dolayısıyla başvuruya **oynanabilir bir dikey dilim + 3 kişilik oynanış videosu** eklemek kabul şansını belirgin şekilde artırır.

### 11.2 Faz 0 — Başvuru Sürümü (17 Eylül – 11 Ekim, 3,5 hafta)
Hedef: konsepti kanıtlayan **oynanabilir dikey dilim** + oynanış videosu.

**Kapsam içi (indirgenemez çekirdek):**
- 3 oda + 2 pencere (Kasa/İstasyon, İstasyon/Mutfak)
- Müşteri spawn + sipariş pop-up + sipariş zaman çarkı + sabır çarkı + teslim noktası
- 15 sn hazırlık fazı (§3.4.3)
- **Etkileşim vurgusu / highlight sistemi** (§4.1.2), Şef varyantı dahil
- **`LevelConfig` ScriptableObject** (§11.9) — alanların tamamı tanımlı, Faz 0'da kullanılmayanlar boş/kapalı
- **Sipariş belirleme sistemi** (§7.3) — sabit **ve** rastgele kolların ikisi de, alan bazında seçilebilir. Hangi seviyede hangisinin kullanılacağı bir `LevelConfig` kararıdır, kod kararı değil.
- İletişim kod sistemi: çark altyapısı **5 kategoriyi destekleyecek şekilde veri odaklı** kurulur (§3.6.0); Faz 0'da **Yön ve Sayı kanallarının animasyonları ve değerleri üretilir**. Renk, Şekil ve Vücut Bölgesi kanalları `LevelConfig`'te kapalı gelir — altyapı hazır, içerik yok.
- **Duvar malzeme listesi panosu** (§3.6.2) — Sayı ve Yön kanallarının önkoşuludur
- **Tarif kitapçığı** (§3.6.2) — kabuk + açılım şablonu + içindekiler + kategori atlama; sayfalar açık varyant listesinden türetilir, sayfa sayısı koda gömülü değildir
- Şef: kör görüş render (§4.1.1) + malzeme kapları + hamburger birleştirme (§6.7.3) + ızgara (Çiğ/Pişmiş/Yanmış) + çöp kutusu
- Komi: aktarım + paketleme
- Kasiyer: sipariş alma + kodlama + teslim
- Hata sayacı + duvar hata göstergesi (§7.1.2) + kazanma/kaybetme
- 2 elle yazılmış seviye

**Kapsam dışı (Faz 0'da üretilmeyecek içerik):**
- **Yangın olayı** (tüp, kapı, alarm, söndürme, ızgara kilidi) — ikincil mekanik, yüksek maliyet. **Yanma fazı kapsam içindedir**; yanmış et söndürme gerekmeden alınıp çöpe atılır (bkz. §5.3.2).
- Intercom / malzeme stok sistemi
- Patates/Ekstra/İçecek/Dondurma **üretim makineleri ve ürünleri**
- **Sos pompaları ve Renk kanalı** (§5.6, §6.7.4) — 18 Eyl 2026'da kapsamdan çıkarıldı (kapasite). Faz 0.5'e **ilk eklenecek** kalemdir; sistemi veri odaklı kurulduğu için geri açmak `LevelConfig` işidir.
- Şekil ve Vücut Bölgesi kanallarının **animasyonları ve değerleri** *(kanal altyapısı kapsam içidir — yukarı bak; eksik olan yalnızca içeriktir)*
- Yıldız sistemi, seviye seçim haritası, "sonsuzluk algısı" arayüzü
- **Lobide rol seçimi** (§8.1) — Faz 0.5'e ertelendi; katılma sırası yeterli
- **Şef→Komi gibberish sesi** (§10.4) — Faz 0.5'e ertelendi; Faz 0'da Şef'in sesi Komi'de hiç çalınmaz, mekanik sonuç aynıdır
- Final sanat/ses geçişi — yer tutucu yeterli

**Bu liste içerik kapsamıdır, sistem kapsamı değildir.** Kapsam dışı bir içeriğin **sistemi** kapsam içindeyse (çarkın 5 kategori desteklemesi gibi) veri odaklı olarak yazılır ve kapalı gelir. Sistemi de kapsam dışı olanlar (yangın, stok, intercom) hiç yazılmaz.

*Kapsam revizyonu 18 Eyl 2026'da yapıldı; gerekçeler `docs/PLAN.md` → Karar Günlüğü'ndedir.*

### 11.3 Faz 0.5 — Tampon (12 – 25 Ekim, 2 hafta)
Başvuru ile program başlangıcı arasındaki boşluk. Faz 0'dan sarkan işler, ilk gerçek playtest (3 kişilik Steam oturumu), ve §11.6'daki açık maddelerin kapatılması. Bu iki hafta **planlanmış tampondur** — yeni özellik eklemek için değil, Faz 0'ın kaçınılmaz gecikmelerini emmek için ayrılmıştır.

### 11.4 8 Haftalık Program Planı (26 Ekim – 20 Aralık) — Başvuruya Girecek Plan

| Hafta | Hedef | Çıktı |
|---|---|---|
| 1 | Sipariş sistemi tamamlanması + seviye veri altyapısı (sabit/randomize, §7.3) | 20 seviyeyi besleyebilen veri hattı |
| 2 | İçecek + Dondurma (Komi) + Patates/Ekstra (Şef) kategorileri; Şekil ve Vücut Bölgesi kanalları | 5 kanalın tamamı aktif |
| 3 | Yangın sistemi + Intercom + malzeme stok sistemi | Tüm §5 sistemleri çalışır |
| 4 | Seviye içeriklerinin doldurulması + zorluk dengesi | **Alpha — baştan sona oynanabilir** |
| 5 | Yıldız sistemi, seviye seçim arayüzü, meta ilerleme | Oyun döngüsü kapanıyor |
| 6 | Sanat geçişi: final karakterler, animasyonlar, mutfak ortamı | Görsel kimlik tamam |
| 7 | Ses tasarımı + game feel cilası | İşitsel kimlik tamam |
| 8 | Playtest, denge, bug, Steam sayfası | **Beta / demo build** |

### 11.5 Kapsam Gerçekçiliği — Açık Uyarı
"8 haftada oyunu tamamen eksiksiz bitirmek" hedefi, mevcut kapsama göre **gerçekçi değil**. 20 seviye + 12 varyant + 5 iletişim kanalı (~21 ayrı okunabilir animasyon) + 4 ürün kategorisi + yangın/intercom/stok sistemleri + tam sanat ve ses geçişi + bunların hepsinin 3 kişilik netcode ile senkronu — 3 kişilik, tam zamanlı olmayan bir ekip için 8 haftaya yaklaşık 2-3 kat fazla iş demek.

**Bu bir sorun değil, çünkü DeepJam'in beklentisi de bitmiş oyun değil.** Hızlandırma programları güçlü bir dikey dilim, net bir ticari plan ve yatırımcı/yayıncıya gösterilebilir bir ürün ister. Gerçekçi 8 haftalık çıktı: **8-10 seviyelik, cilalı, tam hissi veren bir demo** — 20 seviyelik bitmiş oyun değil. Plan buna göre kurulmalı; 20 seviye programdan sonraki hedef olarak konumlandırılmalı.

### 11.6 Tasarım Boşlukları — Kapandı ✓
Taramada bulunan 8 maddenin tamamı kapandı: §3.4 kazanma koşulu · §3.4.1 süre formülü · §3.4.2 müşteri akışı · §4.1.1 Şef görüşü · §5.4.1 stok modeli · §5.4.2 stok inandırıcılığı · §6.7.1 içecek · §6.7.2 dondurma · §3.6 sayı kanalı okunabilirliği.

GDD, Faz 0 kodlamasına başlamak için yeterli durumdadır. Bundan sonraki değişiklikler playtest bulgularından gelecektir.

### 11.7 Takım & Kapasite
Ersel (geliştirici, tasarım ve mimari kararlar) + 1 3D artist + 1 animasyoncu.

**Kritik yol: animasyon.** 5 kanalın tamamı için ~21 ayrı, mesafeden okunabilir animasyon gerekiyor (4 yön + 5 sayı + 4 renk + 4 şekil + 4 vücut bölgesi). Tek animasyoncu için bu, programın en dar boğazı. Faz 0'da yalnızca 3 kanalın (13 animasyon) tutulması bu yüzden kritik.

**Kapasite (netleşti ✓):** Haftada **en az 20-25 saat**. ALES hazırlığı bu proje lehine bırakıldı, takvim çakışması yok.

**Kodlama yöntemi:** Kod Claude Code üzerinden yazılıyor. Bu, GDD'nin kesinliğini normalden daha kritik hale getiriyor — belirsiz kalan her tanım doğrudan hatalı implementasyona dönüşür. Bu yüzden bu doküman prose değil, **uygulanabilir spesifikasyon** (formül, state machine, parametre tablosu) biçiminde tutulmalıdır.

### 11.8 Yayın Hedefi
Steam. Net bir çıkış tarihi yok; DeepJam sonrası (2027) hedefleniyor.

### 11.9 Mimari Zorunluluk — Tek `LevelConfig` Kaynağı

Seviyeden seviyeye değişen **her şey** tek bir ScriptableObject'te (`LevelConfig`) tanımlanmalıdır. Bunlar ayrı ayrı sistemler olarak yazılırsa proje dağılır; tek kaynakta toplanırsa hem kolay hem test edilebilir olur.

`LevelConfig` içeriği:
- Toplam müşteri sayısı, yedek havuz boyutu, müşteriler arası aralık, sabır süresi (§3.4.2, §3.4.4)
- Sipariş slotları: sabit mi randomize mi, randomize parametreleri (§7.3)
- Açık olan hamburger varyantları ve ürün kategorileri (§6.7.5)
- Sahnede aktif olan makineler ve malzeme kapları (§6.7.5)
- Emote çarkında görünen kategoriler/değerler (§6.7.5)
- Garnitür numara eşleşmesi ve protein yön eşleşmesi (§3.6.3)
- Sos pompalarının konumu (§5.6)
- Malzeme başlangıç stokları veya tetik tanımları (§5.4.1)
- Süre çarpanı (§3.4.1)
- Tarif kitapçığında görünen açılımlar (açık varyant listesinden türetilir, §3.6.2)
- Seviye 1'e özel kontrol ipuçlarının açık/kapalı olması (§6.7.5)
- Sinyal çarkında açık olan kategori sayısı ve her kategorinin değer listesi (§3.6.0, §6.7.5)

**Alan silme yasağı (bağlayıcı):** Bir faz veya seviye o alanı kullanmıyor olabilir; bu, alanın **tanımlanmayacağı anlamına gelmez.** Faz 0'da stok, fritöz, içecek, dondurma, Renk/Şekil/Vücut Bölgesi kanalları kullanılmaz — ama `LevelConfig`'te karşılıkları **tanımlı ve boş/kapalı** bırakılır. Alan silinip sonradan geri eklenirse hem asset'ler hem onları okuyan kod iki kez yazılır.

**Sabitleme yasağı (bağlayıcı):** Bu listedeki hiçbir değer koda gömülmez. "Bu seviyede siparişler sabit olsun", "bu seviyede garnitür numaraları değişmesin", "bu seviyede 2 kanal açık olsun" — hepsi `LevelConfig` üzerinden ayarlanır ve her seviye için bağımsız seçilebilir. Kod bu değerlerin **hiçbirini varsaymaz.**

**Sabit/rastgele ikiliği (bağlayıcı):** Bu listedeki alanların çoğu yalnızca "hangi değer" değil, "**elle mi girilecek yoksa bir aralıktan/havuzdan mı çekilecek**" sorusunu da taşır (§7.3). Sayısal alanlar (müşteri sayısı, aralık, sabır, stok, süre çarpanı) için *elle değer* **veya** *min–max aralık*; seçim alanları (varyant, içecek, dondurma, patates/ekstra, eksik malzeme) için *sabit seçim* **veya** *havuzdan rastgele* seçilebilir olmalıdır. İkisi aynı seviyede karışık kullanılabilir ve her sipariş slotu kendi modunu taşır. Uygulamada bu, alan başına ayrı bir mekanizma değil **tek bir ortak veri tipiyle** çözülür (§7.3.4).

**Uygulama ilkesi:** Oda yerleşimi tüm bölümlerde sabit olduğu için (§6.1), tüm makineler ve kaplar sahnede hep bulunur; `LevelConfig` bölüm başında hangilerinin **aktif/görünür** olacağını belirler. Prefab spawn/despawn yerine aktiflik anahtarı tercih edilir — daha basit ve network açısından daha öngörülebilir.

## İleride Değişebilecekler (Bilinçli Kararlar, Kapalı Değil)

Aşağıdakiler şu an bilinçli olarak sadeleştirilmiş durumdadır; playtest sonrası yeniden açılabilir:

1. **Pişirme fazları.** Şu an Çiğ → Pişmiş → Yanmış (3 faz). İleride ara fazlar geri gelebilir ve müşteriler pişmişlik tercihi belirtebilir (örn. "az pişmiş köfteli hamburger") — bu, sipariş sistemine yeni bir boyut ve muhtemelen yeni bir iletişim kanalı gerektirir.
2. **Etlerin kimlik görünürlüğü.** Şu an protein türü Şef için siluetten okunabilir. İleride bu da gizlenip Komi'ye tam bağımlılık kurulabilir (oyunu belirgin şekilde zorlaştırır).
3. **Geri alma yasağı.** Yanlış paketlenen ürünün geri alınamaması (§5.3.1) şu an katı bir kural; test sonrası yumuşatılabilir.
4. **Hızlı teslimat ödülü.** §7.1'de değerlendirilip alınmadı, geri dönülebilir.
5. **Dil planı (karar verildi, uygulama en sona bırakıldı).**
   - **Geliştirme boyunca ana dil Türkçe kalır** — isimlendirme ve iletişim bu şekilde kolaylaşıyor.
   - **Nihai sürümde oyunun ana dili İngilizce olur.**
   - Oyun, açıldığında **kullanıcının işletim sistemi dilini** algılar ve destekleniyorsa o dilde başlar (örn. Windows Fransızca ise oyun Fransızca açılır).
   - Desteklenmeyen bir dil ise **İngilizce'ye düşer** (Türkçe'ye değil).
   - *Kod tarafı notu:* Mevcut `Localization Settings` zinciri fallback olarak `tr` kullanıyor. Nihai sürümde bu **`en`** olarak değiştirilmelidir; ayrıca bir İngilizce `Locale` + String Table eklenmelidir.
   - **Neden en sona bırakıldı:** eşya isimleri, UI metinleri ve arayüz düzeni oturmadan yapılan çeviri iki kez yapılır. Tarif kitabı ve duvar listeleri büyük ölçüde görsel olduğu için (fotoğraf, sayı, renk+desen) çeviri yükü yalnızca arayüz metinlerindedir.

## Açık Sorular

**Yok.** Tarama sonucu bulunan tüm tasarım boşlukları kapatıldı (§11.6). Bundan sonraki
değişiklikler playtest bulgularından gelecektir; "İleride Değişebilecekler" listesi açık soru
değil, **bilinçli olarak ertelenmiş** kararlardır.

> Bu dokümanda **cevabı olmayan hiçbir madde bırakılmamıştır.** Uygulama sırasında bir belirsizlik
> çıkarsa bu bir GDD eksiğidir — kod tarafında varsayım yapılmaz, GDD'ye geri dönülür ve burası
> güncellenir.
