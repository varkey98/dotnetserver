using Grpc.Core;
using Newtonsoft.Json;

namespace GrpcServer.Services;

public class CatFactService(ILogger<CatFactService> logger, HttpClient client) : CatWorld.CatWorldBase
{
    private readonly ILogger<CatFactService> logger = logger;

    private readonly HttpClient httpClient = client;

    public override async Task<CatFactResponse> CatFact(CatFactRequest request, ServerCallContext context)
    {

        return await UpstreamService(request);
    }

    public override async Task CatFactStream(IAsyncStreamReader<CatFactRequest> requestStream, IServerStreamWriter<CatFactResponse> responseStream, ServerCallContext context)
    {
        while(await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
        {
            var request = requestStream.Current;
            await responseStream.WriteAsync(await UpstreamService(request));
        }
    }

    private async Task<CatFactResponse> UpstreamService(CatFactRequest request)
    {
        var response = await httpClient.GetAsync("https://catfact.ninja/fact");
        var factJson = await response.Content.ReadAsStringAsync(); 
        FactResponse fact = new();
        fact = JsonConvert.DeserializeAnonymousType(factJson, fact);

        return new CatFactResponse
        {
            Message = "Hello " + request.Name,
            Fact = fact?.Fact
        };
    }
}

public class FactResponse
{
    public string Fact {get; set;}
    public int Length {get; set;}
}
