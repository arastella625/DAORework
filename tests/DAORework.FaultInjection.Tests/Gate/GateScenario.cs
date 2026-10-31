namespace DAORework.FaultInjection.Tests.Gate;

/// <summary>
/// One change the gate kills at every write: the commands that set up its starting state, the command itself, and
/// the command group ("mods" or "fixes") whose recover, status and rollback settle it. Mod ids are written as their
/// placeholders. <see cref="OnInstall"/> says whether the change is to the install folder rather than the documents folder.
/// </summary>
internal sealed record GateScenario(string Name, string Group, bool OnInstall, IReadOnlyList<string[]> Setup, string[] Command)
{
    public static GateScenario Activate { get; } = new("activate two mods", "mods", false, [], ["mods", "activate", "{addin}", "{override}", "-p", GateFixtures.Profile, "--yes"]);

    public static GateScenario Deactivate { get; } = new(
        "deactivate the add-in",
        "mods",
        false,
        [["mods", "activate", "{addin}", "{override}", "-p", GateFixtures.Profile, "--yes"]],
        ["mods", "deactivate", "{addin}", "-p", GateFixtures.Profile, "--yes"]);

    public static GateScenario Switch { get; } = new(
        "activate a mod that replaces another's file",
        "mods",
        false,
        [["mods", "activate", "{override}", "-p", GateFixtures.Profile, "--yes"]],
        ["mods", "activate", "{override-hd}", "-p", GateFixtures.Profile, "--yes"]);

    public static GateScenario ApplyLargeAddressAware { get; } = new("apply the 4 GB patch", "fixes", true, [], ["fixes", "apply", "laa", "-p", GateFixtures.Profile, "--yes"]);

    public static GateScenario RemoveLargeAddressAware { get; } = new(
        "remove the 4 GB patch",
        "fixes",
        true,
        [["fixes", "apply", "laa", "-p", GateFixtures.Profile, "--yes"]],
        ["fixes", "remove", "laa", "-p", GateFixtures.Profile, "--yes"]);

    public static GateScenario ApplyDxvk { get; } = new("apply DXVK", "fixes", true, [], ["fixes", "apply", "dxvk", "{dxvk-2.3}", "-p", GateFixtures.Profile, "--yes"]);

    public static GateScenario UpgradeDxvk { get; } = new(
        "upgrade DXVK over the one DAORework placed",
        "fixes",
        true,
        [["fixes", "apply", "dxvk", "{dxvk-2.2}", "-p", GateFixtures.Profile, "--yes"]],
        ["fixes", "apply", "dxvk", "{dxvk-2.3}", "-p", GateFixtures.Profile, "--yes"]);

    public static GateScenario RemoveDxvk { get; } = new(
        "remove DXVK",
        "fixes",
        true,
        [["fixes", "apply", "dxvk", "{dxvk-2.3}", "-p", GateFixtures.Profile, "--yes"]],
        ["fixes", "remove", "dxvk", "-p", GateFixtures.Profile, "--yes"]);

    public static GateScenario AdoptAddInInPlace { get; } = new("adopt an add-in where it is", "mods", false, [], ["mods", "adopt", "addin:hand_addin", "-p", GateFixtures.Profile, "--yes"]);

    public static GateScenario AdoptOverrideFolder { get; } = new("adopt an override folder", "mods", false, [], ["mods", "adopt", "override:Hand Hair", "-p", GateFixtures.Profile, "--yes"]);

    public static GateScenario AdoptLooseFiles { get; } = new(
        "adopt the loose override files",
        "mods",
        false,
        [],
        ["mods", "adopt", "override:(loose)", "--name", "Loose Gate", "-p", GateFixtures.Profile, "--yes"]);

    public static GateScenario AdoptDuplicateNames { get; } = new(
        "adopt two folders sharing a file name",
        "mods",
        false,
        [],
        ["mods", "adopt", "override:Hand Hair", "override:Hand Hair Alt", "-p", GateFixtures.Profile, "--yes"]);

    public string[] Status => [Group, "status", "-p", GateFixtures.Profile];

    public string[] Rollback => [Group, "rollback", "-p", GateFixtures.Profile, "--yes"];

    public string[] Recover(bool finish) => [Group, "recover", "-p", GateFixtures.Profile, finish ? "--finish" : "--rollback"];
}
