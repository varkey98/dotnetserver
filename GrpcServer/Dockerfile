FROM mcr.microsoft.com/dotnet/sdk:8.0 as stage

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
RUN dotnet publish -r linux-x64 --self-contained true

# ENTRYPOINT ["ls"]
# CMD [ "-l", "/src/bin/Release/net8.0/linux-x64/publish/GrpcServer"]

FROM ubuntu:24.04 as core

RUN apt update && apt install -y dotnet-sdk-8.0

COPY --from=stage /src/bin/Release/net8.0/linux-x64/publish/GrpcServer .

EXPOSE 5001

ENTRYPOINT [ "./GrpcServer" ]
CMD ["--urls=http://0.0.0.0:5001"]
