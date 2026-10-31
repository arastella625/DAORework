using DAORework.Core.Profiles;

namespace DAORework.Application.Profiles;

public sealed record UpdateProfilePathRequest(string Name, ProfilePathKind Kind, string Path);
