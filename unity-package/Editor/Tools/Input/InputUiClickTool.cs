using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DAGP.MCP.Tools.InputOps
{
    /// <summary>
    /// Triggers a UI click on a UGUI element. Two modes:
    ///   1) by GameObject name/path — direct ExecuteEvents.pointerClick on that target
    ///   2) by screen coordinates — EventSystem raycasts at (x,y), clicks topmost hit
    /// Works in Edit Mode AND Play Mode. Requires an EventSystem in the scene (most UGUI scenes have one).
    /// </summary>
    public class InputUiClickTool : IDAGPTool
    {
        public string Name => "input.ui_click";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var es = EventSystem.current;
            if (es == null) throw new InvalidOperationException("no EventSystem in scene — UGUI input requires one");

            GameObject target = GameObjectFinder.Resolve(parameters);
            var raycastResults = new List<RaycastResult>();
            float? screenX = parameters.Value<float?>("x");
            float? screenY = parameters.Value<float?>("y");

            if (target == null && screenX.HasValue && screenY.HasValue)
            {
                var pd = new PointerEventData(es) { position = new Vector2(screenX.Value, screenY.Value) };
                es.RaycastAll(pd, raycastResults);
                if (raycastResults.Count > 0) target = raycastResults[0].gameObject;
            }

            if (target == null) throw new InvalidOperationException("no UI target found (provide name or x+y)");

            var pointer = new PointerEventData(es)
            {
                position = screenX.HasValue && screenY.HasValue ? new Vector2(screenX.Value, screenY.Value) : Vector2.zero,
                button = PointerEventData.InputButton.Left,
                clickCount = 1,
                clickTime = Time.unscaledTime
            };

            // Full press → release → click cycle
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerUpHandler);
            var clicked = ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);

            return Task.FromResult<JToken>(new JObject
            {
                ["clicked"] = clicked,
                ["target"] = target.name,
                ["instanceId"] = target.GetInstanceID(),
                ["raycastHits"] = raycastResults.Count
            });
        }
    }
}
