using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DAGP.MCP.Tools
{
    /// <summary>End-to-end pipeline check. Returns Unity version, platform, active scene name.</summary>
    public class PingTool : IDAGPTool
    {
        public string Name => "ping";
        public bool RequiresMainThread => true;


        public Task<JToken> Execute(JObject parameters)
        {
            var result = new JObject
            {
                ["pong"] = true,
                ["unityVersion"] = Application.unityVersion,
                ["editorPlatform"] = Application.platform.ToString(),
                ["activeScene"] = EditorSceneManager.GetActiveScene().name,
                ["isPlaying"] = EditorApplication.isPlaying,
                ["packageVersion"] = "0.1.0"
            };
            return Task.FromResult<JToken>(result);
        }
    }
}
