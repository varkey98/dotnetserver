namespace GrpcServer.Services;

using Grpc.Core;
using Ai.Traceable.Edge.Decision.Service.Api.V1;

public class EdgeDecisionServiceTest : EdgeDecisionService.EdgeDecisionServiceBase
{
    private readonly ILogger<EdgeDecisionServiceTest> logger;

    public EdgeDecisionServiceTest(ILogger<EdgeDecisionServiceTest> logger) {
        this.logger = logger;
    }

    public override async Task<AssessRiskResponse> AssessRisk(AssessRiskRequest request, ServerCallContext context)
    {
        DateTime deadline = context.Deadline;
    
        // Check if the deadline is set
        if (deadline != DateTime.MaxValue)
        {
            Console.WriteLine($"{DateTime.Now.Millisecond}: {deadline.Millisecond}");
        }
        foreach(var header in context.RequestHeaders)
        {
            Console.WriteLine($"{header.Key} : {header.Value}");
        }

        foreach (var attr in request.GenericRequest.InputPayload) 
        {
            Console.WriteLine($"{attr.Key} : {attr.Value}");
        }
        return new AssessRiskResponse();
    }
}