namespace DAORework.Application.Abstractions;

/// <summary>Keeps a second window off the same library (ADR-0044). Held until disposed, and let go by the system if the app dies.</summary>
public interface IWindowLock : IDisposable
{
    /// <summary>Takes the lock, or returns false when another copy of the window holds it.</summary>
    bool TryTake();
}
