using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DAGP.MCP.Tools.ConsoleOps
{
    /// <summary>Writes a message to Unity Console. type: Log (default) / Warning / Error.</summary>
    public class ConsoleSendLogTool : IDAGPTool
    {
        public string Name => "console.send_log";
        public bool RequiresMainThread => false;

        public Task<JToken> Execute(JObject parameters)
        {
            var message = (string)parameters["message"] ?? throw new ArgumentException("'message' required");
            var type = ((string)parameters["type"] ?? "Log").ToLowerInvariant();
            switch (type)
            {
                case "warning": Debug.LogWarning(message); break;
                case "error":   Debug.LogError(message);   break;
                default:        Debug.Log(message);        break;
            }
            return Task.FromResult<JToken>(new JObject { ["sent"] = true, ["type"] = type, ["message"] = message });
        }
    }
}
