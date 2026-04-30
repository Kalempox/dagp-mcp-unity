using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace DAGP.MCP.Tools.InputOps
{
    /// <summary>
    /// Sends a real Windows keyboard event via SendInput. Requires the Unity Editor (specifically the
    /// Game View) to have focus — the event hits whichever app is foreground.
    /// keyCode: virtual-key code byte (e.g. 'A'=65, 'Space'=32, 'Left'=37, 'Up'=38, 'Right'=39, 'Down'=40, 'Enter'=13).
    /// holdMs: how long to hold key down before release (default 50).
    /// </summary>
    public class InputSendKeyTool : IDAGPTool
    {
        public string Name => "input.send_key";
        public bool RequiresMainThread => false;

        public async Task<JToken> Execute(JObject parameters)
        {
            var keyCode = parameters.Value<int?>("keyCode") ?? throw new ArgumentException("'keyCode' required (Windows VK_ code)");
            var holdMs = parameters.Value<int?>("holdMs") ?? 50;
            var focusGameView = parameters.Value<bool?>("focusGameView") ?? true;

            if (focusGameView) TryFocusGameView();

            SendVirtualKey((ushort)keyCode, true);
            await Task.Delay(holdMs);
            SendVirtualKey((ushort)keyCode, false);

            return new JObject
            {
                ["sent"] = true,
                ["keyCode"] = keyCode,
                ["holdMs"] = holdMs,
                ["isPlaying"] = EditorApplication.isPlaying
            };
        }

        static void TryFocusGameView()
        {
            try
            {
                var t = Type.GetType("UnityEditor.GameView,UnityEditor");
                if (t != null) { var w = EditorWindow.GetWindow(t, false, null, false); if (w != null) w.Focus(); }
            }
            catch { /* best effort */ }
        }

        // ─── Win32 SendInput ──────────────────────────────────────────────────
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        const uint INPUT_KEYBOARD = 1;
        const uint KEYEVENTF_KEYUP = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT { public uint type; public InputUnion u; }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public Padding _pad; // ensures struct size matches MOUSEINPUT/HARDWAREINPUT union
        }

        [StructLayout(LayoutKind.Sequential, Size = 24)] struct Padding { }

        static void SendVirtualKey(ushort vk, bool down)
        {
            var input = new INPUT { type = INPUT_KEYBOARD };
            input.u.ki = new KEYBDINPUT { wVk = vk, dwFlags = down ? 0u : KEYEVENTF_KEYUP };
            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
