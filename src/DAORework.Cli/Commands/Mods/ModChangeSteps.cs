using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Changes;

namespace DAORework.Cli.Commands.Mods;

/// <summary>Planning and applying a change in the documents folder, grouped so the mod change commands stay within five dependencies (ENGINEERING_STANDARDS §3).</summary>
public sealed record ModChangeSteps(PlanTurnOn Plan, ApplyModPlan Apply);
