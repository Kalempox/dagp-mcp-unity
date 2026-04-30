using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace DAGP.MCP.Tools.PlayOps
{
    /// <summary>
    /// Requests Unity to enter Play Mode. Returns immediately — actual transition is async.
    /// IMPORTANT: Default Unity settings reload the domain on Play, which briefly disconnects this bridge.
    /// To keep the bridge alive, enable 'Edit > Project Settings > Editor > Enter Play Mode Options
    ///   (Disable Domain Reload + Disable Scene Reload)'.
    /// Caller should poll play.is_playing until isPlaying=true (typical wait 1-3s with reload, &lt;100ms without).
    /// </summary>
    public class PlayEnterTool : IDAGPTool
    {
        public string Name => "play.enter";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            if (EditorApplication.isPlaying)
                return Task.FromResult<JToken>(new JObject { ["alreadyPlaying"] = true });

            var domainReloadDisabled = EditorSettings.enterPlayModeOptionsEnabled
                && (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) != 0;

            EditorApplication.isPlaying = true;

            return Task.FromResult<JToken>(new JObject
            {
                ["requested"] = true,
                ["domainReloadDisabled"] = domainReloadDisabled,
                ["note"] = domainReloadDisabled
                    ? "Bridge will survive transition. Poll play.is_playing for confirmation."
                    : "Domain will reload — bridge briefly disconnects (~1-3s). Reconnect, then poll play.is_playing."
            });
        }
    }
}
