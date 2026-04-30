using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace DAGP.MCP.Tools.ProjectOps
{
    /// <summary>
    /// Adds a Unity package via Package Manager. identifier examples:
    /// 'com.unity.cinemachine', 'com.unity.cinemachine@2.9.7', 'file:./LocalPackage', 'https://github.com/.../package.git'
    /// </summary>
    public class ProjectAddPackageTool : IDAGPTool
    {
        public string Name => "project.add_package";
        public bool RequiresMainThread => true;

        public async Task<JToken> Execute(JObject parameters)
        {
            var id = (string)parameters["identifier"] ?? throw new ArgumentException("'identifier' required");
            var request = Client.Add(id);
            while (!request.IsCompleted) await Task.Delay(150);

            if (request.Status == StatusCode.Failure)
                throw new InvalidOperationException($"package add failed: {request.Error?.message}");

            return new JObject
            {
                ["added"] = true,
                ["packageId"] = request.Result?.packageId,
                ["name"] = request.Result?.name,
                ["version"] = request.Result?.version,
                ["source"] = request.Result?.source.ToString()
            };
        }
    }
}
