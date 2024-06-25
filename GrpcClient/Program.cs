// See https://aka.ms/new-console-template for more information

using Grpc.Net.Client;
using GrpcServer;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Hello World!");
        List<string> users = ["Liam", "Olivia", "Noah", "Emma", "Oliver", "Charlotte", "James",	"Amelia", "Elijah", "Sophia"];

        GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:8080");
        var client = new CatWorld.CatWorldClient(channel);

        var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var call = client.CatFactStream(cancellationToken: cancellationToken.Token);

        Task task = Task.WhenAll(new []
        {
            // write to stream
            Task.Run(async()=> {
                foreach(var user in users)
                {
                    await call.RequestStream.WriteAsync(new CatFactRequest{
                        Name = user
                    });
                }

                await call.RequestStream.CompleteAsync();
            }),

            // read from stream
            Task.Run(async() => {
                while(!cancellationToken.IsCancellationRequested && await call.ResponseStream.MoveNext(cancellationToken.Token))
                {
                    Console.WriteLine(call.ResponseStream.Current);
                }
            })

        });

        task.Wait();
    }
}