using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DAGP.MCP.Tools.BatchOps
{
    /// <summary>
    /// Runs N tool calls sequentially in one round-trip. Returns per-step result.
    /// stopOnError: true halts on first failure (default). Each step: { method, params }.
    /// </summary>
    public class BatchExecuteTool : IDAGPTool
    {
        public string Name => "batch.execute";
        public bool RequiresMainThread => true;

        public async Task<JToken> Execute(JObject parameters)
        {
            var stepsToken = parameters["steps"] as JArray ?? throw new ArgumentException("'steps' (array) required");
            var stopOnError = parameters.Value<bool?>("stopOnError") ?? true;

            var results = new JArray();
            int passed = 0, failed = 0;

            foreach (var step in stepsToken)
            {
                var method = (string)step["method"];
                var stepParams = step["params"] as JObject ?? new JObject();
                if (string.IsNullOrEmpty(method))
                {
                    results.Add(new JObject { ["ok"] = false, ["error"] = "missing 'method'" });
                    failed++;
                    if (stopOnError) break;
                    continue;
                }

                if (!Tools.ToolRegistry.TryGet(method, out var tool))
                {
                    results.Add(new JObject { ["ok"] = false, ["method"] = method, ["error"] = "unknown tool" });
                    failed++;
                    if (stopOnError) break;
                    continue;
                }

                try
                {
                    var result = await tool.Execute(stepParams);
                    results.Add(new JObject { ["ok"] = true, ["method"] = method, ["result"] = result });
                    passed++;
                }
                catch (Exception ex)
                {
                    results.Add(new JObject { ["ok"] = false, ["method"] = method, ["error"] = ex.Message });
                    failed++;
                    if (stopOnError) break;
                }
            }

            return new JObject
            {
                ["passed"] = passed,
                ["failed"] = failed,
                ["total"] = passed + failed,
                ["results"] = results
            };
        }
    }
}
