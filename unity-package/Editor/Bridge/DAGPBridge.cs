using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DAGP.MCP.Settings;
using DAGP.MCP.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DAGP.MCP.Bridge
{
    /// <summary>TCP server that receives newline-delimited JSON-RPC tool calls from the Node MCP server.</summary>
    [InitializeOnLoad]
    public static class DAGPBridge
    {
        static TcpListener _listener;
        static CancellationTokenSource _cts;
        static TcpClient _client;
        static bool _running;
        static string _lastError;
        static DateTime _lastMessageAt;

        public static bool IsRunning => _running;
        public static int Port => DAGPSettings.Port;
        public static string LastError => _lastError;
        public static DateTime LastMessageAt => _lastMessageAt;

        static DAGPBridge()
        {
            EditorApplication.quitting += Stop;
            // Domain reload tears down all static state; Stop() releases the listener
            // cleanly before reload, then InitializeOnLoad re-runs and AutoStart restarts us.
            AssemblyReloadEvents.beforeAssemblyReload += StopForReload;
            AssemblyReloadEvents.afterAssemblyReload += AfterReload;
            if (DAGPSettings.AutoStart) EditorApplication.delayCall += Start;
        }

        public static void Start()
        {
            if (_running) return;
            try
            {
                _cts = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Loopback, Port);
                _listener.Start();
                _running = true;
                _lastError = null;
                Debug.Log($"[DAGP-MCP] Bridge listening on tcp://127.0.0.1:{Port}/ (newline-delimited JSON-RPC)");
                _ = AcceptLoop(_cts.Token);
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                _running = false;
                Debug.LogError($"[DAGP-MCP] Bridge start failed: {ex.Message}");
                // Don't give up — try again shortly. Common cause: port still in TIME_WAIT
                // from a previous editor instance, or AutoStart racing with other init.
                EditorApplication.delayCall += () => { if (!_running && DAGPSettings.AutoStart) Start(); };
            }
        }

        static void StopForReload()
        {
            Debug.Log("[DAGP-MCP] Bridge stopping (assembly reload — will auto-restart).");
            Stop();
        }

        static void AfterReload()
        {
            // Belt-and-suspenders: InitializeOnLoad already runs Start via delayCall,
            // but if AutoStart was toggled off then on, this still picks it up.
            if (!_running && DAGPSettings.AutoStart) EditorApplication.delayCall += Start;
        }

        public static void Stop()
        {
            if (!_running) return;
            try { _cts?.Cancel(); } catch { }
            try { _listener?.Stop(); } catch { }
            try { _client?.Close(); } catch { }
            _client = null;
            _listener = null;
            _cts = null;
            _running = false;
            Debug.Log("[DAGP-MCP] Bridge stopped.");
        }

        static async Task AcceptLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _listener != null)
            {
                TcpClient client;
                try { client = await _listener.AcceptTcpClientAsync(); }
                catch (ObjectDisposedException) { break; }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    // Transient accept failure — log and keep listening.
                    if (ct.IsCancellationRequested) break;
                    Debug.LogWarning($"[DAGP-MCP] Accept failed (continuing): {ex.Message}");
                    try { await Task.Delay(250, ct); } catch { break; }
                    continue;
                }

                _client = client;
                Debug.Log($"[DAGP-MCP] Client connected from {client.Client.RemoteEndPoint}.");
                _ = HandleClient(client, ct);
            }
        }

        static async Task HandleClient(TcpClient client, CancellationToken ct)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" })
                {
                    while (!ct.IsCancellationRequested && client.Connected)
                    {
                        string line;
                        try { line = await reader.ReadLineAsync(); }
                        catch { break; }
                        if (line == null) break;
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        _lastMessageAt = DateTime.Now;
                        var response = await DispatchMessage(line);
                        var json = JsonConvert.SerializeObject(response, new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                        });
                        await writer.WriteLineAsync(json);
                    }
                }
            }
            catch (Exception ex)
            {
                if (IsExpectedDisconnect(ex)) { /* normal client close on Windows */ }
                else { _lastError = ex.Message; Debug.LogWarning($"[DAGP-MCP] Client error: {ex.Message}"); }
            }
            Debug.Log("[DAGP-MCP] Client disconnected.");
        }

        static bool IsExpectedDisconnect(Exception ex)
        {
            var msg = ex.Message ?? string.Empty;
            return msg.Contains("forcibly closed") || msg.Contains("transport connection")
                || msg.Contains("aborted") || msg.Contains("iptal edildi")
                || ex is System.IO.IOException || ex is SocketException;
        }

        static async Task<DAGPResponse> DispatchMessage(string raw)
        {
            DAGPRequest req = null;
            try { req = JsonConvert.DeserializeObject<DAGPRequest>(raw); }
            catch (Exception ex) { return DAGPResponse.Fail(null, DAGPErrorCodes.ParseError, ex.Message); }

            if (req == null || string.IsNullOrEmpty(req.Method))
                return DAGPResponse.Fail(req?.Id, DAGPErrorCodes.InvalidRequest, "missing method");

            // Built-in introspection: list every registered tool. Useful when MCP-side and Unity-side disagree.
            if (req.Method == "system.list_tools")
            {
                var arr = new JArray();
                foreach (var n in ToolRegistry.Names) arr.Add(n);
                return DAGPResponse.Ok(req.Id, new JObject { ["count"] = arr.Count, ["tools"] = arr });
            }

            if (!ToolRegistry.TryGet(req.Method, out var tool))
            {
                var available = string.Join(", ", ToolRegistry.Names);
                return DAGPResponse.Fail(req.Id, DAGPErrorCodes.MethodNotFound,
                    $"unknown tool: {req.Method}. Available ({ToolRegistry.Names.Count()}): {available}");
            }

            try
            {
                JToken result;
                if (tool.RequiresMainThread)
                {
                    var tcs = new TaskCompletionSource<JToken>();
                    MainThreadDispatcher.Enqueue(async () =>
                    {
                        try { tcs.SetResult(await tool.Execute(req.Params ?? new JObject())); }
                        catch (Exception ex) { tcs.SetException(ex); }
                    });
                    result = await tcs.Task;
                }
                else
                {
                    result = await tool.Execute(req.Params ?? new JObject());
                }
                return DAGPResponse.Ok(req.Id, result);
            }
            catch (Exception ex)
            {
                return DAGPResponse.Fail(req.Id, DAGPErrorCodes.InternalError, ex.Message);
            }
        }
    }
}
