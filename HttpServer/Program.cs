using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;
using RestSharp;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging();
builder.Services.AddHttpClient();
builder.Services.Configure<KestrelServerOptions>(o =>
{
  o.AllowSynchronousIO = false;
  o.ConfigureEndpointDefaults((listener) =>
  {
    listener.Protocols = HttpProtocols.Http1;
  });
  o.ListenAnyIP(5001, opts =>
  {
    opts.Protocols = HttpProtocols.Http1;
  });
});
var app = builder.Build();
// app.MapPost("/hello", async delegate (HttpContext context)
// {
//     long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();

//     HttpRequest request = context.Request;

//     // parse request 
//     Request req = new();
//     using (var sr = new StreamReader(context.Request.Body))
//     {
//         string requestJson = await sr.ReadToEndAsync();
//         req = JsonConvert.DeserializeAnonymousType(requestJson, req);
//     }

//     var upstreamRequest = new UpStreamRequest { Val1 = req.Num1, Val2 = req.Num2 };
//     var upstreamReqBody = new StringContent(
//         JsonConvert.SerializeObject(upstreamRequest),
//         Encoding.UTF8,
//         Application.Json);


//     using (Activity activity = Activity.Current.Source.StartActivity("GET", ActivityKind.Client))
//     {

//         var httpRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost:8090/sumOfSquares")
//         {
//             Content = upstreamReqBody
//         };
//         var textMapPropagator = Propagators.DefaultTextMapPropagator;
//         if (textMapPropagator is not TraceContextPropagator)
//         {
//             textMapPropagator.Inject(new PropagationContext(activity.Context, Baggage.Current), httpRequest, HttpRequestMessageContextPropagation.HeaderValueSetter);
//         }
//         var httpResponseMessage = await httpClient.SendAsync(httpRequest);

//         var upstreamResponse = new UpStreamResponse();
//         var upstreamResponseJson = await httpResponseMessage.Content.ReadAsStringAsync();
//         upstreamResponse = JsonConvert.DeserializeAnonymousType(upstreamResponseJson, upstreamResponse);
//         req.Num2 = upstreamResponse.Result;

//     }

//     using (Activity activity = Activity.Current.Source.StartActivity("GET", ActivityKind.Client))
//     {

//         var httpRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost:8090/add")
//         {
//             Content = upstreamReqBody
//         };
//         var textMapPropagator = Propagators.DefaultTextMapPropagator;
//         if (textMapPropagator is not TraceContextPropagator)
//         {
//             textMapPropagator.Inject(new PropagationContext(activity.Context, Baggage.Current), httpRequest, HttpRequestMessageContextPropagation.HeaderValueSetter);
//         }

//         var httpResponseMessage = await httpClient.SendAsync(httpRequest);
//         var upstreamResponse = new UpStreamResponse();
//         var upstreamResponseJson = await httpResponseMessage.Content.ReadAsStringAsync();
//         upstreamResponse = JsonConvert.DeserializeAnonymousType(upstreamResponseJson, upstreamResponse);
//         req.Num1 = upstreamResponse.Result;
//     }


//     // send response back to client
//     foreach (var header in request.Headers) 
//     {
//         context.Response.Headers.TryAdd("echo." + header.Key,header.Value);
//     }
//     string responseJson = JsonConvert.SerializeObject(req);
//     context.Response.ContentType = "application/json";
//     context.Response.ContentLength = responseJson.Length;
//     await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(responseJson));
// });

app.MapPost("/dotnetecho", async delegate (HttpContext context)
{
    long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();

    HttpRequest request = context.Request;

    // parse request 
    Request req = new();
    using (var sr = new StreamReader(context.Request.Body))
    {
        string requestJson = await sr.ReadToEndAsync();
        req = JsonConvert.DeserializeAnonymousType(requestJson, req);
    }

    var upstreamRequest = new UpStreamRequest { Val1 = req.Num1, Val2 = req.Num2 };
    var upstreamReqBody = new StringContent(
        JsonConvert.SerializeObject(upstreamRequest),
        Encoding.UTF8,
        Application.Json);


    // send response back to client
    foreach (var header in request.Headers) 
    {
        context.Response.Headers.TryAdd("echo." + header.Key,header.Value);
    }
    string responseJson = JsonConvert.SerializeObject(req);
    context.Response.ContentType = "application/json";
    context.Response.ContentLength = responseJson.Length;
    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(responseJson));
});

app.MapPost("/echo/{*path}", async delegate (HttpContext context)
{
    HttpRequest request = context.Request;
    string body = "";
    using (var sr = new StreamReader(context.Request.Body))
    {
        body = await sr.ReadToEndAsync();
    }

    context.Response.ContentLength = body.Length;
    context.Response.ContentType = context.Request.ContentType;
    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(body));
    await context.Response.CompleteAsync();
});

app.MapGet("/health", async delegate (HttpContext context) 
{
    StatusClass status = new() { Status="OK"};
    string responseJson = JsonConvert.SerializeObject(status);
    context.Response.ContentType = "application/json";
    context.Response.ContentLength = responseJson.Length;
    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(responseJson));
});

app.MapGet("/cat-fact", async delegate (HttpContext context)
{

  var options = new RestClientOptions("https://catfact.ninja");
  var client = new RestClient(options);
  var request = new RestRequest("fact").AddJsonBody(new UpStreamRequest());
  var response = await client.GetAsync<CatFact>(request);
  string responseJson = JsonConvert.SerializeObject(response);
  context.Response.ContentType = "application/json";
  context.Response.ContentLength = responseJson.Length;
  await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(responseJson));
});

app.MapPost(
    "/client-test",
    async delegate(HttpContext context, HttpClient client)
    {
        long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        HttpRequest request = context.Request;
        using var sr = new StreamReader(request.Body);
        string body = await sr.ReadToEndAsync();
         var upstreamReqBody = new StringContent(
        JsonConvert.SerializeObject(body),
        Encoding.UTF8,
        Application.Json);

        string endpoint = Environment.GetEnvironmentVariable("DOTNET_CLIENT_URL") ?? "https://httpbin.org/post";
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = upstreamReqBody
        };
        var res = await client.SendAsync(httpRequest);
        body = await res.Content.ReadAsStringAsync();     

        // send response back to client
        context.Response.ContentType = "application/json";
        await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(body));
    }
);

app.MapPost("/httpbin-post", async delegate(HttpContext context)
{
    // parse request 
    Request req = new();
    using (var sr = new StreamReader(context.Request.Body))
    {
        string requestJson = await sr.ReadToEndAsync();
        req = JsonConvert.DeserializeAnonymousType(requestJson, req);
    }

    var options = new RestClientOptions("https://httpbin.org");
    var client = new RestClient(options);
    var request = new RestRequest("post").AddJsonBody(req);
    var response = await client.PostAsync<HttpbinPostResponse>(request);
    string responseJson = JsonConvert.SerializeObject(response);
    context.Response.ContentType = "application/json";
    context.Response.ContentLength = responseJson.Length;
    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(responseJson));
});


app.Run();

public class UpStreamResponse
{
    int result;
    string message;

    public int Result
    {
        get { return this.result; }
        set { this.result = value; }

    }
    public string ReturnMessage
    {
        get { return this.message; }
        set { this.message = value; }

    }

}

public class UpStreamRequest
{
    public int Val1 { get; set; }
    public int Val2 { get; set; }

}
public class Message
{
    string _name;
    string _message;
    string _address;

    public string Name
    {
        get { return this._name; }
        set { this._name = value; }

    }
    public string ReturnMessage
    {
        get { return this._message; }
        set { this._message = value; }

    }
    public string Address
    {
        get { return this._address; }
    }

}

public class Request
{
    public List<DataItem> Data { get; set; }
    public string Authorization { get; set; }
    public string Additional { get; set; }
    public int Num1 { get; set; }
    public int Num2 { get; set; }

    public class DataItem
    {
        public string Name { get; set; }
        public int Telephone { get; set; }
        public string Curr_City { get; set; }
        public string Weather { get; set; }
        public List<NestedItem> Nested { get; set; }
    }

    public class NestedItem
    {
        public string Name { get; set; }
        public int Telephone { get; set; }
        public string Curr_City { get; set; }
        public string Weather { get; set; }
    }
}

internal static class HttpRequestMessageContextPropagation
{
    internal static Action<HttpRequestMessage, string, string> HeaderValueSetter => (request, name, value) =>
    {
        request.Headers.Remove(name);
        request.Headers.Add(name, value);
    };
}

public class StatusClass
{
    public string Status {get; set;}
}

public class CatFact
{
    public int Length {get; set;}
    public string Fact {get; set;}
}

public class HttpbinPostResponse
{
    public Request Json {get; set;}
    public string Origin {get; set;}
    public string Url {get; set;}
}