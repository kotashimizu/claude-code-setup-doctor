using SetupDoctor.Core.Abstractions;

namespace SetupDoctor.Core.Policies;

// Cowork(Claude DesktopのVM機能)関連パスの解決。
// パッケージ識別子(Claude_*)は非公式情報のためワイルドカード検索で解決し、固定値に依存しない。
public static class CoworkPaths
{
    public static string? ResolveVmBundlesDirectory(ISystemInfoProvider sys)
    {
        var msixPackagesRoot = sys.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Packages");
        if (Directory.Exists(msixPackagesRoot))
        {
            string[] candidates;
            try
            {
                candidates = Directory.GetDirectories(msixPackagesRoot, "Claude_*");
            }
            catch
            {
                candidates = Array.Empty<string>();
            }

            foreach (var candidate in candidates)
            {
                var msixBundles = Path.Combine(candidate, "LocalCache", "Roaming", "Claude", "vm_bundles");
                if (Directory.Exists(msixBundles))
                    return msixBundles;
            }
        }

        var nonMsixBundles = sys.ExpandEnvironmentVariables(@"%APPDATA%\Claude\vm_bundles");
        if (Directory.Exists(nonMsixBundles))
            return nonMsixBundles;

        return null;
    }
}
