using System;
using System.Threading.Tasks;
using DAGP.MCP.Services;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DAGP.MCP.Tools.CaptureOps
{
    /// <summary>
    /// Captures the same camera at multiple resolutions in one call. Use to detect UI overflow
    /// across phone (1170x2532), tablet (2048x2732), desktop (1920x1080) etc.
    /// presets: 'mobile' (3 phone aspects), 'desktop' (3 desktop aspects), 'all' (6).
    /// resolutions: array of {name, width, height} overrides preset.
    /// </summary>
    public class CaptureAspectMatrixTool : IDAGPTool
    {
        public string Name => "capture.aspect_matrix";
        public bool RequiresMainThread => true;

        static readonly (string name, int w, int h)[] MobilePresets = {
            ("phone_portrait_9x16",  1080, 1920),
            ("phone_portrait_19_5x9", 1170, 2532),
            ("tablet_portrait_3x4",   1620, 2160)
        };
        static readonly (string name, int w, int h)[] DesktopPresets = {
            ("hd_16x9",     1280, 720),
            ("fhd_16x9",    1920, 1080),
            ("ultrawide_21x9", 2560, 1080)
        };

        public Task<JToken> Execute(JObject parameters)
        {
            var preset = ((string)parameters["preset"] ?? "mobile").ToLowerInvariant();
            var cameraName = (string)parameters["camera"];

            var cam = ResolveCamera(cameraName);
            if (cam == null) throw new InvalidOperationException("no Camera found");

            var resolutions = ResolveResolutions(preset, parameters["resolutions"] as JArray);
            var captures = new JArray();

            foreach (var (name, w, h) in resolutions)
            {
                var path = CapturePaths.DefaultPath($"aspect_{name}");
                var size = CameraCapture.RenderToPng(cam, w, h, path);
                captures.Add(new JObject
                {
                    ["name"] = name, ["width"] = w, ["height"] = h, ["path"] = path, ["sizeBytes"] = size
                });
            }

            return Task.FromResult<JToken>(new JObject
            {
                ["captured"] = captures.Count,
                ["preset"] = preset,
                ["camera"] = cam.name,
                ["images"] = captures
            });
        }

        static (string, int, int)[] ResolveResolutions(string preset, JArray custom)
        {
            if (custom != null && custom.Count > 0)
            {
                var list = new System.Collections.Generic.List<(string, int, int)>();
                foreach (var r in custom)
                    list.Add(((string)r["name"] ?? "custom", (int)r["width"], (int)r["height"]));
                return list.ToArray();
            }
            switch (preset)
            {
                case "desktop": return DesktopPresets;
                case "all":
                    var all = new (string, int, int)[MobilePresets.Length + DesktopPresets.Length];
                    System.Array.Copy(MobilePresets, all, MobilePresets.Length);
                    System.Array.Copy(DesktopPresets, 0, all, MobilePresets.Length, DesktopPresets.Length);
                    return all;
                default: return MobilePresets;
            }
        }

        static Camera ResolveCamera(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                var go = GameObject.Find(name);
                if (go != null) { var c = go.GetComponent<Camera>(); if (c != null) return c; }
            }
            if (Camera.main != null) return Camera.main;
            var all = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            return all.Length > 0 ? all[0] : null;
        }
    }
}
