namespace Shos.DI.WebServer;

using System;
using System.Net;
using System.Text;

class SampleServer : IDisposable
{
    static readonly Encoding utf8           = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    static readonly string[] htmlMediaTypes = ["text/html", "text/*", "*/*"];

    readonly HttpListener  listener      = new();
    readonly WebAppManager webAppManager = new();

    public void Start(params string[] prefixes)
    {
        foreach (var prefix in prefixes)
            listener.Prefixes.Add(prefix);
        listener.Start();
        listener.BeginGetContext(OnRequested, null);
        Log($"Listening...\n{string.Join("\n", prefixes)}");
    }

    public void Stop()
    {
        listener.Stop();
        listener.Close();
    }

    public void Dispose() => Stop();

    protected virtual string? GetContent(HttpListenerRequest request)
        => AcceptsHtml(request) && request.Url is not null
           ? webAppManager.GetView(request.Url)
           : null;

    void OnRequested(IAsyncResult result)
    {
        HttpListenerContext context;
        try {
            if (!listener.IsListening)
                return;
            context = listener.EndGetContext(result);
            listener.BeginGetContext(OnRequested, null);
        } catch (Exception e) when (e is HttpListenerException or ObjectDisposedException or InvalidOperationException) {
            return; // The listener has been stopped.
        }

        try {
            ProcessRequest(context);
        } catch (Exception e) {
            ReturnInternalError(context.Response, e);
        }
    }

    // No Accept header means "anything is acceptable".
    static bool AcceptsHtml(HttpListenerRequest request)
        => request.AcceptTypes is null ||
           request.AcceptTypes.Select(acceptType => acceptType.Split(';')[0].Trim())
                              .Any(mediaType => htmlMediaTypes.Contains(mediaType, StringComparer.OrdinalIgnoreCase));

    static bool CanAccept(HttpMethod expected, string requested)
        => string.Equals(expected.Method, requested, StringComparison.OrdinalIgnoreCase);

    void ProcessRequest(HttpListenerContext context)
    {
        var request  = context.Request ;
        var response = context.Response;
        Log(request);

        if (request.IsWebSocketRequest) {
            Respond(response, HttpStatusCode.BadRequest, "WebSocket is not supported.");
            return;
        }
        if (!CanAccept(HttpMethod.Get, request.HttpMethod)) {
            response.AddHeader("Allow", HttpMethod.Get.Method);
            Respond(response, HttpStatusCode.MethodNotAllowed, "Method Not Allowed.");
            return;
        }

        var content = GetContent(request);
        if (content is null)
            Respond(response, HttpStatusCode.NotFound, "Not Found.");
        else
            Respond(response, HttpStatusCode.OK, content, "text/html");
    }

    static void Respond(HttpListenerResponse response, HttpStatusCode statusCode, string content, string mediaType = "text/plain")
    {
        response.StatusCode  = (int)statusCode;
        response.ContentType = $"{mediaType}; charset=utf-8";

        using (var writer = new StreamWriter(response.OutputStream, utf8))
            writer.WriteLine(content);

        response.Close();
        Log(response);
    }

    static void Log(HttpListenerRequest request)
    {
        Log($"Request:\n{request.HttpMethod} {request.Url}");

        foreach (var key in request.Headers.AllKeys) {
            if (key is not null)
                Log($"{key}: {request.Headers[key]}");
        }
        Log();
    }

    static void Log(HttpListenerResponse response)
    {
        Log($"Response:\n{response.StatusCode} {response.StatusDescription}");
        foreach (var key in response.Headers.AllKeys) {
            if (key is not null)
                Log($"{key}: {response.Headers[key]}");
        }
        Log();
    }

    static void Log(string log = "") => Console.WriteLine(log);

    static void ReturnInternalError(HttpListenerResponse response, Exception cause)
    {
        Console.Error.WriteLine(cause);
        try {
#if DEBUG
            // Like ASP.NET Core's developer exception page: show the details only in Debug builds.
            Respond(response, HttpStatusCode.InternalServerError, cause.ToString());
#else
            Respond(response, HttpStatusCode.InternalServerError, "Internal Server Error.");
#endif
        } catch (Exception e) {
            Console.Error.WriteLine(e);
            response.Abort();
        }
    }
}
