using System;
using System.IO;
using Steamworks;
using UnityEngine;

// Production ses saglayicisi: gercek mikrofon girisini Facepunch.Steamworks Voice API
// uzerinden yakalar ve agdan gelen sikistirilmis paketleri cozer.
public class SteamworksVoiceProvider : IVoiceProvider
{
    private readonly MemoryStream _decompressStream = new(1024 * 16);
    private bool _localMuted;

    public bool ShouldTransmitLocalVoice => true;

    public void Initialize()
    {
        SteamUser.VoiceRecord = true;

        // DecompressVoice, cikisi SteamUser.SampleRate'e gore uretir (varsayilan 48000).
        // Bunu cikis cihazinin gercek AudioSettings.outputSampleRate'iyle eslemezsek,
        // OnAudioFilterRead'e enjekte edilen orneklerin hizi cihazin oynatma hiziyla
        // uyusmuyor — bu da "helyum" perde kaymasina ve arabellek alt-akisindan
        // (underrun) kaynakli cizirtiya yol aciyor.
        SyncDecodeSampleRateToOutput();
    }

    private static void SyncDecodeSampleRateToOutput()
    {
        int outputRate = AudioSettings.outputSampleRate;
        uint clamped = (uint)Mathf.Clamp(outputRate, 11025, 48000);
        SteamUser.SampleRate = clamped;
    }

    public void Shutdown()
    {
        SteamUser.VoiceRecord = false;
    }

    public void Tick()
    {
    }

    public void SetLocalCaptureMuted(bool muted)
    {
        _localMuted = muted;
        SteamUser.VoiceRecord = !muted;
    }

    public bool TryReadLocalVoicePacket(out byte[] packet)
    {
        packet = null;

        if (_localMuted || !SteamUser.HasVoiceData)
            return false;

        var data = SteamUser.ReadVoiceDataBytes();
        if (data == null || data.Length == 0)
            return false;

        packet = data;
        return true;
    }

    public void ConfigureRemoteSpeaker(GameObject speakerObject, AudioSource speakerSource)
    {
        // Production'da ses agdan gelen paketler cozulup enjekte edilir; ekstra kurulum gerekmez.
    }

    public void DecompressAndEnqueue(AudioSource speakerSource, byte[] compressedPacket)
    {
        _decompressStream.SetLength(0);
        int written = SteamUser.DecompressVoice(compressedPacket, _decompressStream);
        if (written <= 0)
            return;

        var bytes = _decompressStream.GetBuffer();
        int sampleCount = written / sizeof(short);
        var samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
            samples[i] = BitConverter.ToInt16(bytes, i * sizeof(short)) / 32768f;

        var player = speakerSource.GetComponent<VoiceStreamPlayer>();
        player?.Enqueue(samples);
    }
}
