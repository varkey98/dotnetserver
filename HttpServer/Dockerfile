FROM ubuntu:22.04 AS stage

RUN apt update -y && apt install -y dotnet-sdk-6.0
RUN mkdir -p src/Properties
COPY HttpServer.csproj src/
COPY Program.cs src/
COPY appsettings.json src/
COPY appsettings.Development.json src/
COPY Properties/launchSettings.json src/Properties/
COPY config.yaml src/


WORKDIR /src/
RUN dotnet publish -r linux-arm64 -f net6.0 --self-contained false

EXPOSE 5001

ENV DOTNET_gcServer=1
ENV DOTNET_GCDynamicAdaptationMode=1

ENTRYPOINT [ "/src/bin/Debug/net6.0/linux-arm64/publish/HttpServer" ]
CMD ["--urls=http://0.0.0.0:5001"]

# CMD ["ls", "-l", "/opt/traceable"]