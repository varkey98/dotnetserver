using CommandLine;

public class Flags
{
    [Option('m', "mode", Required = true, HelpText = "The type of call for the client to make")]
    public Mode Mode { get; set; }

    [Option('e', "endpoint", Required = true, HelpText = "The endpoint of the server")]
    public required string Endpoint { get; set; }
}

[Flags]
public enum Mode
{
    UnaryCall,
    ClientStreamingCall,
    ServerStreamingCall,
    BidirectionalStreamingCall
}
