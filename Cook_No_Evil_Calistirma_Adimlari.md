# Cook No Evil! — Claude Code Çalıştırma Adımları

Bu dosya, `Cook_No_Evil_GDD_Spec.md` ile birlikte proje klasörünüzde dursun. Her aşamayı SIRAYLA, AYNI Code sohbet penceresinde, bir öncekinin tamamlanmasını bekleyerek uygulayın.

---

## Aşama 0 — Kurulum (Bir Kere Yapılır)

1. Unity 6000.5.7f1 ile boş bir proje oluşturun.
2. `Cook_No_Evil_GDD_Spec.md` dosyasını proje kök klasörüne (Assets'in yanına) koyun.
3. Claude Desktop → **Code** sekmesi → bu proje klasörünü açın.
4. Sohbet kutusuna `/mcp` yazıp Enter'a basın → unityMCP'nin "connected" göründüğünü doğrulayın.

---

## Aşama 1 — İskelet ve Uyumluluk Testi

**Yapıştırılacak mesaj:**
> Cook_No_Evil_GDD_Spec.md dosyasını oku. Bölüm 2 Adım 1-2'ye göre klasör yapısını ve CLAUDE.md dosyasını oluştur. Ardından Facepunch.Steamworks + com.community.netcode.transport.facepunch köprü paketini kur ve Risk Düzeltmesi 1'de tarif edilen smoke testi (host başlat + tek client bağlan) yap. Başka hiçbir oyun kodu yazma, sadece bunu doğrula.

**Bekleyin, kontrol edin:** Smoke test geçti mi? Geçtiyse devam edin. Geçmediyse Claude Code zaten Zorunlu Karar Alma Protokolü gereği size soracaktır — B planını (Steamworks.NET alternatifi) onaylayın veya reddedin.

**Commit:**
> Bu aşamayı git'e commit'le.

---

## Aşama 2 — Bileşen 1: Network, Lobby, VoIP

**Yapıştırılacak mesaj:**
> Bileşen 1'i kur: NetworkTransportManager, SteamLobbyManager, RoleManager, VoIPController (IVoiceProvider/SteamworksVoiceProvider/MockVoiceProvider dahil). Transport seçimini NetworkManager Start çağrısından önce yapmayı unutma.

**Test edin:** Unity Editor'de Multiplayer Play Mode ile 3 instance açıp lobiye bağlanabiliyor musunuz?

**Commit:**
> Bu aşamayı git'e commit'le.

---

## Aşama 3 — Bileşen 2: İletişim ve Oyun Döngüsü

**Yapıştırılacak mesaj:**
> Bileşen 2'yi kur: EmoteSystem, IntercomSystem, DumbwaiterSystem, GameLoopManager (5 dakika sayaç, 3 hata/strike sistemi, win/fail state).

**Test edin:** Sayaç çalışıyor mu, strike sistemi tetikleniyor mu?

**Commit:**
> Bu aşamayı git'e commit'le.

---

## Aşama 4 — Bileşen 3: Yemek ve Olay Sistemleri (En Riskli Kısım)

**Yapıştırılacak mesaj:**
> Bileşen 3'ü kur: CookingStateMachine ve FireEventSystem. Risk Düzeltmesi 2'deki üç parçalı çözümü uygula: (a) Şef'in kamerasına URP post-process desatürasyon profili, (b) süre barı UI'ını sadece Yamak'ın HUD'unda oluştur, (c) duman VFX'ini Şef'in kamerası için culling mask ile tamamen render dışı bırak.

**Test edin:** Multiplayer Play Mode'da Şef gerçekten rengi/dumanı görmüyor mu, Yamak süre barını görüyor mu?

**Commit:**
> Bu aşamayı git'e commit'le.

---

## Genel Kurallar (Her Aşamada Geçerli)

- Claude Code bir konuda "hangisini istersiniz" diye sorarsa, acele etmeden düşünüp cevap verin — bu Zorunlu Karar Alma Protokolü'nün çalıştığının işareti, hata değil.
- Bir aşama beklenmedik şekilde bozulursa: `son commit'e geri dön` diyerek önceki çalışan hale dönebilirsiniz.
- Aynı sohbet penceresinde kalın — yeni sohbet açmayın.
