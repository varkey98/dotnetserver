#!\usr\bin\env bash

#clean
#dotnet nuget locals all --clear
#dotnet restore

#add
dotnet add package Microsoft.IO.RecyclableMemoryStream
dotnet add package OpenTelemetry.Api -s C:\Users\varkeychan_jacob\dotnetagent\opentelemetry-dotnet\src\OpenTelemetry.Api\bin\Debug --prerelease
dotnet add package OpenTelemetry.Api.ProviderBuilderExtensions -s C:\Users\varkeychan_jacob\dotnetagent\opentelemetry-dotnet\src\OpenTelemetry.Api.ProviderBuilderExtensions\bin\Debug --prerelease
dotnet add package OpenTelemetry -s C:\Users\varkeychan_jacob\dotnetagent\opentelemetry-dotnet\src\OpenTelemetry\bin\Debug --prerelease
dotnet add package OpenTelemetry.Instrumentation.Http -s C:\Users\varkeychan_jacob\dotnetagent\opentelemetry-dotnet\src\OpenTelemetry.Instrumentation.Http\bin\Debug --prerelease
dotnet add package OpenTelemetry.Exporter.Console -s C:\Users\varkeychan_jacob\dotnetagent\opentelemetry-dotnet\src\OpenTelemetry.Exporter.Console\bin\Debug --prerelease
dotnet add package OpenTelemetry.Extensions.Hosting -s C:\Users\varkeychan_jacob\dotnetagent\opentelemetry-dotnet\src\OpenTelemetry.Extensions.Hosting\bin\Debug --prerelease              
dotnet add package OpenTelemetry.Instrumentation.AspNetCore -s C:\Users\varkeychan_jacob\dotnetagent\opentelemetry-dotnet\src\OpenTelemetry.Instrumentation.AspNetCore\bin\Debug --prerelease
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol -s C:\Users\varkeychan_jacob\dotnetagent\opentelemetry-dotnet\src\OpenTelemetry.Exporter.OpenTelemetryProtocol\bin\Debug --prerelease