namespace DAORework.Cli.Composition;

/// <summary>Process exit codes (SDD §9.1).</summary>
public static class ExitCodes
{
    public const int Success = 0;
    public const int Failure = 1;
    public const int InvalidUsage = 2;
    public const int Unexpected = 70;
}
