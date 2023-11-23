using System.Net;

var handler = new HttpClientHandler();
handler.ClientCertificateOptions = ClientCertificateOption.Manual;
handler.ServerCertificateCustomValidationCallback = 
    (httpRequestMessage, cert, cetChain, policyErrors) =>
{
    return true;
};

using var client = new HttpClient(handler) {
    DefaultRequestVersion = HttpVersion.Version30,
    DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
};



Console.WriteLine("--- localhost:5001 ---");

HttpResponseMessage resp = await client.GetAsync("https://localhost:5001/");
string body = await resp.Content.ReadAsStringAsync();

Console.WriteLine(
    $"status: {resp.StatusCode}, version: {resp.Version}, " +
    $"body: {body.Substring(0, Math.Min(100, body.Length))}");
