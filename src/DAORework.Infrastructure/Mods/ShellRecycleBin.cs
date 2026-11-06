using DAORework.Application.Abstractions;
using Microsoft.VisualBasic.FileIO;

namespace DAORework.Infrastructure.Mods;

/// <summary>The Windows Recycle Bin, through the framework's own wrapper of the shell (ADR-0039). No dialog is shown unless the shell hits an error.</summary>
public sealed class ShellRecycleBin : IRecycleBin
{
    public bool IsAvailable => OperatingSystem.IsWindows();

    public void Send(string path)
    {
        if (Directory.Exists(path))
        {
            FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        }
        else
        {
            FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        }
    }
}
