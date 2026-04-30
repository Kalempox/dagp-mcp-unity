using System;
using System.Threading.Tasks;
using DAGP.MCP.Services;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace DAGP.MCP.Tools.CaptureOps
{
    /// <summary>
    /// Renders the active SceneView (designer perspective) to a PNG. Useful for "asset placed but invisible
    /// in game view" diagnosis — the SceneView shows everything regardless of camera framing.
    /// </summary>
    public class CaptureSceneViewTool : IDAGPTool
    {
        public string Name => "capture.scene_view";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var width = parameters.Value<int?>("width") ?? 1280;
            var height = parameters.Value<int?>("height") ?? 720;
            var path = CapturePaths.Resolve((string)parameters["path"]) ?? CapturePaths.DefaultPath("sceneview");

            // Force-open a SceneView if none is active, then wait one repaint cycle so the camera is initialised.
            var sv = SceneView.lastActiveSceneView;
            var autoOpened = false;
            if (sv == null)
            {
                sv = EditorWindow.GetWindow<SceneView>(false, "Scene", focus: false);
                autoOpened = true;
            }
            if (sv == null || sv.camera == null)
                throw new InvalidOperationException("could not obtain a SceneView camera");

            sv.Focus();
            sv.Repaint();
            // Note: do NOT pre-render sv.camera here — URP throws NRE on SceneView cameras lacking
            // UniversalAdditionalCameraData. CameraCapture.RenderToPng's own Render() handles this safely.
            var size = CameraCapture.RenderToPng(sv.camera, width, height, path);

            if (size < 4096)
            {
                throw new InvalidOperationException(
                    $"SceneView render returned {size} bytes — likely empty. Open the Scene tab in Unity and ensure it shows your scene, then retry.");
            }

            return Task.FromResult<JToken>(new JObject
            {
                ["captured"] = true,
                ["path"] = path,
                ["width"] = width,
                ["height"] = height,
                ["sizeBytes"] = size,
                ["autoOpenedSceneView"] = autoOpened
            });
        }
    }
}
