using System;
using System.Threading.Tasks;
using DAGP.MCP.Services;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DAGP.MCP.Tools.CaptureOps
{
    /// <summary>
    /// Renders the active Camera (or named camera) to a PNG. Works in Edit and Play mode.
    /// Output saved under &lt;project&gt;/.dagp/screenshots/ unless 'path' is supplied.
    /// Default resolution: Game View size if Play mode, otherwise 1920x1080.
    /// </summary>
    public class CaptureGameViewTool : IDAGPTool
    {
        public string Name => "capture.game_view";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var width = parameters.Value<int?>("width") ?? 1920;
            var height = parameters.Value<int?>("height") ?? 1080;
            var cameraName = (string)parameters["camera"];
            var path = CapturePaths.Resolve((string)parameters["path"]) ?? CapturePaths.DefaultPath("gameview");

            var cam = ResolveCamera(cameraName);
            if (cam == null) throw new InvalidOperationException("no Camera found in scene");

            var size = CameraCapture.RenderToPng(cam, width, height, path);
            return Task.FromResult<JToken>(new JObject
            {
                ["captured"] = true,
                ["path"] = path,
                ["width"] = width,
                ["height"] = height,
                ["camera"] = cam.name,
                ["isPlaying"] = EditorApplication.isPlaying,
                ["sizeBytes"] = size
            });
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
