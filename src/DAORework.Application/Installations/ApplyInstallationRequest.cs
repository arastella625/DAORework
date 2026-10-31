using DAORework.Core.Installations;

namespace DAORework.Application.Installations;

public sealed record ApplyInstallationRequest(string ProfileName, Installation Installation);
