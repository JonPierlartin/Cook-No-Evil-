using CookNoEvil.UI;
using UnityEngine;

// Arayüz kitinin Ayarlar kartı boyunu içeriğine göre ayarlar, ama ölçümü kendi OnEnable'ında yapar: o anda alt
// nesnelerin (sütunların düzen grupları) OnEnable'ı henüz çalışmamıştır, içerik 0 ölçülür ve kart hep en kısa
// boyunda kalır (satırlar düğmelerin üstüne ve kartın dışına taşar). Kit dosyaları düzenlenmediği için ölçüm
// burada, kart açıldığı karenin sonunda — herkes etkinleştikten sonra — bir kez daha yaptırılır.
// Kit bu hatayı kendi tarafında düzeltirse bileşen kaldırılır.
[RequireComponent(typeof(CNESettingsPanel))]
public sealed class SettingsCardLayoutRefresh : MonoBehaviour
{
    private CNESettingsPanel _panel;
    private bool _pending;

    private void Awake()
    {
        _panel = GetComponent<CNESettingsPanel>();
    }

    private void OnEnable()
    {
        _pending = true;
    }

    private void LateUpdate()
    {
        if (!_pending)
            return;

        _pending = false;
        _panel.RefreshLayout();
    }
}
