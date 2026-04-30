using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DAGP.MCP.Utils
{
    /// <summary>Locates GameObjects by name, path ('Parent/Child'), or InstanceID across loaded scenes.</summary>
    public static class GameObjectFinder
    {
        /// <summary>Find by 'Parent/Child' path or by name. Returns null if not found.</summary>
        public static GameObject Find(string nameOrPath)
        {
            if (string.IsNullOrEmpty(nameOrPath)) return null;
            var direct = GameObject.Find(nameOrPath);
            if (direct != null) return direct;

            // Fallback: scan all roots in active scene by exact name match
            var scene = EditorSceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                var match = FindRecursive(root.transform, nameOrPath);
                if (match != null) return match.gameObject;
            }
            return null;
        }

        /// <summary>Resolve by InstanceID (preferred when ambiguous names exist).</summary>
        public static GameObject FindByInstanceId(int instanceId)
        {
            var obj = UnityEditor.EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            return obj;
        }

        static Transform FindRecursive(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var hit = FindRecursive(t.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }

        /// <summary>Resolve from JObject params: prefers 'instanceId', falls back to 'name' or 'path'.</summary>
        public static GameObject Resolve(Newtonsoft.Json.Linq.JObject parameters)
        {
            var instanceId = parameters.Value<int?>("instanceId");
            if (instanceId.HasValue) return FindByInstanceId(instanceId.Value);
            var nameOrPath = (string)parameters["name"] ?? (string)parameters["path"];
            return Find(nameOrPath);
        }
    }
}
