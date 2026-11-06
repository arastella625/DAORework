namespace DAORework.Application.Abstractions;

/// <summary>The Recycle Bin (ADR-0039), so what is removed can still be put back. Windows has one; elsewhere it isn't available.</summary>
public interface IRecycleBin
{
    bool IsAvailable { get; }

    /// <summary>Sends a file or a folder to the Recycle Bin. Throws <see cref="IOException"/> when it can't.</summary>
    void Send(string path);
}
