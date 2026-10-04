using UnityEngine;

// Yürüme sesi: karakterin ayağı yere her bastığında (ProceduralCharacterAnimator.StepCount) kısa bir adım sesi
// çalar. Tamamen yerel sunum — adım, her istemcide o karakterin kendi animasyonundan doğar; ağ verisi yoktur.
// Kaynak 3B ve Linear'dır ve RoleAwareAudioRange taşır: sağır rol kendi adımını çok kısık ve boğuk duyar, başkasının
// adımını (dar yarıçapın dışında) hiç duymaz (GDD 4.1.3). Role göre kod burada yoktur.
public class FootstepAudio : MonoBehaviour
{
    [Tooltip("Karakter modeli (adımlar modelin animasyonundan okunur).")]
    [SerializeField] private PlayerCharacterVisual character;
    [Tooltip("Adım sesinin çaldığı 3B kaynak (ayak hizasında; Linear rolloff + RoleAwareAudioRange).")]
    [SerializeField] private AudioSource source;
    [Tooltip("Adım sesleri; her adımda biri rastgele seçilir.")]
    [SerializeField] private AudioClip[] clips;
    [Tooltip("Her adımda perde bu aralıktan rastgele seçilir (aynı ses tekrar etmesin).")]
    [SerializeField] private Vector2 pitchRange = new(0.9f, 1.12f);

    private ProceduralCharacterAnimator _animator;
    private int _lastStepCount;

    private void Update()
    {
        var animator = character != null ? character.Animator : null;
        if (animator != _animator)
        {
            // Model yeniden kuruldu (rol değişti / yeniden doğdu): sayaç yeni animasyondan başlar.
            _animator = animator;
            _lastStepCount = animator != null ? animator.StepCount : 0;
            return;
        }

        if (_animator == null || _animator.StepCount == _lastStepCount)
            return;

        _lastStepCount = _animator.StepCount;
        if (clips == null || clips.Length == 0)
            return;

        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }
}
