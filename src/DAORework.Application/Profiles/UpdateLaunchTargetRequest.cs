using DAORework.Core.Launching;

namespace DAORework.Application.Profiles;

public sealed record UpdateLaunchTargetRequest(string Name, LaunchTarget Target);
