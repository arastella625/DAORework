namespace DAORework.App.ViewModels;

/// <summary>Whether the window knows which files mods share yet (P2.6-61). Reading them all can take a minute on a real library.</summary>
public enum SharedFilesCheck
{
    /// <summary>Not known yet, which is where it starts: nothing may say all is well until it is known.</summary>
    Checking,

    /// <summary>Known, so the counts can be believed.</summary>
    Done,

    /// <summary>The check failed or was stopped, so the counts mean nothing.</summary>
    Failed,
}
