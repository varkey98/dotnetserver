using Grpc.AspNetCore.Server;
using GrpcServer.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// // Add services to the container.
builder.Services.AddGrpc();
 
builder.Services.AddLogging();
builder.Services.AddHttpClient();
builder.Services.Configure<KestrelServerOptions>(o => {
    o.AllowSynchronousIO = false;
    o.ConfigureEndpointDefaults((listener)=> {
        listener.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();


// Configure the HTTP request pipeline.
app.MapGrpcService<CatFactService>();
app.MapGrpcService<HealthService>(); // Health check endpoint
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
