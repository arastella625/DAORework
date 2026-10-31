using DAORework.Core.Mods.Apply;

namespace DAORework.Application.Mods.Apply;

public sealed record RecoverModPlanRequest(string DocumentsDirectory, RecoveryChoice Choice);
