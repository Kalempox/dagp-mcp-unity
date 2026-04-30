using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DAGP.MCP.Tools.RuntimeOps
{
    /// <summary>
    /// Invokes a public method on a MonoBehaviour at runtime. Bypasses input layer entirely — useful for
    /// directly triggering game actions (e.g. ScoreManager.AddPoints(10), PlayerController.Jump()).
    /// args: array of JSON values matched positionally to the method signature.
    /// </summary>
    public class RuntimeInvokeMethodTool : IDAGPTool
    {
        public string Name => "runtime.invoke_method";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var go = GameObjectFinder.Resolve(parameters);
            if (go == null) throw new InvalidOperationException("gameobject not found");

            var componentType = (string)parameters["componentType"] ?? throw new ArgumentException("'componentType' required");
            var methodName = (string)parameters["method"] ?? throw new ArgumentException("'method' required");
            var jArgs = parameters["args"] as JArray ?? new JArray();

            var component = go.GetComponents<Component>().FirstOrDefault(c =>
                c != null && (c.GetType().Name == componentType || c.GetType().FullName == componentType));
            if (component == null) throw new InvalidOperationException($"{componentType} not found on {go.name}");

            var method = ResolveMethod(component.GetType(), methodName, jArgs.Count);
            if (method == null) throw new InvalidOperationException($"no public method '{methodName}({jArgs.Count} args)' on {component.GetType().Name}");

            var paramInfos = method.GetParameters();
            var args = new object[paramInfos.Length];
            for (int i = 0; i < paramInfos.Length; i++)
                args[i] = ConvertArg(jArgs[i], paramInfos[i].ParameterType);

            object result;
            try { result = method.Invoke(component, args); }
            catch (TargetInvocationException ex) { throw ex.InnerException ?? ex; }

            return Task.FromResult<JToken>(new JObject
            {
                ["invoked"] = true,
                ["target"] = go.name,
                ["component"] = component.GetType().FullName,
                ["method"] = methodName,
                ["argCount"] = args.Length,
                ["returned"] = result == null ? null : JToken.FromObject(result.ToString()),
                ["returnType"] = method.ReturnType.FullName
            });
        }

        static MethodInfo ResolveMethod(Type t, string name, int argCount)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            return t.GetMethods(flags).FirstOrDefault(m => m.Name == name && m.GetParameters().Length == argCount);
        }

        static object ConvertArg(JToken token, Type targetType)
        {
            if (token == null || token.Type == JTokenType.Null) return null;
            if (targetType == typeof(string)) return (string)token;
            if (targetType == typeof(int))    return (int)token;
            if (targetType == typeof(float))  return (float)token;
            if (targetType == typeof(double)) return (double)token;
            if (targetType == typeof(bool))   return (bool)token;
            if (targetType == typeof(Vector3)) return token.ToVector3();
            if (targetType == typeof(Vector2)) { var v = token.ToVector3(); return new Vector2(v.x, v.y); }
            if (targetType == typeof(Color))  return token.ToColor();
            if (targetType.IsEnum) return Enum.Parse(targetType, (string)token, true);
            return token.ToObject(targetType);
        }
    }
}
