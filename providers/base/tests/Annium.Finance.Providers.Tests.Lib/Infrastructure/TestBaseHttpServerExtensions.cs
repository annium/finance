using System;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Annium.Logging;
using Annium.Net.Http;
using Annium.Net.Servers.Web;
using Annium.Testing;

namespace Annium.Finance.Providers.Tests.Lib.Infrastructure;

/// <summary>
/// Wires an <see cref="IHttpRequest"/> client and a local HTTP server into a <see cref="TestBase"/>-derived
/// test, so a test can drive a provider's HTTP calls against a canned response instead of the real exchange.
/// </summary>
public static class TestBaseHttpServerExtensions
{
    /// <summary>
    /// Registers the HTTP request factory a test needs to call <see cref="CreateHttpRequest"/> later.
    /// </summary>
    /// <param name="test">The test instance to register the factory into.</param>
    /// <param name="key">The key the factory is registered under.</param>
    public static void RegisterHttpRequestFactory(this TestBase test, string key = "")
    {
        test.Register(container =>
        {
            container.AddHttpRequestFactory(key, true);
        });
    }

    /// <summary>
    /// Creates an HTTP request pointed at the given local test server.
    /// </summary>
    /// <param name="test">The test instance the request factory is registered on.</param>
    /// <param name="server">The local test server to target.</param>
    /// <param name="key">The key the request factory is registered under.</param>
    /// <returns>An HTTP request ready to be sent to the server.</returns>
    public static IHttpRequest CreateHttpRequest(this TestBase test, IServer server, string key = "") =>
        test.GetKeyed<IHttpRequestFactory>(key).New(server.HttpUri());

    /// <summary>
    /// Starts a local test server that always answers with the given status code and a JSON-serialized body.
    /// </summary>
    /// <typeparam name="T">The type of the response body.</typeparam>
    /// <param name="test">The test instance to trace server activity through.</param>
    /// <param name="statusCode">The status code the server responds with.</param>
    /// <param name="body">The value serialized to JSON and sent as the response body.</param>
    /// <returns>The running server; dispose it to stop listening.</returns>
    public static IServer RunHttpServerWithJsonResponse<T>(this TestBase test, HttpStatusCode statusCode, T body) =>
        test.RunHttpServerWithResponse(statusCode, MediaTypeNames.Application.Json, JsonSerializer.Serialize(body));

    /// <summary>
    /// Starts a local test server that always answers with the given status code, content type and body.
    /// </summary>
    /// <param name="test">The test instance to trace server activity through.</param>
    /// <param name="statusCode">The status code the server responds with.</param>
    /// <param name="contentType">The content type of the response body.</param>
    /// <param name="body">The raw response body.</param>
    /// <returns>The running server; dispose it to stop listening.</returns>
    public static IServer RunHttpServerWithResponse(
        this TestBase test,
        HttpStatusCode statusCode,
        string contentType,
        string body
    ) =>
        test.RunHttpServer(
            async (_, response) =>
            {
                var payload = Encoding.UTF8.GetBytes(body);
                response.StatusCode(statusCode);
                response.ContentType = contentType;
                response.ContentLength64 = payload.Length;
                await response.OutputStream.WriteAsync(payload);
            }
        );

    /// <summary>
    /// Runs a test server with the specified request handler.
    /// </summary>
    /// <param name="test">Test instance</param>
    /// <param name="handle">The function to handle HTTP requests.</param>
    /// <returns>An IAsyncDisposable to stop the server.</returns>
    public static IServer RunHttpServer(
        this TestBase test,
        Func<HttpListenerRequest, HttpListenerResponse, Task> handle
    )
    {
        var handler = new HttpHandler(async ctx =>
        {
            test.Trace("start");

            ctx.Response.Headers.Clear();
            try
            {
                await handle(ctx.Request, ctx.Response);
            }
            catch
            {
                ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            finally
            {
                ctx.Response.Close();
            }

            test.Trace("done");
        });

        var server = ServerBuilder.New(test.Get<IServiceProvider>()).WithHttpHandler(handler).Start().NotNull();
        test.Trace("started server at port {port}", server.Port);

        return server;
    }
}

/// <summary>
/// HTTP handler implementation for test servers.
/// </summary>
file class HttpHandler : IHttpHandler
{
    /// <summary>
    /// The function to handle HTTP requests.
    /// </summary>
    private readonly Func<HttpListenerContext, Task> _handle;

    /// <summary>
    /// Initializes a new instance of the HttpHandler class.
    /// </summary>
    /// <param name="handle">The function to handle HTTP requests.</param>
    public HttpHandler(Func<HttpListenerContext, Task> handle)
    {
        _handle = handle;
    }

    /// <summary>
    /// Handles an incoming HTTP request.
    /// </summary>
    /// <param name="socket">The HTTP listener context.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task HandleAsync(HttpListenerContext socket, CancellationToken ct)
    {
        return _handle(socket);
    }
}

/// <summary>
/// Convenience helpers for setting the status code on a raw <see cref="HttpListenerResponse"/> from within
/// a <see cref="TestBaseHttpServerExtensions.RunHttpServer"/> request handler.
/// </summary>
public static class HttpListenerResponseTestExtensions
{
    /// <summary>
    /// Sets the response status code to OK (200).
    /// </summary>
    /// <param name="response">The HTTP listener response.</param>
    public static void Ok(this HttpListenerResponse response)
    {
        response.StatusCode(HttpStatusCode.OK);
    }

    /// <summary>
    /// Sets the response status code to given status code.
    /// </summary>
    /// <param name="response">The HTTP listener response.</param>
    /// <param name="code">Status code to set for response</param>
    public static void StatusCode(this HttpListenerResponse response, HttpStatusCode code)
    {
        response.StatusCode((int)code);
    }

    /// <summary>
    /// Sets the response status code to given status code.
    /// </summary>
    /// <param name="response">The HTTP listener response.</param>
    /// <param name="code">Status code to set for response</param>
    public static void StatusCode(this HttpListenerResponse response, int code)
    {
        response.StatusCode = code;
    }
}
