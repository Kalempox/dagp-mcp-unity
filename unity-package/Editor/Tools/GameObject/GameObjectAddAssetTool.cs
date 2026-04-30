using System;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DAGP.MCP.Tools.GameObjectOps
{
    /// <summary>
    /// Adds an asset to the active scene. Either instantiates a prefab (assetPath) or
    /// creates a primitive (primitiveType: cube/sphere/capsule/cylinder/plane/quad).
    /// </summary>
    public class GameObjectAddAssetTool : IDAGPTool
    {
        public string Name => "gameobject.add_asset";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var assetPath = (string)parameters["assetPath"];
            var primitive = (string)parameters["primitiveType"];
            var name = (string)parameters["name"];
            var parentName = (string)parameters["parent"];
            var position = parameters["position"].ToVector3();

            GameObject instance;
            if (!string.IsNullOrEmpty(assetPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null) throw new ArgumentException($"prefab not found at: {assetPath}");
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            }
            else if (!string.IsNullOrEmpty(primitive))
            {
                if (!Enum.TryParse<PrimitiveType>(primitive, true, out var ptype))
                    throw new ArgumentException($"unknown primitiveType: {primitive}");
                instance = GameObject.CreatePrimitive(ptype);
            }
            else
            {
                instance = new GameObject();
            }

            if (!string.IsNullOrEmpty(name)) instance.name = name;
            instance.transform.position = position;

            if (!string.IsNullOrEmpty(parentName))
            {
                var parent = GameObjectFinder.Find(parentName);
                if (parent == null) throw new InvalidOperationException($"parent not found: {parentName}");
                instance.transform.SetParent(parent.transform, worldPositionStays: false);
            }

            Undo.RegisterCreatedObjectUndo(instance, $"Add {instance.name}");

            return Task.FromResult<JToken>(new JObject
            {
                ["created"] = true,
                ["name"] = instance.name,
                ["instanceId"] = instance.GetInstanceID(),
                ["position"] = instance.transform.position.ToJ()
            });
        }
    }
}
