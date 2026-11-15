using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void EmptySnapshotIsEmpty() => Assert.True(ClipboardSnapshot.Empty.IsEmpty);
}
