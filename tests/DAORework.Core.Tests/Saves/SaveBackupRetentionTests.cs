using DAORework.Core.Diagnostics;
using DAORework.Core.Saves;
using Xunit;

namespace DAORework.Core.Tests.Saves;

public sealed class SaveBackupRetentionTests
{
    private static readonly DateTimeOffset _start = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(SaveBackupRetention.MaximumKeepLatest + 1)]
    public void Create_OutOfRange_Fails(int keepLatest)
    {
        Assert.Equal(DiagnosticCodes.SaveBackupRetentionInvalid, SaveBackupRetention.Create(keepLatest).Error!.Code);
    }

    [Fact]
    public void Expired_KeepsTheNewestRegardlessOfInputOrder()
    {
        var backups = Enumerable.Range(0, 5).Select(day => new SaveBackup($"b{day}.zip", _start.AddDays(day), 1)).Reverse().Skip(1).Append(new SaveBackup("b4.zip", _start.AddDays(4), 1)).ToList();

        var expired = SaveBackupRetention.Create(2).Value.Expired(backups);

        Assert.Equal(["b2.zip", "b1.zip", "b0.zip"], expired.Select(b => b.Path));
    }

    [Fact]
    public void Expired_FewerThanTheLimit_DeletesNothing()
    {
        Assert.Empty(SaveBackupRetention.Default.Expired([new SaveBackup("a.zip", _start, 1)]));
    }
}
