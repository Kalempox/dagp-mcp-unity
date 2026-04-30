using System;
using System.Reflection;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DAGP.MCP.Tools.RuntimeOps
{
    /// <summary>
    /// Reads a field or property from a MonoBehaviour at runtime. Works in Play AND Edit mode.
    /// Path: 'componentType.field' (e.g. 'ScoreManager.currentScore', 'PlayerHealth.hp').
    /// Supports public + private (NonPublic) — DAGP needs visibility for QA.
    /// </summary>
    public class RuntimeGetFieldTool : IDAGPTool
    {
        public string Name => "runtime.get_field";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var go = GameObjectFinder.Resolve(parameters);
            if (go == null) throw new InvalidOperationException("gameobject not found");

            var componentType = (string)parameters["componentType"] ?? throw new ArgumentException("'componentType' required");
            var fieldName = (string)parameters["field"] ?? throw new ArgumentException("'field' required");

            var component = ResolveComponent(go, componentType);
            if (component == null) throw new InvalidOperationException($"{componentType} not found on {go.name}");

            var (kind, value) = ReadMember(component, fieldName);
            return Task.FromResult<JToken>(new JObject
            {
                ["target"] = go.name,
                ["component"] = component.GetType().FullName,
                ["field"] = fieldName,
                ["kind"] = kind,
                ["value"] = value == null ? null : JToken.FromObject(SafeToString(value))
            });
        }

        static Component ResolveComponent(GameObject go, string typeName)
        {
            foreach (var c in go.GetComponents<Component>())
                if (c != null && (c.GetType().Name == typeName || c.GetType().FullName == typeName)) return c;
            return null;
        }

        static (string kind, object value) ReadMember(object obj, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var t = obj.GetType();
            var f = t.GetField(name, flags);
            if (f != null) return ("field", f.GetValue(obj));
            var p = t.GetProperty(name, flags);
            if (p != null && p.CanRead) return ("property", p.GetValue(obj));
            throw new InvalidOperationException($"no field or readable property '{name}' on {t.Name}");
        }

        static object SafeToString(object v)
        {
            if (v == null) return null;
            if (v is Vector3 v3) return new { x = v3.x, y = v3.y, z = v3.z };
            if (v is Vector2 v2) return new { x = v2.x, y = v2.y };
            if (v is Color c) return new { r = c.r, g = c.g, b = c.b, a = c.a };
            if (v is Quaternion q) return new { x = q.x, y = q.y, z = q.z, w = q.w };
            if (v is UnityEngine.Object u) return u != null ? u.name : null;
            if (v.GetType().IsPrimitive || v is string || v is bool) return v;
            if (v is System.Collections.IEnumerable && !(v is string))
            {
                var arr = new System.Collections.Generic.List<object>();
                foreach (var item in (System.Collections.IEnumerable)v) arr.Add(SafeToString(item));
                return arr;
            }
            return v.ToString();
        }
    }
}
