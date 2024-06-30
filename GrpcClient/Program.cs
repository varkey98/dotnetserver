// See https://aka.ms/new-console-template for more information

using CommandLine;
using Grpc.Net.Client;
using GrpcServer;

public class Program
{
    public static void Main(string[] args)
    {
        var parser = new Parser(opts =>
        {
            opts.CaseInsensitiveEnumValues = true;
        });
        parser.ParseArguments<Flags>(args).WithParsed(Run).WithNotParsed(ErrorHandling);
    }

    private static void UnaryCall(GrpcChannel channel)
    {
        List<string> temp =
        [
            "Liam",
            "Olivia",
            "Noah",
            "Emma",
            "Oliver",
            "Charlotte",
            "James",
            "Amelia",
            "Elijah",
            "Sophia"
        ];

        string name = temp.OrderBy(s => Guid.NewGuid()).First();
        var client = new CatWorld.CatWorldClient(channel);

        using var call = client.CatFactAsync(new CatFactRequest{Name = name});
        Task task = Task.Run(async() =>
        {
            CatFactResponse response = await call.ResponseAsync;
            Console.WriteLine(response);
        });

        task.Wait();

    }

    private static void BiderctionalStreamingCall(GrpcChannel channel)
    {
        List<string> temp =
        [
            "Liam",
            "Olivia",
            "Noah",
            "Emma",
            "Oliver",
            "Charlotte",
            "James",
            "Amelia",
            "Elijah",
            "Sophia"
        ];
        List<string> users = [];
        for (int i = 0; i < 1; ++i)
        {
            users.AddRange(temp);
        }

        var client = new CatWorld.CatWorldClient(channel);
        var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(1000));
        using var call = client.CatFactStream(cancellationToken: cancellationToken.Token);

        Task task = Task.WhenAll(
            [
                // write to stream
                Task.Run(async () =>
                {
                    foreach (var user in users)
                    {
                        await call.RequestStream.WriteAsync(new CatFactRequest { Name = user });
                    }

                    await call.RequestStream.CompleteAsync();
                }),
                // read from stream
                Task.Run(async () =>
                {
                    while (
                        !cancellationToken.IsCancellationRequested
                        && await call.ResponseStream.MoveNext(cancellationToken.Token)
                    )
                    {
                        Console.WriteLine(call.ResponseStream.Current);
                    }
                })
            ]
        );

        task.Wait(cancellationToken.Token);
    }

    static void Run(Flags opts)
    {
        Console.WriteLine("Hello World!");
        GrpcChannel channel = GrpcChannel.ForAddress(opts.Endpoint);

        switch (opts.Mode)
        {
            case Mode.UnaryCall:
                UnaryCall(channel);
                break;
            case Mode.ClientStreamingCall:
            case Mode.ServerStreamingCall:
                break;
            case Mode.BiderctionalStreamingCall:
                BiderctionalStreamingCall(channel);
                break;
        }
    }

    static void ErrorHandling(IEnumerable<Error> errors)
    {
        Console.WriteLine("Fail");
    }
}
