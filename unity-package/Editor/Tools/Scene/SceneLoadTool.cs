using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;

namespace DAGP.MCP.Tools.Scene
{
    /// <summary>Loads a scene by asset path. Mode: 'single' (default) or 'additive'.</summary>
    public class SceneLoadTool : IDAGPTool
    {
        public string Name => "scene.load";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var path = (string)parameters["path"] ?? throw new ArgumentException("'path' required");
            var modeStr = ((string)parameters["mode"] ?? "single").ToLowerInvariant();
            var saveCurrent = parameters.Value<bool?>("saveCurrent") ?? false;

            if (saveCurrent && EditorSceneManager.GetActiveScene().isDirty)
                EditorSceneManager.SaveOpenScenes();

            var mode = modeStr == "additive" ? OpenSceneMode.Additive : OpenSceneMode.Single;
            var scene = EditorSceneManager.OpenScene(path, mode);

            return Task.FromResult<JToken>(new JObject
            {
                ["loaded"] = scene.IsValid(),
                ["name"] = scene.name,
                ["path"] = scene.path,
                ["mode"] = modeStr
            });
        }
    }
}
