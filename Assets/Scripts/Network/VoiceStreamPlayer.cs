using UnityEngine;

// Agdan/mock kaynaktan gelen PCM ornekleri icin kucuk bir dairesel arabellek.
// OnAudioFilterRead'in tetiklenmesi icin AudioSource'un "playing" durumda olmasi
// gerektiginden, sessiz ve donguleyen bir tasiyici klip atanip oynatilir; asil ses
// bu filtre uzerinden enjekte edilir.
[RequireComponent(typeof(AudioSource))]
public class VoiceStreamPlayer : MonoBehaviour
{
    private const int BufferSeconds = 2;
    private const int CarrierSampleRate = 48000;

    public AudioSource Source { get; set; }

    private readonly object _lock = new();
    private float[] _ringBuffer;
    private int _writeIndex;
    private int _readIndex;
    private int _available;

    private void Awake()
    {
        if (Source == null)
            Source = GetComponent<AudioSource>();

        _ringBuffer = new float[CarrierSampleRate * BufferSeconds];

        var silentCarrier = AudioClip.Create("VoiceSilentCarrier", CarrierSampleRate, 1, CarrierSampleRate, false);
        Source.clip = silentCarrier;
        Source.loop = true;
        Source.Play();
    }

    public void Enqueue(float[] samples)
    {
        lock (_lock)
        {
            foreach (var sample in samples)
            {
                _ringBuffer[_writeIndex] = sample;
                _writeIndex = (_writeIndex + 1) % _ringBuffer.Length;

                if (_available < _ringBuffer.Length)
                {
                    _available++;
                }
                else
                {
                    // Arabellek dolduysa en eski ornegin uzerine yazildi; okuma ucunu ilerlet.
                    _readIndex = (_readIndex + 1) % _ringBuffer.Length;
                }
            }
        }
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        lock (_lock)
        {
            for (int i = 0; i < data.Length; i += channels)
            {
                float sample = 0f;
                if (_available > 0)
                {
                    sample = _ringBuffer[_readIndex];
                    _readIndex = (_readIndex + 1) % _ringBuffer.Length;
                    _available--;
                }

                for (int c = 0; c < channels; c++)
                    data[i + c] = sample;
            }
        }
    }
}
