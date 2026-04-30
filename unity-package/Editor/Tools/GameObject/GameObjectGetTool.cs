using System;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DAGP.MCP.Tools.GameObjectOps
{
    /// <summary>Returns transform, components, parent/children of a GameObject (by name, path, or instanceId).</summary>
    public class GameObjectGetTool : IDAGPTool
    {
        public string Name => "gameobject.get";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var go = GameObjectFinder.Resolve(parameters);
            if (go == null) throw new InvalidOperationException("gameobject not found");

            var t = go.transform;
            var components = new JArray();
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) { components.Add(new JObject { ["type"] = "<missing>" }); continue; }
                JToken enabledToken = (c is Behaviour b) ? new JValue(b.enabled) : JValue.CreateNull();
                components.Add(new JObject
                {
                    ["type"] = c.GetType().FullName,
                    ["enabled"] = enabledToken
                });
            }

            var children = new JArray();
            for (int i = 0; i < t.childCount; i++)
            {
                var ch = t.GetChild(i);
                children.Add(new JObject { ["name"] = ch.name, ["instanceId"] = ch.gameObject.GetInstanceID() });
            }

            return Task.FromResult<JToken>(new JObject
            {
                ["name"] = go.name,
                ["instanceId"] = go.GetInstanceID(),
                ["active"] = go.activeSelf,
                ["activeInHierarchy"] = go.activeInHierarchy,
                ["tag"] = go.tag,
                ["layer"] = go.layer,
                ["layerName"] = LayerMask.LayerToName(go.layer),
                ["isStatic"] = go.isStatic,
                ["scene"] = go.scene.name,
                ["parent"] = t.parent ? t.parent.name : null,
                ["position"] = t.position.ToJ(),
                ["localPosition"] = t.localPosition.ToJ(),
                ["rotationEuler"] = t.rotation.ToJEuler(),
                ["localScale"] = t.localScale.ToJ(),
                ["childCount"] = t.childCount,
                ["children"] = children,
                ["components"] = components
            });
        }
    }
}
