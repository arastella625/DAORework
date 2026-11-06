using DAORework.Core.Mods.Apply;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Mods.Apply;

public sealed record ApplyModPlanRequest(string DocumentsDirectory, ModActivationPlan Plan)
{
    /// <summary>Gets what is told how far the change has got, after each step (P2.6-22).</summary>
    public IProgress<ChangeSteps>? Progress { get; init; }
}
