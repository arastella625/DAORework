namespace DAORework.Application.Fixes;

public sealed record ApplyFixRequest(string InstallDirectory, PlannedFix Planned);
