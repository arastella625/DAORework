namespace DAORework.Core.Mods.AddIns;

/// <summary>The add-ins the game loads and the files they share (P2.5-23).</summary>
/// <param name="LoadOrder">Every add-in <c>AddIns.xml</c> enables, in the order the game loads them: a shared file comes from the one lower down.</param>
/// <param name="Clashes">Each file name two or more of them give the game, and whose copy it uses.</param>
/// <param name="Unreadable">Archives and add-in folders in <c>AddIns</c> that couldn't be read, so what's inside isn't compared.</param>
public sealed record AddInsReport(IReadOnlyList<ListedAddIn> LoadOrder, IReadOnlyList<AddInClash> Clashes, IReadOnlyList<string> Unreadable);
