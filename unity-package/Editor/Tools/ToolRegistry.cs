using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DAGP.MCP.Tools
{
    /// <summary>Auto-discovers every IDAGPTool in the Editor assembly and registers it by Name.</summary>
    [InitializeOnLoad]
    public static class ToolRegistry
    {
        static readonly Dictionary<string, IDAGPTool> _tools = new Dictionary<string, IDAGPTool>();

        static ToolRegistry()
        {
            DiscoverTools();
        }

        static void DiscoverTools()
        {
            _tools.Clear();
            var asm = Assembly.GetExecutingAssembly();
            var toolTypes = asm.GetTypes()
                .Where(t => typeof(IDAGPTool).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in toolTypes)
            {
                try
                {
                    var instance = (IDAGPTool)Activator.CreateInstance(type);
                    if (_tools.ContainsKey(instance.Name))
                    {
                        Debug.LogWarning($"[DAGP-MCP] Duplicate tool name: {instance.Name} ({type.Name})");
                        continue;
                    }
                    _tools[instance.Name] = instance;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DAGP-MCP] Failed to register {type.Name}: {ex.Message}");
                }
            }

            Debug.Log($"[DAGP-MCP] Registered {_tools.Count} tool(s): {string.Join(", ", _tools.Keys)}");
        }

        public static bool TryGet(string name, out IDAGPTool tool) => _tools.TryGetValue(name, out tool);

        public static IEnumerable<string> Names => _tools.Keys;
    }
}
