using System.IO;
using UnityEngine;

namespace DAGP.MCP.Services
{
    /// <summary>Shared camera-to-PNG helper used by capture.* tools.</summary>
    public static class CameraCapture
    {
        public static int RenderToPng(Camera cam, int width, int height, string path)
        {
            // antiAliasing=1 (no MSAA) — URP doesn't honour the legacy RenderTexture.antiAliasing path,
            // and MSAA RenderTexture needs explicit Resolve(), which spams "Missing resolve surface" errors.
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;

            var bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);

            File.WriteAllBytes(path, bytes);
            return bytes.Length;
        }
    }
}
