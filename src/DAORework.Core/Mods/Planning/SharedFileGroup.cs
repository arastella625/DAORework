namespace DAORework.Core.Mods.Planning;

/// <summary>The files one set of mods shares, as a large plan or <i>Combined files</i> shows them.</summary>
/// <param name="Title">Who wins, such as <i>JB3 wins over Commoner Clothes</i>.</param>
/// <param name="Kinds">What they are, such as <i>120 textures and models</i>.</param>
/// <param name="FileNames">Each file.</param>
/// <param name="SameContent">Whether every copy of every file is the same.</param>
public sealed record SharedFileGroup(string Title, string Kinds, IReadOnlyList<string> FileNames, bool SameContent);
