using System.Security.Cryptography;
using System.Text;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// A named mutex for the data folder (ADR-0044), so a copy working on another folder isn't stopped. The system lets it go when
/// the process ends, however it ends, so a crash can't leave the app refusing to open.
/// </summary>
public sealed class DataFolderWindowLock : IWindowLock
{
    private readonly string _name;
    private Mutex? _held;

    public DataFolderWindowLock(IOptions<DaoReworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var folder = Path.GetFullPath(options.Value.AppDataDirectory).ToUpperInvariant();
        _name = "DAORework-window-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(folder)))[..32];
    }

    public bool TryTake()
    {
        try
        {
            var mutex = new Mutex(initiallyOwned: true, _name, out var first);
            if (first)
            {
                _held = mutex;
                return true;
            }

            mutex.Dispose();
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false; // The name is taken by a copy this one can't open, such as one run as administrator.
        }
    }

    public void Dispose()
    {
        _held?.Dispose();
        _held = null;
    }
}
