using UnityEngine;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Yeni Input System ya da eski Input Manager hangisi aciksa onu kullanir
    /// (Project Settings > Player > Active Input Handling).
    /// </summary>
    public static class CNEInput
    {
        /// <summary>ESC ya da gamepad "geri" (B / daire) bu karede basildi mi?</summary>
        public static bool CancelPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) return true;
            var gamepad = UnityEngine.InputSystem.Gamepad.current;
            if (gamepad != null && gamepad.buttonEast.wasPressedThisFrame) return true;
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1);
#else
            return false;
#endif
        }
    }
}
