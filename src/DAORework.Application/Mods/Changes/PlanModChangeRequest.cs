using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Mods.Changes;

public sealed record PlanModChangeRequest(string DocumentsDirectory, ModChange Change);
