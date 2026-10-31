using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Mods.Backups;

public sealed record BackUpDeploymentRequest(string DocumentsDirectory, ModActivationPlan Plan);
