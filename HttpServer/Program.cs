using System.Text;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging();
var ta = Traceable.TraceableAgentBuilder.CreateBuilder().AddAspNetCoreInstrumentation(builder.Services).Build();
builder.Services.AddSingleton(ta);
var app = builder.Build();

app.MapPost("/hello", async delegate (HttpContext context)
{
    long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();

    HttpRequest request = context.Request;

    // parse request 
    Request req = new();
    using (var sr = new StreamReader(context.Request.Body))
    {
        string requestJson = sr.ReadToEndAsync().Result;
        req = JsonConvert.DeserializeAnonymousType(requestJson, req);
    }

    // create upstream request
    var upstreamRequest = new UpStreamRequest { Val1 = req.Num1, Val2 = req.Num2 };
    var downStreamReqBody = new StringContent(
       JsonConvert.SerializeObject(upstreamRequest),
       Encoding.UTF8,
       Application.Json);

    var httpClient = new HttpClient();

    // send request to upstream server, async call as await switches context to a worker thread
    var upstreamResponse = await MakeAsyncPostRequest(httpClient, "http://localhost:5000/add", downStreamReqBody);

    // populate result into response of server
    req.Num1 = upstreamResponse.Result;

    // hardcoded delay
    string minDelay = Environment.GetEnvironmentVariable("MIN_DELAY_TO_RETURN", EnvironmentVariableTarget.Machine);
    int minDelayValue;
    if (!int.TryParse(minDelay, out minDelayValue))
    {
        minDelayValue = 20;
    }

    while (DateTimeOffset.Now.ToUnixTimeMilliseconds() - start < minDelayValue)
    {
        // send requests to upstream server, sync call as Task.Result will block the current thread
        var httpResponseMessage = httpClient.PostAsync("http://localhost:5000/sumOfSquares", downStreamReqBody).Result;
        upstreamResponse = new UpStreamResponse();
        var upstreamRequestJson = httpResponseMessage.Content.ReadAsStringAsync().Result;
        upstreamResponse = JsonConvert.DeserializeAnonymousType(upstreamRequestJson, upstreamResponse);

        // populate result into response of server
        req.Num2 = upstreamResponse.Result;
    } // introduce delay to match real world cases

    // send response back to client
    string responseJson = JsonConvert.SerializeObject(req);
    context.Response.ContentType = "application/json";
    context.Response.ContentLength = responseJson.Length;
    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(responseJson));
});


app.Run();

async Task<UpStreamResponse> MakeAsyncPostRequest(HttpClient httpClient, string uri, StringContent reqBody)
{
    var httpResponseMessage = await httpClient.PostAsync("http://localhost:5000/add", reqBody);
    var upstreamResponse = new UpStreamResponse();
    var upstreamRequestJson = await httpResponseMessage.Content.ReadAsStringAsync();
    upstreamResponse = JsonConvert.DeserializeAnonymousType(upstreamRequestJson, upstreamResponse);
    return upstreamResponse;
}


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