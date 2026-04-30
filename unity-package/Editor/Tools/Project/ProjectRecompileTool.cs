using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Compilation;

namespace DAGP.MCP.Tools.ProjectOps
{
    /// <summary>Forces a script recompile + asset refresh. Use after editing C# files outside Unity.</summary>
    public class ProjectRecompileTool : IDAGPTool
    {
        public string Name => "project.recompile";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            AssetDatabase.Refresh();
            CompilationPipeline.RequestScriptCompilation();
            return Task.FromResult<JToken>(new JObject
            {
                ["requested"] = true,
                ["note"] = "Recompile queued. Tools/scenes will be unavailable for ~1-3s during reload."
            });
        }
    }
}
