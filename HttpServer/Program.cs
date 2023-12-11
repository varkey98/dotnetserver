
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.IO;

const int DefaultBufferThreshold = 1024 * 30;

static string getRequestBody(Stream body)
{
    string bodyText="";
    // body.Position = 0;
    var bodyStream = new StreamReader(body);
    // if (body.CanSeek) body.Seek(0, SeekOrigin.Begin);
    if (body.CanRead) bodyText = bodyStream.ReadToEndAsync().Result;
    return bodyText;
}

var builder = WebApplication.CreateBuilder(args);

// dependency injection
builder.Services.AddOpenTelemetry()
    .WithTracing(b =>
    {
        b
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter(opts => {
                opts.Endpoint = new Uri("http://34.168.86.196:4317");
            })
            .AddConsoleExporter();
        
    });
var app = builder.Build();





// TODO body is not coming in case of AspNetCore Instrumentation
// TODO request details are not coming in case of HttpClient Instrumentation
// TODO learn async, await, task, ?


var httpClient = new HttpClient();


app.MapPost("/hello", async delegate(HttpContext context)
{
    Console.WriteLine("I'm here even after getting blocked!!");
    HttpRequest request = context.Request;
    
    StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8);
    string jsonstring = getRequestBody(context.Request.Body);

    // var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://catfact.ninja/fact");
    // requestMessage.Headers.Add("jacob", new []{"test"});
    // var response = await httpClient.SendAsync(requestMessage);
    // // var html = await httpClient.GetStringAsync("https://example.com/");

    // string body = response.Content.ReadAsStringAsync().Result;

    string body = jsonstring;
    context.Response.ContentType = "text/plain";
    
    if (string.IsNullOrWhiteSpace(body))
    {
        await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World!"));
        context.Response.ContentLength = 11;
    }
    else
    {
        await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes(body));
        context.Response.ContentLength = body.Length;

    }
});

// app.Use(async (context, next) =>
// {
//     context.Request.EnableBuffering();
//     await next();
// });

app.Run();

