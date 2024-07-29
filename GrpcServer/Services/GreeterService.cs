using System.Text;
using Grpc.Core;
using Newtonsoft.Json;

namespace GrpcServer.Services;

public class CatFactService(ILogger<CatFactService> logger, HttpClient client) : CatWorld.CatWorldBase
{
    private readonly ILogger<CatFactService> logger = logger;

    private readonly HttpClient httpClient = client;

    public override async Task<CatFactResponse> CatFact(CatFactRequest request, ServerCallContext context)
    {
        foreach(var header in context.RequestHeaders)
        {
            Console.WriteLine($"{header.Key} : {header.Value}");
        }
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

    public override async Task<CatFactResponse> CatFactClientStream(IAsyncStreamReader<CatFactRequest> requestStream, ServerCallContext context)
    {
        StringBuilder names = new();
        while(await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
        {
            names.Append(requestStream.Current.Name + ", ");
        }
        int length = names.Length -1;
        return await UpstreamService(new CatFactRequest{Name=names.ToString()[..length]});
    }

    public override async Task CatFactServerStream(CatFactServerStreamRequest request, IServerStreamWriter<CatFactResponse> responseStream, ServerCallContext context)
    {
        for(int i=0; i<request.Count; ++i)
        {
            await responseStream.WriteAsync(await UpstreamService(new CatFactRequest{Name=request.Name}));
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
