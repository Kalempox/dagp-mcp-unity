using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor.TestTools.TestRunner.Api;

namespace DAGP.MCP.Tools.ConsoleOps
{
    /// <summary>
    /// Runs Unity Test Framework tests. mode: 'EditMode' (default) or 'PlayMode'. testFilter: substring match on test name.
    /// </summary>
    public class EditorRunTestsTool : IDAGPTool
    {
        public string Name => "editor.run_tests";
        public bool RequiresMainThread => true;

        public async Task<JToken> Execute(JObject parameters)
        {
            var modeStr = ((string)parameters["mode"] ?? "EditMode");
            var testFilter = (string)parameters["testFilter"];
            var mode = modeStr.Equals("PlayMode", System.StringComparison.OrdinalIgnoreCase)
                ? TestMode.PlayMode : TestMode.EditMode;

            var api = UnityEngine.ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter { testMode = mode };
            if (!string.IsNullOrEmpty(testFilter)) filter.testNames = new[] { testFilter };

            var collector = new ResultCollector();
            api.RegisterCallbacks(collector);
            api.Execute(new ExecutionSettings(filter));

            while (!collector.Done) await Task.Delay(150);
            api.UnregisterCallbacks(collector);

            return new JObject
            {
                ["mode"] = modeStr,
                ["filter"] = testFilter ?? "(all)",
                ["passed"] = collector.Passed,
                ["failed"] = collector.Failed,
                ["skipped"] = collector.Skipped,
                ["total"] = collector.Passed + collector.Failed + collector.Skipped,
                ["failures"] = JArray.FromObject(collector.Failures)
            };
        }

        class ResultCollector : ICallbacks
        {
            public bool Done;
            public int Passed, Failed, Skipped;
            public readonly List<string> Failures = new List<string>();
            public void RunStarted(ITestAdaptor _) { }
            public void TestStarted(ITestAdaptor _) { }
            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.HasChildren) return; // only leaf tests count
                if (result.TestStatus == TestStatus.Passed) Passed++;
                else if (result.TestStatus == TestStatus.Skipped || result.TestStatus == TestStatus.Inconclusive) Skipped++;
                else { Failed++; Failures.Add($"{result.FullName}: {result.Message}"); }
            }
            public void RunFinished(ITestResultAdaptor _) { Done = true; }
        }
    }
}
