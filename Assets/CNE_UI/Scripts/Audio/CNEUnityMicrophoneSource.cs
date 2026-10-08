using System;
using UnityEngine;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Unity Microphone API'siyle mikrofon seviyesi. Varsayilan aygiti 16 kHz'de dinler.
    /// Ses sohbeti Steam Voice gibi baska bir yoldan geliyorsa ikisinin ayni anda mikrofonu acmasi
    /// denenmeden kullanmayin; o durumda sohbet sisteminden okuyan bir kaynak yazin.
    /// </summary>
    [CreateAssetMenu(menuName = "Cook No Evil/UI/Mikrofon Seviyesi (Unity Microphone)", fileName = "CNE_UnityMicrophone")]
    public sealed class CNEUnityMicrophoneSource : CNEMicLevelSource
    {
        const int SampleRate = 16000;
        const int Window = 512;

        [NonSerialized] AudioClip clip;
        [NonSerialized] float[] buffer;
        [NonSerialized] bool running;

        public override bool IsAvailable
        {
            get { return Microphone.devices.Length > 0; }
        }

        public override bool Begin()
        {
            if (running) return true;
            if (!IsAvailable) return false;
            clip = Microphone.Start(null, true, 1, SampleRate);
            running = clip != null;
            return running;
        }

        public override void End()
        {
            if (running) Microphone.End(null);
            running = false;
            clip = null;
        }

        public override float ReadRms()
        {
            if (!running || clip == null || clip.samples <= Window) return 0f;
            if (buffer == null) buffer = new float[Window];
            int pos = Microphone.GetPosition(null) - Window;
            if (pos < 0) pos = 0;
            if (pos > clip.samples - Window) pos = clip.samples - Window;
            clip.GetData(buffer, pos);
            float sum = 0f;
            for (int i = 0; i < Window; i++) sum += buffer[i] * buffer[i];
            return Mathf.Sqrt(sum / Window);
        }
    }
}
