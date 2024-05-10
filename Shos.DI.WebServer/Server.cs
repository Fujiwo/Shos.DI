namespace Shos.DI.WebServer;

using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;

class SampleServer : IDisposable
{
    readonly HttpListener listener = new();

    public void Start(params string[] prefixes)
    {
        foreach (var prefix in prefixes)
            listener.Prefixes.Add(prefix);
        listener.Start();
        listener.BeginGetContext(OnRequested, null);
    }

    protected virtual string? GetContent(HttpListenerRequest request)
        => new WebAppManager().GetView(request);

    void OnRequested(IAsyncResult result)
    {
        if (!listener.IsListening)
            return;

        HttpListenerContext context = listener.EndGetContext(result);
        listener.BeginGetContext(OnRequested, listener);

        try {
            if (ProcessGetRequest(context))
                return;
        } catch (Exception e) {
            ReturnInternalError(context.Response, e);
        }
    }

    static bool CanAccept(HttpMethod expected, string requested)
        => string.Equals(expected.Method, requested, StringComparison.CurrentCultureIgnoreCase);

    bool ProcessGetRequest(HttpListenerContext context)
    {
        var request         = context.Request ;
        var response        = context.Response;
        if (!CanAccept(HttpMethod.Get, request.HttpMethod) || request.IsWebSocketRequest)
            return false;

        var content         = GetContent(request);
        response.StatusCode = (int)HttpStatusCode.OK;

        using (var writer = new StreamWriter(response.OutputStream, Encoding.UTF8))
            writer.WriteLine(content ?? "Not Found.");

        response.Close();
        return true;
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

    public void Dispose() => Stop();
}
