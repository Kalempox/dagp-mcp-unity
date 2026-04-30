using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;

namespace DAGP.MCP.Tools.Scene
{
    /// <summary>Saves the active scene (or all open scenes if 'all' = true).</summary>
    public class SceneSaveTool : IDAGPTool
    {
        public string Name => "scene.save";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var all = parameters.Value<bool?>("all") ?? false;

            bool ok;
            if (all)
            {
                ok = EditorSceneManager.SaveOpenScenes();
            }
            else
            {
                var scene = EditorSceneManager.GetActiveScene();
                ok = EditorSceneManager.SaveScene(scene);
            }

            return Task.FromResult<JToken>(new JObject
            {
                ["saved"] = ok,
                ["all"] = all,
                ["activeScene"] = EditorSceneManager.GetActiveScene().name
            });
        }
    }
}
