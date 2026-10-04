using System;
using UnityEngine;

// Bir elin parmak pozu: parmak eklemleri veridir (prefab kurulurken doldurulur), kıvrılma miktarı çağırandan gelir.
// Her parmak ayrı sürülür — işaret, orta, serçe, başparmak (el modeli dört parmaklıdır) — böylece aynı el "işaret
// et" (işaret düz, diğerleri kapalı), "tut", "rahat" ve SAYI (kaç parmak açık) pozlarını alabilir. Yalnızca gösterir.
public class HandPose : MonoBehaviour
{
    public enum Group
    {
        Index,
        // Orta parmak (eski adı korunur: serileştirilmiş değer 1).
        Others,
        Thumb,
        Pinky
    }

    [Serializable]
    public struct Joint
    {
        public Transform bone;
        public Group group;
        [Tooltip("Eklemin kendi yerel uzayında kıvrılma ekseni (avuç içine doğru pozitif).")]
        public Vector3 localAxis;
        [Tooltip("Tam kıvrıkken bu eklemin dönüşü (derece).")]
        public float maxAngle;
        [Tooltip("Dinlenme (açık el) dönüşü; prefab kurulurken yakalanır.")]
        public Quaternion restRotation;
    }

    [SerializeField] private Joint[] joints = Array.Empty<Joint>();

    // 0 = açık, 1 = tam kıvrık.
    public void Apply(float indexCurl, float middleCurl, float pinkyCurl, float thumbCurl)
    {
        foreach (var joint in joints)
        {
            if (joint.bone == null)
                continue;

            float curl = joint.group switch
            {
                Group.Index => indexCurl,
                Group.Thumb => thumbCurl,
                Group.Pinky => pinkyCurl,
                _ => middleCurl
            };

            joint.bone.localRotation = joint.restRotation * Quaternion.AngleAxis(joint.maxAngle * Mathf.Clamp01(curl), joint.localAxis);
        }
    }
}
