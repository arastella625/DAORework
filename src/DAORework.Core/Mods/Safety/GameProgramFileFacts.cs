using DAORework.Core.Formats;
using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Safety;

/// <summary>What a mod's file is, from its first bytes: whether it has a Windows program header, and what the header says.</summary>
public sealed record GameProgramFileFacts(GameProgramFile File, bool HasExecutableHeader, PortableExecutableImage? Image);
