namespace Shos.ID.WebServer;

using System.Net.WebSockets;
using System.Net;
using System.Text;

class SampleServer
{
    HttpListener listener;

    public void Start(params string[] prefixes)
    {
        listener = new HttpListener();
        foreach (var prefix in prefixes)
            listener.Prefixes.Add(prefix);
        listener.Start();
        listener.BeginGetContext(OnRequested, null);
    }

    void OnRequested(IAsyncResult ar)
    {
        if (!listener.IsListening)
            return;

        HttpListenerContext context = listener.EndGetContext(ar);
        listener.BeginGetContext(OnRequested, listener);

        try {
            if (ProcessGetRequest(context))
                return;
            if (ProcessPostRequest(context))
                return;
            if (ProcessWebSocketRequest(context))
                return;
        } catch (Exception e) {
            ReturnInternalError(context.Response, e);
        }
    }

    static bool CanAccept(HttpMethod expected, string requested)
        => string.Equals(expected.Method, requested, StringComparison.CurrentCultureIgnoreCase);

    static bool ProcessGetRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        if (!CanAccept(HttpMethod.Get, request.HttpMethod) || request.IsWebSocketRequest)
            return false;

        response.StatusCode = (int)HttpStatusCode.OK;
        using (var writer = new StreamWriter(response.OutputStream, Encoding.UTF8))
            writer.WriteLine($"you have sent headers:\n{request.Url}\n{request.Headers}");
        response.Close();
        return true;
    }

    static bool ProcessPostRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        if (!CanAccept(HttpMethod.Post, request.HttpMethod))
            return false;

        response.StatusCode = (int)HttpStatusCode.OK;
        using (var writer = new StreamWriter(response.OutputStream, Encoding.UTF8))
            writer.WriteLine($"you have sent headers:\n{request.Headers}");
        response.Close();
        return true;
    }

    static bool ProcessWebSocketRequest(HttpListenerContext context)
    {
        if (!context.Request.IsWebSocketRequest)
            return false;

        WebSocket webSocket = context.AcceptWebSocketAsync(null).Result.WebSocket;
        ProcessReceivedMessage(webSocket, message =>
        {
            webSocket.SendAsync(
                Encoding.UTF8.GetBytes($"you have sent message:\n{message}"),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        });

        return true;
    }

    static async void ProcessReceivedMessage(WebSocket webSocket, Action<string> onMessage)
    {
        var buffer = new ArraySegment<byte>(new byte[1024]);
        while (webSocket.State == WebSocketState.Open) {
            WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(
                buffer,
                CancellationToken.None);
            if (receiveResult.MessageType == WebSocketMessageType.Text) {
                var message = Encoding.UTF8.GetString(
                    buffer
                        .Slice(0, receiveResult.Count)
                        .ToArray());
                onMessage.Invoke(message);
            }
        }
    }

    static void ReturnInternalError(HttpListenerResponse response, Exception cause)
    {
        Console.Error.WriteLine(cause);
        response.StatusCode = (int)HttpStatusCode.InternalServerError;
        response.ContentType = "text/plain";
        try {
            using (var writer = new StreamWriter(response.OutputStream, Encoding.UTF8))
                writer.Write(cause.ToString());
            response.Close();
        } catch (Exception e) {
            Console.Error.WriteLine(e);
            response.Abort();
        }
    }

    public void Stop()
    {
        listener.Stop();
        listener.Close();
    }
}
