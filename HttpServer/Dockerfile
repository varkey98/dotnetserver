FROM ubuntu:24.04 AS stage

RUN apt update -y && apt install -y dotnet-sdk-8.0
RUN mkdir -p src/Properties
COPY HttpServer.csproj src/
COPY Program.cs src/
COPY appsettings.json src/
COPY appsettings.Development.json src/
COPY Properties/launchSettings.json src/Properties/

WORKDIR /src/
RUN dotnet publish -r linux-arm64 -f net8.0 --self-contained false

EXPOSE 5001

ENV DOTNET_gcServer=1
ENV DOTNET_GCDynamicAdaptationMode=1

ENTRYPOINT [ "/src/bin/Release/net8.0/linux-arm64/publish/HttpServer" ]
CMD ["--urls=http://0.0.0.0:5001"]