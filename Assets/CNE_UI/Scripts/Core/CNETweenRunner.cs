using UnityEngine;

namespace CookNoEvil.UI
{
    /// <summary>CNETween'i her karede ilerleten gizli calistirici. Elle eklemeyin; CNETween kendisi olusturur.</summary>
    [AddComponentMenu("")]
    public sealed class CNETweenRunner : MonoBehaviour
    {
        void Update()
        {
            CNETween.Step(Time.unscaledDeltaTime);
        }
    }
}
