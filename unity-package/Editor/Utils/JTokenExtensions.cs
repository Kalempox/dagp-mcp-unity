using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DAGP.MCP.Utils
{
    /// <summary>Conversions between JToken and Unity types (Vector3, Quaternion, Color).</summary>
    public static class JTokenExtensions
    {
        public static JObject ToJ(this Vector3 v) => new JObject { ["x"] = v.x, ["y"] = v.y, ["z"] = v.z };
        public static JObject ToJ(this Quaternion q) => new JObject { ["x"] = q.x, ["y"] = q.y, ["z"] = q.z, ["w"] = q.w };
        public static JObject ToJEuler(this Quaternion q) { var e = q.eulerAngles; return e.ToJ(); }
        public static JObject ToJ(this Color c) => new JObject { ["r"] = c.r, ["g"] = c.g, ["b"] = c.b, ["a"] = c.a };

        public static Vector3 ToVector3(this JToken t, Vector3 fallback = default)
        {
            if (t == null || t.Type == JTokenType.Null) return fallback;
            if (t is JArray a && a.Count >= 3) return new Vector3((float)a[0], (float)a[1], (float)a[2]);
            return new Vector3(t.Value<float?>("x") ?? fallback.x, t.Value<float?>("y") ?? fallback.y, t.Value<float?>("z") ?? fallback.z);
        }

        public static Quaternion ToQuaternion(this JToken t, Quaternion fallback = default)
        {
            if (t == null || t.Type == JTokenType.Null) return fallback;
            if (t.Value<float?>("w").HasValue)
                return new Quaternion(t.Value<float>("x"), t.Value<float>("y"), t.Value<float>("z"), t.Value<float>("w"));
            return Quaternion.Euler(t.ToVector3());
        }

        public static Color ToColor(this JToken t, Color fallback = default)
        {
            if (t == null || t.Type == JTokenType.Null) return fallback;
            return new Color(t.Value<float?>("r") ?? fallback.r, t.Value<float?>("g") ?? fallback.g,
                             t.Value<float?>("b") ?? fallback.b, t.Value<float?>("a") ?? 1f);
        }
    }
}
