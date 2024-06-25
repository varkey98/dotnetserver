using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Newtonsoft.Json;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using static System.Net.Mime.MediaTypeNames;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging();
builder.Services.AddTraceableAgent();
builder.Services.Configure<KestrelServerOptions>(o => o.AllowSynchronousIO = false);
var app = builder.Build();
var httpClient = new HttpClient();
app.MapPost("/hello", async delegate (HttpContext context)
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


    using (Activity activity = Activity.Current.Source.StartActivity("GET", ActivityKind.Client))
    {

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost:8090/sumOfSquares")
        {
            Content = upstreamReqBody
        };
        var textMapPropagator = Propagators.DefaultTextMapPropagator;
        if (textMapPropagator is not TraceContextPropagator)
        {
            textMapPropagator.Inject(new PropagationContext(activity.Context, Baggage.Current), httpRequest, HttpRequestMessageContextPropagation.HeaderValueSetter);
        }
        var httpResponseMessage = await httpClient.SendAsync(httpRequest);

        var upstreamResponse = new UpStreamResponse();
        var upstreamResponseJson = await httpResponseMessage.Content.ReadAsStringAsync();
        upstreamResponse = JsonConvert.DeserializeAnonymousType(upstreamResponseJson, upstreamResponse);
        req.Num2 = upstreamResponse.Result;

    }

    using (Activity activity = Activity.Current.Source.StartActivity("GET", ActivityKind.Client))
    {

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost:8090/add")
        {
            Content = upstreamReqBody
        };
        var textMapPropagator = Propagators.DefaultTextMapPropagator;
        if (textMapPropagator is not TraceContextPropagator)
        {
            textMapPropagator.Inject(new PropagationContext(activity.Context, Baggage.Current), httpRequest, HttpRequestMessageContextPropagation.HeaderValueSetter);
        }

        var httpResponseMessage = await httpClient.SendAsync(httpRequest);
        var upstreamResponse = new UpStreamResponse();
        var upstreamResponseJson = await httpResponseMessage.Content.ReadAsStringAsync();
        upstreamResponse = JsonConvert.DeserializeAnonymousType(upstreamResponseJson, upstreamResponse);
        req.Num1 = upstreamResponse.Result;
    }


    // send response back to client
    string responseJson = JsonConvert.SerializeObject(req);
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