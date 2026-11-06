namespace DAORework.Core.Mods.Apply;

/// <summary>How far a running change has got: <see cref="Done"/> of its <see cref="Total"/> steps (P2.6-22).</summary>
public sealed record ChangeSteps(int Done, int Total);
