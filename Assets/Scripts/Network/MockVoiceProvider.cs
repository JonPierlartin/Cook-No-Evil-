using UnityEngine;

// Local Debug (Mock) ses saglayicisi: agdan hicbir ses paketi iletmez. Bunun yerine
// her uzak konusmacinin AudioSource'unda uretilmis bir test tonunu donguyle calar
// (GDD 2.1: "Dummy Test Sesi"). Gercek bir kayit eklenmek istenirse ConfigureRemoteSpeaker
// icindeki klip, Resources'tan yuklenen bir AudioClip ile degistirilebilir.
public class MockVoiceProvider : IVoiceProvider
{
    private AudioClip _dummyClip;

    public bool ShouldTransmitLocalVoice => false;

    public void Initialize()
    {
        _dummyClip = CreateDummyToneClip();
    }

    public void Shutdown()
    {
    }

    public void Tick()
    {
    }

    public void SetLocalCaptureMuted(bool muted)
    {
        // Mock modda yakalama olmadigi icin mute'un islevsel bir etkisi yok.
    }

    public bool TryReadLocalVoicePacket(out byte[] packet)
    {
        packet = null;
        return false;
    }

    public void ConfigureRemoteSpeaker(GameObject speakerObject, AudioSource speakerSource)
    {
        speakerSource.clip = _dummyClip;
        speakerSource.loop = true;
        speakerSource.Play();
    }

    public void DecompressAndEnqueue(AudioSource speakerSource, byte[] compressedPacket)
    {
        // Mock modda ag uzerinden ses paketi gelmez; bu metod hic cagrilmaz.
    }

    private static AudioClip CreateDummyToneClip()
    {
        const int sampleRate = 44100;
        const float duration = 2f;
        const float frequency = 220f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);

        var clip = AudioClip.Create("DummyTestVoiceLoop", sampleCount, 1, sampleRate, false);
        var data = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            // Konusma ritmi hissi vermesi icin genligi yavasca module ediyoruz.
            float envelope = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 2f * t);
            data[i] = envelope * 0.2f * Mathf.Sin(2f * Mathf.PI * frequency * t);
        }

        clip.SetData(data, 0);
        return clip;
    }
}
