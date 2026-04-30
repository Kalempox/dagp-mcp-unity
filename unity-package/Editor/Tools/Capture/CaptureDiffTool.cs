using System;
using System.IO;
using System.Threading.Tasks;
using DAGP.MCP.Services;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DAGP.MCP.Tools.CaptureOps
{
    /// <summary>
    /// Pixel-diffs two PNG captures. Returns % of changed pixels and a diff image highlighting changes.
    /// threshold: 0..255 per-channel difference to count as "changed" (default 5).
    /// </summary>
    public class CaptureDiffTool : IDAGPTool
    {
        public string Name => "capture.diff";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var pathA = CapturePaths.Resolve((string)parameters["pathA"] ?? throw new ArgumentException("'pathA' required"));
            var pathB = CapturePaths.Resolve((string)parameters["pathB"] ?? throw new ArgumentException("'pathB' required"));
            var threshold = parameters.Value<int?>("threshold") ?? 5;
            var diffPath = CapturePaths.Resolve((string)parameters["outPath"]) ?? CapturePaths.DefaultPath("diff");

            if (!File.Exists(pathA)) throw new FileNotFoundException(pathA);
            if (!File.Exists(pathB)) throw new FileNotFoundException(pathB);

            // Load + extract pixels in one block per file, then dispose immediately. This avoids
            // any cross-frame texture lifetime issues in Edit mode.
            int width, height;
            Color32[] pixA, pixB;
            LoadPixels(pathA, out pixA, out width, out height);
            int widthB, heightB;
            LoadPixels(pathB, out pixB, out widthB, out heightB);

            if (width != widthB || height != heightB)
                throw new InvalidOperationException(
                    $"dimension mismatch: A={width}x{height} B={widthB}x{heightB}. Capture both at the same resolution.");

            var diff = new Color32[pixA.Length];
            int changed = 0;
            for (int i = 0; i < pixA.Length; i++)
            {
                int dr = Math.Abs(pixA[i].r - pixB[i].r);
                int dg = Math.Abs(pixA[i].g - pixB[i].g);
                int db = Math.Abs(pixA[i].b - pixB[i].b);
                int avg = (dr + dg + db) / 3;
                if (avg > threshold) { changed++; diff[i] = new Color32(255, 0, 0, 255); }
                else { diff[i] = new Color32(pixA[i].r, pixA[i].g, pixA[i].b, 80); }
            }

            var diffTex = new Texture2D(width, height, TextureFormat.RGBA32, false, false) { hideFlags = HideFlags.HideAndDontSave };
            diffTex.SetPixels32(diff);
            diffTex.Apply(false, false);
            var diffBytes = diffTex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(diffTex);
            File.WriteAllBytes(diffPath, diffBytes);

            float pct = (float)changed / pixA.Length * 100f;
            return Task.FromResult<JToken>(new JObject
            {
                ["pathA"] = pathA,
                ["pathB"] = pathB,
                ["diffPath"] = diffPath,
                ["width"] = width,
                ["height"] = height,
                ["totalPixels"] = pixA.Length,
                ["changedPixels"] = changed,
                ["changedPercent"] = Math.Round(pct, 3),
                ["threshold"] = threshold,
                ["verdict"] = pct < 0.1f ? "identical" : pct < 1f ? "near-identical" : pct < 10f ? "minor-changes" : "major-changes"
            });
        }

        static void LoadPixels(string path, out Color32[] pixels, out int width, out int height)
        {
            var bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false) { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                if (!ImageConversion.LoadImage(tex, bytes, markNonReadable: false))
                    throw new InvalidOperationException($"failed to decode PNG: {path}");
                width = tex.width;
                height = tex.height;
                pixels = tex.GetPixels32();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }
    }
}
