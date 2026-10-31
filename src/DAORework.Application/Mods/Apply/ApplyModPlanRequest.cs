using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Mods.Apply;

public sealed record ApplyModPlanRequest(string DocumentsDirectory, ModActivationPlan Plan);
