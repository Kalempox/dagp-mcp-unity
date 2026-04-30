using Newtonsoft.Json.Linq;

namespace DAGP.MCP.Bridge
{
    /// <summary>JSON-RPC 2.0 request envelope received from the Node MCP server.</summary>
    public class DAGPRequest
    {
        public string Jsonrpc { get; set; } = "2.0";
        public string Id { get; set; }
        public string Method { get; set; }
        public JObject Params { get; set; }
    }

    /// <summary>JSON-RPC 2.0 response envelope sent back to the Node MCP server.</summary>
    public class DAGPResponse
    {
        public string Jsonrpc { get; set; } = "2.0";
        public string Id { get; set; }
        public JToken Result { get; set; }
        public DAGPError Error { get; set; }

        public static DAGPResponse Ok(string id, JToken result) =>
            new DAGPResponse { Id = id, Result = result };

        public static DAGPResponse Fail(string id, int code, string message) =>
            new DAGPResponse { Id = id, Error = new DAGPError { Code = code, Message = message } };
    }

    public class DAGPError
    {
        public int Code { get; set; }
        public string Message { get; set; }
    }

    public static class DAGPErrorCodes
    {
        public const int ParseError = -32700;
        public const int InvalidRequest = -32600;
        public const int MethodNotFound = -32601;
        public const int InvalidParams = -32602;
        public const int InternalError = -32603;
    }
}
