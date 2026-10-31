using DAORework.Core.Profiles;

namespace DAORework.Application.Mods;

public sealed record BuildModInventoryRequest(RuntimeProfile Profile, bool IncludeHashes = false);
