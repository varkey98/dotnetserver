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

    string responseMessage = $"Hello {req.Name}. This request has been processed successfully.";

    Message message = new Message
    {
        Name = req.Name,
        ReturnMessage = responseMessage
    };


    // send response back to client
    string responseJson = JsonConvert.SerializeObject(message);
    context.Response.ContentType = "application/json";
    context.Response.ContentLength = responseJson.Length;
    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(responseJson));
});


app.Run();

public class Message
{
    string _name;
    string _message;

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

}

public class Request
{
    public string Name {get; set;}
}