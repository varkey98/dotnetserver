FROM ubuntu:24.04 as core

RUN apt update && apt install -y software-properties-common && add-apt-repository -y ppa:dotnet/backports && apt install -y dotnet-sdk-6.0

RUN mkdir -p src/Properties
RUN mkdir src/Protos
RUN mkdir src/Services
COPY GrpcServer.csproj src/
COPY Program.cs src/
COPY appsettings.json src/
COPY appsettings.Development.json src/
COPY Properties/launchSettings.json src/Properties/
COPY Protos/ src/Protos/
COPY Services/ src/Services/

WORKDIR /src/
RUN dotnet publish -r linux-arm64 -f net6.0 --self-contained false

EXPOSE 5001

ENTRYPOINT [ "/src/bin/Debug/net6.0/linux-arm64/GrpcServer" ]
CMD ["--urls=http://0.0.0.0:5001"]
