using Grpc.AspNetCore.Server;
using GrpcServer.Services;
using Microsoft.Extensions.DependencyInjection;
using Traceable.Instrumentation.Grpc.Implementation;

var builder = WebApplication.CreateBuilder(args);

// // Add services to the container.
builder.Services.AddGrpc();
 
builder.Services.AddLogging();
builder.Services.AddHttpClient();
builder.Services.AddTraceableAgent();


var app = builder.Build();


// Configure the HTTP request pipeline.
app.MapGrpcService<CatFactService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
