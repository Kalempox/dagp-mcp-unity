using System;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DAGP.MCP.Tools.ComponentOps
{
    /// <summary>
    /// Add a component (componentType) and/or modify serialized fields (fields).
    /// componentType: short name ("Rigidbody") or full type name ("UnityEngine.Rigidbody").
    /// fields: { fieldName: value } — primitives + Vector3 ({x,y,z}) + Color ({r,g,b,a}) supported.
    /// </summary>
    public class ComponentUpdateTool : IDAGPTool
    {
        public string Name => "component.update";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var go = GameObjectFinder.Resolve(parameters);
            if (go == null) throw new InvalidOperationException("gameobject not found");

            var typeName = (string)parameters["componentType"] ?? throw new ArgumentException("'componentType' required");
            var add = parameters.Value<bool?>("add") ?? false;

            var type = ResolveComponentType(typeName);
            if (type == null) throw new ArgumentException($"component type not found: {typeName}");

            var component = go.GetComponent(type);
            if (component == null)
            {
                if (!add) throw new InvalidOperationException($"{typeName} not on {go.name} (pass add=true to add it)");
                component = Undo.AddComponent(go, type);
            }

            var changed = new JObject();
            if (parameters["fields"] is JObject fields)
            {
                var so = new SerializedObject(component);
                foreach (var kv in fields)
                {
                    var prop = so.FindProperty(kv.Key);
                    if (prop == null) { changed[kv.Key] = "<not found>"; continue; }
                    SetSerializedProperty(prop, kv.Value);
                    changed[kv.Key] = kv.Value;
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(component);
            }

            return Task.FromResult<JToken>(new JObject
            {
                ["updated"] = true,
                ["target"] = go.name,
                ["component"] = component.GetType().FullName,
                ["added"] = add,
                ["changes"] = changed
            });
        }

        static Type ResolveComponentType(string name)
        {
            var t = Type.GetType(name);
            if (t != null) return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType(name) ?? asm.GetType("UnityEngine." + name);
                if (t != null && typeof(Component).IsAssignableFrom(t)) return t;
            }
            return null;
        }

        static void SetSerializedProperty(SerializedProperty prop, JToken value)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer: prop.intValue = (int)value; break;
                case SerializedPropertyType.Float: prop.floatValue = (float)value; break;
                case SerializedPropertyType.Boolean: prop.boolValue = (bool)value; break;
                case SerializedPropertyType.String: prop.stringValue = (string)value; break;
                case SerializedPropertyType.Vector3: prop.vector3Value = value.ToVector3(); break;
                case SerializedPropertyType.Vector2: var v = value.ToVector3(); prop.vector2Value = new Vector2(v.x, v.y); break;
                case SerializedPropertyType.Color: prop.colorValue = value.ToColor(); break;
                case SerializedPropertyType.Quaternion: prop.quaternionValue = value.ToQuaternion(); break;
                case SerializedPropertyType.Enum: prop.enumValueIndex = (int)value; break;
                case SerializedPropertyType.LayerMask: prop.intValue = (int)value; break;
                default: prop.stringValue = value.ToString(); break;
            }
        }
    }
}
