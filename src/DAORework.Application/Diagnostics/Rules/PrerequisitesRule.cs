using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;

namespace DAORework.Application.Diagnostics.Rules;

/// <summary>
/// The two runtimes the game's installer bundles: the DirectX 9 June 2010 runtime (checked through one of the
/// D3DX DLLs it installs) and the Visual C++ 2005 runtime (a side-by-side assembly under WinSxS).
/// </summary>
public sealed class PrerequisitesRule : IDiagnosticRule
{
    /// <summary>One of the D3DX9 DLLs the June 2010 runtime installs; the game links against the DirectX 9 line.</summary>
    public const string DirectXProbeDll = "d3dx9_31.dll";

    /// <summary>The side-by-side assembly name prefix of the VC++ 2005 CRT.</summary>
    public const string VcRedistSxsPrefix = "x86_microsoft.vc80.crt_";

    private const string DirectXRecommendation = "Install the DirectX End-User Runtime (June 2010) from Microsoft, or run the game's own _CommonRedist installer.";
    private const string VcRedistRecommendation = "Install the Microsoft Visual C++ 2005 SP1 Redistributable (x86), or run the game's own installer once.";

    private readonly IFileSystem _fileSystem;
    private readonly IKnownFolders _knownFolders;

    public PrerequisitesRule(IFileSystem fileSystem, IKnownFolders knownFolders)
    {
        _fileSystem = fileSystem;
        _knownFolders = knownFolders;
    }

    public Task<IReadOnlyList<DiagnosticIssue>> EvaluateAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_knownFolders.Windows) || string.IsNullOrWhiteSpace(_knownFolders.SystemX86))
        {
            return Task.FromResult<IReadOnlyList<DiagnosticIssue>>(
                [new DiagnosticIssue(DiagnosticSeverity.Info, DiagnosticCodes.PrerequisitesNotChecked, "DirectX and Visual C++ runtimes are only checked on Windows.")]);
        }

        IReadOnlyList<DiagnosticIssue> issues = [CheckDirectX(), CheckVcRedist()];
        return Task.FromResult(issues);
    }

    private DiagnosticIssue CheckDirectX()
    {
        var dll = Path.Combine(_knownFolders.SystemX86, DirectXProbeDll);
        return _fileSystem.File.Exists(dll)
            ? new DiagnosticIssue(DiagnosticSeverity.Info, DiagnosticCodes.DirectXRuntimeFound, $"The DirectX 9 runtime is installed ({DirectXProbeDll} found).")
            : new DiagnosticIssue(DiagnosticSeverity.Warning, DiagnosticCodes.DirectXRuntimeMissing, $"The DirectX 9 runtime was not found ({dll} is missing).", DirectXRecommendation);
    }

    private DiagnosticIssue CheckVcRedist()
    {
        var winSxs = Path.Combine(_knownFolders.Windows, "WinSxS");
        var found = _fileSystem.Directory.Exists(winSxs)
            && _fileSystem.Directory.EnumerateDirectories(winSxs, VcRedistSxsPrefix + "*").Any();
        return found
            ? new DiagnosticIssue(DiagnosticSeverity.Info, DiagnosticCodes.VcRedistFound, "The Visual C++ 2005 runtime is installed.")
            : new DiagnosticIssue(DiagnosticSeverity.Warning, DiagnosticCodes.VcRedistMissing, "The Visual C++ 2005 runtime was not found under WinSxS.", VcRedistRecommendation);
    }
}
