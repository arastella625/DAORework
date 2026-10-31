using System.Globalization;
using DAORework.FaultInjection.Tests.Host;

namespace DAORework.FaultInjection.Tests;

/// <summary>
/// The entry point when the gate starts this assembly as the CLI to kill (ADR-0014):
/// <c>host &lt;killAt&gt; &lt;appData&gt; &lt;cli arguments…&gt;</c>. A <c>killAt</c> of -1 never kills.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return args.Length >= 3 && args[0] == FaultHost.Verb
            ? FaultHost.Run(int.Parse(args[1], CultureInfo.InvariantCulture), args[2], args[3..])
            : 2;
    }
}
