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

    // Örneklere uygulanan çarpan (konuşanın mikrofon seviyesi × dinleyenin sesli sohbet seviyesi). Ses filtreyle
    // enjekte edildiği için AudioSource.volume buna uygulanmaz; seviye burada çarpılır. Ses iş parçacığından okunur.
    public volatile float Gain = 1f;

    // Sesin geldiği yön: −1 = tam sol, 0 = orta, +1 = tam sağ. Aynı sebeple (enjeksiyon) AudioSource'un 3B ayarına
    // güvenilmez; yön burada, kanallara eşit güçlü dağıtımla verilir. Ses iş parçacığından okunur.
    public volatile float Pan;

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
        float gain = Gain;
        // Eşit güçlü dağıtım: ortada iki kanal da 1 (mono kaynağın seviyesi değişmez), kenarda biri 0'a iner.
        float angle = (Mathf.Clamp(Pan, -1f, 1f) + 1f) * 0.25f * Mathf.PI;
        float left = Mathf.Min(1f, Mathf.Cos(angle) * 1.41421356f);
        float right = Mathf.Min(1f, Mathf.Sin(angle) * 1.41421356f);
        lock (_lock)
        {
            for (int i = 0; i < data.Length; i += channels)
            {
                float sample = 0f;
                if (_available > 0)
                {
                    sample = Mathf.Clamp(_ringBuffer[_readIndex] * gain, -1f, 1f);
                    _readIndex = (_readIndex + 1) % _ringBuffer.Length;
                    _available--;
                }

                if (channels == 2)
                {
                    data[i] = sample * left;
                    data[i + 1] = sample * right;
                }
                else
                {
                    for (int c = 0; c < channels; c++)
                        data[i + c] = sample;
                }
            }
        }
    }
}
