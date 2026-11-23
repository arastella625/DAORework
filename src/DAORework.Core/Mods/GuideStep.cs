namespace DAORework.Core.Mods;

/// <summary>One step of the cozy guide (ADR-0051): a heading, two sentences at most, and the button that goes where it's done.</summary>
public sealed record GuideStep(GuideStepKind Kind, string Title, string Text, string Button, bool IsDone);
