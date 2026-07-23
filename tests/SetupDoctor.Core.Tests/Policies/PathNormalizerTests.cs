using SetupDoctor.Core.Policies;

namespace SetupDoctor.Core.Tests.Policies;

public sealed class PathNormalizerTests
{
    [Theory]
    [InlineData(@"C:\Users\志水\.local\bin", @"c:\users\志水\.local\bin")]
    [InlineData(@"C:\Foo\", @"c:\foo")]        // 末尾スラッシュ除去
    [InlineData(@"""C:\Foo""", @"c:\foo")]      // クォート除去
    public void Normalize_ReturnsExpected(string input, string expected)
        => Assert.Equal(expected, PathNormalizer.Normalize(input));

    [Fact]
    public void Split_HandlesEmpty()
    {
        var entries = PathNormalizer.Split(null);
        Assert.Empty(entries);
    }

    [Fact]
    public void Split_IgnoresEmptySegments()
    {
        var entries = PathNormalizer.Split(@"C:\Foo;;C:\Bar;");
        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public void Contains_CaseInsensitive()
    {
        var path = @"C:\WINDOWS\System32;C:\Users\test\.local\bin";
        Assert.True(PathNormalizer.Contains(path, @"C:\Users\Test\.local\bin"));
    }

    [Fact]
    public void Contains_FalseWhenNull()
        => Assert.False(PathNormalizer.Contains(null, @"C:\foo"));

    [Fact]
    public void Append_AddsEntry()
    {
        var result = PathNormalizer.Append(null, @"C:\New\Bin");
        Assert.Equal(@"C:\New\Bin", result);
    }

    [Fact]
    public void Append_NoDuplicates()
    {
        var existing = @"C:\Windows;C:\Users\test\.local\bin";
        var result = PathNormalizer.Append(existing, @"C:\Users\Test\.local\bin");
        // 重複なし → 変更なし
        Assert.Equal(existing, result);
    }

    [Fact]
    public void Append_JapaneseUsername()
    {
        var result = PathNormalizer.Append(@"C:\Windows", @"C:\Users\志水\.local\bin");
        Assert.Contains(@"C:\Users\志水\.local\bin", result);
    }

    [Fact]
    public void Split_SpaceInPath()
    {
        var path = @"C:\Program Files\Git\bin;C:\Windows";
        var entries = PathNormalizer.Split(path);
        Assert.Equal(2, entries.Count);
        Assert.Contains(@"C:\Program Files\Git\bin", entries);
    }
}
