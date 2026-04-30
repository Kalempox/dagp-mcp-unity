using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DAGP.MCP.Tools
{
    /// <summary>One tool = one method exposed to MCP clients. Implementations live under Editor/Tools/.</summary>
    public interface IDAGPTool
    {
        /// <summary>Method name as it appears over the wire (e.g. "ping", "scene.get_info").</summary>
        string Name { get; }

        /// <summary>True if this tool must run on the Editor main thread (most do — anything touching Unity APIs).</summary>
        bool RequiresMainThread { get; }

        /// <summary>Execute the tool. Return JToken result; throw on user-facing errors.</summary>
        Task<JToken> Execute(JObject parameters);
    }
}
