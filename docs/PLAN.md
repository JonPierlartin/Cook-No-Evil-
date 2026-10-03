# Cook No Evil! — Faz 0 Planı

> **Bu dosya Claude Code için bir talimat dosyası DEĞİLDİR.**
> Buradaki sıraya bakarak önden iş yapılmaz. Claude Code için bağlayıcı olan, kullanıcının o an
> verdiği tek adım ve `CLAUDE.md`'deki Çalışma Protokolü'dür. Bu dosya Ersel'in ve teknik
> danışmanın planlama dosyasıdır: takvim, sıralama, saat tahminleri, kararlar ve riskler.
>
> **Doğruluk kaynakları:** tasarım → `docs/GDD.md` · mühendislik → `CLAUDE.md` · plan → bu dosya.
> Üçü farklı türde gerçek tutar ve birbirini tekrar etmez.

**Son güncelleme:** 30 Eylül 2026

---

## 1. Takvim (sabit)

| Kilometre | Tarih |
|---|---|
| **Başvuru son tarihi (gerçek deadline)** | **11 Ekim 2026** |
| Program başlangıcı | 26 Ekim 2026 |
| Program bitişi | 20 Aralık 2026 |
| Final etkinliği (İstanbul) | 15-16 Ocak 2027 |

**Hedef:** 11 Ekim'e oynanabilir dikey dilim + 3 kişilik oynanış videosu. Bu oyun kâğıt üzerinde
anlatılamıyor; başvurunun gücü videodan geliyor.

**Kod donması: 8 Ekim akşamı.** 9-11 Ekim kod günü değil — seviye yazımı, cila, video çekimi ve
başvuru metni günleridir.

### Çalışma blokları

| Blok | Tarih | Kapasite (25 sa/hafta) | Planlanan yük |
|---|---|---|---|
| Hafta 1 | 18–24 Eylül | 25 sa | 23 sa |
| Hafta 2 | 25 Eylül – 1 Ekim | 25 sa | 25 sa |
| Hafta 3 | 2–8 Ekim | 25 sa | 25 sa |
| Teslim bloğu | 9–11 Ekim (Cu/Ct/Pz) | ~14 sa | 14 sa |
| **Toplam** | | **~89 sa** | **87 sa** |

*18 Eyl revizyonu: randomizasyon sistemi (§7.3) `LevelConfig`'i 2 sa'ten 6 sa'e, property drawer'lar
+2 sa çıkardı → toplam 91 sa. Sos kanalı (4 sa) kesildi → 87 sa. Bkz. §5 ve Karar Günlüğü.*

**Uyarı — bu plan 20 sa/hafta'da yürümez.** 20 sa/hafta'da kapasite ~72 saate düşer ve 13 saatlik
açık oluşur. Plan 25 sa/hafta varsayımıyla kuruldu.

---

## 2. Faz 0 kapsamı

Bağlayıcı tanım `docs/GDD.md` §11.2'de, Claude Code'un uyacağı sınır `CLAUDE.md` → "Faz 0 Kapsam
Sınırı" bölümünde. Burada yalnızca 18 Eylül'de yapılan kapsam değişiklikleri kayıtlıdır.

### Kapsama eklendi

| Ne | Gerekçe |
|---|---|
| Çöp kutusu (yangın koşulu olmadan) | Yanmış et ve yanlış hamburger için tek çıkış yolu. Olmazsa ızgara ilk yanmada kalıcı kilitlenir. |
| Duvar malzeme listesi panosu (§3.6.2) | Garnitür numaraları ve protein yönleri bölüm başında değişiyor (§3.6.3). Pano olmadan Sayı ve Yön kanalları çalışmaz. |
| 15 sn hazırlık fazı (§3.4.3) | Komi'nin sos brifingini yapabileceği tek pencere. |
| Duvar hata sayacı — X X X (§7.1.2) | Yıldız sistemi kapsam dışı olduğu için hata görünürlüğü başka bir yerden gelmeli. |
| Sos pompaları + §5.6 konum bağımlılığı | Renk kanalı kapsam içi; sos pompası yoksa kanal hiçbir yere varmıyor. **Taşma kalemi — bkz. §5.** |

### Kapsamdan çıkarıldı

| Ne | Gerekçe |
|---|---|
| Yangın olayı (tüp, kapı, alarm, söndürme, ızgara kilidi) → Faz 1 | Yanma zorunlu (bedelsiz pişirme Komi'yi gereksiz kılar), yangın değil. Yangın altyapısı ≈ 12 sa, bütçenin %14'ü. |
| Lobide rol seçimi (§8.1) → Faz 0.5 | Hiçbir şeyi bloke etmiyor; 3 kişilik demoda katılma sırası yeterli. |
| Şef→Komi gibberish (§10.4) → Faz 0.5 | Komedi katmanı, mekanizma değil. Faz 0'da Şef'in sesi Komi'de hiç çalınmaz — mekanik sonuç aynı, maliyet 3 sa yerine 0,5 sa. |
| 3. seviye | 2 seviye yazılacak. |

---

## 3. Haftalık plan

### Hafta 1 — 18–24 Eylül (23 sa)

> **Hafta sonu hedefi:** Şef, tek başına, tek makinede, çiğ eti ızgaraya koyup pişmişini alıp doğru
> sırayla hamburger birleştirebiliyor; yanmışı çöpe atabiliyor. 3 kişilik test gerekmiyor.

| # | İş | Bağımlılık gerekçesi | Tahmin |
|---|---|---|---|
| 1 | Etkileşim sahipliği + sunucu otoritesi düzeltmesi (K6) | Oyunun tamamı bozuk; üstüne hiçbir şey yazılamaz. | 3 sa |
| 2 | Highlight sistemi — normal render (§4.1.2) | 1'in doğrulanması buna bağlı; ayrıca tasarım gereği. Şef varyantı kontur render'ı bekliyor. | 3 sa |
| 3 | `Yamak` → `Komi` yeniden adlandırma | Davranış değiştirmiyor; kod büyüdükçe pahalılaşıyor. | 1 sa |
| 4 | Input Action temizliği (Bug 3) | Girdi katmanına dokunmuşken hallet, ikinci kez açma. | 0,5 sa |
| 5 | `ItemType` kategori + `BurgerRecipe` yeniden yapısı | §6.7.3 kategori sırası bu veriye dayanıyor. | 2 sa |
| 6 | Birleştirme kategori sırası + ekmek alt/üst (§6.7.3) | 5'siz yazılamaz. | 2,5 sa |
| 7 | Ortak ilerleme temel sınıfı (K5/K6) + ızgara (2 yuva, çiğ/pişmiş/yanmış, K7 attach) | En yüksek getirili mimari adım — Faz 1'de fritöz/içecek/dondurma/söndürme bedava gelir. | 6 sa |
| 8 | Çöp kutusu (§5.3.2, yangınsız) | 7'den hemen sonra zorunlu. | 1 sa |
| 9 | **Kontur render spike — 4 sa katı timebox** | Projenin en büyük teknik bilinmeyeni (K2). Zor olduğunu 3. haftada öğrenirsek video biter. 4 saatte ekranda kontur yoksa durulur ve raporlanır. | 4 sa |

**Gerçekleşen (18-21 Eyl):**

| # | İş | Durum |
|---|---|---|
| 1 | Etkileşim sunucu otoritesi + çağırana bağlama | ✅ |
| 1.5 | Etkileşim olaylarına oyuncu kimliği | ✅ |
| 1.6 | Steam'siz Local Debug — MPPM ile tek makinede 3 oyuncu | ✅ |
| 1.7 | Spawn pozisyonu yarışı (`CharacterController`) | ✅ 10/10 |
| 1.8 | `IngredientContainer` + rol kapılı istasyonlar | ✅ |
| 3 | `Yamak` → `Komi` | ✅ |
| 2a | Crosshair geri bildirimi + `IInteractionGate` | ✅ |
| 2b | Yerleştirme çerçevesi — **tasarım düzeltildi, 2d ile yerine konuldu** | ✅ (atıldı) |
| 2c | Elde tutulan nesnenin görünmesi | ✅ |
| 2d | Yerleştirme önizlemesi + tek `IngredientRegistry` | ✅ |
| 2e | İstemci/sunucu menzil kontrolü tek fonksiyonda | ✅ |
| 9 | **Kör görüş (kontur render)** — hazır URP özelliğiyle ilk denemede çalıştı, K2 desen testi geçti. **En büyük teknik risk kapandı.** | ✅ |
| 9b | Şef için yerleştirme önizlemesi (nabız gibi atan beyaz kontur) — ekip onayladı | ✅ |
| 4.0 | Envanter mimarisi — tasarım raporu alındı, denetlendi (öğe = ağ nesnesi, slot = fiş, elde parent yok, tek kapı `ItemMover`) | ✅ |
| T | **Temizlik:** `IngredientType`/`IngredientRegistry` → `ItemType`/`ItemRegistry` (mekanik, .meta korunur) — `2675553` | ✅ |
| 4.1a | Öğe prefabları ve altyapı: `Item`, `ItemPresence`, `ItemType.itemPrefab`, NGO prefab kaydı, registry doğrulaması. Oynanış değişmez — `a8a4546` | ✅ |
| D0 | **Düzeltme:** hedef görünür olmalı — etkileşim ışını oyuncu/engelde durur; Şef halkası derinlik testine alınır — `2a5690d` | ✅ (6/6 oyun testi geçti) |
| 4.1b | Envanter öğe fişine göçer: kaptan alma = spawn, birleştirme = despawn (`ItemMover`), seçili slot kuralı, arayüzler öğe üzerinden çözer — `09a9a59` | ✅ (tüm oyun testleri 4.2 testiyle kapandı; sızıntı ölçümü K2d'de Claude Code'a doğrulatılıyor) |
| 4.2 | Yuvaya yerleştirme (`ItemSlot` + `ItemMover`, K7) — `6a87ac1` | ✅ (oyun testi geçti; **Şef'te tezgahtaki ekmek yarım kontur** → K2d) |
| K2d | Şef yuvadaki ince nesneleri tam görsün — kök neden gömülmeydi (pivot); eşikler değişmedi; `_DepthBias` 0,25→0,02; sızıntı 0, yuva crosshair tablosu doğru — `f241530` | ✅ (oyun testi geçti) |
| D | Karakterler round başında doğuyor, round dışı etkileşim kapalı, test rol sırası Inspector'da; NV/RPC sıra garantisi yok (kabul) — `b7720b9` | ✅ (oyun testi geçti; istemci Şef yolu ilk kez test edildi, temiz) |
| 7a | Ortak ilerleme bileşeni (`ServerProgress`, K5) + köfte öğesi + köfte kabı; köfte rengi faza göre — `efee0d1` | ✅ (oyun testi geçti) |
| D2 | **Düzeltme:** etkileşim bağlamı — tıklama anındaki seçili slot RPC ile gider, sunucu etkileşimde `ActiveSlotIndex`'i okumaz — `8fd932a` | ✅ (oyun testi geçti) |
| 7b | Izgara: 2 yuva, yuvadaki köfteyi sunucu pişirir, Çiğ→Pişmiş→Yanmış — `0a79563` | ✅ (oyun testi geçti) |
| 6a | Birleştirme: `ItemType.category` + zorunlu kategori sırası + görünür yığılma — `30855e2` | ✅ (oyun testi geçti) |
| 6b | Ekmeğin iki parçası + hamburger öğesi (katmanlar + pişmişlik fazı) — `7b17d4a` | ✅ (kural ve akış testleri geçti; **2 bug**: hamburger yuvaya konunca yok oluyor, Şef'te katmanlar ayrık) |
| D4 | **Düzeltme:** hamburger yuvaya konunca yok oluyor + MissingReferenceException; Şef'te katman aralıkları | ✅ (`76da044`) — yok olma + hata kapandı (oyunda doğrulandı); katman boşluğu **elde sürüyor** → D5 |
| D5 | **Düzeltme:** kopya görseller (elde / önizleme) — hamburger katman boşluğu, yarım ekmeğin gömülmesi | ❌ `b11e568` — birinci şahısta boşluk **büyüdü** (dünya AABB şişmesi); üçüncü şahıs ve önizleme bitişik → D6 |
| D6 | **Düzeltme:** yığın yüksekliğini yerel mesh sınırlarından ölç + önizlemede faz rengini kapat — `a99dfe7` | ✅ (8/8 oyun testi geçti, 30 Eyl) |
| E1 | Ekmek görseli: alt/üst ayrı modeller (GDD §6.7.3) — `9ba8091`; ek yeri pırıltısı `4d76c5a` | ✅ (oyun testi geçti) |
| D3 | Envanter doluyken üst ekmek konabilsin — `9ba8091` | ✅ |
| D7 | runInBackground + gömülü doğma `fe38438` · cızırtı, DURDURULDU + zaman aşımı, sağır Komi, ikonlar `fed5b33` · Komi sesleri `1d4d1b2` | ✅ (oyun testi geçti) |
| H | **Harita:** artist haritası ana sahnede, 5 garnitür + kaplar, buzdolabı eti, 2 tezgah, rol doğma noktaları, mutfak kapısı — `6b327ae` | ✅ |
| D8 | Pencere yuvaları (3'er, iki yönlü), çöp (3 oda), et zorunlu, ızgara yüksekliği, sağırlık düzeltmesi, elde öğe görünürlüğü — `c87b383`, `13b7af4` | ✅ (elde öğe + iki yönlü pencere testi bekliyor) |
| 8 | Çöp kutusu — D8'de yapıldı · input temizliği (Previous/Next) | ⏳ input |
| 16a+16b | `NumericValue` + `Selection<T>`, `LevelConfig`, `BurgerVariant`, kanallar, istasyon kimlikleri, sunucu çözümlemesi — property drawer'sız | ✅ kod; oyun testi bekliyor |
| 13a+13b | Veri odaklı sinyal çarkı `R` + sinyal yayını (`09f733c`); cooldown ve rol kısıtları kalktı, etkileşim jesti iptal ediyor (PLAN 12 de burada kapandı). Genel `E` çarkı (14) açık | ✅ kod; oyun testi bekliyor |
| 15 | Duvar malzeme panosu (İstasyon + Kasa, replike eşleşmeden) + ikon üretici editör aracı; çarkta yön değerleri kendi yönünde | ✅ kod; oyun testi bekliyor |
| 15b | Pano yeniden düzenlendi (kod yazmaz, yerleşim anlatır; resim + ad) · Tavuk/Balık/Veji türleri (yalnızca pano; alınamaz) · hotbar görünümü | ✅ kod; oyun testi bekliyor |
| 17a | Müşteri akışı (çözümlenmiş seviyeden), sipariş alma, sabır + çark (yalnızca Kasiyer), hata sayacı, bölüm sonu altyapısı | ✅ kod; oyun testi bekliyor |
| 17b | Sipariş pop-up'ı (varyant görseli + X'li eksikler), sipariş süresi (SO + içerikten sinyal sayımı), sipariş çarkı (yalnızca Kasiyer), süre hatası | ✅ kod; oyun testi bekliyor |
| 23 | Kese kağıdı + kap, 2 paketleme alanı, pakete ürün koyma (gerçek öğe olarak içinde), içerikle birlikte çöp, içerikten türeyen paket fotoğrafı, Kasa penceresi rolleri | ✅ kod; oyun testi bekliyor |
| 24 | Teslim (müşteriye paket), sunucu doğrulaması (birebir içerik + servis edilebilir faz), memnun/öfkeli ayrılış, başarı/hata sesleri (Kasa + mutfak); müşteri rotası duvarın dışından; pakete tek hamburger | ✅ kod; oyun testi bekliyor |
| 25 | Duvar hata paneli (3 oda; Şef yalnızca yanan X'i görür), kazan/kaybet, sonuç ekranı, temiz yeniden başlatma, sıralı seviye listesi | ✅ kod; oyun testi bekliyor |
| 22 | Tarif kitapçığı: Kasa'da nesne (yalnızca Kasiyer), yerel görünüm, içindekiler + varyant açılımları açık varyantlardan, ESC yalnızca kitabı kapatır | ✅ kod; oyun testi bekliyor |
| L | Lobide rol seçimi (her rolden bir tane olmadan başlamaz), ESC menüsü + lobiye dönüş, pop-up yalnızca Kasiyer'e | ✅ kod; oyun testi bekliyor |
| L2 | Sonuç ekranından lobiye dönüş, lobide bölüm seçimi (ok tuşları; kazanınca sıradaki açılır), karakter modelleri + Ketçap yürüme animasyonu | ✅ kod; oyun testi bekliyor |

**Envanter tahmini (21 Eyl, 4.0 raporu):** Claude Code'un tahmini 11-14 sa + ~2 sa test; danışman 5-6 sa demişti. Raporun 4.3'ü (test amaçlı köfte yuvası + kullanıcısız `BurgerAssembly` iskeleti) **kaldırıldı**: `ServerProgress` ve kategori değerleri ızgara adımına, `BurgerAssembly` birleştirme adımına (Adım 6) taşındı. Kalan envanter işi ~8-10 sa. `ServerProgress` zaten ızgaranın 6 saatinin içindeydi.

**Tahmin muhasebesi (21 Eyl):** "Highlight" diye 3 saat planlanan iş, tasarım netleştikçe 2a-2e'ye
bölündü ve **~10 saat** tuttu (2b'nin çerçevesi yanlış anlaşılma yüzünden atıldı; 2c zaten ileride
yapılacaktı; registry birikmiş borçtu). Hafta 1'in gerçekleşen yükü **~42 saat**, plan 23 saatti.

**Hafta 1'den kalan:** kontur spike (4) · malzeme kategorisi (2) · birleştirme sırası (2,5) · ızgara
+ ortak ilerleme sınıfı (6) · çöp (1) · input temizliği (0,5) = **~16 sa**, 24 Eylül'e 3 gün.
**Hafta 1 hedefi (Şef tek başına hamburger yapıyor) 24 Eylül'e yetişmeyecek** — ızgara büyük
ihtimalle Hafta 2'ye kayar. 24 Eylül'de tam yeniden plan yapılacak.

### Hafta 2 — 25 Eylül – 1 Ekim (25 sa)

> **Hafta sonu hedefi: ilk 3 kişilik uçtan uca test.** Kasiyer sinyal gönderiyor → Komi görüp sesle
> anlatıyor → Şef kör görüşle yapıyor. Sipariş elle tetiklenebilir.

| # | İş | Tahmin |
|---|---|---|
| 10 | Kontur render entegrasyonu | 2 sa |
| 11 | Şef highlight varyantı — kontur kalınlaşma/nabız (§4.1.2) | 2 sa |
| 12 | Emote sistemi düzeltmesi: cooldown kaldır, animasyon-bloklama, etkileşimle iptal (§3.6.0) | 3 sa |
| 13 | İç içe sinyal çarkı `R` — 3 kanal + "Sipariş Bitti" | 4,5 sa |
| 14 | Genel emote çarkı `E` ayrımı | 1 sa |
| 15 | Duvar malzeme listesi panosu + LevelConfig'ten eşleşme | 1,5 sa |
| 16a | Ortak `Randomizable` veri tipleri (sayısal: elle değer / min–max · seçim: sabit / havuz) + property drawer'lar (§7.3.4) | 4 sa |
| 16b | `LevelConfig` SO — alanların tamamı tanımlı, sipariş slotu yapısı, Faz 0'da kullanılmayanlar boş (K8) | 2 sa |
| 17 | Müşteri spawn + sipariş/teslim noktaları + eşzamanlı 3 sınırı (§3.4.2) | 5 sa |
| 19 | Animasyon entegrasyonu (1. tur) | 2,5 sa |

**Hafta 2 toplamı: 27,5 sa** — kapasitenin 2,5 sa üstünde. Randomizasyon sistemi bu haftaya düştü.

### Hafta 3 — 2–8 Ekim (25 sa)

> **Hafta sonu hedefi:** Oyun kendini oynatıyor. Müşteri geliyor, süre işliyor, hata sayılıyor,
> seviye kazanılıyor/kaybediliyor.

| # | İş | Tahmin |
|---|---|---|
| 20 | Sipariş pop-up + sabır çarkı + sipariş çarkı (§3.6.1, §3.4.4) | 4 sa |
| 21 | 15 sn hazırlık fazı (§3.4.3) | 1 sa |
| 22 | **Tarif kitapçığı** — içindekiler + kategori atlama + sayfa çevirme + ESC (§3.6.2) | 6 sa |
| 23 | Paketleme: kese kağıdı 3 görsel durumu + 2 alan + içerik fotoğrafı (§5.3.1) | 5 sa |
| 24 | Teslim + sunucu tarafı doğrulama (§5.3.1) | 3 sa |
| 25 | Hata sayacı + duvar X X X ×3 oda + kazanma/kaybetme + `RoundEnded` (§3.4, §7.1.2) | 3 sa |
| 26 | Teslimat geri bildirim sesleri — Şef için zorunlu (§7.1.1) | 1 sa |
| 27 | Seviye 1 kontrol ipuçları (§6.7.5) | 1 sa |
| 28 | Animasyon entegrasyonu (2. tur) | 1 sa |
| 18 | VoIP: Kasiyer sunucuda mute + konuşmacı AudioSource'unu gerçek oyuncuya parent'la + Kasa↔Mutfak 3D mesafe zayıflaması (K4) | 2,5 sa |

**Hafta 3 toplamı: 27,5 sa** — kapasitenin 2,5 sa üstünde.

*Sos pompaları (§5.6, §6.7.4) bu haftadan **çıkarıldı** — 18 Eyl'deki kapsam eklemeleri onun
yerini aldı. Bkz. §5 ve Karar Günlüğü.*

### Teslim bloğu — 9–11 Ekim (12 sa)

| İş | Tahmin |
|---|---|
| 2 seviyenin yazımı + denge | 3 sa |
| Bug / cila | 5 sa |
| Video: 3 kişiyi topla, en az 2 çekim denemesi, kurgu | 4 sa |
| Başvuru metni + 8 haftalık kilometre planı (GDD §11.4 hazır) | (video ile paralel) |

**Video çekimi için takvimi şimdi ayarla.** 10 Ekim Cumartesi akşamını kilitle, 11'i yedek tut.
Üç kişinin aynı akşamı boşaltması 3 gün kala ayarlanmaz.

---

### Yeniden plan — 22 Eylül (24 Eylül'den öne alındı)

**Kapasite:** Ersel **~40 sa/hafta** çalışacak (22 Eyl beyanı). Kısıt saat değil, **Claude Code
kullanım hakkı** — gün içinde ne kadar adım koşturulabildiği. 22 Eyl–8 Eki: ~2,3 hafta ≈ **~90 sa**.
Kalan plan ~71 sa, sapma ×1,5 ile ~100 sa → **sıkışık ama yakın.** Kesme rezervi (7 sa) yerinde;
1 Ekim karar noktası geçerli.

**Kullanım hakkını koruma kuralları:** Claude Code'a oyun içi test yaptırılmaz (zaten kural);
teşhis dışında ekran görüntüsü yok; bir adım 2 saati aşacaksa bölünür; raporlar kısa.

**Yeni sıra (bağımlılığa göre):**
1. D2 etkileşim bağlamı (slot yarışı) — ~1 sa
2. 7b ızgara — ~3 sa
3. 6 birleştirme (kategori, alt/üst ekmek, görünür yığılma, hamburger öğesi) — ~4 sa
4. 8 çöp — ~1 sa · input temizliği — 0,5 sa
   → **Şef tek başına hamburger yapabiliyor (eski Hafta 1 hedefi)**
5. Pencere (Şef→Komi yuvası, `ItemSlot` ile ucuz) — ~1 sa
6. 16a/16b `Randomizable` + `LevelConfig` — 6 sa
7. 12–15 emote düzeltmesi, iç içe sinyal çarkı, `E` ayrımı, duvar panosu — ~10 sa
8. 17 müşteri + sipariş/teslim noktaları — 5 sa · 20 sipariş pop-up/sabır/sipariş çarkı — 4 sa
   → **1 Ekim hedefi: ilk 3 kişilik uçtan uca test**
9. 22 tarif kitapçığı — 6 sa · 23 paketleme (içindekiler gerçek öğe) — ~7 sa · 24 teslim — 3 sa
10. 25 hata sayacı + kazan/kaybet — 3 sa · 26 teslim sesleri — 1 sa · 18 VoIP — 2,5 sa
11. 21 hazırlık fazı, 27 kontrol ipuçları, 19/28 animasyon — ~5 sa
**Yeni kesme adayları (rezerve eklendi):** 15 sn hazırlık fazı (asıl gerekçesi sos brifingiydi, sos
kesildi) · kontrol ipuçları · ikinci teslim sesi seti.

## 4. Karar noktaları

| Tarih | Karar |
|---|---|
| **24 Eylül** | Kontur render spike sonucu. 4 saatte kontur çıkmadıysa yaklaşım değişir — bu, video kimliğini doğrudan etkilediği için tek başına ele alınır. |
| **1 Ekim akşamı** | (a) 1–19 arası kapandı mı? (b) Animasyonlar geldi mi? (c) 3 kişilik zincir baştan sona yürüdü mü? İkisi "hayır" ise §5'teki kesme sırası devreye girer. |
| **8 Ekim akşamı** | Kod donar. Bu tarihten sonra yalnızca seviye verisi, denge ve bug düzeltmesi. |

---

## 5. Kesme sırası (geride kalırsak — yukarıdan aşağı)

Panik anında karar vermemek için önceden yazıldı. Sıra, "geç kesilebilme kolaylığı"na göre
kurulmuştur: en üstteki kalemler additive olduğu için son ana kadar bekletilebilir.

| Sıra | Ne | Tasarruf | Bedeli | Durum |
|---|---|---|---|---|
| 1 | Sos pompaları + Renk kanalı | 4 sa | §5.6 gider — Şef'in körlüğünün en keskin gösterimi kaybolur. Izgara aynı bağımlılığı daha zayıf gösterir. Animasyoncunun 4 renk animasyonu da düşer. | **KESİLDİ — 18 Eyl 2026** |
| 2 | 2. seviye | 1,5 sa | Tek seviyeyle de video çekilebilir. | Rezervde |
| 3 | Genel emote çarkı `E` | 1 sa | Sosyal emote'lar, oynanışa etkisiz. | Rezervde |
| 4 | VoIP mesafe zayıflaması | 2,5 sa | Faz 0'da Kasiyer zaten sunucuda mute; K4 kısmen ihlal ama demo etkilenmez. | Rezervde |
| 5 | Property drawer'lar | 2 sa | Inspector çirkinleşir, davranış değişmez. Ersel seviye yazarken yavaşlar. | Rezervde (son çare) |
| | **Kalan acil rezerv** | **7 sa** | | |

**Tarif kitapçığındaki içindekiler + kategori atlama kesme listesinden çıkarıldı** (18 Eyl) —
kitapçığın varlık sebebi "kapılar dizisi" olması; kategori atlama olmadan mekanik anlamını yitirir.

**Kesilmeyecekler (ne olursa olsun):** etkileşim sahipliği düzeltmesi · kontur render · ızgara +
yanma · birleştirme kategori sırası · iç içe sinyal çarkı · müşteri/sipariş döngüsü · paketleme ·
hata sayacı + kazanma/kaybetme. Bunlardan biri düşerse video konsepti kanıtlamaz.

---

## 6. Ekip görevleri (kod dışı)

### Animasyoncu — kritik yol (GDD §11.7)

**14 animasyon.** 10 zorunlu, 4 koşullu.

| Set | Adet | Durum |
|---|---|---|
| Yön (yukarı / aşağı / sağ / sol) | 4 | Zorunlu |
| Sayı (1–5, parmak) | 5 | Zorunlu |
| "Sipariş Bitti" | 1 | Zorunlu |
| Renk (4 sos — el rengi değişip sallanma) | 4 | **Koşullu — 1 Ekim kararına bağlı** |

**Bağlayıcı kısıtlar:**
- **Animasyon başına 1,2–1,5 sn.** Bu bir tercih değil; GDD §3.4.1'deki sipariş süresi formülü bu
  aralığa göre kalibre edildi. 2,5 sn'yi aşarsa formül baştan türetilir.
- First-person'da, **pencere arkasından, mesafeden okunabilir** olacak — Komi bunları karşıdan
  görüyor. Kol ve gövde silueti belirleyici; yüz ifadesi işe yaramaz.
- Önce blockout istenecek. Kod placeholder'la ilerler, final animasyonu beklemez.

### 3D artist

- **Gri-kutu 3 oda + 2 pencere yerleşimi.** Bağlayıcı kısıtlar: Kasa↔Mutfak arası normal konuşmanın
  anlaşılamayacağı kadar uzak (§10.4) · Kasiyer hem duvar panosunu hem Komi'yi görebilmeli ·
  Komi hem panoyu hem pencereyi görebilmeli (§3.6.2) · teslim kuyruğu Kasa/İstasyon penceresinden
  görünmeli (§3.6.2) · sos pompaları İstasyon/Mutfak penceresinden bölüm başında okunabilmeli (§5.6).
- **Siluetle ayrışacaklar** (§4.1.1): malzeme kapları (marul/domates/turşu/soğan/peynir), ızgara,
  birleştirme tezgahı, paketleme alanı, çöp kutusu. Şef bunları yardımsız bulabilmeli.
- **Siluetle ayrışMAYacaklar** (§5.6): **sos pompaları** — hepsi birebir aynı form, yalnızca
  renk + kaba desen farkı. *Bu satır artist'e yazılı gitmezse güzel farklı şekiller çizer ve
  oyunun en iyi mekaniği sessizce ölür.*
- **Tarif kitapçığı görselleri:** her hamburger varyantı ayırt edilebilir çizilecek (balık mı et mi
  siluetten/görselden anlaşılmalı). Müşteri pop-up'ındaki görsel ile kitapçıktaki görsel **aynı
  olmak zorunda** (§3.6.2). Malzeme seti birebir aynı olan varyantlar (Deluxe ↔ Veji Deluxe) yine de
  görsel olarak farklı çizilir.
- **Duvar hata sayacı:** dijital saat görünümlü pano, yan yana 3 X.
- **Çarpışma hacimleri (teknik, bağlayıcı):** etkileşilebilir her nesnenin (kaplar, tezgah, ızgara
  yuvaları, makineler, pencereler) çarpışma hacmi **primitif** (kutu/küre/kapsül) veya **convex**
  mesh olmalı — non-convex mesh collider menzil kontrolünü bozar. Görsel model detaylı olabilir;
  çarpışma hacmi ayrı ve basit tutulur.
- **Materyaller:** URP Lit veya Unlit kullanılmalı. Özel shader gerekiyorsa `DepthNormals` geçişi
  olmalı — yoksa nesne Şef'in kör görüşünde **hiç görünmez**.
- **Pivot noktaları:** elde tutulan ve yerleştirilen öğelerin (ekmek, köfte, bardak vb.) pivotu
  **tabanda** olmalı. Pivot merkezdeyse öğe tezgaha/ızgaraya yarı gömülü oturur.

---

## 7. Karar günlüğü

| Tarih | Karar | Gerekçe |
|---|---|---|
| 18 Eyl 2026 | Yangın Faz 1'e, yanma Faz 0'da kalıyor | Yanma olmadan ızgarada eti unutmanın bedeli yok; bedel yoksa Komi'nin uyarısı isteğe bağlı hale gelir ve oyunun en önemli bağımlılığı çöker. Yangın ise hatanın sonucunun süslenmesi — 12 sa maliyet. |
| 18 Eyl 2026 | Lobide rol seçimi Faz 0.5'e | Hiçbir şeyi bloke etmiyor, demoda katılma sırası yeterli. |
| 18 Eyl 2026 | Gibberish Faz 0.5'e | Faz 0'da Şef'in sesi Komi'de hiç çalınmaz; mekanik sonuç aynı, maliyet 1/6. |
| 18 Eyl 2026 | 2 seviye | Kapasite. |
| 18 Eyl 2026 | **Tarif kitapçığı Faz 0'da kalıyor, sayfa sınırı yok, içindekiler + kategori atlama dahil** | Ersel'in kararı. Gerekçe: pop-up formatı kitapçıkla birlikte değişiyor (ürün fotoğrafı ↔ malzeme listesi); sonradan eklemek iki kez iş demek. **Bedeli:** sos kanalıyla aynı haftaya düşüyor ve ikisi birden sığmıyor; sos taşma kalemi oldu. |
| 18 Eyl 2026 | Duvar hata sayacı: sönük X Şef'e görünmez, yanan X görünür | Ersel'in kararı. Şef hata sayısını görünen X sayısını sayarak okuyor — tek anlamlı. Uygulama: rendering layer mask (bkz. GDD §7.1.2). |
| 18 Eyl 2026 | Sinyal sayımına "Sipariş Bitti" dahil | GDD §3.4.1'in kendi kalibrasyon noktası onu sayıyor ve o jestin de animasyonu var. |
| 18 Eyl 2026 | Üç dosyalı doküman mimarisi (GDD / CLAUDE / PLAN) | CLAUDE.md haftalık takvimle şişmemeli; ama çalışma protokolü chat mesajında değil CLAUDE.md'de durmalı, yoksa her yeni chat'te kaybolur. |
| 18 Eyl 2026 | **Her alan sabit **veya** rastgele olabilir; her sipariş slotu kendi modunu taşır (§7.3 yeniden yazıldı)** | Ersel'in kararı. "Bölüm çok hızlı bitti, 2. müşteriyi koyayım" demek bir kod değişikliği olmamalı. Danışman bu beş alanı Faz 0 kararına çevirmişti — oysa §11.9 hepsini zaten `LevelConfig` alanı olarak tanımlıyordu; hata düzeltildi. **Bedeli:** `LevelConfig` 2 sa → 6 sa. |
| 18 Eyl 2026 | Property drawer'lar Faz 0'da yazılacak | Ersel'in kararı: "kötü editör arayüzü = boşa zaman kaybı". Davranışa etkisi yok, seviye yazma hızını etkiliyor. **Bedeli:** +2 sa. |
| 18 Eyl 2026 | **Sos kanalı (§5.6) Faz 0'dan kesildi** | Randomizasyon sistemi + drawer 6 saat ekledi; kesme sırasının 1. kalemi bu. **Animasyoncuya hemen bildirilmeli** — 14 animasyonun 4'ü (renk seti) artık gerekmiyor, 10'a düştü. Faz 0.5'e ilk eklenecek kalem budur. |
| 18 Eyl 2026 | İçecek makinesi basılı tutma **değil** (§6.7.1 yeniden yazıldı); dondurma kolu basılı tutma **kalıyor** | GDD §6.7.1 zaten "düğmeye basar" diyordu. K5 temel sınıfı iki modu birden desteklemeli: tetikle-sunucu-yürütsün ve basılı-tut. |
| 21 Eyl 2026 | Şef'in yerleştirme önizlemesi nabız gibi atan beyaz kontur | Şef'in dünyasında her şey beyaz çizgi; sabit çizgi gerçek nesneden ayırt edilemezdi. Ekip test edip onayladı. Yedek plan beyaz yarı saydam dolguydu (§4.1.1'e istisna olurdu). |
| 21 Eyl 2026 | **Envanter öğeleri ağ nesnesi olacak** (sayı-envanteri kaldırılıyor) | Köftenin pişme ilerlemesi, hamburgerin bileşimi, kese kağıdının içeriği tip numarasıyla taşınamaz; K5/K7 zaten bunu gerektiriyor. Izgaradan önce yapılmalı, yoksa ızgara yeniden yazılır. Yan kazanç: tezgaha konan malzeme tezgahta görünür. |
| 21 Eyl 2026 | Alınan öğe seçili slota, seçili slot doluysa ilk boş slota girer | Oyuncu baktığı slotun dolmasını bekler (GDD §4.1). |
| 21 Eyl 2026 | Yerleştirme önizlemesi öğenin o anki hâlini gösterir | Önizleme "tam olarak bu oraya gider" der (GDD §4.1.2 ②). |
| 21 Eyl 2026 | Hamburger tek öğe, katmanlar öğenin içindeki listede | Beş ayrı nesneden oluşan hamburgeri taşımak beş parent işlemi olurdu; biri başarısız olursa hamburger bölünür. |
| 21 Eyl 2026 | Paketin içindekiler gerçek öğe olarak paketin içinde kalır | Teslimde sipariş birebir kontrol edilecek ("turşusuz muydu?"); "1 hamburger" notu bunu imkânsız kılar. |
| 21 Eyl 2026 | Pakete yalnızca tamamlanmış ürün girer; çiğ/yarım ürün engeli **Komi'nin paketlemesinde**, Şef'in eylemlerinde değil | Şef'in eylemi pişmişliğe göre engellenseydi crosshair ona "çiğ" derdi; Şef Komi'siz yoklayabilirdi, temel bağımlılık çökerdi. Komi durumu zaten gördüğü için engel ona bir şey sızdırmaz. |
| 21 Eyl 2026 | Lobide karakter yok: Faz 0'da karakterler round başında doğar, ayrı lobi sahnesi Faz 0.5'te | Lobide ayrılanın karakteri "hayalet" kalıyordu; ayrı sahne şimdi test edilmiş Steam akışına dokunurdu. |
| 21 Eyl 2026 | Hedef görünür olmalı: arada oyuncu/engel varsa hedef yok sayılır, önizleme engelin arkasında kalır | Test: Şef'in halkası önündeki oyuncunun üstünden görünüyordu; etkileşim ışını da oyuncuların içinden geçiyordu. |
| 21 Eyl 2026 | Seçili slot doluysa **seçili slottan sonraki** ilk boş slot, sona gelince başa sarar | Ersel'in netleştirmesi; önceki "ilk boş slot" yazımı eksikti. | *Git geçmişi: ekleme mantığı ilk commit'ten beri "ilk boş slot"; hiç değişmemiş. Seçili slot 1 iken iki kural aynı görünüyordu.*
| 22 Eyl 2026 | Yuva: boşsa koy, doluysa al; yer değiştirme yok | Danışman önerisi (GDD'de tanımsızdı); Ersel itiraz ederse değişir. |
| 22 Eyl 2026 | Test için rol sırası Inspector'dan ayarlanabilir (lobide rol seçimi değil) | Şef hep host olduğu için Şef istasyonlarının istemci yolu test edilemiyordu; ızgaradan önce kapatılmalı. |
| 22 Eyl 2026 | Aynı karede iki oyuncunun aynı yuvaya tıklaması Faz 0'da kabul edilen sınır | Yuvaları farklı roller sırayla kullanıyor; niyet RPC'ye taşımanın maliyeti şimdilik değmez. |
| 22 Eyl 2026 | K2d (Şef ince nesne konturu) D'den ve ızgaradan önce | Izgaradaki köfteler de tezgaha yatık ince nesneler; Şef onları siluetten tanıyamazsa ızgara oynanamaz (GDD §4.1.1 "kimlik siluetten okunur"). |
| 22 Eyl 2026 | Birleştirme tezgahının (sarı küp) malzemeyi "silmesi" bug değil, eksik geri bildirim | Tezgah malzemeyi tüketip yalnızca bir listeye yazıyor, üstünde hamburger görünmüyor. Adım 6'da (kategori sırası + görünür yığılma) çözülecek; 24 Eylül yeniden planında Adım 6'nın ızgaradan önce mi sonra mı geleceğine karar verilecek. |
| 22 Eyl 2026 | Slot değiştirip aynı ağ tick'inde tıklama yarışı Faz 0'da kabul | NGO'da NV deltası tick'te, RPC anında gidiyor; insan için bir tick (~33 ms) içinde tuş+tık nadir. Görülürse RPC'ye slot numarası eklenir. |
| 22 Eyl 2026 | **Açık karar:** gerçek modellerde mesh orijini | Halka mesh orijininden genişliyor. Öneri: artist brief'e "mesh orijini merkezde, prefab kökü tabanda" (kod yok). |
| 22 Eyl 2026 | Sıra: ızgara (7a/7b) → birleştirme (6) → çöp (8) | Birleştirmedeki hamburger katmanı köftenin pişmişlik fazını taşıyor (CLAUDE.md hedef mimari); faz ızgarayla doğuyor. Izgara ayrıca K5 ortak bileşeninin ilk kullanıcısı ve en riskli çekirdek mekanik. |
| 22 Eyl 2026 | 24 Eylül yeniden planı öne alındı; **açık soru: gerçek haftalık kapasite** | Kalan plan ~71 sa (tahmin sapması ×1,5 ile ~100 sa), 25 sa/hafta ile 8 Ekim'e ~55-60 sa kalıyor. Hafta 1'de ~42+ sa çalışıldı. Kapasite netleşmeden kesme kararı verilmez. |
| 22 Eyl 2026 | Kapasite ~40 sa/hafta; asıl kısıt Claude Code kullanım hakkı | Ersel'in beyanı. Plan sığıyor ama sıkışık; kesme rezervi korunuyor. |
| 22 Eyl 2026 | Slot/tık yarışı **şimdi** düzeltiliyor (D2) | Ersel: etkileşim noktaları çoğaldıkça sonradan eklemek pahalılaşır. Doğru — her yeni istasyon aynı "aktif öğe" yolunu kullanacak. |
| 22 Eyl 2026 | D2 yaklaşımı: etkileşim bağlamı (oyuncu + slot) gate'lere ve olaylara açıkça geçer | Alternatif (slot seçimini sunucuya RPC ile yazmak) sahip tarafında slot değiştirmeyi gecikmeli gösterir ve iki kaynak (tahmin + sunucu) yaratır. Bağlam yaklaşımı gizli durum ve sıra varsayımı içermez. |
| 25 Eyl 2026 | Üst ekmek yalnızca yarılanmış ekmekle konur | GDD'de tanımsızdı. Bütün ekmekle kapatmaya izin verilirse alt yarısı sessizce israf olur ve kör Şef bunu fark edemez. |
| 30 Eyl 2026 | Pencereler iki yönlü, 3'er hamburger yuvası (Mutfak: Şef+Komi, Kasa: Komi+Kasiyer) | Ersel'in kararı; GDD §5.1.1/5.1.2 güncellendi. Kasa penceresinin sayısı GDD'de yoktu. |
| 30 Eyl 2026 | Protein zorunlu; etsiz hamburger kapatılamaz | Ersel'in kararı; GDD §6.7.3'teki "garnitürden önce protein" kuralının netleşmesi. |
| 30 Eyl 2026 | Her odada kendi rolünün çöp kutusu, aynı script | Ersel'in kararı; GDD §5.3.2. |
| 30 Eyl 2026 | Fazla müşteriler ve yedek havuz **varsayılan sipariş slotunu** kullanır | Ersel'in kararı (16b). GDD §7.3.1 slot ile müşteri sayısı farkını tanımlamıyordu. GDD §7.3.1'e işlendi (3 Eki). |
| 30 Eyl 2026 | Varyantta malzeme başına "çıkarılabilir" işareti; "komple randomize" yalnızca işaretlileri alır | Ersel'in kararı (16b). Adım "tüm malzemeler" diyordu, GDD §7.3.2 "çıkarılabilir"; tümü alınırsa etsiz (yapılamaz) sipariş çıkabilirdi. GDD §7.3.2'ye işlendi (3 Eki). |
| 3 Eki 2026 | Panoda numara/yön oku yazmaz; kod malzemenin panodaki yerinden okunur (proteinler yön düzeninde, garnitürler sırayla), giriş = resim + ad | Ersel'in kararı: oyuncular dili kendileri bulduklarını hissetsin. GDD §3.6.2 / §3.6.3'e işlendi. |
| 3 Eki 2026 | Pakete aynı türden ikinci ürün (ikinci hamburger) girmez; paket yeniden açılıp farklı ürün eklenebilir | Ersel'in kararı (Adım 23 testi). **GDD §5.3.1'e işlenmedi** (Adım 24 GDD düzenlemeyi yasakladı). |
| 3 Eki 2026 | Sipariş Penceresi = Kasa'nın doğu duvarındaki küçük pencere, müşteriler arka arkaya sıra olur; sabır yalnızca sıranın başındakinde işler | Ersel'in kararı (17a testi); sabrın başlama anı Claude'un seçimi. **GDD'ye işlenmedi.** |
| 30 Eyl 2026 | Kaplar sınırsız; malzeme/stok editörden ayarlanınca değişecek | Ersel'in kararı. LevelConfig'te stok alanları tanımlı ve boş. |
| 3 Eki 2026 | Sabır süresi ve müşteriler arası bekleme **her müşteri için ayrı** çekilir | Ersel'in kararı. GDD §7.3.3'e işlendi. |
| 3 Eki 2026 | Duvar malzeme listesinde eşleşme değişimi **satırın yer değiştirmesiyle** görünür; pano diğer kanalların eşleşmelerini de gösterebilir | Ersel'in notu; GDD §3.6.3'e işlendi. Pano adımı (15) çözülmüş eşleşmeyi (`ResolvedLevel` kanal sırası) okuyacak — istemcilere replikasyon o adımda gerekir. |
| 18 Eyl 2026 | Commit mesajı biçimi `<Kapsam>: <iş>`, kapsam = iş birimi (dosya adı değil) | Ersel'in kararı. Bir adım birden fazla dosyaya dokunuyor; dosya adına göre etiketlenirse aynı işin commit'leri geçmişte birbirinden kopuyor. Biçim `CLAUDE.md` → Sürüm Kontrolü ve Build bölümünde. |

---

## 8. Riskler

| Risk | Etki | Azaltma |
|---|---|---|
| Kontur render (K2) URP 17.5'te beklenenden zor | Video kimliği kaybolur — en yüksek etkili risk | Hafta 1'de 4 sa timebox'lı spike; erken öğren |
| Animasyonlar geç gelir | Sinyal çarkı test edilemez, §3.4.1 doğrulanamaz | Blockout önce istendi; kod placeholder'la ilerler |
| Animasyon süresi 2,5 sn'yi aşar | §3.4.1'deki tüm katsayılar geçersiz | Animasyoncuya 1,2–1,5 sn yazılı kısıt olarak verildi; ilk ölçümde doğrulanacak |
| Tahminler %25 sapar | 85 sa → 106 sa, kapasiteyi aşar | §5'teki kesme sırası; 1 Ekim karar noktası |
| Haftalık kapasite 20 sa'e düşer | 13 sa açık | §5'teki kesme sırasının ilk 3 kalemi |
| Video için 3 kişi aynı akşam bulunamaz | Başvurunun en güçlü parçası eksik kalır | 10 Ekim şimdiden kilitlenir, 11 yedek |

---

## 9. Çalışma ritmi (danışman ↔ Ersel)

**Her oturum sonunda danışman şunları verir:**
1. `CLAUDE.md` değiştiyse **tam dosya** (tak-çıkar).
2. `docs/GDD.md` değiştiyse **yama dosyası** (çapalı). *Tam dosya verilemez: 910 satırı yeniden
   üretmek sessiz içerik kaybı riski taşır. Ersel güncel `GDD.md`'yi danışmana verirse tam dosya
   da üretilebilir.*
3. `docs/PLAN.md` değiştiyse tam dosya.
4. Neyin neden değiştiğinin kısa özeti.

**Kalıcı hatırlatmalar — danışmanın takip edeceği maddeler:**
- [x] **İlk push — 18 Eyl 2026.** Doküman revizyonu + Adım 1 kodu. Commit biçimi `CLAUDE.md` →
      Sürüm Kontrolü ve Build bölümünde.
- [ ] **Her adım kapandığında commit + push.** Adım 1'den sonra bu ritim bozulmayacak; iki haftalık
      commit edilmemiş iş birikmesi bir daha yaşanmayacak.
- [ ] **Temizlik Borcu takibi.** Şu an açık: `PlayerInteractor` teşhis log'u (Adım 2'ye bağlandı),
      `LobbyUIController` teşhis log'u, `Previous`/`Next` input action çakışması, `sprintMultiplier`
      kalıntısı, `VoIPController` dosya başı yorumu, `Ekmek`/`Kofte` geçici ikonları.
- [ ] **Animasyoncuya sos kesintisini bildir** — 14 animasyon 10'a düştü (4 renk animasyonu iptal).
- [ ] **Video çekimi için 10 Ekim Cumartesi akşamını kilitle**, 11'i yedek tut.

---

## 10. Açık sorular

| # | Soru | Neyi bloke ediyor |
|---|---|---|
| 1 | Tarif kitapçığı sayfa düzeni: bir açılım = bir hamburger mi (sol büyük resim / sağ malzeme listesi), yoksa bir açılım = birden fazla hamburger mi (sol N resim alt alta / sağ N liste)? | 3D artist brief'i (görsel boyutu ve adedi) + kitapçık UI kodu |
