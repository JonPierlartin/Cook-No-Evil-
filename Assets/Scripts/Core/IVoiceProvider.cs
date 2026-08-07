using UnityEngine;

// Dependency Inversion: VoIPController bu arayuzle calisir, Production'da
// SteamworksVoiceProvider gercek mikrofon/Steam Voice API'sini, Local Debug'da
// MockVoiceProvider agdan bagimsiz bir test tonu dongusunu kullanir.
public interface IVoiceProvider
{
    // Mock modda hicbir ses agdan iletilmez (dogrudan lokal donguyle simule edilir).
    bool ShouldTransmitLocalVoice { get; }

    void Initialize();
    void Shutdown();
    void Tick();

    void SetLocalCaptureMuted(bool muted);

    // Uretimde: yerel mikrofondan sikistirilmis bir ses paketi okur (yoksa false doner).
    bool TryReadLocalVoicePacket(out byte[] packet);

    // Bir uzak konusmaci icin AudioSource kurulumu (Mock: test klibini atar ve oynatir; Uretim: no-op).
    void ConfigureRemoteSpeaker(GameObject speakerObject, AudioSource speakerSource);

    // Agdan gelen sikistirilmis bir ses paketini cozup ilgili AudioSource'un
    // VoiceStreamPlayer arabellegine ekler (yalnizca Uretim modunda kullanilir).
    void DecompressAndEnqueue(AudioSource speakerSource, byte[] compressedPacket);
}
