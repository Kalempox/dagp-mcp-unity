using System.Threading.Tasks;
using DAGP.MCP.Services;
using Newtonsoft.Json.Linq;

namespace DAGP.MCP.Tools.ConsoleOps
{
    /// <summary>
    /// Returns recent Unity Console log entries (most recent last). Filter by type and substring.
    /// type: Log / Warning / Error / Assert / Exception (case-insensitive). count: 1..2000.
    /// </summary>
    public class ConsoleGetLogsTool : IDAGPTool
    {
        public string Name => "console.get_logs";
        public bool RequiresMainThread => false;

        public Task<JToken> Execute(JObject parameters)
        {
            var count = parameters.Value<int?>("count") ?? 50;
            var type = (string)parameters["type"];
            var contains = (string)parameters["contains"];
            var includeStack = parameters.Value<bool?>("includeStackTrace") ?? false;

            var entries = ConsoleLogBuffer.Snapshot(count, type, contains);
            var arr = new JArray();
            foreach (var e in entries)
            {
                var obj = new JObject
                {
                    ["time"] = e.Time.ToString("HH:mm:ss.fff"),
                    ["type"] = e.Type,
                    ["message"] = e.Message
                };
                if (includeStack) obj["stackTrace"] = e.StackTrace;
                arr.Add(obj);
            }

            return Task.FromResult<JToken>(new JObject
            {
                ["count"] = entries.Count,
                ["totalBuffered"] = ConsoleLogBuffer.Count,
                ["entries"] = arr
            });
        }
    }
}
