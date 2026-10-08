# Cook No Evil! · Arayüz kiti için proje özeti

*8 Ekim 2026 · UI Kit 0.1.1 ve `anamenu-sahne-kurulum-prompt.md` projeyle karşılaştırıldıktan sonra yazıldı.*
*Amaç: kiti ve kurulum prompt'unu yazan sohbetin, projenin gerçek durumunu bilerek ikisini revize etmesi.*

Kit ve prompt projeye **alınmadı** (yalnızca iki dosya alındı, bkz. §9). Aşağıdaki her şey depodaki koddan ve
sahneden okundu; tahmin yok. "Karar gerekiyor" yazan yerler proje sahibinin (Ersel) cevaplaması gereken sorulardır.

---

## 1. Kısa özet: kitin varsaydığı ile projedeki durum

| Konu | Kit / prompt varsayımı | Projedeki durum |
|---|---|---|
| Unity sürümü | 6000.7.0f1 | **6000.5.7f1** (proje kuralı: bu sürümde kalınır) |
| Render | URP, "Forward mı Forward+ mı" | URP 17.5.0, **Forward+**, HDR açık |
| Yazı | TextMeshPro | Mevcut arayüzün tamamı **eski uGUI `Text`**; TMP Essential Resources projede **yok** |
| Dil | Kendi CSV tablosu (`CNELocalization`), tr + en | **Unity Localization 1.5.12**, tablo `UIStrings`, yalnızca **Türkçe** |
| Ayarlar | `CNESettings` (PlayerPrefs `cne.settings.*`) | `GameSettings` (PlayerPrefs `settings.*`); oyun kodu bunu okuyor |
| Görünüm seçimi | `CNEViewMode` enum (Off / Stylized / CNEToon) | `LookPreference` (metin kimlikli, görünümler kendini kaydeder) |
| Müzik | `CNEMusicPlayer` + jukebox şeridi, "kitte müzik yok" | `MusicPlayer` + 2 parça var |
| Sahne | Ayrı `MainMenu` sahnesi | **Tek sahne**: menü, lobi ve oyun aynı sahnede |
| Menü olayları | "Bağlama" | Çalışan lobi akışı var; bağlanmazsa yeni menü işlevsiz kalır |
| Editör otomasyonu | "Kur" komutu | Kur **onay pencereleri açıyor** ve açık sahneyi değiştiriyor (otomasyonu kilitler) |

---

## 2. Proje temelleri

- **Oyun:** 3 oyunculu asimetrik co-op mutfak oyunu, birinci şahıs. Roller: Şef (kör, mutfak), Komi (sağır,
  istasyon), Kasiyer (dilsiz, kasa).
- **Unity 6000.5.7f1.** Paketler: URP 17.5.0, Input System 1.20.0 (*Active Input Handling* = yalnızca Input System),
  uGUI 2.5.0, Localization 1.5.12, Netcode for GameObjects 2.13.1, glTFast 6.20.0, Addressables (Localization için).
  **Cinemachine yok.** Paket eklemek / sürüm değiştirmek için proje sahibine sorulur.
- **Ağ:** Steam lobileri (Facepunch.Steamworks), host = sunucu. Editörde Steam'siz yerel test yolu var
  (Local Host / Local Join düğmeleri; yalnızca Editor ve Development Build'de görünür).
- **asmdef yok**; her şey `Assembly-CSharp`.
- **Klasörler:** `Assets/Scripts/{Core, Network, Player, Systems, UI}`, `Assets/Scenes`, `Assets/Prefabs`,
  `Assets/NewAssets/<paket>` (artist paketleri), `Assets/CNEToon` (toon shader ve outline), `Assets/Localization`.
- **Kod kuralları:** SOLID; veri ScriptableObject'te; seviyeden seviyeye değişen değer koda gömülmez; Türkçe yorum;
  geçici çözüm ("play-around") yok. Sahne dosyalarında yalnızca amaçlanan değişiklik commit'lenir.

---

## 3. Sahne mimarisi (kritik)

Build'de iki sahne var: `Assets/Scenes/SampleScene.unity` (her şey) ve `Assets/Scenes/ToonLookdev.unity` (shader
test sahnesi, ek olarak yüklenir).

`SampleScene` içinde kalıcı iki kök vardır ve ayrımı korunmalıdır:
- `NetworkBootstrap` (ağ nesnesi değil): `NetworkManager`, taşıyıcılar, `SteamLobbyManager`.
- `GameSystems` (ağ nesnesi): `RoleManager`, `GameLoopManager`, `LevelDirector`, `PlayerSpawner`, `EmoteSystem`…

İki Canvas:
- `LobbyCanvas` — ana menü, lobi oluştur / listele / şifre pencereleri, lobi içi (rol seçimi), hata ekranı.
  Round başlayınca kapanır.
- `GameplayCanvas` — oyun içi arayüz. Round dışında kapalıdır (yani **ana menüde ve lobide ayarlar menüsü yok**).

Akış: açılış → ana menü (ilk ekran) → Lobi Oluştur / Lobilere Gözat → lobi içi (rol seç, host bölüm seçer, Başlat)
→ round (aynı sahne; karakterler o anda doğar) → bölüm sonu ekranı → "Lobiye dön" → lobi içi.
Oturumdan ayrılınca ilk ekrana dönülür.

Round dışında sahnenin `Main Camera`'sı çizer (tek `AudioListener` onun üstünde); yerel karakter doğunca kendi
kamerası devralır, despawn'da sahne kamerası geri açılır.

**Ayrı bir `MainMenu` sahnesi şu an yok.** Eklenirse: açılış sahnesi, sahneler arası geçiş ve `NetworkManager`'ın
`DontDestroyOnLoad` davranışı ele alınmalıdır (oturumdan sonra tek sahneye dönüşte NetworkManager'ın çiftlendiği
bir tuzak daha önce görüldü; lookdev sahnesi bu yüzden *ek olarak* yükleniyor).
**Karar gerekiyor:** ayrı sahne mi, yoksa gece salonu + yeni menü mevcut sahnenin ilk ekranı mı.

---

## 4. Mevcut arayüz envanteri (16 parça)

### Menü ve lobi — `LobbyCanvas`
| # | Pencere | Script | İçerik |
|---|---|---|---|
| 1 | Ana menü (ilk ekran) | `LobbyUIController` | Logo, Lobi Oluştur, Lobilere Gözat, Çıkış, müzik parçası düğmesi |
| 2 | Lobi oluştur | `LobbyBrowserUI` | Lobi adı, şifreli kutucuğu + şifre, onay / iptal |
| 3 | Lobi listesi | `LobbyBrowserUI`, `LobbyListRow` | Satırlar (ad, oyuncu sayısı, kilit), Yenile, kapat |
| 4 | Şifre penceresi | `LobbyBrowserUI` | Şifre alanı, katıl / iptal |
| 5 | Lobi içi | `LobbyUIController` | Davet Et, Başlat (yalnız host), Ayrıl |
| 6 | Rol seçimi (lobi içinin parçası) | `LobbyUIController` | Üç rol düğmesi, oyuncu listesi, host için bölüm seçimi (`<` / `>`) |
| 7 | Hata / bağlantı koptu ekranı | `LobbyUIController.ShowErrorScreen` | Metin + Tamam |

### Oyun içi — `GameplayCanvas`
| # | Pencere | Script | Not |
|---|---|---|---|
| 8 | Hotbar | `HotbarUI` | Envanter slotları. Şef'te ikonlar yalnızca kontur çizilir |
| 9 | Crosshair | `CrosshairUI` | Üç durum: nötr / kullanılabilir / engelli |
| 10 | Sinyal çarkı (R) | `SignalWheelUI` + `WheelView` | Yalnız Kasiyer; kategori dilimleri → değer baloncukları |
| 11 | Emote çarkı (E) | `EmoteWheelUI` + `WheelView` | Herkes |
| 12 | Tarif kitapçığı | `RecipeBookUI` | Yalnız Kasiyer; ESC yalnızca kitabı kapatır |
| 13 | ESC menüsü | `PauseMenuUI` | **Yerel; oyunu DURDURMAZ.** Ayarlar kartı + Lobiye dön |
| 14 | Ayarlar kartı | `SettingsMenuUI` | Bkz. §5 |
| 15 | "DURDURULDU" katmanı | `PauseOverlayUI` | Bir oyuncu kopunca; oyun gerçekten donuk |
| 16 | Bölüm sonu ekranı | `RoundResultUI` | Sonuç + host için "Lobiye dön" |

Dünyadaki göstergeler (ekran penceresi değil): müşterinin sipariş balonu ve süre çarkı, duvardaki malzeme panoları,
hata panelleri (üç X), paketin üstündeki fotoğraf, salondaki GİRİŞ / ÇIKIŞ tabelaları.

Henüz olmayanlar: ana menüde / lobide ayarlar, "Nasıl oynanır" ekranı, bölüm seçim haritası, kontrol ipuçları.

**Mevcut görünüm:** yazı tipi Bangers (kâğıt kart + bant + kareli masa örtüsü teması), sprite'lar kodla üretiliyor
(`Assets/UI/Generated/`). Hotbar, crosshair, bölüm sonu, "DURDURULDU" ve hata ekranı yer tutucu görünümde. Kit bu
temanın yerine geçecekse hepsinin sırayla taşınması gerekir; kit 0.1.1 yalnızca ana menü + ayarları kapsıyor.

---

## 5. Ayarlar — çakışmanın ayrıntısı

Projede tek kaynak `GameSettings` (statik, PlayerPrefs). Kit içe alındığı anda `CNESettings.Boot`
(`RuntimeInitializeOnLoadMethod`, BeforeSceneLoad) `AudioListener.volume` ve `Screen.fullScreenMode`'u kendi
değerleriyle yazar: **iki sistem aynı şeyi yazar.**

| Ayar | `GameSettings` (proje) | `CNESettings` (kit) | Kim okuyor (proje) |
|---|---|---|---|
| Ana ses | `MasterVolume` 0–1, varsayılan **1** | 0–1, varsayılan 0,8 | `AudioListener.volume` |
| Müzik | `MusicVolume` 0–1, varsayılan **0,5** | 0–1, varsayılan 0,6 | `MusicPlayer` |
| Sesli sohbet | `VoiceVolume` 0–1, varsayılan **1** | 0–1, varsayılan 0,8 | `VoIPController` (örneklerde çarpılır) |
| Mikrofon | `MicGain` **0–2**, 1 = olduğu gibi | `MicVolume` 0–1, 0,8 = normal | Paketle gider, **alıcıda** uygulanır |
| Fare hassasiyeti | `MouseSensitivity` çarpan, 1 = varsayılan | 0,1–3, varsayılan 1 | `PlayerController` |
| Müzik parçası | `MusicTrack` (dizin) | `MusicTrack` | `MusicPlayer` |
| Tam ekran | Ayar değil; `Screen.fullScreenMode` (FullScreenWindow / Windowed) | Kayıtlı ayar | — |
| Görünüm | `LookPreference.Selected` (metin kimliği) | `CNEViewMode` enum | `CNELookApplier`, `StylizedLookController` |
| Dil | Yok (Unity Localization seçer) | `Language` | — |

Değişiklik bildirimi: `GameSettings.Changed` (parametresiz `Action`), `LookPreference.Changed`.

**Görünüm seçimi nasıl çalışır:** her görünüm kendini `LookPreference.Register(id, etiket)` ile kaydeder. Şu an
kayıtlılar: `""` (KAPALI), `"stylized"` (STİLİZE), `"cne"` (CNE TOON). Bir görünümün klasörü silinirse seçeneği
kendiliğinden kaybolur. Kayıt anahtarı `settings.look`. Kitin döner düğmesi sabit üç değerli enum; liste dinamik
olmalı ya da bu üç kimliğe eşlenmeli.

**Mikrofon testi:** kitin VU ibresi `Microphone` API'siyle çalışıyor gibi; projede ses Steam Voice'tan gelir
(Facepunch), `Microphone` kullanılmıyor. Yerel testte ses iletilmez. İkisi aynı anda mikrofonu açarsa ne olacağı
denenmedi.

**Sağırlık kuralı (bozulmamalı):** Komi'nin sağırlığı ana ses ayarından bağımsızdır; ayarlarla telafi edilemez.
Müzik kaynağında "Bypass Listener Effects" açık (boğukluk filtresi müziğe uygulanmaz).

**Karar gerekiyor:** tek kaynak hangisi. Öneri: kitin kartı yalnızca *görünüm* olsun, değerleri `GameSettings` ve
`LookPreference`'a yazsın (kit tarafında bir "ayar sağlayıcı" arayüzü olursa köprü tek dosya olur).

---

## 6. Dil — çakışmanın ayrıntısı

- Proje: `com.unity.localization` 1.5.12. Tablo koleksiyonu `UIStrings` (`Assets/Localization/`), tek dil `tr`.
  Başlangıç seçicileri: komut satırı → sistem dili → sabit (yedek `tr`). Metinler `LocalizeStringEvent` ile bağlanır.
- 29 anahtar var: `lobby.*` (durum metinleri), `role.sef / komi / kasiyer / none`, `error.*` (Steam, lobi dolu,
  round sürüyor, zaman aşımı…), `button.host / invite / start_game / leave / ok`, `panel.connection_lost`,
  `game.paused`, `sign.entrance`, `sign.exit`.
- **Tabloda olmayan çok metin var:** lobi listesi, rol seçimi, ayarlar kartı, bölüm sonu ekranı, kitapçık ve pano
  metinleri şu an Inspector'da düz Türkçe metin. Proje sahibinin kararı: "lokalizasyon işi en son".
- Kit: `Localization/CNE_Strings.csv` + `CNELocalization` + `CNELocalizedText`, tr ve en hazır, dil seçimi
  PlayerPrefs'te, sistem dili Türkçe değilse `en`.

İki sistem yan yana kalırsa dil değiştirince ekranın yarısı değişir. **Karar gerekiyor:** hangisi esas. Kitin
tablosu esas alınırsa mevcut 29 anahtarın ve düz metinlerin oraya taşınması, dünyadaki tabelaların da kitin
sistemine bağlanması gerekir; Unity Localization esas alınırsa kitin `CNELocalization`'ı ona sarılmalıdır.

**Yazı tipi tuzakları (ölçüldü):** `Font.HasCharacter` dinamik fontta yedek fonta düşen harfi de "var" sayar;
Türkçe harfler fontun cmap tablosundan doğrulanmalı. Righteous ve Bungee'de Ğ İ Ş Ç Ö Ü ı ğ ş ç ö ü **tam**.
Bangers'ta kalın kesim yok (`FontStyle.Bold` harfleri yapıştırır) ve küçük "i" noktasız çiziliyor.

---

## 7. Müzik ve sesler

- `MusicPlayer` (sahne nesnesi `Muzik`, tekil `Instance`): `tracks` (ad + klip), 2B, döngü, seviye
  `baseVolume × GameSettings.MusicVolume`, ilk ekranda da çalar. `SelectNextTrack()`, `CurrentTrackName`.
- Parçalar `Assets/Audio/Music/`: `Muzik_ChubbyCat.wav` (**CC-BY 4.0, atıf zorunlu**) ve `Muzik_JustSmoreFun.mp3`
  (CC0). İkisi de deneme.
- `MusicTrackButton`: tıklayınca sıradaki parça; ana menüde (sol alt) ve ayarlar kartında.
- AudioMixer **yok**. Ayrı "efekt sesi" ayarı yok.
- Kitin `CNEMusicPlayer` + jukebox şeridi aynı işi yapıyor (çapraz geçişi var, bizimkinde yok). Biri seçilmeli.
- Kitin arayüz sesleri (`CNEUIAudio`) projede karşılığı olmayan yeni bir şey; çakışma yok.

---

## 8. Lobi ve menü olaylarını bağlamak için API

`LobbyUIController.Instance`:
- `BeginHost(string lobbyName, string password)` — lobi kurar (şifre boş olabilir).
- `BeginJoin(ulong lobbyId, string password)`
- `LeaveToInitialScreen()` — oturumdan ayrıl, ilk ekrana dön.
- `ShowErrorScreen(string errorKey)`

`LobbyBrowserUI`: `OpenCreateDialog()`, `OpenList()`, `CloseAll()`.

`SteamLobbyManager.Instance`: `RequestLobbyListAsync()` → `List<LobbyInfo>` (`Id`, `Name`, `Members`, `MaxMembers`,
`Locked`); olaylar `OnLobbyCreated`, `OnLobbyJoined`, `OnLobbyError`, `OnHostDisconnected`, `OnPasswordRequired`;
`OpenInviteOverlay()`, `LeaveLobby()`, `IsInLobby`, `IsHost`.

Kurallar: lobiler herkese açık listelenir; şifre **sunucuda** doğrulanır (bağlantı onayında); round sürerken lobi
listede görünür ama katılma reddedilir. Rol seçimi yalnızca lobide; üç rol de birer oyuncuda olmadan "Başlat" kapalı.

Kitin `onCreateLobby` / `onBrowseLobbies` olayları `LobbyBrowserUI.OpenCreateDialog` / `OpenList`'e bağlanabilir;
"Nasıl oynanır" için projede ekran yok. `onQuitRequested` için projede `Application.Quit` zaten var.

---

## 9. Görsel sistem (arayüzün uyması gerekenler)

- **Toon shader `CNE/Toon`** (`Assets/CNEToon/`, belge `docs/ToonShader.md`): emission `_EMISSION_ON` anahtarı +
  `_EmissionColor` (HDR) + `_EmissionBaseTint`; ışıma maskesi `_PropMap`'in **G kanalı**. Ek ışıkları (point)
  **destekler**, basamaklı. Material Variant ile denenmedi. Saydam yüzey çizmez (cam ve su URP Lit saydam).
- **Outline** (`CNEOutlineFeature`): yalnızca `Outline` / `Outline Silhouette` rendering layer'larındaki nesneler.
- **Görünüm ve post-process:** bloom vb. yalnızca **CNE TOON görünümü seçili ve oyuncu round içindeyken** kurulan
  bir global Volume'dan gelir (`CNEPostProcessController`, çalışırken kurar). Menüde ve lobide şu an post-process
  yok. Sahne kamerasında post-processing kapalı.
- **Şef'in kör görüşü:** ayrı renderer (derinlik + normalden kontur). Arayüz derinliğe yazmaz, Şef'te görünmez;
  Şef'in görmesi gereken arayüz ayrıca düşünülmeli (hotbar bunu yapıyor).
- **Işık:** sahnede tek directional (sıcak renk, gölgeli). Mimari gölge atmaz. Tavan lambalarında ve sarkıtlarda
  **ışık kaynağı yok** (`Socket_Light` noktaları boş). Oyunda gece / gündüz yok.
- **Renk paleti:** `cook_no_evil_palet_256.png` (16 px hücre). Kitin `CNEPalette` renkleri paletle uyumlu; sinyal
  renkleri (ketçap, hardal, mayonez, barbekü, sinyal fonu `#172A3A`) arayüzde kullanılmaz kuralı projede de geçerli.

---

## 10. Salon — sahne notundaki varsayımlarla farklar

Salon `SampleScene`'de kurulu (kök `Salon`, dünya 10,8 / 0,30 / −14,98; Kasa kökü + (2,10; 0; −8,20) ✓). Kurulum
bir editör aracıyla yapılır ve yeniden çalıştırılabilir (*CNE → Harita → Install In Scene*; `HaritaSceneInstaller`).
Yerleşim prefab'ı: `Assets/Prefabs/Harita/Salon_Yerlesim.prefab`. Kasa, İstasyon ve Mutfak da aynı sahnede.

| Sahne notunun varsayımı | Projedeki durum |
|---|---|
| Kapılar (`Leaf_*`) kapalı | Oyun sahnesinde **açık** (giriş içe, çıkış dışa): müşteriler içinden yürüyor, kapı animasyonu yok |
| GİRİŞ / ÇIKIŞ tabelaları `MI_Decal_Giris / Cikis`, emission 1,6 | Yüz düz materyale (`MI_Tabela_Zemin`) çevrildi; yazı dünya uzayı Canvas + Localization (`sign.entrance`, `sign.exit`). **Işımıyor**, materyalle kısılamaz |
| Balıklar için script yazılacak | `AquariumFish` zaten var (`SM_Aquarium` üstünde): rastgele hedef, yumuşak dönüş, süzülme, kıvrılma. Burun Unity'de yerel **−X** (doğrulandı). `Time.deltaTime` kullanıyor (unscaled değil) |
| Salonda kontur yok | Proje sahibinin kararıyla salon eşyaları **çizgi alıyor** (zemine / masaya oturanlar) |
| Mimari `ARCH_Salon.glb` | Mimari **kodla** üretiliyor (`Harita_Mimari`: duvar / zemin / tavan mesh'leri, desenler materyalde, dünya uzayında). Tek mesh olduğu için "yalnızca salon duvarı" ayrı nesne değil |
| Sokak | Vitrinin dışında yalnızca 6 m düz zemin. `SM_StreetLamp` vb. **yok** |
| Emission materyalleri | `Assets/NewAssets/Salon/Materials/` ve `Assets/NewAssets/Mutfak_C1/Materials/`; kubbeler, jukebox ve OPEN `MI_Palette_Emission`'ı paylaşıyor (not doğru) |
| Müşteri / oyuncu yok | Müşteriler round başında doğar; menüde zaten yoklar |

Salonu başka bir sahneye "oynanış objesi taşımadan" getirmenin en temiz yolu yerleşim prefab'ı + mimarinin o sahne
için ayrıca üretilmesidir; mimari üretimi şu an tüm haritayı birlikte kuruyor (yalnız salon + Kasa duvarı için ayrı
bir giriş noktası gerekir).

---

## 11. Çalışma kuralları (Claude Code tarafı)

Kurulum prompt'unun bunlarla uyumlu yazılması gerekir:

- **Play Mode çalıştırılmaz**, oyuncu sürülmez, oyun içi ekran görüntüsü alınmaz — proje sahibi *o iş için* açıkça
  istemedikçe. Doğrulama editörde kamera → RenderTexture önizlemesiyle yapılır (Overlay arayüzü bu görüntüde
  **çıkmaz**). Kadraj seçenekleri bu yolla, arayüzsüz gösterilebilir; arayüzlü görüntü için açık izin gerekir.
- Oyun içi her kabul kriteri proje sahibi tarafından test edilir; raporda "kullanıcı testi bekliyor" yazılır.
- **Onay penceresi açan editör komutu otomasyonu kilitler.** Kitin "Kur" komutu `DisplayDialog`,
  `SaveCurrentModifiedScenesIfUserWantsTo` ve `NewScene` çağırıyor. Pencere açmayan bir giriş noktası (ör.
  `CNEUIKitBuilder.BuildSilent()`: prefab + font üretir, sahne açmaz, soru sormaz) gerekir; TMP Essential Resources
  da aynı şekilde sessiz içe alınabilmeli.
- Sahne ve prefab YAML'ı elle düzenlenmez; değişiklik editör üzerinden yapılır. Sahne kaydındaki sıralama / layout
  gürültüsü commit'lenmez.
- Paket, mimari ve GDD'de olmayan tasarım kararları için durulur ve sorulur. Push yalnızca istenince yapılır.
- Geri bildirimi olmayan mekanik yazılmaz; yeni metinler lokalizasyona proje sahibi isteyince eklenir.

---

## 12. Projeye şimdiden alınanlar

Yalnızca GİRİŞ / ÇIKIŞ tabelalarını kitin stiline almak için, paketteki GUID'leriyle:
`Assets/CNE_UI/Fonts/Righteous-Regular.ttf` (+ lisans) ve `Assets/CNE_UI/Art/Kit/ui_arrow.png`.
Tabelalar: Righteous, ok görseli, marul yeşili `#74BF4A` (giriş) ve peynir turuncusu `#F28C1E` (çıkış), zemin
`#2C3237`. Kitin yeni sürümü aynı yollara ve GUID'lere yazarsa üstüne oturur.

---

## 13. Revizyonda cevaplanması gereken sorular

1. **Unity sürümü:** kit 6000.5.7f1'i hedeflemeli (ya da projenin 6000.7'ye taşınması ayrıca kararlaştırılmalı).
2. **Ayarlar:** kit kendi deposunu mu kullanacak, yoksa `GameSettings` + `LookPreference`'a mı bağlanacak?
3. **Dil:** kitin CSV sistemi mi, Unity Localization mı? Diğeri nasıl taşınacak / sarılacak?
4. **Müzik:** `CNEMusicPlayer` mı, mevcut `MusicPlayer` mı?
5. **Ana menü:** ayrı sahne mi, mevcut sahnenin ilk ekranı mı? Ayrı sahneyse açılış ve lobiye geçiş nasıl olacak?
6. **Menü olayları:** yeni menü mevcut lobi akışına (§8) bağlanacak mı? Bağlanmazsa eski menü ne olacak?
7. **Geçiş sırası:** kit yalnızca ana menü + ayarları kapsıyor; lobi pencereleri, rol seçimi, ESC menüsü, HUD,
   çarklar ve kitapçık ne zaman ve hangi sırayla taşınacak? Arada iki tema yan yana duracak.
8. **TextMeshPro:** kabul ediliyorsa TMP Essential Resources projeye eklenecek; mevcut `Text` tabanlı ekranlar
   taşınana kadar iki yazı sistemi birlikte yaşayacak.
9. **Gece menüsü ışığı:** menü ayrı sahne değilse gece ışığı, post-process ve kapalı dükkân düzeni oyun sahnesinin
   durumuyla nasıl ayrışacak (round başlayınca gündüz düzenine dönüş)?
10. **Kur komutu:** pencere açmayan bir kurulum yolu eklenecek mi?
