using System;
using UnityEngine;
using UnityEngine.UI;

// Müşterinin kafasındaki zaman çarkı (GDD 3.4.4): sipariş alınmadan önce SABIR, alındıktan sonra SİPARİŞ SÜRESİ
// (sabır çarkı yerini sipariş çarkına bırakır). İkisi de YALNIZCA Kasiyer'in ekranında çizilir — Komi ve Şef zaman
// baskısını yalnızca Kasiyer'den öğrenir. "Kim görür" kuralının tek yeri burasıdır (visibleToRoles). Yalnızca
// gösterir: süreler sunucuda işler, buraya replike değer gelir.
public class CustomerTimerDisplay : MonoBehaviour
{
    [SerializeField] private Customer customer;
    [Tooltip("Çarkın kökü; görünmediğinde kapatılır.")]
    [SerializeField] private GameObject wheelRoot;
    [Tooltip("Dolum tipi Radial olan görsel: kalan süre oranı.")]
    [SerializeField] private Image fill;
    [Tooltip("Çarkı görebilen roller (GDD 3.4.4: yalnızca Kasiyer).")]
    [SerializeField] private PlayerRole[] visibleToRoles = { PlayerRole.Kasiyer };
    [Header("Sabır (sipariş alınmadan önce)")]
    [SerializeField] private Color fullColor = new(0.3f, 0.85f, 0.35f);
    [SerializeField] private Color emptyColor = new(0.9f, 0.2f, 0.15f);
    [Header("Sipariş süresi (alındıktan sonra)")]
    [SerializeField] private Color orderFullColor = new(0.3f, 0.6f, 1f);
    [SerializeField] private Color orderEmptyColor = new(0.9f, 0.2f, 0.15f);

    private void LateUpdate()
    {
        var state = customer.IsSpawned ? customer.State.Value : CustomerState.Arriving;
        bool patience = state == CustomerState.WaitingToOrder;
        bool order = state == CustomerState.Ordered;

        bool visible = (patience || order)
            && RoleManager.Instance != null
            && Array.IndexOf(visibleToRoles, RoleManager.Instance.LocalRole) >= 0;

        if (wheelRoot.activeSelf != visible)
            wheelRoot.SetActive(visible);

        if (!visible)
            return;

        float total = patience ? customer.PatienceTotal.Value : customer.OrderTimeTotal.Value;
        float remaining = patience ? customer.PatienceRemaining.Value : customer.OrderTimeRemaining.Value;
        float ratio = total > 0f ? Mathf.Clamp01(remaining / total) : 0f;
        fill.fillAmount = ratio;
        fill.color = patience ? Color.Lerp(emptyColor, fullColor, ratio) : Color.Lerp(orderEmptyColor, orderFullColor, ratio);
    }
}
