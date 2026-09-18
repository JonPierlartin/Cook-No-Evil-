# Cook No Evil! — Faz 0 Planı

> **Bu dosya Claude Code için bir talimat dosyası DEĞİLDİR.**
> Buradaki sıraya bakarak önden iş yapılmaz. Claude Code için bağlayıcı olan, kullanıcının o an
> verdiği tek adım ve `CLAUDE.md`'deki Çalışma Protokolü'dür. Bu dosya Ersel'in ve teknik
> danışmanın planlama dosyasıdır: takvim, sıralama, saat tahminleri, kararlar ve riskler.
>
> **Doğruluk kaynakları:** tasarım → `docs/GDD.md` · mühendislik → `CLAUDE.md` · plan → bu dosya.
> Üçü farklı türde gerçek tutar ve birbirini tekrar etmez.

**Son güncelleme:** 18 Eylül 2026

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
| 5 | `IngredientType` kategori + `BurgerRecipe` yeniden yapısı | §6.7.3 kategori sırası bu veriye dayanıyor. | 2 sa |
| 6 | Birleştirme kategori sırası + ekmek alt/üst (§6.7.3) | 5'siz yazılamaz. | 2,5 sa |
| 7 | Ortak ilerleme temel sınıfı (K5/K6) + ızgara (2 yuva, çiğ/pişmiş/yanmış, K7 attach) | En yüksek getirili mimari adım — Faz 1'de fritöz/içecek/dondurma/söndürme bedava gelir. | 6 sa |
| 8 | Çöp kutusu (§5.3.2, yangınsız) | 7'den hemen sonra zorunlu. | 1 sa |
| 9 | **Kontur render spike — 4 sa katı timebox** | Projenin en büyük teknik bilinmeyeni (K2). Zor olduğunu 3. haftada öğrenirsek video biter. 4 saatte ekranda kontur yoksa durulur ve raporlanır. | 4 sa |

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
