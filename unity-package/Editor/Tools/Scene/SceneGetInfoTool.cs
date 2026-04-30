using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DAGP.MCP.Tools.Scene
{
    /// <summary>Returns active scene name, path, root GameObject names, build index, dirty flag.</summary>
    public class SceneGetInfoTool : IDAGPTool
    {
        public string Name => "scene.get_info";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var scene = EditorSceneManager.GetActiveScene();
            var roots = new JArray();
            foreach (var go in scene.GetRootGameObjects())
            {
                roots.Add(new JObject
                {
                    ["name"] = go.name,
                    ["active"] = go.activeSelf,
                    ["childCount"] = go.transform.childCount
                });
            }

            var result = new JObject
            {
                ["name"] = scene.name,
                ["path"] = scene.path,
                ["buildIndex"] = scene.buildIndex,
                ["isDirty"] = scene.isDirty,
                ["isLoaded"] = scene.isLoaded,
                ["rootCount"] = scene.rootCount,
                ["loadedSceneCount"] = SceneManager.sceneCount,
                ["rootGameObjects"] = roots
            };
            return Task.FromResult<JToken>(result);
        }
    }
}
