using Grpc.Core;
using Grpc.Health.V1;

namespace GrpcServer.Services;

public class HealthService: Health.HealthBase
{
    public override async Task<HealthCheckResponse> Check(HealthCheckRequest request, ServerCallContext context)
    {
        await Task.Delay(10);

        HealthCheckResponse response = new() 
        {
            Status = HealthCheckResponse.Types.ServingStatus.Serving,
        };
        return response;
    }


    public override Task Watch(HealthCheckRequest request, IServerStreamWriter<HealthCheckResponse> responseStream, ServerCallContext context)
    {
        return Task.CompletedTask;
    }
        
}