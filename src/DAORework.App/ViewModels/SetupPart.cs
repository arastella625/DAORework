namespace DAORework.App.ViewModels;

/// <summary>One kind of mod on Home's readiness strip (ADR-0026): what is on, and whether it is ready.</summary>
/// <param name="Title">*Add-ins*, *Override mods* or *Combined files*.</param>
/// <param name="Count">What is there, such as *3 on · 2 found in your game*.</param>
/// <param name="Status">*Ready*, or what needs a look, such as *1 overlap*.</param>
/// <param name="IsReady">Whether nothing about this kind needs a look.</param>
/// <param name="Target">Where the part opens from Home.</param>
/// <param name="Kind">The kind the part filters the Mods tab to.</param>
/// <param name="IsChecking">Whether the answer isn't known yet, so the part neither passes nor asks for a look.</param>
public sealed record SetupPart(string Title, string Count, string Status, bool IsReady, HomeTarget Target, ModKindFilter Kind, bool IsChecking = false)
{
    public bool NeedsALook => !IsReady && !IsChecking;
}
