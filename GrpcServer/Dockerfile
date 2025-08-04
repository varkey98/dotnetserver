FROM mcr.microsoft.com/dotnet/sdk:9.0 AS stage

ARG RID=linux-arm64

RUN mkdir -p src/Properties
COPY . /src/

WORKDIR /src
RUN dotnet publish -r ${RID} -f net9.0 --self-contained true

FROM ubuntu:22.04 AS depl
ARG RID=linux-arm64

RUN mkdir /build
WORKDIR /build
COPY --from=stage /src/bin/Release/net9.0/${RID}/publish/ .

EXPOSE 5001

ENV DOTNET_gcServer=1
ENV DOTNET_GCDynamicAdaptationMode=1
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

ENTRYPOINT [ "/build/GrpcServer" ]