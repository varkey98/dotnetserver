FROM mcr.microsoft.com/dotnet/sdk:8.0 as stage

RUN mkdir -p src/Properties
COPY HttpServer.csproj src/
COPY Program.cs src/
COPY appsettings.json src/
COPY appsettings.Development.json src/
COPY Properties/launchSettings.json src/Properties/

WORKDIR /src/
RUN dotnet publish -r linux-x64 --self-contained false

FROM ubuntu:24.04 as core

RUN apt update && apt install -y dotnet-sdk-8.0

COPY --from=stage /src/bin/Release/net8.0/linux-x64/publish/HttpServer .

EXPOSE 5001

ENV DOTNET_gcServer 1
ENV DOTNET_GCDynamicAdaptationMode 1

ENTRYPOINT [ "./HttpServer" ]
CMD ["--urls=http://0.0.0.0:5001"]

# ENTRYPOINT [ "env" ]
