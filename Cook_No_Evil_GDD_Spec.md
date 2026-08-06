# OYUN PROJESİ ÖLÇEKLEME VE BAŞLATMA ŞABLONU
### (Asimetrik Co-op, Steam SDR & GDD Edition — Rev. 5, Risk Düzeltmeli)

Bu belge, Unity 6000.5.7f1 tabanlı, Steam SDR kullanan ve 3 farklı rol (Kör, Sağır, Dilsiz) etrafında şekillenen **Cook No Evil!** projesinin temel yapı taşlarını ve GDD (Game Design Document) mekaniklerini belirlemek ve Claude Code'u projeyi sıfırdan kurması için yönlendirmek amacıyla hazırlanmıştır.

> **REVİZYON NOTU (Bu sürümde eklendi)**
> İnceleme sonucunda: (1) Facepunch–NGO arasındaki eksik transport paketi netleştirildi (Bölüm 2.1), (2) Kör oyuncunun pişme durumunu ayırt edememesinin bir network/veri gizleme meselesi değil, render (post-process + rol-bazlı UI + duman culling) meselesi olduğu netleştirildi (Bölüm 3/4), (3) transport geçiş zamanlaması için uygulama notu eklendi (Bölüm 2.1), (4) Claude Code'un mimari/yapısal kararlarda kullanıcıya soracağı, ama isimlendirme gibi önemsiz konularda ilerlemeyi durdurmayacağı kapsamı sınırlandırılmış karar alma protokolü eklendi (Bölüm A).

---

## BÖLÜM A: YAPAY ZEKA İÇİN ANA KOMUT (PROMPT) TASLAĞI

Sen uzman bir Senior Unity Multiplayer Geliştiricisisin. Unity 6000.5.7f1 motorunu kullanarak, "Cook No Evil!" adında, 3 oyunculu asimetrik co-op bir yemek pişirme oyunu oluşturacağız. Hiçbir kod yazmaya başlamadan önce, projenin "Yapay Zeka Hafızasını", "Network Mimarisini", "Güvenlik Kurallarını" ve "GDD (Oyun Tasarım Dokümanı) Temellerini" çıkarman gerekiyor. Claude Code ile çalışırken aşağıdaki adımları sırasıyla uygula. Yazacağın tüm kodları SOLID prensiplerine uygun olacak şekilde yaz.

> ### 🛑 ZORUNLU KARAR ALMA PROTOKOLÜ (Kapsamı Sınırlandırılmış)
>
> Claude Code, aşağıdaki KAPSAMA giren bir konuda iki veya daha fazla makul seçenek arasında karar vermesi gerektiğinde kendi başına seçim YAPMAYACAK; seçenekleri kısaca özetleyip kullanıcıya soracak ve açık onay aldıktan sonra ilerleyecektir:
>
> **Kapsam İçi (mutlaka sor):** paket/kütüphane seçimi veya sürümü (örn. transport paketi uyumsuzluğunda alternatif seçimi), ana mimari yaklaşım (örn. bir sistemin nasıl senkronize edileceği, hangi network deseninin kullanılacağı), bu belgede tanımlanmamış yeni bir oyun mekaniği veya kural, GDD'de belirtilenle çelişen ya da GDD'de hiç yer almayan bir tasarım kararı, ve geri dönüşü zor/maliyetli olan (çokça kod yazıldıktan sonra değiştirilmesi pahalı olacak) her türlü yapısal seçim.
>
> **Kapsam Dışı (kendi mühendislik muhakemesini kullanabilir, sormasına gerek yok):** değişken/fonksiyon/sınıf isimlendirmesi, kod formatlama ve stil tercihleri, dosya/klasör içi küçük organizasyon detayları, yorum satırı yazma şekli, ve GDD'de zaten net şekilde tanımlanmış bir kararın uygulanma detayı.
>
> Belirsizlik durumunda hangi kategoriye girdiğinden emin değilse, Claude Code yine de sormayı tercih edecektir — ama kapsam dışı, tersine çevrilebilir ve önemsiz konularda ilerlemeyi durdurmayacaktır.

---

## BÖLÜM 1: PROJE BİLGİLERİ VE TEKNOLOJİ YIĞINI

- **Oyun Adı:** Cook No Evil!
- **Oyun Türü:** 3-Player Asymmetric Co-op Party / Cooking
- **Kamera Açısı:** First-Person (FPS - Rol'e göre değişen render/UI durumları)
- **Unity Sürümü:** 6000.5.7f1 **(Tüm projede KESİNLİKLE bu sürüm baz alınacaktır.)**
- **Teknoloji Yığını:**
  - Networking: Netcode for GameObjects (NGO)
  - Steam Entegrasyonu: Facepunch.Steamworks (Kesin tercih) **+ com.community.netcode.transport.facepunch köprü paketi** *(bkz. Bölüm 2.1 — Risk Düzeltmesi 1)*
  - Transport: Steam Datagram Relay (SDR) Transport. (Not: Local testler için Unity Transport / UDP fallback mekanizması kurulmalıdır.)
  - Ses Sistemi: Facepunch.Steamworks Voice. (Not: Seste asimetrik filtreleme için gelen Steam ses paketleri Unity'nin AudioSource bileşenine aktarılacaktır. Kör Şef için AudioSource üzerinden Hyper-Spatial Audio ayarlanacak, Sağır Yamak için Audio Mixer/Low-Pass filter kullanılacak, Dilsiz Kasiyer'in mikrofon girdisi Host tarafından Mute'lanacaktır.)
  - Render Pipeline: URP (Kör oyuncu için durum körlüğü - State Blindness, Sağır için UI vurguları.)
  - Input: Unity Input System (New).

---

## BÖLÜM 2: AI HAFIZASI VE KURALLAR (CLAUDE_CONTEXT.md)

**Adım 1:** Proje kök dizininde `Cook_No_Evil_hafiza/` klasörünü ve `docs/` klasörünü oluştur.

**Adım 2:** Claude Code'un okuyacağı `CLAUDE.md` dosyasını oluştur. İçeriğinde şunlar mutlaka olsun:

### 2.1 Network Mimarisi ve Test Edilebilirlik

- Oyun Dedicated Server kullanmaz. Steam lobileri üzerinden bir oyuncu Host olur. Bağlantılar Steam Datagram Relay (SDR) üzerinden P2P sağlanır.
- **Server-Authoritative:** Host olan oyuncu aynı zamanda Server'dır. Tüm oyun mantığı (pişme süresi, puanlama) Host'ta hesaplanır.
- **Host Disconnect Senaryosu:** Host oyundan çıkarsa veya bağlantısı koparsa, host migration (göç) YOKTUR. Oyun anında sona erer ve client'lar Ana Menü'ye "Sunucu Bağlantısı Koptu" uyarısıyla döndürülür.
- **Local Test Stratejisi (Unity 6 Multiplayer Play Mode):** Geliştirme sürecinde 3 farklı asimetrik rolü tek bilgisayarda, düşük sistem kaynağı tüketerek hızlıca test edebilmek için Unity 6'nın yerleşik Multiplayer Play Mode (Çoklu Instance) aracı kullanılacaktır. Claude Code; Local Debug Mode (UDP - 127.0.0.1) altyapısını ve Network Manager yapılandırmasını, ParrelSync gibi üçüncü parti araçlara ihtiyaç duymadan, doğrudan bu yerleşik Multiplayer Play Mode ekosistemiyle kusursuz çalışacak şekilde inşa etmelidir.
- **VoIP ve Local Test Çakışması Çözümü:** Yapay zeka, VoIP sistemi için bağımlılığı tersine çevirme (Dependency Inversion) prensibini kullanarak bir arayüz (`IVoiceProvider`) yazacaktır. Production (Steam) modunda `SteamworksVoiceProvider` gerçek mikrofon verisini kullanacak; Local Debug (Mock) modunda ise `MockVoiceProvider`, ağ üzerinden ses iletmeye çalışmadan diğer oyuncuların AudioSource bileşenlerinden döngüde çalan bir "Dummy Test Sesi" (hazır bir radyo/konuşma .wav dosyası) oynatacaktır. Kasiyer'in Mute mekaniği Local Mode'da bu test sesinin susturulmasıyla simüle edilecektir.
- **Uygulama Notu — Transport Geçiş Zamanlaması:** NGO'da transport bileşeni (Steam SDR veya Local UDP), NetworkManager'a `StartHost()`/`StartClient()`/`StartServer()` çağrılmadan ÖNCE atanmış olmalıdır; ağ zaten başladıktan sonra transport'un değiştirilmesi desteklenmez. `NetworkTransportManager` wrapper'ı bu yüzden hangi transport'un kullanılacağına Start çağrısından önce karar vermeli ve NetworkManager'ın transport referansını buna göre ayarlamalıdır.

> ### ⚠ RİSK DÜZELTMESİ 1 — Transport Paketi Netleştirmesi
>
> Facepunch.Steamworks, NGO'ya doğrudan bağlanmaz. Aradaki köprüyü sağlayan paket, Unity'nin resmi "multiplayer-community-contributions" deposundaki `com.community.netcode.transport.facepunch` paketidir. Bu, topluluk tarafından bakımı yapılan bir pakettir ve gelecekteki NGO sürümleriyle uyumluluğu Unity tarafından garanti edilmez.
>
> **Claude Code'a verilecek talimat:** (1) Bu paketi açıkça bu isimle kur, (2) kurulumdan hemen sonra Unity 6000.5.7f1 + mevcut NGO sürümüyle bir smoke test (host başlat + tek client bağlan) yaparak uyumluluğu doğrula, (3) uyumsuzluk çıkarsa alternatif olarak Steamworks.NET tabanlı bir community transport'a geçme kararını kod yazmaya başlamadan ÖNCE kullanıcıya sor.

### 2.2 Asimetrik Rol Dağılımı ve GDD Temelleri

- **Kasiyer (Dilsiz - "Speak No Evil"):** Koca gözlü bir Yazar Kasa formundadır.
  - *Kısıtlama:* Mikrofonu server tarafından susturulur (Mute).
  - *Yetenek & Görev:* Müşteriden siparişi alır, müşteri sabır barını yönetir ve içecek istasyonunda içecek doldurur (Rolü pasiflikten kurtarmak için). Siparişi Yamak'a özel Emote Wheel ile anlatır. Yangın çıkarsa acil durum kapısından girip yangın tüpüyle ateşi söndürür.
- **Yamak (Sağır - "Hear No Evil"):** Kulaklık takmış Ketçap/Hardal Şişesi formundadır.
  - *Kısıtlama:* Ana oyun sesleri, pişme cızırtıları ve yangın alarmı tamamen kapatılır/boğuklaştırılır (Low-Pass Filter).
  - *Yetenek & Görev:* Tüm UI barlarını (pişme durumları, ateş ikonları) en net gören kişidir. Dilsiz'in emote'larını çözer, Şef'e (Kör) sesli komutlarla yönlendirme yapar.
- **Şef (Kör - "See No Evil"):** Gözleri bantlı Hamburger formundadır.
  - *Kısıtlama:* "Durum Körlüğü" (State Blindness). Yemeğin çiğ, pişmiş veya yanmış olduğunu gösteren renk değişimlerini veya UI barlarını göremez.
  - *Yetenek & Görev:* Sadece Yamak'ın sesli komutlarına ve abartılmış 3D Uzamsal Sese güvenir. Pişirme işlemini yapar.
  - *Dumbwaiter & Çöp Kutusu & Malzeme Yönetimi:* Oyun başında mutfakta belirli miktarda temel malzeme (örn. 3 siparişlik) hazır bulunur. Hedeflenen müşteri sayısı eldeki malzemeden fazlaysa veya bir malzeme yanarak/yanlış gelerek Şef tarafından "Çöp Kutusu"na atılıp yok edilirse, Şef diyafona basarak Kasiyer'den tekrar malzeme istemek zorundadır. Malzeme Kasiyer'den Dumbwaiter aracılığıyla tek yönlü gelir.

### 2.3 Oyun Döngüsü (Game Loop) ve Kazanma/Kaybetme (Win/Fail State)

- **Round Süresi:** Her round 5 dakikadır.
- **Hata (Strike) Sistemi:** Oyuncuların bölüm boyunca maksimum 3 hata yapma hakkı vardır. Müşterinin sabır barı tamamen dolarsa VEYA müşteriye yanlış malzeme içeren bir yemek teslim edilirse 1 hata (strike) yapılmış sayılır.
- **Kazanma Koşulu:** Süre bitmeden ve 3 hata limitini doldurmadan X adet (örn. 10) doğru siparişi müşteriye teslim etmek.
- **Kaybetme Koşulu (Fail State):** Toplam 3 hata sınırına ulaşılması VEYA yangın çıkıp 30 saniye içinde söndürülememesi (Mutfak patlar) VEYA süre bittiğinde hedef sipariş sayısına ulaşılamaması.

### 2.4 Güvenlik ve Ekip Çalışması (Mecburi Geliştirme Kuralları)

- Hiçbir API Key, token (gelecekte eklenecek Steam Web API, Analytics vb.) şifre asla C# scriptlerine hardcode yazılmayacak.
- Hassas veriler `Assets/LocalSecrets` klasöründeki dosyalardan okunacak.
- **Git Temizliği:** Claude Code, sisteme başlamadan önce `.gitignore` dosyasına `Assets/LocalSecrets/` ve `Assets/LocalSecrets.meta` ekleyecek. Eğer bu klasör zaten git geçmişine girmişse, terminalden `git rm -r --cached Assets/LocalSecrets/` komutunu çalıştırarak geçmişten temizleyecektir.
- Yazılan cloud/secret scriptleri ayar dosyasını bulamazsa asla `NullReferenceException` fırlatmayacak, konsola uyarı basıp oyunu normal akışında devam ettirecektir.

---

## BÖLÜM 3: MİMARİ DOSYA YAPISI

### Bileşen 1: Steam Network, Lobby & VoIP

- `NetworkTransportManager`: Steam SDR ve Local UDP arasında geçiş yapan wrapper. *(Geçiş kararı NetworkManager Start çağrısından ÖNCE verilmelidir — bkz. Bölüm 2.1 Uygulama Notu.)*
- `SteamLobbyManager`: Lobi kurma, katılma ve host disconnect yönetimi.
- `RoleManager`: Oyuncu doğduğunda rolünü atar, UI/VFX/Ses kısıtlamalarını etkinleştirir. **(ÖNEMLİ:** Rol atama mantığı dışarıdan bir fonksiyon çağrısıyla gelecek şekilde soyutlanmalıdır - bir interface/enum arkasına gizlenmeli. Böylece ileride lobi seçim ekranı eklendiğinde RoleManager'ın kendisi değil, sadece çağıran taraf değiştirilebilir olmalıdır.)
- `VoIPController`: `IVoiceProvider` arayüzü ile çalışan, AudioSource entegreli, Rol tabanlı sesli sohbet yönetimi (Kasiyer mute, Sağır low-pass, Kör hyper-spatial).

### Bileşen 2: İletişim ve Seviye Sistemleri

- `EmoteSystem` & `IntercomSystem` & `DumbwaiterSystem`
- `GameLoopManager`: 5 dakikalık sayacı, 3 hata (strike) sistemini, skor hedefini ve Win/Fail statelerini (Server-side) yönetir.

### Bileşen 3: Yemek (Cooking) ve Olay (Event) Sistemleri

- `CookingStateMachine`: Çiğ -> Az Pişmiş -> İyi Pişmiş -> Yanıyor -> Yandı. **Etin 3D modeli/materyali normal şekilde senkronize edilir (sıradan NetworkVariable yeterlidir); Şef'in bu durumu ayırt edememesi bir render/görsel meselesidir, veri gizleme değil** — bkz. Risk Düzeltmesi 2 aşağıda.
- `FireEventSystem`: Yanan yemekten tetiklenir. 30 saniye içinde söndürülmezse GameLoopManager'a Game Over sinyali gönderir. **Duman VFX'i (particle system), Şef'in (Kör) kamerasında KESİNLİKLE render edilmeyecek şekilde kurulmalıdır** (o kameraya özel bir culling mask/layer exclusion ile) — desatürasyon filtresi dumanı gizlemek için yeterli değildir, bkz. Risk Düzeltmesi 2 aşağıda.

> ### ✅ RİSK DÜZELTMESİ 2 — Render/VFX Meselesi, Network Meselesi Değil
>
> Önceki sürümde "pişme durumu verisi Şef'e hiç ulaşmamalı" şeklinde bir gereksinim tanımlanmıştı. Bu yanlıştı: etin modeli sahnedeki gerçek, paylaşılan bir obje olduğu için zaten tüm client'lara (Şef dahil) normal şekilde senkronize edilmek ZORUNDADIR. Şef'in bu bilgiyi "alamaması" iki ayrı, tamamen client-taraflı render kararından kaynaklanır:
>
> **1) Model/renk ayırt edilemezliği:** Şef'in kamerasına özel bir URP post-process Volume (desatürasyon/siyah-beyazımsı filtre) uygulanır. Et modeli teknik olarak normal renginde durur, Şef sadece bu görsel filtre yüzünden çiğ/pişmiş/yanık farkını ayırt edemez.
>
> **2) UI süre barı:** Pişme durumunu gösteren HUD elemanı sadece Yamak'ın Canvas'ında instantiate edilir; Şef'in arayüzünde bu eleman hiç oluşturulmaz (basit bir rol kontrolü — "if role == Şef, bu UI'ı hiç gösterme").
>
> **3) Duman (Smoke VFX):** Desatürasyon filtresi rengi gizler ama dumanın kendisini (bir parçacık efekti olarak varlığını) gizlemez — gri tonlu da olsa duman görülebilir kalır ve Şef'e "burada yangın var" bilgisini görsel olarak sızdırır. Bu yüzden duman VFX'i render/görsel filtreyle değil, Şef'in kamerasına özel bir culling mask/layer exclusion ile TAMAMEN gizlenmelidir (yani duman objesi o kamera için hiç render edilmez).
>
> **Claude Code'a verilecek talimat:** CookingStateMachine durumu sıradan bir NetworkVariable ile tüm client'lara senkronize edilecek. Kısıtlama, NetworkObject görünürlük filtresi veya hedefli ClientRpc ile DEĞİL, (a) Şef'in kamerasına uygulanan bir URP Volume/post-process profili, (b) süre barı UI'ının sadece Yamak'ın HUD'unda instantiate edilmesi ve (c) duman VFX'inin Şef'in kamerası için culling mask/layer exclusion ile tamamen render dışı bırakılmasıyla sağlanacak.

---

## BÖLÜM 4: KRİTİK UYARILAR (RED LINES) ⚠️

- **[KIRMIZI ÇİZGİ 1 - Client-Side Rendering İzolasyonu]:** Durum Körlüğü kritiktir. Yanan etin dumanı, pişme barları veya malzeme isimleri Şef'in (Kör) ekranında ANLAŞILIR şekilde görünmemelidir. Alttaki veri (et modeli, durum) Server-Authoritative yönetilip normal şekilde tüm client'lara senkronize edilir; kısıtlama veri gizleme ile değil, **Şef'in kamerasına özel URP post-process (desatürasyon) filtresi, süre barı UI'ının sadece Yamak'ın HUD'unda oluşturulması VE duman VFX'inin Şef'in kamerası için culling mask/layer exclusion ile tamamen render dışı bırakılmasıyla sağlanır.**
- **[KIRMIZI ÇİZGİ 2 - Ses Tasarımı ve Filtreleme]:** Sağır oyuncunun Audio Mixer'ına kesinlikle Low-Pass Filter eklenmeli; Kör oyuncunun etkileşimleri ve VoIP'si AudioSource üzerinden Hyper-Spatial Audio olarak abartılı ayarlanmalıdır.
- **[KIRMIZI ÇİZGİ 3 - Steam Transport]:** Standart UDP değil, Facepunch.Steamworks + **com.community.netcode.transport.facepunch köprü paketi** ile Steam Transport (SDR) kullanılacak. Sadece Unity Editor içindeki testler için (Multiplayer Play Mode) Local Transport Fallback yazılacaktır.

---

## BÖLÜM B: BAŞLATMA KOMUTU

*(Claude Code'a verilecek nihai komut):*

> "Sen Senior Unity Multiplayer Geliştiricisisin. Unity 6000.5.7f1 projemizi yukarıdaki Cook No Evil! GDD kurallarına, Steam SDR mimarisine, oyun döngüsüne (Game Loop) ve Asimetrik Co-op yapısına göre kur. Paket/kütüphane seçimi, ana mimari yaklaşım, GDD'de tanımlanmamış bir mekanik veya geri dönüşü zor bir yapısal karar gerektiğinde kendi başına seçim yapma — seçenekleri özetleyip bana sor ve onay bekle; isimlendirme veya kod stili gibi önemsiz, tersine çevrilebilir konularda bu şart aranmaz. İlk olarak CLAUDE.md dosyasını, ardından Facepunch.Steamworks + com.community.netcode.transport.facepunch köprü paketi, NGO ve LocalSecrets entegrasyonu (Git history temizliği dahil) için gerekli klasör yapısını (Assets/Scripts/Network, Assets/Scripts/Roles, Assets/Scripts/Systems, Assets/Scripts/Core) oluştur. NetworkTransportManager'da transport seçimini NetworkManager Start çağrısından önce yap. CookingStateMachine durumunu sıradan bir NetworkVariable ile tüm client'lara senkronize et; Şef'in durumu ayırt edememesini Şef'in kamerasına özel bir URP post-process (desatürasyon) profili ve süre barı UI'ının sadece Yamak'ın HUD'unda oluşturulmasıyla sağla; duman VFX'ini ise Şef'in kamerası için culling mask/layer exclusion ile tamamen render dışı bırak. Son olarak Unity 6 Multiplayer Play Mode destekli Local Test ortamını (IVoiceProvider/MockVoiceProvider dahil) yapılandır."
