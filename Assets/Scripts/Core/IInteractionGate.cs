// "Bu oyuncu bu hedefle SU AN etkilesebilir mi?" sorusunun TEK cevap noktasi (GDD 4.1.2, K6).
// Bir istasyon bileseni bunu uygular; HoldOrPressInteractable ayni nesnedeki tum gate'leri
// toplayip sorar. Ayni sorguyu iki taraf kullanir:
//  - sunucu: RequestInteractServerRpc ve istasyonun kendi tamamlanma mantigi (KARAR),
//  - istemci: crosshair, her karede yerel replike veriden (yalnizca GOSTERIM, tahmin).
// Uygulamalar hem sunucuda hem istemcide calisabilecek, yalnizca replike veriyi okuyan ve
// durum DEGISTIRMEYEN kod olmak zorundadir; her karede cagrilabildigi icin allocation yapmamalidir.
public interface IInteractionGate
{
    // reason: reddedilirse SABIT bir metin (interpolasyon yok -> allocation yok). Yalnizca
    // sunucu logu icindir; oyuncuya/UI'a ASLA gosterilmez (GDD 4.1.2: "Engelli" sebebini gostermez).
    bool CanInteract(ulong clientId, out string reason);
}
