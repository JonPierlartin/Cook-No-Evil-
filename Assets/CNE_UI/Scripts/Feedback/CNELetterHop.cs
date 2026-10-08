using TMPro;
using UnityEngine;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Menu panosu efekti: harfler soldan saga sirayla ziplar ve oluklarina "tik" diye oturur.
    /// Hop() cagirin (CNEMenuBoardRow basinca cagirir).
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("Cook No Evil/UI/Harf Ziplamasi")]
    public class CNELetterHop : MonoBehaviour
    {
        [Tooltip("Ziplama yuksekligi (birim)")] public float height = 7f;
        [Tooltip("Tek harfin ziplama suresi (sn)")] public float letterTime = 0.16f;
        [Tooltip("Harfler arasi gecikme (sn)")] public float stagger = 0.022f;

        TMP_Text text;
        float elapsed = -1f;

        void Awake()
        {
            text = GetComponent<TMP_Text>();
        }

        public void Hop()
        {
            if (!isActiveAndEnabled) return;
            elapsed = 0f;
        }

        void OnDisable()
        {
            if (elapsed >= 0f)
            {
                elapsed = -1f;
                if (text != null) text.ForceMeshUpdate();
            }
        }

        void LateUpdate()
        {
            if (elapsed < 0f || text == null) return;
            elapsed += Time.unscaledDeltaTime;

            text.ForceMeshUpdate();
            var info = text.textInfo;
            int count = info.characterCount;
            float total = letterTime + stagger * Mathf.Max(0, count - 1);
            bool done = elapsed >= total;

            if (!done)
            {
                int visible = 0;
                for (int i = 0; i < count; i++)
                {
                    var ch = info.characterInfo[i];
                    if (!ch.isVisible) continue;
                    float local = (elapsed - visible * stagger) / letterTime;
                    visible++;
                    if (local <= 0f || local >= 1f) continue;
                    float y = Mathf.Sin(local * Mathf.PI) * height * (1f - local * 0.25f);
                    var verts = info.meshInfo[ch.materialReferenceIndex].vertices;
                    int v = ch.vertexIndex;
                    var offset = new Vector3(0f, y, 0f);
                    verts[v] += offset;
                    verts[v + 1] += offset;
                    verts[v + 2] += offset;
                    verts[v + 3] += offset;
                }
            }

            for (int m = 0; m < info.meshInfo.Length; m++)
            {
                var meshInfo = info.meshInfo[m];
                if (meshInfo.mesh == null) continue;
                meshInfo.mesh.vertices = meshInfo.vertices;
                text.UpdateGeometry(meshInfo.mesh, m);
            }

            if (done) elapsed = -1f;
        }
    }
}
