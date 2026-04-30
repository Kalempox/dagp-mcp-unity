using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DAGP.MCP.Tools.PlayOps
{
    /// <summary>Returns the editor play state (playing, paused, compiling) and runtime time stats.</summary>
    public class PlayIsPlayingTool : IDAGPTool
    {
        public string Name => "play.is_playing";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            return Task.FromResult<JToken>(new JObject
            {
                ["isPlaying"] = EditorApplication.isPlaying,
                ["isPaused"] = EditorApplication.isPaused,
                ["isCompiling"] = EditorApplication.isCompiling,
                ["isUpdating"] = EditorApplication.isUpdating,
                ["playModeStateChanged"] = EditorApplication.isPlayingOrWillChangePlaymode,
                ["timeSinceStartup"] = EditorApplication.timeSinceStartup,
                ["realTimeSinceStartup"] = Time.realtimeSinceStartup,
                ["frameCount"] = Time.frameCount
            });
        }
    }
}
