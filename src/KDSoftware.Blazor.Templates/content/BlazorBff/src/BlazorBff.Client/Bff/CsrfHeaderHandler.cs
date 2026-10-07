namespace BlazorBff.Client.Bff;

/// <summary>
/// Adds the header that the BFF requires on every <c>/api</c> request. See <c>UseBffApiProtection</c> on the server.
/// </summary>
public sealed class CsrfHeaderHandler : DelegatingHandler
{
    public const string HeaderName = "X-CSRF";

    public CsrfHeaderHandler()
    {
    }

    public CsrfHeaderHandler(HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add(HeaderName, "1");
        return base.SendAsync(request, cancellationToken);
    }
}