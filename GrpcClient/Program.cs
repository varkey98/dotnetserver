// See https://aka.ms/new-console-template for more information

using CommandLine;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcServer;

public abstract class test
{
  public abstract void Run();
}

public class Impl : test
{

  public void Run2()
  {
    Console.WriteLine("Inside run2!");
  }

  public override void Run()
  {
    Console.WriteLine("Impl");
  }
}

public class ImplSquare : Impl
{
  public override void Run()
  {
    Console.WriteLine("ImplSquare");
    base.Run();
  }
}

public class Program
{

  static readonly List<string> ListOfNames =
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

  public static void Main(string[] args)
  {
    Impl obj_ = new ImplSquare();
    obj_.Run();
    obj_.Run2();
    String obj = "null";
    try
    {
      obj = "test";
    }
    catch (Exception) { }
    try
    {
      Console.WriteLine(obj);

    }
    catch (Exception) { }
    var parser = new Parser(opts =>
    {
      opts.CaseInsensitiveEnumValues = true;
    });
    parser.ParseArguments<Flags>(args).WithParsed(Run).WithNotParsed(ErrorHandling);
  }

  private static void UnaryCall(GrpcChannel channel)
  {
    string name = ListOfNames.OrderBy(s => Guid.NewGuid()).First();
    var client = new CatWorld.CatWorldClient(channel);

    using var call = client.CatFactAsync(new CatFactRequest { Name = name });
    Task task = Task.Run(async () =>
    {
      CatFactResponse response = await call.ResponseAsync;
      Console.WriteLine(response);
    });

    task.Wait();
  }

  private static void ClientStreamingCall(GrpcChannel channel)
  {
    List<string> users = [];
    for (int i = 0; i < 1; ++i)
    {
      users.AddRange(ListOfNames);
    }

    var client = new CatWorld.CatWorldClient(channel);
    var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(1000));
    using var call = client.CatFactClientStream(cancellationToken: cancellationToken.Token);

    Task task = Task.Run(async () =>
    {
      foreach (var user in users)
      {
        await call.RequestStream.WriteAsync(new CatFactRequest { Name = user });
      }

      await call.RequestStream.CompleteAsync();

      CatFactResponse response = await call.ResponseAsync;
      Console.WriteLine(response);
    });

    task.Wait(cancellationToken: cancellationToken.Token);
  }

  private static void ServerStreamingCall(GrpcChannel channel)
  {
    string name = ListOfNames.OrderBy(s => Guid.NewGuid()).First();

    var client = new CatWorld.CatWorldClient(channel);
    var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(1000));
    using var call = client.CatFactServerStream(
        new CatFactServerStreamRequest { Name = name, Count = 10 },
        cancellationToken: cancellationToken.Token
    );

    Task task = Task.Run(async () =>
    {
      while (
              !cancellationToken.IsCancellationRequested
              && await call.ResponseStream.MoveNext(cancellationToken.Token)
          )
      {
        Console.WriteLine(call.ResponseStream.Current);
      }
    });

    task.Wait(cancellationToken.Token);
  }

  private static void BiderctionalStreamingCall(GrpcChannel channel)
  {
    List<string> users = [];
    for (int i = 0; i < 1; ++i)
    {
      users.AddRange(ListOfNames);
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
    GrpcChannelOptions channelOpts = new()
    {
      Credentials = Grpc.Core.ChannelCredentials.Insecure
    };
    GrpcChannel channel = GrpcChannel.ForAddress(opts.Endpoint, channelOpts);

    switch (opts.Mode)
    {
      case Mode.UnaryCall:
        UnaryCall(channel);
        break;
      case Mode.ClientStreamingCall:
        ClientStreamingCall(channel);
        break;
      case Mode.ServerStreamingCall:
        ServerStreamingCall(channel);
        break;
      case Mode.BidirectionalStreamingCall:
        BiderctionalStreamingCall(channel);
        break;
    }
  }

  static void ErrorHandling(IEnumerable<Error> errors)
  {
    Console.WriteLine("Fail");
    foreach (var err in errors)
    {
      Console.WriteLine(err.ToString());
    }
  }
}
