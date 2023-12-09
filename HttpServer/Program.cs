
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
    body.Position = 0;
    var bodyStream = new StreamReader(body);
    if (body.CanSeek) body.Seek(0, SeekOrigin.Begin);
    if (body.CanRead) bodyText = bodyStream.ReadToEndAsync().Result;
    return bodyText;
}

string getResponseBody(Stream body)
{
    var buffer = new MemoryStream();
    body.CopyToAsync(buffer);
    buffer.Position = 0L;

    Console.WriteLine("Reading Body");
    string bodyText="";
    var bodyStream = new StreamReader(buffer);
    if (buffer.CanSeek) body.Seek(0, SeekOrigin.Begin);
    if (buffer.CanRead) bodyText = bodyStream.ReadToEndAsync().Result;

    return bodyText;
}

void Enrich(Activity activity, HttpRequest request)
{

    ReadResponseBody(request.HttpContext.Response, body => {
        activity.AddTag("v3", body);
    });
}

void ReadResponseBody(HttpResponse res, Action<string> onReadBody)
{
    var context = res.HttpContext;
    var priorFeature = context.Features.Get<IHttpResponseBodyFeature>();

    if (priorFeature == null) return;
    RecyclableMemoryStreamManager _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

    var bufferedResponseStream = _recyclableMemoryStreamManager.GetStream();
    var streamResponseBodyFeature = new StreamResponseBodyFeature(bufferedResponseStream, priorFeature);

    context.Features.Set<IHttpResponseBodyFeature>(streamResponseBodyFeature);

    context.Response.OnStarting(async () => {
        try {
            if (bufferedResponseStream.TryGetBuffer(out var data) && data.Array != null) {
                var responseBody = Encoding.UTF8.GetString(data.Array, 0, data.Count);
                onReadBody(responseBody);
            }
        } finally {
            var originalStream = streamResponseBodyFeature.PriorFeature?.Stream;
            if (originalStream is not null) {
                bufferedResponseStream.Position = 0;
                await bufferedResponseStream.CopyToAsync(originalStream).ConfigureAwait(false);
            }

            await bufferedResponseStream.DisposeAsync();
        }
    });
}



void EnableRewind(HttpResponse response, int bufferThreshold = DefaultBufferThreshold, long? bufferLimit = null)
{
    ArgumentNullException.ThrowIfNull(response);

    var body = response.Body;
    if (!body.CanSeek)
    {
        var fileStream = new FileBufferingReadStream(body, bufferThreshold, bufferLimit, "./");
        response.Body = fileStream;
        // check alternatives
        // request.HttpContext.Response.RegisterForDispose(fileStream);
    }
}


// TODO body is not coming in case of AspNetCore Instrumentation
// TODO request details are not coming in case of HttpClient Instrumentation
// TODO learn async, await, task, ?

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithTracing(b =>
    {
        b
            .AddAspNetCoreInstrumentation(options =>
            {
                options.EnrichWithHttpRequest = Enrich;
                options.EnrichWithHttpResponse = (activity, response) =>
                {
                    // Task responseCapturer = CaptureResponse(response, activity);
                    // Func<Task> myFun = async () => await responseCapturer;
                    // response.OnStarting(myFun); 
                    
                    activity.SetTag("v2", getRequestBody(response.Body));
                };
            })
            .AddConsoleExporter();
        
    });

var app = builder.Build();

var httpClient = new HttpClient();

app.MapGet("/hello", async (context) =>
{
    HttpRequest request = context.Request;
    
    // HttpRequestRewindExtensions.EnableBuffering(request);
    
    // Console.WriteLine(getRequestBody(request.Body));
    // Console.WriteLine("Printing body again!!!");
    // Console.WriteLine(getRequestBody(request.Body));

    var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://catfact.ninja/fact");
    requestMessage.Headers.Add("jacob", new []{"test"});
    var response = await httpClient.SendAsync(requestMessage);
    // var html = await httpClient.GetStringAsync("https://example.com/");

    string body = response.Content.ReadAsStringAsync().Result;
    
    context.Response.OnStarting(async () =>
    {
        EnableRewind(context.Response);
    });
    if (string.IsNullOrWhiteSpace(body))
    {
        await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World!"));
    }
    else
    {
        await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes(body));
    }
    
    context.Response.OnCompleted(async () =>
    {
        Console.WriteLine("Completed");
        Console.WriteLine(getResponseBody(context.Response.Body));
    });
});

// app.Use(async (context, next) =>
// {
//     context.Request.EnableBuffering();
//     await next();
// });

app.Run();