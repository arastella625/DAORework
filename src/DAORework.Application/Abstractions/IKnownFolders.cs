namespace DAORework.Application.Abstractions;

/// <summary>Well-known machine folders (ADR-0003). Implemented in Infrastructure; faked in tests.</summary>
public interface IKnownFolders
{
    /// <summary>Gets the Documents folder the shell reports. With OneDrive Known Folder Move this is inside OneDrive.</summary>
    string MyDocuments { get; }

    /// <summary>Gets the user's profile folder, whose classic <c>Documents</c> child may hold pre-redirection saves.</summary>
    string UserProfile { get; }

    /// <summary>Gets the OneDrive root, or null when OneDrive is not set up.</summary>
    string? OneDrive { get; }

    string LocalApplicationData { get; }

    string ProgramFiles { get; }

    string ProgramFilesX86 { get; }

    /// <summary>Gets the Windows folder, or an empty string off Windows.</summary>
    string Windows { get; }

    /// <summary>Gets the 32-bit system folder (<c>SysWOW64</c> on 64-bit Windows), or an empty string off Windows.</summary>
    string SystemX86 { get; }
}
