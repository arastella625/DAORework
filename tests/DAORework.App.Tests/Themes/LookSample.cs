using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using DAORework.App.Controls;

namespace DAORework.App.Tests.Themes;

/// <summary>Builds the sample screen for <see cref="LookSampleTests"/> from the shared styles, with example data.</summary>
internal static class LookSample
{
    public static Control Build(TextBlock title) => Stack(
        16,
        Stack(6, title, new RuneDivider()),
        Columns(Folder("override", "1,204", "19", "1.9 GB"), Folder("AddIns", "2,317", "11", "3.4 GB")),
        InstalledByHand(),
        Severity("Error", "IconError", "DAO_LAUNCHER_EXECUTABLE_NOT_FOUND", "LaunchTarget is OfficialLauncher, but LauncherExecutablePath is not set."),
        Severity("Warning", "IconWarning", "DAO_LAA_DISABLED", "The game executable is limited to 2 GB of memory."),
        Severity("Info", "IconInfo", "DAO_CONFIG_FOUND", "Settings file found (14 sections, 2560x1440)."),
        Columns(AdoptDialog(), RollbackDialog()));

    public static TextBlock Text(string text, params string[] classes)
    {
        var block = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
        block.Classes.AddRange(classes);
        return block;
    }

    private static Card Folder(string name, string files, string folders, string size)
    {
        var facts = new Grid { ColumnDefinitions = new("Auto,*"), RowDefinitions = new("Auto,Auto,Auto"), ColumnSpacing = 14 };
        string[][] rows = [["Files", files], ["Folders", folders], ["Size", size]];
        for (var i = 0; i < rows.Length; i++)
        {
            facts.Children.Add(At(Text(rows[i][0], "Secondary"), i, 0));
            facts.Children.Add(At(Text(rows[i][1]), i, 1));
        }

        return new Card { Content = Stack(4, Text(name, "SectionTitle"), Text($@"C:\Users\Player\Documents\BioWare\Dragon Age\{name}", "Mono", "Secondary"), facts) };
    }

    private static Card InstalledByHand() => new()
    {
        Content = Stack(
            8,
            Text("Installed by hand", "SectionTitle"),
            Text("DAORework did not place these. Adopt one to manage it from now on, without reinstalling it.", "Secondary"),
            Candidate("Qwinn's Ultimate DAO Fixpack", "addin:qwinn_fixpack_3", catalog: true),
            Candidate("Loose override files", "override:(loose)", catalog: false),
            Text("CANNOT BE ADOPTED", "Label"),
            Line(Icon("IconLock", "Muted"), Text("Dragon Age: Awakening and 5 more official DLC · never adopted or changed.", "Secondary"))),
    };

    private static Grid Candidate(string name, string id, bool catalog)
    {
        var heading = Line(Text(name));
        if (catalog)
        {
            heading.Children.Add(new Border { Classes = { "Badge" }, Child = Text("In the catalog") });
        }

        return Columns(Stack(2, heading, Text(id, "Mono", "Secondary")), new Button { Content = "Adopt…", HorizontalAlignment = HorizontalAlignment.Right });
    }

    private static Border Severity(string level, string icon, string code, string message) => new()
    {
        Classes = { "Severity", level },
        Child = Line(Icon(icon, level), Text(level, level), Text(code, "Mono"), Text(message)),
    };

    private static Border AdoptDialog() => Dialog(
        "Adopt Ostagar Night Lighting?",
        string.Empty,
        Stack(8, Line(Icon("IconShield", "Ok"), Text("Back up everything this plan touches")), Line(Icon("IconMove", "Info"), Text(@"Move 212 files into override\DAORework\"))),
        Callout("Warning", "IconWarning", "Differs from the catalog copy: 2 files. Imported from disk as they are."),
        Line(new Button { Content = "Cancel" }, new Button { Content = "Adopt 212 files", Classes = { "accent" } }));

    private static Border RollbackDialog() => Dialog(
        "Roll back to 27 Oct, 21:14?",
        "Danger",
        Stack(8, Line(Icon("IconMinus", "Error"), Text("Deactivate 3 mods activated after this backup")), Line(Icon("IconMove", "Info"), Text("Restore 14 files from the backup"))),
        Callout("Danger", "IconError", "This removes Ostagar Night Lighting and 2 more from the game folder."),
        Line(new Button { Content = "Cancel" }, new Button { Content = "Roll back", Classes = { "danger" } }));

    private static Border Dialog(string title, string rule, params Control[] body)
    {
        var bar = new Border { Classes = { "EmphasisRule" } };
        if (rule.Length > 0)
        {
            bar.Classes.Add(rule);
        }

        return new Border { Classes = { "Dialog" }, Child = Stack(14, [Text(title, "DialogTitle"), bar, .. body]) };
    }

    private static Border Callout(string kind, string icon, string text)
    {
        var body = new Grid { ColumnDefinitions = new("Auto,*"), ColumnSpacing = 10 };
        body.Children.Add(Icon(icon, kind == "Danger" ? "Error" : kind));
        body.Children.Add(At(Text(text), 0, 1));
        return new Border { Classes = { "Callout", kind }, Child = body };
    }

    private static PathIcon Icon(string geometry, string kind) =>
        new() { Data = (Geometry)Avalonia.Application.Current!.FindResource(geometry)!, Classes = { kind } }; // Shared.axaml.

    private static StackPanel Stack(double spacing, params Control[] children)
    {
        var panel = new StackPanel { Spacing = spacing };
        panel.Children.AddRange(children);
        return panel;
    }

    private static StackPanel Line(params Control[] children)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        panel.Children.AddRange(children);
        return panel;
    }

    private static Grid Columns(Control left, Control right) =>
        new() { ColumnDefinitions = new("*,*"), ColumnSpacing = 12, Children = { At(left, 0, 0), At(right, 0, 1) } };

    private static T At<T>(T control, int row, int column)
        where T : Control
    {
        Grid.SetRow(control, row);
        Grid.SetColumn(control, column);
        return control;
    }
}
