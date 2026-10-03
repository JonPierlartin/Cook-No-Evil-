using System;
using UnityEngine;
using UnityEngine.UI;

// Müşterinin kafasındaki sabır çarkı (GDD 3.4.4): sipariş alınmadan önce, YALNIZCA Kasiyer'in ekranında çizilir —
// Komi ve Şef göremez. "Kim görür" kuralının tek yeri burasıdır (visibleToRoles). Yalnızca gösterir: süre sunucuda
// işler, buraya replike değer gelir (Customer.PatienceRemaining / PatienceTotal).
public class CustomerPatienceDisplay : MonoBehaviour
{
    [SerializeField] private Customer customer;
    [Tooltip("Çarkın kökü; görünmediğinde kapatılır.")]
    [SerializeField] private GameObject wheelRoot;
    [Tooltip("Dolum tipi Radial olan görsel: kalan sabır oranı.")]
    [SerializeField] private Image fill;
    [Tooltip("Çarkı görebilen roller (GDD 3.4.4: yalnızca Kasiyer).")]
    [SerializeField] private PlayerRole[] visibleToRoles = { PlayerRole.Kasiyer };
    [SerializeField] private Color fullColor = new(0.3f, 0.85f, 0.35f);
    [SerializeField] private Color emptyColor = new(0.9f, 0.2f, 0.15f);

    private void LateUpdate()
    {
        bool visible = customer.IsSpawned
            && customer.State.Value == CustomerState.WaitingToOrder
            && RoleManager.Instance != null
            && Array.IndexOf(visibleToRoles, RoleManager.Instance.LocalRole) >= 0;

        if (wheelRoot.activeSelf != visible)
            wheelRoot.SetActive(visible);

        if (!visible)
            return;

        float total = customer.PatienceTotal.Value;
        float ratio = total > 0f ? Mathf.Clamp01(customer.PatienceRemaining.Value / total) : 0f;
        fill.fillAmount = ratio;
        fill.color = Color.Lerp(emptyColor, fullColor, ratio);
    }
}
