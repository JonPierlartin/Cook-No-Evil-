# Cook No Evil · UI Kit 0.2.0 — Kurulum Notu

50'ler diner'ı temalı arayüz kiti. İçinde **ana menü**, **Ayarlar kartı** ve buton/kontrol kiti var. Tıklama ve üzerine gelme sesleriyle basma animasyonları da hazır. Kit yalnızca görünüm ve etkileşimdir: ayarları, müziği ve dili oyunun kendi sistemleri yönetir.

**Hedef proje:** Unity **6000.5.7f1**, URP 17.5, Input System 1.20 (yalnızca Input System), uGUI 2.5 + TextMeshPro, **Unity Localization 1.5.12**. Ek paket gerekmez (DOTween vb. yok).

---

## 1. Kurulum

> Paket yalnızca `Assets/CNE_UI/` altına yazar. Kurulum bunlara ek olarak `Assets/TextMesh Pro/` (TMP Essential Resources) ve kitin `CNE_UI` metin tablosunu ekler. Tablonun Addressables girdileri Localization tarafından otomatik eklenir.
>
> `Assets/CNE_UI/Fonts/Righteous-Regular.ttf` (+ lisansı) ve `Assets/CNE_UI/Art/Kit/ui_arrow.png` projede zaten varsa aynı yol ve GUID'le üzerine oturur; onları kullanan tabelalar etkilenmez.

1. Paketi içe aktarın: **Assets ▸ Import Package ▸ Custom Package…** → `CookNoEvil_UIKit_v0.2.0_Unity6000.5.unitypackage` → hepsi seçili → **Import**. Otomasyonda `AssetDatabase.ImportPackage(yol, false)` de kullanılabilir.
2. Derleme bittikten sonra kurulumu çalıştırın. İki yol var:
   - **Otomasyon / pencere istemeyen:** `CookNoEvil.UI.EditorTools.CNEUIKitBuilder.BuildSilent()`. Menüdeki karşılığı **Tools ▸ Cook No Evil ▸ UI Kit ▸ Sessiz kur**.
     - Pencere açmaz. Sahne açmaz, oluşturmaz, kaydetmez; açık sahneye dokunmaz.
     - TMP Essential Resources yoksa sessizce içe aktarır, bitince kendiliğinden devam eder.
     - Başarı satırı: `[CNE UI] Sessiz kurulum tamam (UI Kit 0.2.0)`. Bu satır gelmezse `BuildSilent()`'i bir kez daha çalıştırın.
   - **İnsanlar için:** **Kur + demo sahnesi (pencereli)**. Aynı kurulumu yapar, sonra kitin demo sahnesini (`Scenes/CNE_UI_Demo.unity`) kurar. Sahne kaydetmeyi sorar.

**Kurulum ne yapar?**

- Görsellerin içe aktarma ayarlarını (Sprite, 9-dilim, PPU) denetler; uymayanları düzeltir.
- 5 fonttan TMP font asset'i ve parlama/gölge materyalleri üretir (`Fonts/`).
- `Resources/` altındaki ses setini ve dil görünüşlerini denetler, eksikleri tamamlar.
- Kit metinlerini Unity Localization'daki `CNE_UI` tablosuna yazar (§4).
- 14 prefab üretir (`Prefabs/`). Prefablar geçici bir önizleme sahnesinde kurulur.

Tekrar çalıştırmak güvenlidir. Ancak prefablar her seferinde yeniden yazılır; kalıcı değişiklik için **Prefab Variant** kullanın.

---

## 2. Projeye bağlamak: kaynaklar

Kit oyunun verisini tutmaz. Üç kaynak sınıfı üzerinden konuşur; her birinin projede tek dosyalık bir alt sınıfı yazılır, ondan bir asset oluşturulur ve **CNEMainMenu**'ye atanır. Menü bunları Ayarlar kartına ve jukebox şeritlerine kendisi iletir.

| Kaynak | Ne yapar | Cook No Evil'da bağlanacağı yer |
|---|---|---|
| `CNESettingsSource` | Sürgüler (aralık, gösterim, değer), görünüm seçenekleri, VARSAYILANLAR, kaydetme | `GameSettings` + `LookPreference` |
| `CNEMusicSource` | Parça sayısı, sıra, ad, çalıyor mu, sonraki/önceki | `MusicPlayer` |
| `CNEMicLevelSource` | Mikrofon testi seviyesi (isteğe bağlı) | Şimdilik atanmaz → test satırı gizli kalır |

```csharp
// Örnek iskelet: üye adları projedeki gerçek API'ye göre uyarlanır.
[CreateAssetMenu(menuName = "Cook No Evil/UI/Oyun Ayarları Kaynağı")]
public sealed class GameSettingsUISource : CNESettingsSource
{
    public override CNESliderSpec GetSpec(CNEFloatSetting s) { /* aralık + gösterim */ }
    public override float GetFloat(CNEFloatSetting s) { /* GameSettings'ten oku */ }
    public override void SetFloat(CNEFloatSetting s, float v) { /* GameSettings'e yaz */ }
    public override void GetLookOptions(List<CNEChoice> results) { /* LookPreference'ın kayıtlı görünümleri */ }
    public override string Look { get { /* ... */ } set { /* ... */ } }
    public override void ResetToDefaults() { /* GameSettings varsayılanları */ }
}
```

- Kaynak atanmamışsa Ayarlar kartı **kilitli** açılır ve Console'a hata yazar; kit gizlice kendi deposunu kullanmaz.
- Kitteki `CNEPlayerPrefsSettingsSource` (kendi `cne.demo.*` anahtarları) ve `CNEUnityMicrophoneSource` yalnızca demo içindir.
- **Tam ekran** bir kaynak ayarı değildir. Kart onu doğrudan `Screen.fullScreenMode` (FullScreenWindow / Windowed) ile değiştirir; Unity bunu kendisi hatırlar.
- Kit açılışta hiçbir şeye yazmaz: `AudioListener.volume`, ekran kipi ve PlayerPrefs yalnızca oyuncu bir kontrolü değiştirince ve kaynak üzerinden yazılır.

### Menü olayları

| Olay | Ne zaman |
|---|---|
| `onCreateLobby` | LOBİ OLUŞTUR |
| `onBrowseLobbies` | LOBİLERE GÖZAT |
| `onHowToPlay` | NASIL OYNANIR (ekranı yoksa `showHowToPlay` kapatılır; pano kendini kısaltır) |
| `onQuitRequested` | ÇIKIŞ (kapanmadan hemen önce; `quitApplication` kapatılırsa kapatmayı proje yapar) |

Olaylar Inspector'dan (kalıcı dinleyici) bağlanır. Bağlanmamış bir satıra tıklanınca Console uyarır.

### Görünürlük ve katmanlar

- Menü her gösterilişinde (`SetActive(true)`) pano satırları sırayla yerine oturur ve neon logo yanar.
- Menü gizlenince açık Ayarlar kartı kaydedilip animasyonsuz kapanır.
- Prefabın kökü kendi Canvas'ıdır (Screen Space Overlay, 1920×1080, sortingOrder 0). Arka planı yoktur: sahnenin kamerasının çizdiği 3B salon arkada görünür.
- Başka pencereler (ör. lobi pencereleri) menünün **üstünde** görünecekse menü Canvas'ının sortingOrder'ını onlardan küçük yapın.

---

## 3. Ayarlar kartı

Kartın satırları:

- **SES:** Ana ses, Müzik, Müzik parçası (jukebox).
- **SOHBET:** Sesli sohbet, Mikrofon, mikrofon testi.
- **KONTROL VE GÖRÜNTÜ:** Fare hassasiyeti, Tam ekran, Görünüm.
- **DİL:** Bayraklı dil seçimi.

Davranış:

- **Satır gizleme:** Kaynağın desteklemediği sürgü gizlenir. Görünüm satırı iki seçenekten azsa, mikrofon testi kaynak yoksa, dil satırı projede tek dil varsa gizlenir. Satırı kalmayan bölüm başlığı da gizlenir; kartın boyu içeriğe göre kısalır.
- **Rakam gösterimi:** `Percent` değeri ×100 yazar (ses 0,5 → 50, mikrofon kazancı 1,0 → 100). `Decimal` iki ondalık yazar (hassasiyet 1.00).
- **Görünüm düğmesi:** Kaynağın verdiği seçenek sayısına göre kendini kurar (1–5 kademe). Kayıtlı bir görünüm kalkarsa kademesi de kalkar.
- **Kayıt:** Değerler anında uygulanır; kart kapanınca kaynağın `Save()`'i çağrılır. Esc / gamepad "geri" kartı kapatır.
- **Tek başına kullanım:** `Prefabs/CNE_SettingsPanel` ayrı da kullanılabilir (ör. ileride oyun içi Esc menüsü). Bu durumda `CNESettingsPanel.settings` alanı doldurulur, kart `Open()` / `Close()` ile açılıp kapanır.

---

## 4. Diller (Unity Localization)

Tek dil sistemi projenin Unity Localization'ıdır; kit ayrı bir dil durumu tutmaz.

- **Metinler:** Kit metinleri `CNE_UI` string tablosundadır (`Assets/CNE_UI/Localization/Tables`). Projenin `UIStrings` tablosuna dokunulmaz. Metinler `CNELocalizedText` bileşeniyle bağlanır (içinde bir `LocalizedString`). Tablo yüklenene kadar prefabdaki Türkçe yazı görünür. Dil değişince harfler bir an dönerek yeni dile geçer.
- **Kaynak:** Kit metinlerinin kaynağı `Localization/CNE_Strings.csv`'dir (`key,tr,en`, UTF-8). Kurulum ya da **Tools ▸ … ▸ Kit metinlerini Localization tablosuna yaz** bu dosyayı tabloya şu kurallarla aktarır:
  - Yalnızca projede tanımlı dillere (Locale) yazar; **yeni dil eklemez**.
  - Yalnızca boş girdileri doldurur; tabloda elle düzeltilmiş metinlere dokunmaz.
  - `tr-TR` gibi kodlar CSV'deki `tr` sütunuyla eşleşir.
- **Dil seçimi:** Seçici, Localization Settings'teki Locale'leri listeler ve seçileni `LocalizationSettings.SelectedLocale` yapar. Böylece oyunun bütün metinleri birlikte değişir.
- **Bayrak ve ad:** `Resources/CNE_Languages`'ten gelir (tr → Türkiye, en → ABD; `flag_gb` de pakette). Listede olmayan bir dil bayraksız ve sistemin verdiği adla görünür.
- **Tek dil:** Projede tek Locale varken DİL satırı gizlidir. İkinci dil eklenince satır kendiliğinden görünür.

**Yeni dil eklemek:**

1. Localization Settings'e Locale ekleyin.
2. CSV'ye o dilin sütununu ekleyip kit metinlerini tabloya yazdırın.
3. Bayrağı ekleyin (`Art/Flags/flag_xx.png`, 176×128).
4. `CNE_Languages`'e bir satır ekleyin. CJK diller için `fontOverride` alanına o alfabeyi içeren bir TMP font verin.
5. Seçimin bir sonraki açılışta da geçerli olması için Localization Settings ▸ *Startup Locale Selectors* listesinin başına **Player Pref Locale Selector** ekleyin.

---

## 5. Menü gece ortamı (`CNEMenuAmbience`)

Ana menü görünürken sahneye gece havası verir; menü kapanınca geri alır. Hepsi oyun çalışırken yapılır, sahneye gece değeri yazılmaz.

**Bileşen alanları (sahneye ait bağlantılar):**

- `targetCamera`: sahnenin kamerası.
- `cameraAnchor`: menü kadrajı (isteğe bağlı `fieldOfView` ile).
- `cameraAnchorB` + `driftSeconds`: verilirse kamera oyun çalışırken iki kadraj arasında çok yavaş gidip gelir (smootherstep, gerçek zaman).
- `activateDuringMenu`: gece ışık düzeneği (ay ışığı, lamba ışıkları); sahnede kapalı durur.
- `deactivateDuringMenu` / `disableDuringMenu`: ör. gündüz güneşinin Light bileşeni.
- `menuSun`: menüde `RenderSettings.sun` olacak ışık.

**Profil (`CNEMenuAmbienceProfile`, veri):**

- Ortam ışığı (ambient probe) çarpanı ve rengi.
- Yansıma çarpanı.
- İsteğe bağlı gece gökyüzü.
- Kamera arka plan rengi.

**Kullanım:**

- Bileşeni menü nesnesinin bir çocuğuna koyun; menü `SetActive` oldukça kendiliğinden uygular ve geri alır.
- Editörde önizleme için `Apply()` → kamerayı bir RenderTexture'a çizin → `Restore()`. Önizlemeden sonra sahneyi kaydetmeyin.
- Geri alırken yalnızca hâlâ bu bileşenin koyduğu değerde duran ayarlar eski hâline döner. Menü açıkken başka bir sistem aynı ayarı değiştirdiyse onun değeri korunur. Gece değerlerini yeniden basmak için `Apply()` tekrar çağrılabilir.

---

## 6. Sesler

Hepsi bu proje için sentezlendi (48 kHz, mono, lisans derdi yok). Ayarlar: `Resources/CNE_UISoundSet` (klipler, seviye, perde oynaması, en kısa tekrar aralığı). Kaynaklar 2B'dir ve dinleyici efektlerini atlamaz: ana ses (`AudioListener.volume`) ve dinleyicideki filtreler (ör. Komi'nin sağırlığı) arayüz seslerine de uygulanır.

```csharp
CNEUIAudio.Play(CNEUISound.Confirm);          // sahneye bir şey eklemeye gerek yok
```

| Olay | Ses | Nerede |
|---|---|---|
| Hover | krom tıkırtısı (3 varyasyon) | üzerine gelme / klavye-gamepad ile seçme |
| Press / Release | tıknaz tuş "tak" / geri dönüş | butona basma / bırakma |
| Confirm | servis zili "ding" | Onay (yeşil) butonları |
| Back | yumuşak "tok" | Geri / Kapat / ÇIKIŞ |
| BoardPress | harfler tık tık oturur | ana menü panosu |
| SwitchOn / SwitchOff | şalter "klak" | açık/kapalı şalterleri |
| KnobTick / SliderTick | kademe tıkı | döner düğme, sürgü |
| PanelOpen / PanelClose | tabela iner / kalkar | Ayarlar kartı |
| FlagFlap | bayrak dalgalanır | dil seçimi |
| NeonOn | neon yanar | logo |
| NeedleDrop | plak iğnesi | müzik parçası değişince |
| Error | diner buzzer'ı (kısık) | pasif butona basma, mikrofon yok |
| CashRegister | yazar kasa "ça-çing" | ileride: sipariş / sinyal çarkı |

Yeni olay eklemek: `CNEUISound` enum'una **sona** yeni sayı ekleyin, ses setine satır ekleyin.

---

## 7. Buton kiti

| Prefab | Renk | Rol | Tık sesi |
|---|---|---|---|
| CNE_Button | lila | Normal | Release |
| CNE_Button_Primary | marul yeşili | Onay | servis zili |
| CNE_Button_Back | peynir turuncusu | Geri | tok |
| CNE_Button_Danger | domates kırmızısı | Tehlike | Release |

**CNEButtonFeedback** animasyonu:

- Üzerine gelince hafif büyür ve kalkar.
- Basınca gölgesine çöker ve yassılır; bırakınca yaylanarak geri zıplar.
- Pasif butona basılırsa "hayır" titremesi ve kısık buzzer.

Değerler bileşende ayarlanabilir; hepsi gerçek zamanlıdır (`Time.timeScale`'den bağımsız).

Diğer prefablar: CNE_ArrowButton, CNE_SignTitle (neon tabela başlık), CNE_Slider, CNE_Switch, CNE_RotarySelector, CNE_LanguagePicker, CNE_JukeboxStrip, CNE_VUMeter, CNE_SettingsPanel, CNE_MainMenu.

---

## 8. Bilinen sınırlar

- **Unity içinde henüz açılmadı.** Kod şu yollarla denetlendi:
  - Unity 6000.5'in derleme düzeni taklit edilerek API taslaklarına karşı derlendi: Assembly-CSharp / Assembly-CSharp-Editor / oyuncu derlemesi, Input System ve eski Input ayrı ayrı. Sonuç: 4 yapılandırmada 0 hata, 0 uyarı.
  - Localization çağrıları com.unity.localization 1.5.12 kaynağıyla, TMP çağrıları TMP kaynağıyla karşılaştırıldı.

  İlk kurulumdan sonra Console'a bakın.
- **Gereken:** Unity Localization ve projede bir Localization Settings asset'i (en az bir Locale). Bunlar olmadan kit metinleri yüklenmez.
- **Kapsam:** Kit + ana menü + ayarlar. Lobi pencereleri, lobi içi, hata ekranı, Esc menüsü, bölüm sonu ve HUD sonraki sürümlerde gelecek.
- **asmdef yok:** Scriptler Assembly-CSharp'a derlenir; proje de böyle.
- **Fontlar:** *Dinamik* TMP font asset'leridir; ilk kullanımda değişmiş görünmeleri normaldir. Neon parlaması tam SDF shader'ı gerektirdiği için materyaller *TextMeshPro/Distance Field* kullanır. ShareTechMono'da Ğ ğ İ Ş ş yok; bu harfler VarelaRound'dan gelir.

---

## Sürüm notları

**0.2.0 — projeye uyum:**

- **Hedef sürüm:** Unity 6000.5.7f1.
- **Sessiz kurulum:** `BuildSilent()` eklendi. Pencere açmaz, sahneye dokunmaz, TMP'yi sessiz içe aktarır.
- **Ayarlar:**
  - `CNESettings` kaldırıldı: açılışta `AudioListener` / tam ekran yazmıyor.
  - Kart artık bir `CNESettingsSource` üzerinden oyunun deposuna bağlanıyor.
  - Mikrofon ayarı kazanç (0–2, 1 = olduğu gibi) olarak gösteriliyor.
  - Görünüm düğmesi dinamik oldu.
- **Müzik:** `CNEMusicPlayer` kaldırıldı; jukebox şeridi bir `CNEMusicSource`'u gösteriyor. Kaynak "önceki"yi desteklemiyorsa geri oku gizleniyor.
- **Dil:**
  - CSV dil sistemi kaldırıldı; metinler Unity Localization'daki `CNE_UI` tablosundan geliyor.
  - Dil seçici Locale'leri listeliyor ve tek dilde gizleniyor.
- **Ana menü:**
  - Demo arka planı prefabdan çıktı.
  - NASIL OYNANIR gizlenebiliyor; pano gizlenen satıra göre kısalıyor.
  - Menü her gösterilişinde giriş animasyonu oynuyor.
- **Yeni:** `CNEMenuAmbience` ile menü gece ortamı.
- **Korunan GUID'ler:** Righteous ve ui_arrow aynı yol ve GUID'lerle duruyor.

**0.1.1:** Neon sönerken TMP parlaması yanık kalıyordu; düzeltildi.

**0.1.0:** İlk sürüm.

## 9. Klasörler

```
Assets/CNE_UI/
  Art/          Kit (buton, panel, sürgü, şalter…), Flags (tr, us, gb), Misc (imleç, demo arka planı)
  Audio/UI/     24 ses (.wav)
  Editor/       Kurulum (BuildSilent), asset denetimi, CNE_UI tablosu
  Fonts/        Righteous, Bungee, Varela Round, Pacifico, Share Tech Mono + lisanslar
  Localization/ CNE_Strings.csv (kit metinlerinin kaynağı); Tables/ (kurulum oluşturur)
  Resources/    CNE_UISoundSet, CNE_Languages
  Scripts/      Core, Audio, Feedback, Localization, Settings, Widgets, Menu
  Prefabs/, Scenes/, Demo/   (kurulum oluşturur)
```

## Lisanslar

Fontlar SIL Open Font License 1.1 (`Fonts/Licenses/`), oyunla birlikte dağıtılabilir. Sesler, görseller ve kod bu proje için üretildi.
