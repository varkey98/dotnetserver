# Http Server
## Running with filter

```
docker run -it -v /Users/test/Projects/dotnetagent/deployments/RuntimeStore/build/traceable:/traceable  -e ASPNETCORE_HOSTINGSTARTUPASSEMBLIES=Traceable.Agent -e DOTNET_ADDITIONAL_DEPS=/traceable/additionalDeps/  -e DOTNET_SHARED_STORE=/traceable/store/ -e TA_LOGGING_LOG_LEVEL=Trace -e TA_REMOTE_CONFIG_ENDPOINT=1.2.3.4:5441 -p 5001:5001 httpserver:dev
```