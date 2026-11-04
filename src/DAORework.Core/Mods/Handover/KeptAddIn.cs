namespace DAORework.Core.Mods.Handover;

/// <summary>An <c>AddInItem</c> as <c>Settings\AddIns.xml</c> had it when the handover began: its UID and its XML, as it is.</summary>
public sealed record KeptAddIn(string Uid, string Item);
