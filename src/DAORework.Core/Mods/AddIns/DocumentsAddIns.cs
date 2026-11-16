namespace DAORework.Core.Mods.AddIns;

/// <summary>
/// The add-ins the game loads from the documents folder, with what each gives it (P2.5-23). <see cref="Listed"/> also
/// holds the ones whose folder is elsewhere, such as official DLC in the install folder, which aren't compared.
/// </summary>
/// <param name="Listed">Every add-in <c>AddIns.xml</c> enables.</param>
/// <param name="Contents">The ones with a folder in the documents folder's <c>AddIns</c>, and their file names.</param>
/// <param name="Unreadable">Archives and add-in folders that couldn't be read, documents-relative, so their names aren't compared.</param>
public sealed record DocumentsAddIns(IReadOnlyList<ListedAddIn> Listed, IReadOnlyList<AddInContents> Contents, IReadOnlyList<string> Unreadable)
{
    public static DocumentsAddIns Empty { get; } = new([], [], []);
}
