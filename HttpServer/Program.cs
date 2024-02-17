using System.Text;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging();
var app = builder.Build();


app.MapPost("/hello", async delegate(HttpContext context)
{
    Console.WriteLine("I'm here even after getting blocked!!");
    HttpRequest request = context.Request;
    
    Request req = new Request();
    using (var sr = new StreamReader(context.Request.Body))
    {
        string requestJson = sr.ReadToEndAsync().Result;
        req = JsonConvert.DeserializeAnonymousType(requestJson, req);
    }

    // var httpClient = new HttpClient();
    // var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://catfact.ninja/fact");
    // requestMessage.Headers.Add("jacob", new []{"test"});
    // var response = await httpClient.SendAsync(requestMessage);
    // string body = response.Content.ReadAsStringAsync().Result;

    string name = req.Name;
    string responseMessage = string.IsNullOrEmpty(name)
        ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
        : $"Hello, {name}. This HTTP triggered function executed successfully.";

    var result = new Message { Name = name, ReturnMessage = responseMessage };
    string body = JsonConvert.SerializeObject(result);
    context.Response.ContentType = "application/json";
    context.Response.ContentLength = body.Length;
    await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes(body));
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
    public string Name {get;set;}
}