namespace DAORework.Core.Mods.Apply;

/// <summary>How to settle an apply that did not finish: complete its remaining steps, or restore its backup.</summary>
public enum RecoveryChoice
{
    Finish,
    RollBack,
}
