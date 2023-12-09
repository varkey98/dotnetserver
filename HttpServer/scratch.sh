#!/usr/bin/env bash

#clean
dotnet nuget locals all --clear

#add
dotnet add package OpenTelemetry.Api -s /Users/varkeychanjacob/Projects/opentelemetry-dotnet/src/OpenTelemetry.Api/bin/Debug --prerelease
dotnet add package OpenTelemetry.Api.ProviderBuilderExtensions -s /Users/varkeychanjacob/Projects/opentelemetry-dotnet/src/OpenTelemetry.Api.ProviderBuilderExtensions/bin/Debug --prerelease
dotnet add package OpenTelemetry -s /Users/varkeychanjacob/Projects/opentelemetry-dotnet/src/OpenTelemetry/bin/Debug --prerelease
dotnet add package OpenTelemetry.Instrumentation.Http -s /Users/varkeychanjacob/Projects/opentelemetry-dotnet/src/OpenTelemetry.Instrumentation.Http/bin/Debug --prerelease
dotnet add package OpenTelemetry.Exporter.Console -s /Users/varkeychanjacob/Projects/opentelemetry-dotnet/src/OpenTelemetry.Exporter.Console/bin/Debug --prerelease

