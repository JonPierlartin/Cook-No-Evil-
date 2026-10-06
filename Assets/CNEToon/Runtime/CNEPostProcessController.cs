using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// CNE Toon görünümü açıkken post-process'i ve outline'ı devreye sokar, kapanınca eski hâline döndürür.
// Kendini kurar (sahnede nesnesi yoktur). Görünüm kapalıyken hiçbir kameraya ve Volume'a dokunmaz.
//
// Kör görüşle çizen kameraya (Şef) dokunmaz: yalnızca URP asset'inin VARSAYILAN renderer'ını kullanan kameralara
// uygulanır; kör görüş ayrı bir renderer'dır (ve BlindVisionCamera o kamerada post-process'i kapalı tutar).
public class CNEPostProcessController : MonoBehaviour
{
    private struct CameraState
    {
        public bool PostProcessing;
        public AntialiasingMode Antialiasing;
        public AntialiasingQuality Quality;
    }

    private readonly Dictionary<UniversalAdditionalCameraData, CameraState> _original = new();
    private readonly List<UniversalAdditionalCameraData> _stale = new();

    private CNELookSettings _settings;
    private Volume _globalVolume;
    private Volume _debugVolume;
    private bool _outlineDebugOff;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var host = new GameObject(nameof(CNEPostProcessController)) { hideFlags = HideFlags.HideAndDontSave };
        DontDestroyOnLoad(host);
        host.AddComponent<CNEPostProcessController>();
    }

    private void Awake()
    {
        _settings = CNELookSettings.Load();
        if (_settings == null)
            Debug.LogWarning("[CNEPostProcessController] Resources/CNELookSettings bulunamadı; CNE Toon post-process uygulanmayacak.");

        CNELook.Changed += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        CNELook.Changed -= Refresh;
        RestoreCameras();
        // Editörde Play dışında (sahne görünümü, test sahnesi) outline varsayılan olarak açık kalır.
        CNEOutlineFeature.Enabled = true;
    }

    private void Refresh()
    {
        bool active = CNELook.Active && _settings != null;

        CNEOutlineFeature.Enabled = active && !_outlineDebugOff;
        SetVolume(ref _globalVolume, "CNE Global Volume", active ? _settings.globalProfile : null,
            _settings != null ? _settings.globalPriority : 0f);

        if (!active)
        {
            SetVolume(ref _debugVolume, "CNE Debug Volume", null, 0f);
            RestoreCameras();
        }
    }

    private void LateUpdate()
    {
        if (!CNELook.Active || _settings == null)
            return;

        ApplyToCameras();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        HandleDebugKeys();
#endif
    }

    // Kameralar oyun sırasında doğup kaybolur (oyuncu karakteri, sahne kamerası); her kare etkin olanlara bakılır.
    private void ApplyToCameras()
    {
        foreach (var camera in Camera.allCameras)
        {
            if (camera.cameraType != CameraType.Game)
                continue;

            // Yalnızca varsayılan renderer'la çizen kameralar: kör görüş ayrı bir renderer'dır ve post-process almaz.
            var data = camera.GetUniversalAdditionalCameraData();
            if (data.scriptableRenderer != UniversalRenderPipeline.asset.scriptableRenderer)
                continue;

            if (!_original.ContainsKey(data))
            {
                _original[data] = new CameraState
                {
                    PostProcessing = data.renderPostProcessing,
                    Antialiasing = data.antialiasing,
                    Quality = data.antialiasingQuality,
                };
            }

            data.renderPostProcessing = true;
            data.antialiasing = _settings.antialiasing;
            data.antialiasingQuality = _settings.antialiasingQuality;
        }
    }

    private void RestoreCameras()
    {
        foreach (var pair in _original)
        {
            if (pair.Key == null)
                continue;

            pair.Key.renderPostProcessing = pair.Value.PostProcessing;
            pair.Key.antialiasing = pair.Value.Antialiasing;
            pair.Key.antialiasingQuality = pair.Value.Quality;
        }

        _original.Clear();
    }

    private void SetVolume(ref Volume volume, string volumeName, VolumeProfile profile, float priority)
    {
        if (profile == null)
        {
            if (volume != null)
                Destroy(volume.gameObject);

            volume = null;
            return;
        }

        if (volume == null)
        {
            var host = new GameObject(volumeName) { hideFlags = HideFlags.HideAndDontSave };
            host.transform.SetParent(transform, false);
            volume = host.AddComponent<Volume>();
            volume.isGlobal = true;
        }

        volume.sharedProfile = profile;
        volume.priority = priority;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void HandleDebugKeys()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (WasPressed(keyboard, _settings.grayscaleKey))
        {
            bool show = _debugVolume == null;
            SetVolume(ref _debugVolume, "CNE Debug Volume", show ? _settings.grayscaleProfile : null, _settings.debugPriority);
        }

        if (WasPressed(keyboard, _settings.outlineKey))
        {
            _outlineDebugOff = !_outlineDebugOff;
            CNEOutlineFeature.Enabled = !_outlineDebugOff;
        }
    }

    private static bool WasPressed(Keyboard keyboard, Key key)
    {
        return key != Key.None && keyboard[key].wasPressedThisFrame;
    }
#endif
}
