FROM ubuntu:24.04

RUN apt update && apt install -y dotnet-sdk-8.0

RUN mkdir -p src/Properties
COPY HttpServer.csproj src/
COPY Program.cs src/
COPY appsettings.json src/
COPY appsettings.Development.json src/
COPY Properties/launchSettings.json src/Properties/

EXPOSE 5001

WORKDIR /src
ENTRYPOINT [ "dotnet" ]
CMD [ "run", "--urls=http://0.0.0.0:5001"]