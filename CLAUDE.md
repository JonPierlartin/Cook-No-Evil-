# Cook No Evil! — Claude Code Proje Hafızası

Bu dosya, Claude Code'un bu projede kod yazarken uyması gereken kuralları, mimari kararları ve
GDD (Game Design Document) temellerini tanımlar. `Cook_No_Evil_GDD_Spec.md` bu dosyanın kaynağıdır;
çelişki durumunda GDD dosyası esas alınır ve kullanıcıya sorulur.

## Proje Bilgileri

- **Oyun Adı:** Cook No Evil!
- **Tür:** 3 Oyunculu Asimetrik Co-op Party / Cooking
- **Kamera:** First-Person (role göre değişen render/UI durumları)
- **Unity Sürümü:** 6000.5.7f1 — projede KESİNLİKLE bu sürüm baz alınır.
- **Teknoloji Yığını:**
  - Networking: Netcode for GameObjects (NGO)
  - Steam: Facepunch.Steamworks + `com.community.netcode.transport.facepunch` köprü paketi
  - Transport: Steam Datagram Relay (SDR); local testte Unity Transport/UDP fallback
  - Ses: Facepunch.Steamworks Voice (AudioSource üzerinden asimetrik filtreleme)
  - Render Pipeline: URP
  - Input: Unity Input System (New)

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

## 2.1 Network Mimarisi ve Test Edilebilirlik

- Dedicated server YOK. Steam lobileri üzerinden bir oyuncu Host olur; bağlantılar Steam
  Datagram Relay (SDR) üzerinden P2P sağlanır.
- **Server-Authoritative:** Host aynı zamanda Server'dır. Tüm oyun mantığı (pişme süresi,
  puanlama) Host'ta hesaplanır.
- **Host Disconnect Senaryosu:** Host migration YOKTUR. Host koparsa oyun anında sona erer,
  client'lar "Sunucu Bağlantısı Koptu" uyarısıyla Ana Menü'ye döner.
- **Local Test Stratejisi:** Unity 6'nın yerleşik Multiplayer Play Mode aracı kullanılır
  (ParrelSync gibi 3. parti araçlara gerek yok). Local Debug Mode UDP (127.0.0.1) üzerinden
  çalışır.
- **VoIP/Local Test Çakışması:** `IVoiceProvider` arayüzü ile Dependency Inversion uygulanır.
  Production modda `SteamworksVoiceProvider` gerçek mikrofonu kullanır; Local Debug (Mock) modda
  `MockVoiceProvider` ağa ses göndermeden diğer oyuncuların AudioSource'larından döngüde bir
  Dummy Test Sesi çalar. Kasiyer'in Mute mekaniği Local Mode'da bu test sesinin susturulmasıyla
  simüle edilir.
- **Transport Geçiş Zamanlaması:** Transport bileşeni (Steam SDR veya Local UDP), NetworkManager'a
  `StartHost()`/`StartClient()`/`StartServer()` çağrılmadan ÖNCE atanmış olmalıdır — ağ
  başladıktan sonra transport değiştirilemez. `NetworkTransportManager` wrapper'ı hangi transport
  kullanılacağına Start çağrısından önce karar vermelidir.

### Risk Düzeltmesi 1 — Transport Paketi Netleştirmesi

Facepunch.Steamworks NGO'ya doğrudan bağlanmaz. Köprü paketi:
`com.community.netcode.transport.facepunch` (Unity "multiplayer-community-contributions"
deposundan, topluluk bakımlı, gelecekteki NGO sürümleriyle uyumluluğu garanti değildir).

**Talimat:** (1) Bu paketi açıkça bu isimle kur, (2) kurulumdan hemen sonra Unity 6000.5.7f1 +
mevcut NGO sürümüyle bir smoke test (host başlat + tek client bağlan) yaparak uyumluluğu
doğrula, (3) uyumsuzluk çıkarsa Steamworks.NET tabanlı alternatif bir community transport'a
geçme kararını kod yazmaya başlamadan ÖNCE kullanıcıya sor.

**Smoke test durumu (Aşama 1'de yapıldı — bkz. proje geçmişi):**
- Paket, NGO 2.13.1 ile derlenirken hata verdi (`main` branch HEAD'inde
  `FacepunchTransport.cs` içinde fazladan bir `#endregion`, CS1028). Git URL, bu hatadan önceki
  commit'e (`#0eda04fc2146a4f907a61de6403315bce705279e`) sabitlenerek çözüldü. Bu paket kırılırsa
  veya bu commit artık uygun değilse tekrar kontrol edilmeli.
- Host tarafı (SteamClient.Init, NetworkManager.StartHost(), FacepunchTransport relay socket
  oluşturma) tek Steam hesabıyla doğrulandı ve BAŞARILI.
- **Client tarafı (StartClient) ÇÖZÜLDÜ — doğrulandı.** Tek makinede/tek Steam hesabıyla test
  edilirken alınan `ArgumentException: Invalid Connection`, Facepunch.Steamworks'ün bilinen bir
  platform kısıtından kaynaklanıyordu (aynı SteamID ile aynı makinede relay socket'e kendine
  bağlanma desteklenmiyor, bkz. [Facepunch.Steamworks#692](https://github.com/Facepunch/Facepunch.Steamworks/issues/692)),
  paket/NGO uyumsuzluğu değildi. 3 farklı gerçek Steam hesabıyla lobiye bağlanma testinde
  client bağlantısı uçtan uca BAŞARILI şekilde doğrulandı.

## 2.2 Asimetrik Rol Dağılımı ve GDD Temelleri

- **Kasiyer (Dilsiz — "Speak No Evil"):** Yazar Kasa formunda.
  - Kısıtlama: Mikrofon server tarafından susturulur (Mute).
  - Görev: Sipariş alır, müşteri sabır barını yönetir, içecek istasyonunda içecek doldurur.
    Siparişi Yamak'a Emote Wheel ile anlatır. Yangında acil kapıdan girip tüple söndürür.
- **Yamak (Sağır — "Hear No Evil"):** Ketçap/Hardal Şişesi formunda.
  - Kısıtlama: Oyun sesleri, pişme cızırtıları, yangın alarmı tamamen kapalı/boğuk (Low-Pass
    Filter).
  - Görev: Tüm UI barlarını (pişme durumu, ateş ikonları) en net gören kişi. Dilsiz'in
    emote'larını çözer, Şef'i sesli yönlendirir.
- **Şef (Kör — "See No Evil"):** Gözleri bantlı Hamburger formunda.
  - Kısıtlama: "Durum Körlüğü" (State Blindness) — çiğ/pişmiş/yanmış renk değişimini veya UI
    barlarını göremez.
  - Görev: Sadece Yamak'ın sesli komutlarına ve abartılmış 3D Uzamsal Sese güvenerek pişirir.
  - Dumbwaiter/Çöp/Malzeme: Oyun başında sınırlı temel malzeme vardır (örn. 3 siparişlik).
    Malzeme taşarsa veya yanlış/yanmış malzeme çöpe atılırsa Şef diyafona basıp Kasiyer'den
    tekrar malzeme ister. Malzeme Kasiyer'den Dumbwaiter ile tek yönlü gelir.

## 2.3 Oyun Döngüsü ve Win/Fail State

- Round süresi: 5 dakika.
- Hata (Strike) sistemi: maksimum 3 hata. Müşteri sabır barı dolarsa VEYA yanlış malzemeli
  yemek teslim edilirse 1 strike.
- Kazanma: süre bitmeden ve 3 strike dolmadan X adet (örn. 10) doğru sipariş teslimi.
- Kaybetme: 3 strike VEYA yangın 30 saniyede söndürülemezse (mutfak patlar) VEYA süre bitiminde
  hedef sipariş sayısına ulaşılamazsa.

## 2.4 Güvenlik ve Ekip Çalışması Kuralları

- Hiçbir API Key/token/şifre C# scriptlerine hardcode edilmez.
- Hassas veriler `Assets/LocalSecrets` klasöründeki dosyalardan okunur.
- `.gitignore`'a `Assets/LocalSecrets/` ve `Assets/LocalSecrets.meta` eklenir; bu klasör git
  geçmişine girmişse `git rm -r --cached Assets/LocalSecrets/` ile temizlenir.
- Cloud/secret scriptleri ayar dosyasını bulamazsa `NullReferenceException` fırlatmaz; konsola
  uyarı basıp oyunu normal akışında devam ettirir.

## Bileşen 1 Tamamlanma Raporu

**Kurulanlar:** `NetworkTransportManager` (Steam SDR ↔ Local UDP geçişi), `SteamLobbyManager`
(lobi kurma/katılma, Steam davet overlay + Rich Presence akışı, host-disconnect/lobi-ayrılma
yönetimi), `RoleManager` (server-authoritative rol atama, bağlantı onayı, round başlatma),
`VoIPController` (`IVoiceProvider` soyutlaması ile Production/Local Mock ses akışı, rol bazlı
mute/filtre), tamamen UGUI tabanlı lobi arayüzü (`LobbyUIController`: Host/Davet/Oyunu Başlat/
Lobiden Çık butonları, "Lobi dolu"/"Bağlantı koptu" ayrımı), ve Unity Localization entegrasyonu
(`UIStrings` tablosu, tüm sabit ve dinamik UI metinleri bu sistem üzerinden).

**Gerçek testlerde bulunup düzeltilen tüm hatalar:**
- Facepunch transport köprü paketinin `main` HEAD'inde derleme hatası (CS1028) — belirli bir
  commit'e sabitlendi.
- `ClientRoleEntry` struct'ı NGO'nun kaynak-üretici serileştirmesi için `INetworkSerializeByMemcpy`
  işaretine ihtiyaç duyuyordu.
- `NetworkTransportManager`/`LobbyUIController`, singleton'lara (`NetworkManager.Singleton`,
  `SteamLobbyManager.Instance`) sıra garantisi olmayan `Awake()` içinde erişiyordu — `Start()`'a
  ertelendi.
- Canvas varsayılan olarak WorldSpace render mode'da açılıyordu (Game view'da tamamen görünmez) —
  ScreenSpaceOverlay'e sabitlendi; lobi UI elemanları üst üste biniyordu — VerticalLayoutGroup/
  ContentSizeFitter ile düzenli yerleşim sağlandı.
- `SteamMatchmaking.CreateLobbyAsync` varsayılan olarak GÖRÜNMEZ bir lobi oluşturuyordu
  (`SetFriendsOnly()` çağrılmıyordu) ve Rich Presence `connect` anahtarı hiç yayınlanmıyordu —
  ikisi de olmadan Steam Arkadaşlar listesinden davet/katılma çalışmıyordu.
- Tek bir davet kabulü hem `OnGameLobbyJoinRequested` hem `OnGameRichPresenceJoinRequested`'i
  tetikleyip `StartClient()`'ı iki kez çağırabiliyordu (bağlantı sonsuza dek "Bağlanılıyor..."da
  takılıyordu) — `_networkBusy`/`_currentLobby` senkron guard'larıyla düzeltildi.
- Ses `ServerRpc`'si varsayılan `RequireOwnership=true` yüzünden client'lardan gelen sesi
  reddediyordu — `RequireOwnership=false` yapıldı.
- `NetworkManager.Shutdown()` asenkron olduğu için lobiden ayrılıp hemen yeniden bağlanma, eski
  oturumun yarım kalmış durumuna çarpıp "Bağlandı! Bağlanılıyor..." ekranında sonsuza kadar
  takılıyordu — `WaitForNetworkShutdown` coroutine + `_networkBusy` bayrağıyla düzeltildi.
- **`GameSystems` sahne-içi kalıcı objesi** (`RoleManager`, `VoIPController`) lobiler arası hayatta
  kaldığı için: (1) `RoleManager._assignedRoles`/`IsRoundActive` bir önceki host oturumundan
  sızıyor, yeni lobide rol sayacı/round durumu sıfırlanmıyordu (`OnNetworkSpawn`'da server
  tarafından açıkça temizlendi); (2) `VoIPController._speakerPlayers` altında oluşturulan ses
  objeleri bir önceki oturumdan öksüz kalıyordu (`OnNetworkDespawn`'da yok edildi).
- `RoleManager`, ayrılan bir client'ın rol kaydını `_assignedRoles`'tan silmiyordu — kayıt
  büyüyüp yeni katılan oyuncular `joinOrderIndex` sınırının dışına çıkarak `PlayerRole.None`
  alabiliyordu (`HandleClientDisconnectedOnServer` eklendi).
- Lobi doluyken 4. kişi bağlanmaya çalışınca genel "Sunucu Bağlantısı Koptu" mesajı gösteriliyordu
  — ayrı, doğru bir "Lobi dolu" mesajı/anahtarı eklendi.
- Localization fallback zinciri `en` locale'ine işaret ediyordu ama karşılığı olan bir Locale
  asset'i (İngilizce tablo) hiç yoktu — Türkçe olmayan bir sistemde `SelectedLocale` null kalma
  riski vardı; fallback `tr`'ye çekildi (bkz. aşağıdaki Localization notu).

**Tek bilinen açık sorun:** Round başladığında "Round başladı! Rolün:" yazısının rol adını bazen
boş bıraktığı bildirildi — sadece gerçek çok-makineli (Steam Relay) testte görüldü, gerçek
2-process local-UDP testinde (normal zamanlama ve kasıtlı race senaryosu dahil) tekrar
üretilemedi. `LobbyUIController.RefreshStatusText()` artık rol adını önbellek yerine her
seferinde `RoleManager.Instance.LocalRole`'den taze okuyacak şekilde sertleştirildi ve teşhis
için `HandleRoundActiveChanged` içine bir `Debug.Log` eklendi (`[LobbyUIController] Round
baslama teshis: ...`). **Bir sonraki gerçek çok-makineli testte bu bug tekrar görülürse, ilgili
oyuncunun Player.log dosyasındaki bu satıra bakılmalı** — `LocalRole=None` ise sorun RoleManager
senkronizasyonunda, dolu ama ekranda görünmüyorsa sorun UI/Localization katmanındadır.

## Bileşen 1 Durumu (bkz. proje geçmişi)

`Assets/Scripts/Core` (TransportMode, PlayerRole, IRoleAssignmentStrategy,
SequentialRoleAssignmentStrategy, IVoiceProvider) ve `Assets/Scripts/Network`
(NetworkTransportManager, SteamLobbyManager, RoleManager, VoIPController,
SteamworksVoiceProvider, MockVoiceProvider, VoiceStreamPlayer) ile
`Assets/Scripts/UI/LobbyUIController` kuruldu. UI tamamen UGUI (Canvas/Button/Text);
UI Toolkit KULLANILMADI. Lobiye katılma Steam'in kendi davet overlay'i üzerinden
otomatik olur (`SteamFriends.OnGameLobbyJoinRequested`) — manuel lobi kodu girme
ekranı YOK.

- Yerelde (tek Steam hesabı, Play Mode) uçtan uca doğrulandı: `HostLobby()` →
  Steam lobisi oluşturma → `NetworkManager.StartHost()` (Facepunch transport
  üzerinden) → `RoleManager` ilk oyuncuya rol atıyor → `VoIPController`
  network-spawn oluyor → UGUI (`InviteButton` görünür oluyor, status metni
  güncelleniyor) — hepsi hatasız.
- Test sırasında iki gerçek bug bulunup düzeltildi: (1) `ClientRoleEntry`
  struct'ı NGO 2.13.1'in kaynak-üretici serileştirmesi için
  `INetworkSerializeByMemcpy` işaretine ihtiyaç duyuyordu, (2)
  `NetworkTransportManager` ve `LobbyUIController`, `NetworkManager.Singleton` /
  `SteamLobbyManager.Instance`'a `Awake()` içinde erişiyordu — sıra garantisi
  olmadığı için bu erişimler `Start()`'a ertelendi.
- **Steam'in kendi davet akışıyla GERÇEK oyuncuların katılması ÇÖZÜLDÜ — doğrulandı.**
  3 farklı gerçek Steam hesabıyla lobiye bağlanma testi BAŞARILI: client bağlantısı
  uçtan uca çalıştı.
- Yerel Multiplayer-Play-Mode-eşdeğeri test (2 ayrı process, Local UDP
  transport, `SteamLobbyManager` bypass edilip `NetworkTransportManager` +
  `NetworkManager` doğrudan tetiklenerek) BAŞARILI: host `Sef`, client
  `Yamak` rolü aldı, hata yok.
- **Gerçek 2 makine testinde 3 sorun bulundu (bkz. proje geçmişi):**
  (1) Shift+Tab overlay'i açmıyor, (2) "Arkadaş Davet Et" hiçbir şey
  yapmıyor, (3) Steam Arkadaşlar penceresinde "Oyuna Davet Et" seçeneği
  çıkmıyor. Player.log incelendiğinde `SteamAPI_Init`'in BAŞARILI olduğu
  doğrulandı (lobi oluşturma çağrısı gerçek bir lobi ID'si döndürdü) — sorun
  SteamAPI bağlantısında değil. İki gerçek kod eksikliği bulunup düzeltildi:
  `SteamMatchmaking.CreateLobbyAsync` varsayılan olarak GÖRÜNMEZ bir lobi
  oluşturuyordu (`SetFriendsOnly()` hiç çağrılmıyordu), ve Rich Presence
  `connect` anahtarı hiç yayınlanmıyordu — bu ikisi olmadan Steam Arkadaşlar
  listesinin "Oyuna Davet Et"/"Katıl" akışı çalışmaz (overlay içi davet
  akışından bağımsız bir mekanizma). `SteamLobbyManager.AdvertiseLobbyPresence`
  ile düzeltildi. **Ayrı, kod dışı bir bulgu:** test edilen makinede oyun
  doğrudan bir WinRAR geçici çıkarma klasöründen (`Rar$EXxxxx.tmp`)
  çalıştırılmıştı — Steam overlay hook'u geçici/taşınabilir klasörlerden
  çalıştırılan process'lere güvenilir şekilde inject olmuyor; bu, Shift+Tab
  ve "Oyuna Davet Et"in çalışmamasının birincil nedeni olabilir. Yeniden
  test edilirken zip önce kalıcı bir klasöre (Masaüstü/Belgeler) tam olarak
  çıkarılıp WinRAR kapatıldıktan SONRA .exe çalıştırılmalı. Ayrıca proje
  "Auto Graphics API" ile Windows'ta DX12'yi birincil API olarak seçiyor;
  DX12 + Steam overlay geçmişte bilinen uyumluluk sorunları olan bir
  kombinasyon — sorun WinRAR düzeltmesinden sonra da sürerse Player Settings'te
  DX11'i birincil API yapmak bir sonraki deneme adımı olmalı.

### steam_appid.txt (geçici — gerçek AppID alınana kadar)

Henüz kayıtlı bir Steam AppID'miz yok; `FacepunchTransport`'un varsayılanı
olan **480 (Spacewar, test için Valve'ın izin verdiği ortak AppID)**
kullanılıyor. Geliştirme build'lerini Steam üzerinden başlatmadan (örn.
arkadaşla 2 makine testi) çalıştırabilmek için derlenen `.exe` ile aynı
klasöre içeriği `480` olan bir `steam_appid.txt` konur. Bu dosya **asla** git'e
commit edilmez (`.gitignore`'da `steam_appid.txt` deseni var, ayrıca
`/Builds/` zaten tamamen yok sayılıyor). Gerçek bir Steam AppID alındığında:
(1) bu dosya silinir, (2) `FacepunchTransport` component'indeki `steamAppId`
alanı gerçek ID ile güncellenir.

### Localization: Startup Locale Selector Zinciri

`Assets/Settings/Localization Settings.asset` içindeki `m_StartupSelectors` sırası:
`CommandLineLocaleSelector` (QA için `-language=xx`, zararsız/varsayılan) →
`SystemLocaleSelector` → `SpecificLocaleSelector` (fallback, `tr`). Önceki halinde
fallback yanlışlıkla `en` idi — bu koda karşılık gelen bir Locale asset'i (İngilizce
tablo) hiç yoktu, yani Türkçe olmayan bir sistemde `SystemLocaleSelector` eşleşmezse
zincir HİÇBİR locale'e düşemiyordu. `tr` olarak düzeltildi. Sonuç: bugün tek locale
Türkçe olduğu için herkes Türkçe görür (sistem dili ne olursa olsun, çünkü
`AvailableLocales` içinde başka locale yok — `SystemLocaleSelector` eşleşmeyince
zincir Türkçe'ye düşer), ileride bir İngilizce `Locale` + String Table eklendiğinde
kod DEĞİŞMEDEN İngilizce sistemli oyuncular otomatik İngilizce görmeye başlar.

Şu an nihai fallback dili Türkçe'dir. İngilizce string table eklendiğinde, hangi dilin nihai
fallback olacağına (Türkçe mi, İngilizce mi) karar verilmeli ve Startup Locale Selectors
zinciri buna göre güncellenmeli.

### Round Başlama Rol Adı Bugu — Araştırma Notu

Kullanıcı "Round başladı! Rolün:" yazısının rol adını boş bıraktığını bildirdi.
Gerçek local-UDP 2-process test (host + ayrı bir client process, hem normal
zamanlamayla hem de client'in kendi rol ataması ile `IsRoundActive` bayrağının
AYNI ANDA yarışacağı kasıtlı bir race testiyle) ile tekrar üretilemedi — her
senaryoda rol adı doğru geldi. Yine de `LobbyUIController.RefreshStatusText()`,
rol adını olay bazlı önbelleklenen bir `_localRole` alanından okumak yerine
artık her çağrıda doğrudan `RoleManager.Instance.LocalRole`'den (NetworkList
üzerinden senkronize edilen, server-authoritative kaynak) taze okuyacak şekilde
değiştirildi — `_localRole` alanı tamamen kaldırıldı. Bu, teorik olarak mümkün
olan her türlü önbellek bayatlama senaryosunu yapısal olarak ortadan kaldırıyor.
Ayrıca `LobbyUIController.HandleRoundActiveChanged` içine teşhis amaçlı bir
`Debug.Log` eklendi (round başladığı anda `LocalRole` ve oluşan `statusText`
değerini basar). **Gerçek 3 makine testinde bug hâlâ görülürse bir sonraki
adım:** ilgili oyuncunun Player.log dosyasında `[LobbyUIController] Round
baslama teshis:` satırına bakılmalı — `LocalRole=None` ise sorun
`RoleManager`/NetworkList senkronizasyonunda (Steam Relay gecikmesi altında
`_assignedRoles`'un `IsRoundActive`'e göre gecikmesi ihtimali), değer doluyken
ekranda görünmüyorsa sorun UI/Localization katmanındadır. Teşhis netleşince bu
log kaldırılmalı.

## Player Controller Tamamlanma Raporu

Bileşen 2 (İletişim ve Seviye Sistemleri: EmoteSystem, IntercomSystem, DumbwaiterSystem,
GameLoopManager) kullanıcı tarafından onaylandı ama **kod yazılmadan beklemeye alındı** —
plan `C:\Users\ersel\.claude\plans\goofy-floating-hippo.md` dosyasında saklı. Kullanıcı
Bileşen 2'yi gerçek oynanışta test edebilmek için önce projede hiç bulunmayan bir Player
Controller kurulmasını istedi. Bu bileşen GDD'nin hiçbir Bölümünde tanımlı değil —
tamamen greenfield, ana mimari kararlar kullanıcıyla netleştirildi (bkz. plan dosyası):
client-authoritative hareket (NGO `NetworkTransform`, owner-authoritative), generic
4-slotlu `PlayerInventory` (tüm roller için tek envanter kavramı), ve bilinçli bir kapsam
genişletmesi olarak gerçek tarif doğrulamalı `BurgerAssemblyStation` (Order/Customer/NPC
sistemi KURULMADI, kapsam dışı kaldı).

**Kurulanlar:**
- `Assets/Scripts/Player/PlayerController.cs` — CharacterController ile WASD hareket (ok
  tuşları `InputSystem_Actions.inputactions`'tan kaldırıldı), mouse-look (yaw gövde/pitch
  kamera pivotu, clamp'li), sadece owner çalışır; spawn'da sahnenin statik `Main Camera`'sını
  kapatıp kendi `Player Camera`'sını devreye sokar (sahne kamerası silinmedi, sadece
  devre dışı bırakılıyor).
- `Assets/Scripts/Player/PlayerInteractor.cs` — LMB (`Attack` action) ile raycast bazlı
  hedef bulma (`Interactable` layer, slot 8) ve `HoldOrPressInteractable.BeginPress()`/
  `EndPress()`'i tetikleyen genel yönlendirici; hangi eylemin gerçekleştiğine karışmaz.
- `Assets/Scripts/Player/PlayerInventory.cs` — 4 slotluk generic envanter
  (`NetworkList<int>`, -1=boş, değer `IngredientType.Id`), server-only
  `ServerTryAddItem`/`ServerTryRemoveItem`/`ServerTryRemoveActiveItem`, owner-yazılabilir
  `ActiveSlotIndex`.
- `Assets/Scripts/Systems/HoldOrPressInteractable.cs` — headless (input-agnostic) etkileşim
  primitive'i: `BeginPress()`/`EndPress()` + `Press`/`Hold` (Inspector'dan `holdDuration`
  configli) davranışı. Unity InputSystem'in kendi "Hold" interaction'ına bağımlı değil.
- `Assets/Scripts/Systems/BurgerAssemblyStation.cs` + `Assets/Scripts/Core/BurgerRecipe.cs`
  (+ `IngredientRequirement` struct) — Şef'in masasında sıralı malzeme yerleştirme: ilk
  yerleştirme kesinlikle `IngredientType.IsBread` olmalı, sonrası aktif tarifin (varyasyon/
  hariç-tutma listesi dahil) izin verdiği malzemelerle sınırlı (sıra kuralı yok). Tarif
  tamamlanınca `PlacedIngredients` otomatik sıfırlanır (test edilebilirlik için, kullanıcı
  isteği — müşteri/sipariş sistemi gerektirmez). `activeRecipe` test için Inspector'dan
  sabit seçilir, gerçek bir sipariş kaynağına bağlı değil.
- `Assets/Scripts/Systems/EmoteSystem.cs` + `Assets/Scripts/UI/EmoteWheelUI.cs` — aslında
  Bileşen 2 kapsamındaydı, `EmoteWheelUI`'nin E-basılı-tutma çarkı çalışabilsin diye bu
  görevde inşa edildi (Bileşen 2'nin geri kalanı hâlâ parked). Sadece Kasiyer'de aktif;
  E (`Interact` action, ham started/canceled okunuyor, `HoldOrPressInteractable`'dan
  BAĞIMSIZ) basılı tutulunca çark açılır, mouse pozisyonu dilim seçer, bırakınca
  `EmoteSystem.SelectEmoteServerRpc` çağrılır ve SADECE Yamak'a hedefli `ClientRpc` ile
  iletilir (bu, Red Line 1'in veri-gizleme yasağını ihlal etmez — o kural cooking-state'e
  özgü, bu sadece rol-bazlı bir mesajlaşma eylemi).
- `Assets/Scripts/Network/PlayerSpawner.cs` — `RoleManager`'a eklenen yeni
  `OnServerRoleAssigned` event'ini dinler (rol ataması KESİNLİKLE tamamlandıktan sonra
  tetiklenir), `Player.prefab`'ı `Instantiate` + `SpawnAsPlayerObject` ile spawn eder
  (`RoleManager.HandleConnectionApproval` hâlâ `CreatePlayerObject=false` — NGO'nun otomatik
  spawn'i KULLANILMIYOR, rol bazlı spawn konumu seçilebilsin diye).
- `Assets/Scripts/UI/HotbarUI.cs` — her zaman görünür 4-slotluk hotbar, local
  `PlayerInventory`'yi spawn olduktan sonra lazy-resolve eder, 1-4 tuşlarıyla slot seçimi
  (`InputSystem_Actions.inputactions`'a `HotbarSlot1..4` action'ları eklendi).
- `Assets/Prefabs/Player.prefab` — ilk ve tek prefab (proje daha önce hiç prefab
  içermiyordu). NGO'nun "Default Network Prefabs" otomatik-kayıt özelliği sayesinde
  `Assets/DefaultNetworkPrefabs.asset`'e otomatik eklendi, elle kayıt gerekmedi.
- `GameSystems` sahne objesine `PlayerSpawner` ve `EmoteSystem` eklendi (RoleManager/
  VoIPController ile aynı kalıcı obje deseni). Yeni `GameplayCanvas` (Hotbar + Emote çarkı)
  `LobbyCanvas`'tan ayrı tutuldu.

**RoleManager.cs değişikliği (Bileşen 1'e dokunan tek dosya):** sadece ekleme —
`public event Action<ulong, PlayerRole> OnServerRoleAssigned` eklendi,
`HandleClientConnected`'ın sonunda invoke ediliyor. Mevcut davranış değişmedi. Kullanıcının
isteği üzerine bu değişiklikten sonra Bileşen 1'in hassas geçmişi olan round-start/rejoin
davranışı reflection ile simüle edilerek yeniden doğrulandı: disconnect sonrası
`_assignedRoles` doğru temizleniyor (0'a düşüyor), rejoin sonrası tam olarak 1 kayıt ile
tekrar doluyor (leak/duplikasyon yok), `OnServerRoleAssigned` tam olarak 1 kez tetikleniyor.

**Gerçek testte bulunan ve düzeltilen hatalar:**
- `manage_components` tool'u kısa isimle (`PlayerController`) tip bulamadı (muhtemelen bir
  paket içindeki başka bir `PlayerController` adıyla çakışma) — `PlayerController,
  Assembly-CSharp` şeklinde tam nitelenmiş isimle çözüldü.
- İlk `Player.prefab` oluşturma denemesi yanlışlıkla BOŞ yeni bir GameObject'i prefab olarak
  kaydetti (`manage_gameobject create` + `save_as_prefab` her zaman YENİ bir obje yaratıyor,
  var olanı prefab'a çevirmiyor) — doğru obje `manage_prefabs create_from_gameobject` ile
  (isimle hedefleyerek, instanceID ile değil — instanceID hedeflemesi bu araç setinde
  güvenilir çalışmadı) düzeltildi.
- `EmoteWheelUI`, round aktifken global imleç kilidiyle (`LobbyUIController`'ın round-start'ta
  uyguladığı `Cursor.lockState = Locked`) çakışıyordu — çark, mutlak `Mouse.position`'a göre
  dilim seçtiği için imleç kilitliyken hiçbir dilim seçilemiyordu. Düzeltme: `EmoteWheelUI`
  artık E'ye basılınca (`HandleInteractStarted`) imleci geçici olarak serbest bırakıp
  gösteriyor, E bırakılınca (`HandleInteractCanceled`) round hâlâ aktifse tekrar kilitleyip
  gizliyor. Local Editor'de reflection ile doğrulandı (kapalı→açık→kapalı durumlarında
  lock/visible state'leri beklendiği gibi değişti).
- Yine gerçek 3 kişilik testte iki ayrı hata daha bulundu:
  **(1) "Yamak'ta da E'ye basınca imleç açılıyor" — kök neden ROL SIZINTISI DEĞİLDİ.**
  `EmoteWheelUI.HandleLocalRoleAssigned`'daki abonelik-zamanlı Kasiyer kontrolü kod
  incelemesinde YETERLİ bulundu — Kasiyer olmayan bir client'ta `_interactAction` hiç
  set edilmiyor, yani `HandleInteractStarted`/`HandleInteractCanceled` teorik olarak hiç
  tetiklenemiyor. Asıl kök neden büyük olasılıkla başka bir yerdeydi: Windows/Unity,
  oyun penceresi fokusu kaybedildiğinde (alt-tab, pencere dışına tıklama) imleç kilidini/
  gizliliğini İŞLETİM SİSTEMİ SEVİYESİNDE zorla iptal eder — ama fokus GERİ geldiğinde
  bunu hiçbir kod yeniden uygulamıyordu. Bir oyuncu round sırasında pencere dışına
  tıklayıp geri dönerse imleç açık kalırdı; bunun E tuşuyla aynı ana denk gelmesi
  yanlış bir nedensellik izlenimi yaratmış olabilir. **Düzeltme:** `LobbyUIController`'a
  `OnApplicationFocus(bool hasFocus)` eklendi — fokus geri gelince round aktifse imleç
  tekrar kilitlenip gizleniyor. Local Editor'de simülasyonla doğrulandı: kilitli→(OS
  zorla açar)→fokus geri gelince tekrar kilitli/gizli. Buna EK olarak, kod incelemesinde
  kesin bir sızıntı kanıtlanamamış olsa da savunma amaçlı bir sağlamlaştırma da eklendi:
  `EmoteWheelUI.HandleInteractStarted`/`HandleInteractCanceled` artık abonelik-zamanlı
  önbelleğe güvenmek yerine GÜNCEL `RoleManager.Instance.LocalRole`'ü de kontrol ediyor
  (RoleManager'ın geçmiş round-başlama senkron sorunlarında kullanılan "önbelleğe
  güvenme, canlı oku" ilkesiyle tutarlı) — olası bir rol-senkron zamanlama farkına karşı
  ek bir güvenlik katmanı, ama TEK BAŞINA kanıtlanmış bir kök-neden düzeltmesi değildir.
  **(2)** Çarkta mouse'un üzerinde durduğu değil YANINDAKİ dilim parlıyordu — dilim
  ikonları carkta 90° (yukarı) merkezli yerleştirilmişti ama açı→index hesaplaması 0°'yi
  (sağ) merkez kabul ediyordu; açı hesaplamasına aynı 90° kaydırma eklenerek düzeltildi.
  Local Editor'de doğrulandı (rol-kontrolü: Sef iken `HandleInteractStarted` artık
  imleci hiç etkilemiyor; odak testi: yukarıda; açı düzeltmesi: 90/210/330° test açıları
  artık doğru slot
  index'ine (0/1/2) eşleniyor).

**Test ortamı notu (kod hatası DEĞİL):** Tek Unity Editor içinde arka arkaya birden fazla
`StartHost()`/`Stop Play Mode` döngüsü denenirken, NGO'nun Play Mode çıkışındaki bilinen
`NetworkManager`/`NetworkObject` `OnDestroy()` sıralama hatası (upstream NGO paket sorunu,
proje kodundan bağımsız) yerel UDP soketinin (port 7777) temiz kapanmasını engelledi;
sonraki `StartHost()` denemeleri "address already in use" ile başarısız oldu. Bu SADECE
aynı Editor oturumunda ardışık manuel test döngülerinde görülür — normal tek seferlik
Play Mode kullanımını etkilemez. Diagnostik testler geçici olarak farklı bir port
(`UnityTransport.SetConnectionData` ile 7778/7779) kullanılarak tamamlandı; kalıcı bir kod
değişikliği yapılmadı.

**Bilinçli, kod dışı bir boşluk (unutulmamalı):** `HoldOrPressInteractable` kasıtlı olarak
headless/input-agnostic kuruldu (`BeginPress()`/`EndPress()`). `PlayerInteractor` bunu
raycast ile tetikliyor — yani "oyuncu bir objeye yaklaşınca/bakınca otomatik vurgula" gibi
bir prompt/highlight UI'ı YOK, sadece ham etkileşim çalışıyor. Ayrıca gerçek seviye
geometrisi (odalar, Kasiyer/Yamak/Şef istasyonlarının 3D yerleşimi) hiç yok — `BurgerAssemblyStation`
sahnede geçici bir Cube olarak duruyor. Bunlar bu görevin bilinçli kapsam dışı bıraktığı
konular, GDD'nin hiçbir Bölümü de bunların sahibi değil.

**Basit gri-kutu test seviyesi eklendi (final tasarım DEĞİL):** `TestLevel` sahne objesi
altında `Floor` (20x20 Plane) ve 4 duvar (`Wall_North/South/East/West`, Cube) — sadece
hareket/etkileşim gerçek zeminde test edilebilsin diye. `BurgerAssemblyStation` geçici
`(3,1,0)` konumundan odanın içinde zemine oturan `(7, 0.5, 4)` konumuna taşındı. Oda
`Interactable` layer'ı KULLANMIYOR (sadece istasyon objeleri bu layer'da olmalı, duvar/zemin
değil). Oyuncu spawn noktaları (`PlayerSpawner`, X ekseninde rol×2 ofseti: Kasiyer=2,
Yamak=4, Sef=6) oda sınırları (-10..10) içinde kalıyor, Local Editor testinde `CharacterController`
zeminde doğru duruyor (düşmüyor). Gerçek oda/istasyon 3D yerleşimi hâlâ ayrı, kapsam dışı bir
görev.

**Bileşen 2 üzerinde bu turda netleşen, ileride uygulanacak iki değişiklik** (plan
dosyasında da işaretli): (1) `YamakCarryState` (3 durumlu enum) tasarımı TAMAMEN
KALDIRILDI — Yamak'ın taşıdığı öğe artık generic `PlayerInventory`'nin bir slotu olarak
modellenecek. (2) `ItemHandoffSlot` kendi üzerinde bir `HoldOrPressInteractable` (Press-type)
taşıyacak şekilde genişletilecek, böylece `PlayerInteractor` onu diğer istasyonlarla aynı
şekilde (LMB → `BeginPress`/`EndPress`) tetikleyebilecek.

### Gerçek 3 Makineli Steam Testinde Bulunan Kritik Hata — Round Başlayınca Ekran Değişmiyordu

Kullanıcı 3 gerçek oyuncuyla Steam üzerinden test etti: lobi ve VoIP hatasız çalıştı, ama
"Oyunu Başlat"a basınca hiçbir oyuncu diğerini FPS görünümünde göremedi — ekranda görünürde
hiçbir şey değişmedi. Host'un `Player.log` dosyası (`%USERPROFILE%\AppData\LocalLow\
DefaultCompany\Cook No Evil!\Player.log`) incelendiğinde: 3 rol de doğru atanmıştı
(`Client 0 -> Sef`, `Client 1 -> Yamak`, `Client 2 -> Kasiyer`), `IsRoundActive` gerçekten
`true` olmuştu (`Round baslama teshis: LocalRole=Sef` satırı basılmıştı), hiçbir exception/
hata YOKTU — yani ağ/rol/round-state katmanı sorunsuzdu. Round başladıktan hemen sonra diğer
2 oyuncunun ayrıldığı da log'da görüldü (muhtemelen "hiçbir şey olmadığını" düşünüp oyundan
çıktılar).

**Kök neden:** `LobbyUIController.HandleRoundActiveChanged`, round başladığında SADECE
`startGameButton`/`inviteButton`'ı gizliyor ve status text'i güncelliyordu — tam ekranı
kaplayan `lobbyPanel`'i (ve altındaki 3D oyun görüntüsünü/`GameplayCanvas`'ı) hiç
gizlemiyordu. Yani Player.prefab doğru spawn olup kamera doğru geçiş yapsa bile (bu da ayrıca
doğrulandı, aşağıya bkz.), oyuncular her zaman tam ekran lobi arayüzüne bakmaya devam
ediyordu — bu, mimari bir eksiklik değil, unutulmuş bir UI-geçiş adımıydı (Player Controller
görevinde `GameplayCanvas` kuruldu ama round-start'a hiç bağlanmadı).

**Düzeltme:** `HandleRoundActiveChanged` artık round aktif olunca `lobbyPanel`'i gizliyor,
`GameplayCanvas`'ı (isimle bulunuyor — `LobbyCanvas`'tan ayrı bir sahne kökü olduğu için
Inspector referansı yerine, tek seferlik bir çağrı, performans sorunu yaratmaz) açıyor ve
fare imlecini kilitleyip gizliyor (`Cursor.lockState = Locked`, FPS kontrolü için gerekli —
önceden hiç ayarlanmıyordu). `HandleHostDisconnected`'a da aynı geri-alma eklendi (host round
sırasında koparsa `GameplayCanvas`/imleç kilidi takılı kalmasın diye).

**Doğrulama:** Local Editor testinde `RoleManager.Instance.IsRoundActive.Value = true`
manuel tetiklenerek doğrulandı: `lobbyPanel.active` `True→False`, `Cursor.lockState`
`None→Locked` oldu. Ayrıca `PlayerSpawner.HandleServerRoleAssigned` ve
`PlayerController.OnNetworkSpawn`'a (owner kamerası aktifleşince) birer teşhis `Debug.Log`
eklendi — host'un `Player.log`'unda ikisi de doğru sırayla göründü
(`[PlayerSpawner] Client 0 icin Player.prefab spawn edildi (rol=Sef).` ve
`[PlayerController] Owner kamerasi aktif, sahne kamerasi kapatildi (clientId=0).`), yani
spawn/kamera geçişi zaten sorunsuzdu — sorun SADECE UI katmanındaydı. **Not:** bu doğrulama
sadece HOST'un (bu makinenin) Player.log'undan yapılabildi; diğer 2 oyuncunun makineleri bu
oturumdan erişilemez durumda, onların log'ları ayrıca kontrol edilmedi.

## Mimari Dosya Yapısı (Bölüm 3 özeti)

- **Bileşen 1 — Steam Network, Lobby & VoIP:** `NetworkTransportManager`, `SteamLobbyManager`,
  `RoleManager` (rol atama mantığı interface/enum arkasına soyutlanmış), `VoIPController`
  (`IVoiceProvider` ile).
- **Bileşen 2 — İletişim ve Seviye:** `EmoteSystem`, `IntercomSystem`, `DumbwaiterSystem`,
  `GameLoopManager` (5 dk sayaç, 3 strike, skor hedefi, Win/Fail state — server-side).
- **Bileşen 3 — Yemek ve Olay:** `CookingStateMachine` (Çiğ → Az Pişmiş → İyi Pişmiş → Yanıyor →
  Yandı; sıradan NetworkVariable ile senkronize), `FireEventSystem` (30 sn içinde
  söndürülmezse Game Over).

### Risk Düzeltmesi 2 — Render/VFX Meselesi, Network Meselesi Değil

Pişme durumu verisi Şef'e normal şekilde ulaşır (paylaşılan sahne objesi, server-authoritative
NetworkVariable). Şef'in bu bilgiyi "alamaması" tamamen client-taraflı render kararlarından
kaynaklanır — veri gizleme İLE DEĞİL:

1. **Model/renk ayırt edilemezliği:** Şef'in kamerasına özel URP post-process Volume
   (desatürasyon/siyah-beyaz filtre).
2. **UI süre barı:** Sadece Yamak'ın Canvas'ında instantiate edilir; Şef'in arayüzünde hiç
   oluşturulmaz.
3. **Duman (Smoke VFX):** Desatürasyon rengi gizler ama dumanın varlığını gizlemez — bu yüzden
   duman VFX'i Şef'in kamerası için culling mask/layer exclusion ile TAMAMEN render dışı
   bırakılır (video filtreyle değil).

## Kritik Uyarılar (Red Lines) ⚠️

1. **Client-Side Rendering İzolasyonu:** Durum Körlüğü verisi server-authoritative olarak tüm
   client'lara normal senkronize edilir; kısıtlama Şef'in kamerasına özel post-process
   (desatürasyon) + süre barı UI'ının sadece Yamak HUD'unda oluşturulması + duman VFX'inin
   Şef kamerası için culling mask ile tamamen render dışı bırakılmasıyla sağlanır. Veri gizleme
   (NetworkObject görünürlük filtresi, hedefli ClientRpc) KULLANILMAZ.
2. **Ses Tasarımı:** Sağır oyuncunun Audio Mixer'ına kesinlikle Low-Pass Filter eklenmeli; Kör
   oyuncunun etkileşimleri/VoIP'si AudioSource üzerinden Hyper-Spatial Audio olarak abartılı
   ayarlanmalı.
3. **Steam Transport:** Standart UDP değil, Facepunch.Steamworks +
   `com.community.netcode.transport.facepunch` ile Steam Transport (SDR). Sadece Unity Editor
   içi testler (Multiplayer Play Mode) için Local Transport Fallback yazılır.

## Genel Kodlama Kuralı

Yazılan tüm kodlar SOLID prensiplerine uygun olacak şekilde yazılır.

## Genel Geliştirme Disiplini

- Hiçbir script içinde over-engineering yapılmaz — ihtiyaç duyulmayan soyutlama, esneklik veya
  gelecekte-lazım-olabilir kod yazılmaz.
- Mümkün olduğunda hazır Unity Engine feature'ları kullanılır (Animator, Cinemachine, vb.) —
  bunların elle yeniden yazılmış karşılıkları üretilmez.
- Hiçbir problem için "play-around" (geçici, sorunun kök nedenini çözmeyen dolanma) yapılmaz;
  yazılan kod ölçeklenebilir, modüler ve Editor üzerinden (Inspector'dan) kullanılabilir olur.
- Veri/konfigürasyon saklamak için JSON yerine ScriptableObject kullanılır.
- Vertical Slice kapsamı GDD'de tanımlanandan fazla genişletilmez.
- **Her aşama/bileşen/görev tamamlandığında (kullanıcı ayrıca istemese bile) otomatik olarak
  bir Windows Development Build alınır** (`manage_build`: `target=StandaloneWindows64`,
  `development=true`), `Builds/CookNoEvilDevBuildN/` altına (mevcut en yüksek N'den bir
  fazlasıyla, örn. bir önceki `CookNoEvilDevBuild10` ise yeni build `CookNoEvilDevBuild11`)
  kaydedilir, yanına içeriği `480` olan bir `steam_appid.txt` konur, sonra klasör aynı
  isimle (`CookNoEvilDevBuildN.zip`) zip'lenir. `Builds/` zaten `.gitignore`'da olduğu için
  bu commit'lere karışmaz. Bu adım unutulmaya müsait olduğu için buraya kalıcı kural olarak
  yazıldı — atlanmamalı.
