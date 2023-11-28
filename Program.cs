using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;

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
const string series = "000001";

HttpResponseMessage resp = await client.GetAsync($"https://localhost:5001/studies/1.2.3/series/{series}");
string body = await resp.Content.ReadAsStringAsync();
List<string> fileNames = JsonConvert.DeserializeObject<List<string>>(body);

List<Task> receiverTasks = new List<Task>();
long recieived = 0;
int receiveFile = 0;
Stopwatch sw = new Stopwatch();
sw.Start();
foreach (var filename in fileNames)
{
    string instance = filename;
    string getPath = $"https://localhost:5001/studies/1.2.3/series/{series}/instances/{instance}";

    receiverTasks.Add(Task.Factory.StartNew(() => {
        var handler = new HttpClientHandler();
        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
        handler.ServerCertificateCustomValidationCallback = 
        (httpRequestMessage, cert, cetChain, policyErrors) =>
        {
            return true;
        };

        using var client = new HttpClient(handler) {
            DefaultRequestVersion = HttpVersion.Version30,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Timeout = TimeSpan.FromMinutes(10)
        };

        try {
            var resp = client.GetAsync(getPath).ConfigureAwait(false).GetAwaiter().GetResult();
            Interlocked.Increment(ref receiveFile);
            Interlocked.Add(ref recieived, resp.Content.ReadAsStream().Length);
        } catch(Exception ex) {
            Console.WriteLine($"chunk failed {instance} reasob: {ex.Message}");
        }
    }));
}

await Task.WhenAll(receiverTasks);
sw.Stop();
Console.WriteLine($"Received file count:{receiveFile} Received bytes: {recieived} KB in {sw.ElapsedMilliseconds} ms");