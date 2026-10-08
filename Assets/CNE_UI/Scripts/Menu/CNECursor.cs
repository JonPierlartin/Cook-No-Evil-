using UnityEngine;

namespace CookNoEvil.UI
{
    /// <summary>Menulerde imleci "susler kurdan" yapar; nesne kapaninca sistem imlecine doner.</summary>
    [AddComponentMenu("Cook No Evil/UI/Imlec (Kurdan)")]
    public class CNECursor : MonoBehaviour
    {
        [Tooltip("Texture Type = Cursor olarak iceri alinmis doku (Art/Misc/cursor_kurdan)")]
        public Texture2D cursorTexture;
        [Tooltip("Kurdanin sivri ucu (piksel, sol ust koseden)")] public Vector2 hotspot = new Vector2(3f, 3f);
        public bool resetOnDisable = true;

        void OnEnable()
        {
            if (cursorTexture != null) Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
        }

        void OnDisable()
        {
            if (resetOnDisable) Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
