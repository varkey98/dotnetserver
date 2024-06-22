using System.Text;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging();
builder.Services.AddTraceableAgent();
var app = builder.Build();


app.MapPost("/add", async delegate(HttpContext context)
{
    HttpRequest request = context.Request;
    
    Request req = new Request();
    using (var sr = new StreamReader(context.Request.Body))
    {
        string requestJson = sr.ReadToEndAsync().Result;
        req = JsonConvert.DeserializeAnonymousType(requestJson, req);
    }

    int result = req.Val1 + req.Val2;
    string responseMessage = $"Hello, the value of {req.Val1} + {req.Val2} is {result}.";

    var resp = new Response { Result = result, ReturnMessage = responseMessage };
    string body = JsonConvert.SerializeObject(resp);
    context.Response.ContentType = "application/json";
    context.Response.ContentLength = body.Length;
    await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes(body));
});

app.MapPost("/sumOfSquares", async delegate(HttpContext context)
{
    HttpRequest request = context.Request;
    
    Request req = new Request();
    using (var sr = new StreamReader(context.Request.Body))
    {
        string requestJson = sr.ReadToEndAsync().Result;
        req = JsonConvert.DeserializeAnonymousType(requestJson, req);
    }

    int result = req.Val1 * req.Val1 + req.Val2 * req.Val2;
    string responseMessage = $"Hello, the value of square of {req.Val1} + square of {req.Val2} is {result}.";

    var resp = new Response { Result = result, ReturnMessage = responseMessage };
    string body = JsonConvert.SerializeObject(resp);
    context.Response.ContentType = "application/json";
    context.Response.ContentLength = body.Length;
    await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes(body));
});


app.Run();


public class Response
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

public class Request
{
    public int Val1 {get;set;}
    public int Val2 {get;set;}

}