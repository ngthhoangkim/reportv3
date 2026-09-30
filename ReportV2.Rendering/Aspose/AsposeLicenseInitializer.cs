using Aspose.Words;

namespace ReportV2.Rendering.Aspose;

public static class AsposeLicenseInitializer
{
    private static int _loaded;

    public static void EnsureLoaded(string? licensePath)
    {
        if (Interlocked.CompareExchange(ref _loaded, 1, 0) != 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(licensePath) || !File.Exists(licensePath))
        {
            return;
        }

        var license = new License();
        license.SetLicense(licensePath);
    }
}
