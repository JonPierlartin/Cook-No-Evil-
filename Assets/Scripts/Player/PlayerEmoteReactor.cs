using System.Collections;
using Unity.Netcode;
using UnityEngine;

// Kasiyer'in emote carkinda sectigi tepkiyi HERKESIN gorebilecegi sekilde oynatir.
// Player.prefab'in her client'taki HER kopyasi EmoteSystem.OnEmoteTriggered'i dinler
// (broadcast, hedefsiz ClientRpc), ama sadece OwnerClientId == kasiyerClientId olan
// (yani gercekten Kasiyer'in objesi olan) kopyada tepki oynatilir — boylece ayrica bir
// hedefleme/lookup gerekmeden dogru karakterde, tum client'larda ayni anda calisir.
// Gorsel efekt (renk parlamasi + egilme/ziplama) SADECE Visual child'in local
// transform/material'inde oynatilir — kokte (owner-authoritative NetworkTransform'un
// yonettigi transform'da) DEGIL, aksi halde bu gercek hareket sanilip PlayerController'in
// yaw/hareketiyle catisirdi (bkz. Player.prefab restructuring notu, CLAUDE.md).
public class PlayerEmoteReactor : NetworkBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Renderer visualRenderer;
    [SerializeField] private float flashDuration = 0.6f;
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
        if (EmoteSystem.Instance != null)
            EmoteSystem.Instance.OnEmoteTriggered += HandleEmoteTriggered;
    }

    public override void OnNetworkDespawn()
    {
        if (EmoteSystem.Instance != null)
            EmoteSystem.Instance.OnEmoteTriggered -= HandleEmoteTriggered;

        if (_reactionRoutine != null)
        {
            StopCoroutine(_reactionRoutine);
            _reactionRoutine = null;
        }
    }

    private void HandleEmoteTriggered(ulong kasiyerClientId, int emoteIndex)
    {
        if (OwnerClientId != kasiyerClientId)
            return;

        var availableEmotes = EmoteSystem.Instance?.AvailableEmotes;
        if (availableEmotes == null || emoteIndex < 0 || emoteIndex >= availableEmotes.Length || availableEmotes[emoteIndex] == null)
            return;

        if (_reactionRoutine != null)
            StopCoroutine(_reactionRoutine);
        _reactionRoutine = StartCoroutine(PlayReaction(availableEmotes[emoteIndex].ReactionColor));
    }

    private IEnumerator PlayReaction(Color color)
    {
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flashDuration);
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

        _reactionRoutine = null;
    }
}
