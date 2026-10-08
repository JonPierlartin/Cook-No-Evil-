using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Ana menunun gece ortami: menu gorunurken sahneye uygulanir, menu kapaninca geri alinir. Hepsi calisirken
    /// yapilir; sahne dosyasina gece degeri yazilmaz.
    ///  - Kamera menu kadrajina gecer (kadraj noktasi + istege bagli gorus acisi), arka plani degisebilir.
    ///    Ikinci kadraj noktasi verilirse oyun calisirken iki nokta arasinda cok yavas gidip gelir.
    ///  - "Menude acilan" nesneler (gece isik duzenegi: ay isigi, lamba isiklari) acilir; "menude kapanan"
    ///    nesneler ve bilesenler (ornegin gunduz gunesi) kapanir. RenderSettings.sun istenirse ay isigina gecer.
    ///  - Ortam isigi (ambient probe) ve yansima profile gore kisilir; istenirse gece gokyuzu takilir.
    /// Kullanim: menu nesnesinin bir cocuguna koyun; menu SetActive(true/false) oldukca kendiliginden
    /// uygular/geri alir. Editorde onizleme icin Apply() ve Restore() elle cagrilabilir (sahneyi kaydetmeyin).
    ///
    /// Baska sistemlere saygi: geri alirken yalnizca hala bu bilesenin koydugu degerde duran ayarlar eski
    /// haline doner. Menu acikken baska bir sistem (ornegin bir gorunum uygulayicisi) ayni ayari degistirdiyse
    /// onun degeri korunur. Gece degerlerini yeniden basmak icin Apply() tekrar cagrilabilir; o ayarin yeni
    /// degeri artik "gunduz" degeri sayilir.
    /// </summary>
    [AddComponentMenu("Cook No Evil/UI/Menu Gece Ortami")]
    public sealed class CNEMenuAmbience : MonoBehaviour
    {
        [Tooltip("Gece degerleri (veri)")] public CNEMenuAmbienceProfile profile;

        [Header("Kamera")]
        [Tooltip("Menu acikken sahneyi cizen kamera (sahnenin Main Camera'si)")] public Camera targetCamera;
        [Tooltip("Menu kadraji: kamera bu noktanin konum ve donusunu alir. Bossa kamera kipirdamaz.")] public Transform cameraAnchor;
        [Tooltip("Menu kadrajinin gorus acisi (0 = degistirme)")] public float fieldOfView;
        [Tooltip("Istege bagli ikinci kadraj: kamera iki nokta arasinda cok yavas gidip gelir (yalnizca oyun calisirken)")]
        public Transform cameraAnchorB;
        [Tooltip("Gidip gelmenin tek yon suresi (sn, gercek zaman)")] public float driftSeconds = 50f;

        [Header("Sahne")]
        [Tooltip("Menude acilan nesneler (gece isik duzenegi vb.). Sahnede kapali dursunlar.")]
        public GameObject[] activateDuringMenu = new GameObject[0];
        [Tooltip("Menude kapanan nesneler")] public GameObject[] deactivateDuringMenu = new GameObject[0];
        [Tooltip("Menude kapanan bilesenler (ornegin gunduz gunesinin Light bileseni)")]
        public Behaviour[] disableDuringMenu = new Behaviour[0];
        [Tooltip("Menude ana isik (RenderSettings.sun) bu isik olsun (istege bagli; ornegin ay isigi)")]
        public Light menuSun;

        [Tooltip("Menu gorunur olunca (OnEnable) uygula, gizlenince (OnDisable) geri al.")]
        public bool followVisibility = true;

        interface IOverride
        {
            void Restore();
        }

        /// <summary>Bir ayarin gunduz degerini yakalar, gece degerini basar, geri alirken baskasinin degerine dokunmaz.</summary>
        sealed class Override<T> : IOverride
        {
            readonly Func<T> get;
            readonly Action<T> set;
            readonly Func<T, T, bool> same;
            T captured;
            T applied;
            bool active;

            public Override(Func<T> get, Action<T> set, Func<T, T, bool> same)
            {
                this.get = get;
                this.set = set;
                this.same = same;
            }

            public void Apply(Func<T, T> night)
            {
                Capture();
                Set(night(captured));
            }

            public void ApplyValue(T value)
            {
                Capture();
                Set(value);
            }

            void Capture()
            {
                T current = get();
                if (!active || !same(current, applied)) captured = current; // ilk kez ya da baskasi degistirdi
            }

            void Set(T value)
            {
                applied = value;
                set(value);
                active = true;
            }

            public void Restore()
            {
                if (!active) return;
                if (same(get(), applied)) set(captured);
                active = false;
            }
        }

        readonly List<IOverride> overrides = new List<IOverride>();
        Override<Vector3> camPosition;
        Override<Quaternion> camRotation;
        Override<float> camFov;
        Override<CameraClearFlags> camClear;
        Override<Color> camBackground;
        Override<SphericalHarmonicsL2> ambient;
        Override<float> reflection;
        Override<Material> skybox;
        Override<Light> sun;
        readonly Dictionary<UnityEngine.Object, Override<bool>> states = new Dictionary<UnityEngine.Object, Override<bool>>();
        bool applied;
        float driftTime;

        public bool IsApplied
        {
            get { return applied; }
        }

        void OnEnable()
        {
            if (followVisibility && Application.isPlaying) Apply();
        }

        void OnDisable()
        {
            if (followVisibility) Restore();
        }

        void Update()
        {
            if (!applied || targetCamera == null || cameraAnchor == null || cameraAnchorB == null || driftSeconds <= 0f) return;
            driftTime += Time.unscaledDeltaTime;
            float p = Mathf.PingPong(driftTime / driftSeconds, 1f);
            float t = p * p * p * (p * (p * 6f - 15f) + 10f); // smootherstep: uclarda yumusak durup doner
            camPosition.ApplyValue(Vector3.Lerp(cameraAnchor.position, cameraAnchorB.position, t));
            camRotation.ApplyValue(Quaternion.Slerp(cameraAnchor.rotation, cameraAnchorB.rotation, t));
        }

        /// <summary>Gece ortamini uygular. Uygulanmisken yeniden cagrilirsa gece degerlerini tekrar basar.</summary>
        public void Apply()
        {
            EnsureOverrides();

            if (targetCamera != null)
            {
                if (cameraAnchor != null)
                {
                    camPosition.ApplyValue(cameraAnchor.position);
                    camRotation.ApplyValue(cameraAnchor.rotation);
                    driftTime = 0f;
                }
                if (fieldOfView > 0f)
                {
                    float fov = fieldOfView;
                    camFov.Apply(_ => fov);
                }
                if (profile != null && profile.overrideCameraBackground)
                {
                    var color = profile.cameraBackground;
                    camClear.Apply(_ => CameraClearFlags.SolidColor);
                    camBackground.Apply(_ => color);
                }
            }

            if (profile != null)
            {
                var tint = profile.ambientTint * profile.ambientMultiplier;
                if (tint != Color.white) ambient.Apply(sh => Tint(sh, tint));
                float refl = profile.reflectionMultiplier;
                if (!Mathf.Approximately(refl, 1f)) reflection.Apply(r => r * refl);
                var sky = profile.nightSkybox;
                if (sky != null) skybox.Apply(_ => sky);
            }

            if (menuSun != null)
            {
                var light = menuSun;
                sun.Apply(_ => light);
            }

            for (int i = 0; i < activateDuringMenu.Length; i++) SetActiveState(activateDuringMenu[i], true);
            for (int i = 0; i < deactivateDuringMenu.Length; i++) SetActiveState(deactivateDuringMenu[i], false);
            for (int i = 0; i < disableDuringMenu.Length; i++) SetEnabledState(disableDuringMenu[i], false);

            applied = true;
        }

        /// <summary>Uyguladigi her seyi geri alir (baska sistemin sonradan degistirdigi ayarlara dokunmaz).</summary>
        public void Restore()
        {
            if (!applied) return;
            for (int i = overrides.Count - 1; i >= 0; i--) overrides[i].Restore();
            applied = false;
        }

        void SetActiveState(GameObject go, bool active)
        {
            if (go == null) return;
            Override<bool> o;
            if (!states.TryGetValue(go, out o))
            {
                var target = go;
                o = new Override<bool>(() => target != null && target.activeSelf, v => { if (target != null) target.SetActive(v); }, (a, b) => a == b);
                states.Add(go, o);
                overrides.Add(o);
            }
            o.Apply(_ => active);
        }

        void SetEnabledState(Behaviour behaviour, bool enabled)
        {
            if (behaviour == null) return;
            Override<bool> o;
            if (!states.TryGetValue(behaviour, out o))
            {
                var target = behaviour;
                o = new Override<bool>(() => target != null && target.enabled, v => { if (target != null) target.enabled = v; }, (a, b) => a == b);
                states.Add(behaviour, o);
                overrides.Add(o);
            }
            o.Apply(_ => enabled);
        }

        void EnsureOverrides()
        {
            if (camPosition != null) return;
            camPosition = Add(new Override<Vector3>(
                () => targetCamera != null ? targetCamera.transform.position : Vector3.zero,
                v => { if (targetCamera != null) targetCamera.transform.position = v; },
                (a, b) => (a - b).sqrMagnitude < 1e-8f));
            camRotation = Add(new Override<Quaternion>(
                () => targetCamera != null ? targetCamera.transform.rotation : Quaternion.identity,
                v => { if (targetCamera != null) targetCamera.transform.rotation = v; },
                (a, b) => Quaternion.Angle(a, b) < 0.01f));
            camFov = Add(new Override<float>(
                () => targetCamera != null ? targetCamera.fieldOfView : 60f,
                v => { if (targetCamera != null) targetCamera.fieldOfView = v; },
                (a, b) => Mathf.Abs(a - b) < 1e-4f));
            camClear = Add(new Override<CameraClearFlags>(
                () => targetCamera != null ? targetCamera.clearFlags : CameraClearFlags.Skybox,
                v => { if (targetCamera != null) targetCamera.clearFlags = v; },
                (a, b) => a == b));
            camBackground = Add(new Override<Color>(
                () => targetCamera != null ? targetCamera.backgroundColor : Color.black,
                v => { if (targetCamera != null) targetCamera.backgroundColor = v; },
                (a, b) => a == b));
            ambient = Add(new Override<SphericalHarmonicsL2>(
                () => RenderSettings.ambientProbe,
                v => RenderSettings.ambientProbe = v,
                (a, b) => a == b));
            reflection = Add(new Override<float>(
                () => RenderSettings.reflectionIntensity,
                v => RenderSettings.reflectionIntensity = v,
                (a, b) => Mathf.Abs(a - b) < 1e-5f));
            skybox = Add(new Override<Material>(
                () => RenderSettings.skybox,
                v => RenderSettings.skybox = v,
                (a, b) => a == b));
            sun = Add(new Override<Light>(
                () => RenderSettings.sun,
                v => RenderSettings.sun = v,
                (a, b) => a == b));
        }

        Override<T> Add<T>(Override<T> o)
        {
            overrides.Add(o);
            return o;
        }

        /// <summary>Ortam probunu kanal kanal renkle carpar (RGB; 9 katsayi).</summary>
        static SphericalHarmonicsL2 Tint(SphericalHarmonicsL2 sh, Color tint)
        {
            var result = sh;
            for (int c = 0; c < 9; c++)
            {
                result[0, c] = sh[0, c] * tint.r;
                result[1, c] = sh[1, c] * tint.g;
                result[2, c] = sh[2, c] * tint.b;
            }
            return result;
        }
    }
}
