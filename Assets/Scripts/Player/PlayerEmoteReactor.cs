using System.Collections;
using Unity.Netcode;
using UnityEngine;

// Bir oyuncunun emote carkinda (E) sectigi tepkiyi HERKESIN gorebilecegi sekilde oynatir. Sure
// EmoteDefinition'dan gelir; etkilesim tepkiyi keser (EmoteSystem.OnPlaybackCancelled).
// Player.prefab'in her client'taki HER kopyasi EmoteSystem.OnEmoteTriggered'i dinler
// (broadcast, hedefsiz ClientRpc), ama sadece OwnerClientId == kasiyerClientId olan
// (yani gercekten Kasiyer'in objesi olan) kopyada tepki oynatilir — boylece ayrica bir
// hedefleme/lookup gerekmeden dogru karakterde, tum client'larda ayni anda calisir.
// Gorsel efekt (renk parlamasi + egilme/ziplama) SADECE Visual child'in local
// transform/material'inde oynatilir — kokte (owner-authoritative NetworkTransform'un
// yonettigi transform'da) DEGIL, aksi halde bu gercek hareket sanilip PlayerController'in
// yaw/hareketiyle catisirdi.
public class PlayerEmoteReactor : NetworkBehaviour
{
    [Tooltip("El hareketi olan emote'lar karakterin animasyon bileşeninde oynar.")]
    [SerializeField] private PlayerCharacterVisual character;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Renderer visualRenderer;
    [SerializeField] private float bounceHeight = 0.2f;
    [SerializeField] private float tiltAngle = 12f;

    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");

    private MaterialPropertyBlock _propertyBlock;
    private Color _originalColor = Color.white;
    private Coroutine _reactionRoutine;
    private Vector3 _visualRestLocalPosition;
    private Quaternion _visualRestLocalRotation;

    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();

        if (visualRenderer != null && visualRenderer.sharedMaterial != null)
            _originalColor = visualRenderer.sharedMaterial.GetColor(BaseColorPropertyId);

        if (visualRoot != null)
        {
            _visualRestLocalPosition = visualRoot.localPosition;
            _visualRestLocalRotation = visualRoot.localRotation;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (EmoteSystem.Instance == null)
            return;

        EmoteSystem.Instance.OnEmoteTriggered += HandleEmoteTriggered;
        EmoteSystem.Instance.OnPlaybackCancelled += HandlePlaybackCancelled;
    }

    public override void OnNetworkDespawn()
    {
        if (EmoteSystem.Instance != null)
        {
            EmoteSystem.Instance.OnEmoteTriggered -= HandleEmoteTriggered;
            EmoteSystem.Instance.OnPlaybackCancelled -= HandlePlaybackCancelled;
        }

        StopReaction();
    }

    private void HandlePlaybackCancelled(ulong clientId)
    {
        if (OwnerClientId == clientId)
            StopReaction();
    }

    private void StopReaction()
    {
        if (character != null && character.Animator != null)
            character.Animator.CancelGesture();

        if (_reactionRoutine == null)
            return;

        StopCoroutine(_reactionRoutine);
        RestoreVisual();
        _reactionRoutine = null;
    }

    private void HandleEmoteTriggered(ulong kasiyerClientId, int emoteIndex)
    {
        if (OwnerClientId != kasiyerClientId)
            return;

        var availableEmotes = EmoteSystem.Instance?.AvailableEmotes;
        if (availableEmotes == null || emoteIndex < 0 || emoteIndex >= availableEmotes.Length || availableEmotes[emoteIndex] == null)
            return;

        StopReaction();
        var emote = availableEmotes[emoteIndex];
        // Öncelik: artist klibi → kodla üretilen el animasyonu → yer tutucu tepki (zıplama).
        bool hasAnimator = character != null && character.Animator != null;
        if (emote.HasClip && hasAnimator)
        {
            character.Animator.PlayClips(emote.Clip, emote.LeftClip, emote.Duration, emote.ClipAnchor);
            return;
        }

        if (emote.ProceduralAnimation != null && hasAnimator)
        {
            character.Animator.Play(emote.ProceduralAnimation, emote.Duration);
            return;
        }

        _reactionRoutine = StartCoroutine(PlayReaction(emote.ReactionColor, emote.Duration));
    }

    private IEnumerator PlayReaction(Color color, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float wave = Mathf.Sin(t * Mathf.PI);

            if (visualRenderer != null)
            {
                visualRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorPropertyId, Color.Lerp(_originalColor, color, wave));
                visualRenderer.SetPropertyBlock(_propertyBlock);
            }

            if (visualRoot != null)
            {
                visualRoot.localPosition = _visualRestLocalPosition + Vector3.up * (wave * bounceHeight);
                visualRoot.localRotation = _visualRestLocalRotation * Quaternion.Euler(0f, 0f, wave * tiltAngle);
            }

            yield return null;
        }

        RestoreVisual();
        _reactionRoutine = null;
    }

    private void RestoreVisual()
    {
        if (visualRenderer != null)
        {
            visualRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorPropertyId, _originalColor);
            visualRenderer.SetPropertyBlock(_propertyBlock);
        }

        if (visualRoot != null)
        {
            visualRoot.localPosition = _visualRestLocalPosition;
            visualRoot.localRotation = _visualRestLocalRotation;
        }
    }
}
