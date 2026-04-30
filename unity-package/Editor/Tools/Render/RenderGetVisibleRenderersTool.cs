using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DAGP.MCP.Tools.RenderOps
{
    /// <summary>
    /// Returns Renderers actually visible to a Camera (frustum + bounds + isVisible check). Use to confirm
    /// "asset placed but cannot be seen" — pixel position can be correct yet invisible (behind camera, culled,
    /// alpha=0, layer-masked). This is a cheap alternative to capture.game_view for binary "is it shown?" checks.
    /// </summary>
    public class RenderGetVisibleRenderersTool : IDAGPTool
    {
        public string Name => "render.get_visible_renderers";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var cameraName = (string)parameters["camera"];
            var includeUI = parameters.Value<bool?>("includeUI") ?? false;

            var cam = ResolveCamera(cameraName);
            if (cam == null) throw new System.InvalidOperationException("no Camera found");

            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            var allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var visible = new JArray();
            var hidden = new JArray();

            foreach (var r in allRenderers)
            {
                if (!includeUI && r is CanvasRenderer) continue;

                var inFrustum = GeometryUtility.TestPlanesAABB(planes, r.bounds);
                var layerVisible = (cam.cullingMask & (1 << r.gameObject.layer)) != 0;
                var enabled = r.enabled && r.gameObject.activeInHierarchy;
                var isVisible = inFrustum && layerVisible && enabled;

                var entry = new JObject
                {
                    ["name"] = r.name,
                    ["type"] = r.GetType().Name,
                    ["layer"] = LayerMask.LayerToName(r.gameObject.layer),
                    ["inFrustum"] = inFrustum,
                    ["layerVisible"] = layerVisible,
                    ["enabled"] = enabled,
                    ["bounds"] = new JObject
                    {
                        ["centerX"] = r.bounds.center.x, ["centerY"] = r.bounds.center.y, ["centerZ"] = r.bounds.center.z,
                        ["sizeX"] = r.bounds.size.x, ["sizeY"] = r.bounds.size.y, ["sizeZ"] = r.bounds.size.z
                    }
                };
                if (isVisible) visible.Add(entry); else hidden.Add(entry);
            }

            return Task.FromResult<JToken>(new JObject
            {
                ["camera"] = cam.name,
                ["totalRenderers"] = allRenderers.Length,
                ["visibleCount"] = visible.Count,
                ["hiddenCount"] = hidden.Count,
                ["visible"] = visible,
                ["hidden"] = hidden
            });
        }

        static Camera ResolveCamera(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                var go = GameObject.Find(name);
                if (go != null) { var c = go.GetComponent<Camera>(); if (c != null) return c; }
            }
            return Camera.main ?? UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).FirstOrDefault();
        }
    }
}
