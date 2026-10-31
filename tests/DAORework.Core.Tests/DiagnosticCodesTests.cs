using System.Reflection;
using System.Text.RegularExpressions;
using DAORework.Core.Diagnostics;
using Xunit;

namespace DAORework.Core.Tests;

public sealed class DiagnosticCodesTests
{
    private static readonly Regex _codeFormat = new("^DAO_[A-Z0-9]+(_[A-Z0-9]+)+$", RegexOptions.CultureInvariant);

    private static List<(string Name, string Value)> AllCodes() =>
        typeof(DiagnosticCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (f.Name, (string)f.GetRawConstantValue()!))
            .ToList();

    [Fact]
    public void AllCodes_FollowTheDaoAreaConditionFormat()
    {
        var offenders = AllCodes().Where(c => !_codeFormat.IsMatch(c.Value)).ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void AllCodes_AreUnique()
    {
        var duplicates = AllCodes().GroupBy(c => c.Value).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Catalog_IsNotEmpty()
    {
        Assert.NotEmpty(AllCodes());
    }
}
