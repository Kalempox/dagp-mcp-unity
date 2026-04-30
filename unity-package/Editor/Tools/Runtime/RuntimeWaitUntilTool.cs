using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DAGP.MCP.Tools.RuntimeOps
{
    /// <summary>
    /// Polls a condition every pollMs until true or timeoutMs elapses. Conditions:
    ///   gameobject_exists  — { name }
    ///   gameobject_active  — { name } (also active in hierarchy)
    ///   field_equals       — { name, componentType, field, value }
    ///   is_playing         — bool ; { value: true|false }
    ///   scene_loaded       — { sceneName }
    /// Returns waitMs and final state. Throws on timeout.
    /// </summary>
    public class RuntimeWaitUntilTool : IDAGPTool
    {
        public string Name => "runtime.wait_until";
        public bool RequiresMainThread => false;

        public async Task<JToken> Execute(JObject parameters)
        {
            var condition = (string)parameters["condition"] ?? throw new ArgumentException("'condition' required");
            var timeoutMs = parameters.Value<int?>("timeoutMs") ?? 5000;
            var pollMs = Math.Max(parameters.Value<int?>("pollMs") ?? 100, 25);

            var sw = Stopwatch.StartNew();
            int polls = 0;
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                bool met = false;
                var tcs = new TaskCompletionSource<bool>();
                Bridge.MainThreadDispatcher.Enqueue(() => {
                    try { tcs.SetResult(Check(condition, parameters)); }
                    catch (Exception ex) { tcs.SetException(ex); }
                });
                met = await tcs.Task;
                polls++;
                if (met) return new JObject
                {
                    ["satisfied"] = true,
                    ["condition"] = condition,
                    ["waitMs"] = sw.ElapsedMilliseconds,
                    ["polls"] = polls
                };
                await Task.Delay(pollMs);
            }
            throw new TimeoutException($"condition '{condition}' not met within {timeoutMs}ms ({polls} polls)");
        }

        static bool Check(string condition, JObject p)
        {
            switch (condition)
            {
                case "gameobject_exists":
                    return Utils.GameObjectFinder.Find((string)p["name"]) != null;

                case "gameobject_active":
                {
                    var go = Utils.GameObjectFinder.Find((string)p["name"]);
                    return go != null && go.activeInHierarchy;
                }
                case "is_playing":
                    return UnityEditor.EditorApplication.isPlaying == (p.Value<bool?>("value") ?? true);

                case "scene_loaded":
                {
                    var name = (string)p["sceneName"];
                    var s = UnityEngine.SceneManagement.SceneManager.GetSceneByName(name);
                    return s.IsValid() && s.isLoaded;
                }

                case "field_equals":
                {
                    var go = Utils.GameObjectFinder.Find((string)p["name"]);
                    if (go == null) return false;
                    var ct = (string)p["componentType"];
                    var fname = (string)p["field"];
                    var expected = p["value"]?.ToString();

                    // Special-case: read fields/properties directly on the GameObject (activeSelf, isStatic, tag, layer...)
                    if (ct == "GameObject")
                    {
                        var v = ReadMember(go, fname);
                        return string.Equals(v?.ToString(), expected, StringComparison.OrdinalIgnoreCase);
                    }

                    foreach (var c in go.GetComponents<Component>())
                    {
                        if (c == null || (c.GetType().Name != ct && c.GetType().FullName != ct)) continue;
                        var v = ReadMember(c, fname);
                        return string.Equals(v?.ToString(), expected, StringComparison.OrdinalIgnoreCase);
                    }
                    return false;
                }
                default: throw new ArgumentException($"unknown condition: {condition}");
            }
        }

        static object ReadMember(object obj, string name)
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var t = obj.GetType();
            var f = t.GetField(name, flags);
            if (f != null) return f.GetValue(obj);
            var prop = t.GetProperty(name, flags);
            return prop != null && prop.CanRead ? prop.GetValue(obj) : null;
        }
    }
}
