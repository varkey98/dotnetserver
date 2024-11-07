FROM ubuntu:24.04 as core

RUN apt update && apt install -y dotnet-sdk-8.0

RUN mkdir -p src/Properties
RUN mkdir src/Protos
RUN mkdir src/Services
COPY GrpcServer.csproj src/
COPY Program.cs src/
COPY appsettings.json src/
COPY appsettings.Development.json src/
COPY Properties/launchSettings.json src/Properties/
COPY Protos/cat_fact.proto src/Protos/
COPY Services/GreeterService.cs src/Services/

WORKDIR /src/
RUN dotnet publish -r linux-arm64 --self-contained true

EXPOSE 5441

ENTRYPOINT [ "/src/bin/Release/net8.0/linux-arm64/publish/GrpcServer" ]
CMD ["--urls=http://0.0.0.0:5441"]
