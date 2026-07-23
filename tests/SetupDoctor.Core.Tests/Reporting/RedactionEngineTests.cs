using SetupDoctor.Reporting;

namespace SetupDoctor.Core.Tests.Reporting;

public sealed class RedactionEngineTests
{
    [Fact]
    public void Redact_MasksEmail()
    {
        var result = RedactionEngine.Redact("ユーザー: test@example.com でログイン");
        Assert.DoesNotContain("test@example.com", result);
        Assert.Contains("[EMAIL_REDACTED]", result);
    }

    [Fact]
    public void Redact_MasksLongToken()
    {
        var token = new string('A', 40);
        var result = RedactionEngine.Redact($"token={token}");
        Assert.DoesNotContain(token, result);
        Assert.Contains("[TOKEN_REDACTED]", result);
    }

    [Fact]
    public void Redact_PreservesShortStrings()
    {
        var result = RedactionEngine.Redact("version=1.2.3");
        Assert.Equal("version=1.2.3", result);
    }

    [Fact]
    public void Redact_HandlesNullEmpty()
    {
        Assert.Equal(string.Empty, RedactionEngine.Redact(string.Empty));
        Assert.Equal(string.Empty, RedactionEngine.Redact(""));
    }

    [Fact]
    public void Redact_MasksUserProfilePath()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(profile)) return; // 環境に依存するためスキップ

        var result = RedactionEngine.Redact($"path={profile}\\foo");
        Assert.DoesNotContain(profile, result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("%USERPROFILE%", result);
    }

    [Fact]
    public void Redact_Dictionary_RedactsValues()
    {
        var dict = new Dictionary<string, string>
        {
            ["email"] = "leak@example.com",
            ["version"] = "1.0",
        };
        var result = RedactionEngine.Redact((IReadOnlyDictionary<string, string>)dict);
        Assert.Contains("[EMAIL_REDACTED]", result["email"]);
        Assert.Equal("1.0", result["version"]);
    }
}
