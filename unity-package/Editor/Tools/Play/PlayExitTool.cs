using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace DAGP.MCP.Tools.PlayOps
{
    /// <summary>Requests Unity to exit Play Mode. Returns immediately.</summary>
    public class PlayExitTool : IDAGPTool
    {
        public string Name => "play.exit";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            if (!EditorApplication.isPlaying)
                return Task.FromResult<JToken>(new JObject { ["alreadyStopped"] = true });

            EditorApplication.isPlaying = false;
            return Task.FromResult<JToken>(new JObject
            {
                ["requested"] = true,
                ["note"] = "Exit requested. Poll play.is_playing for confirmation."
            });
        }
    }
}
