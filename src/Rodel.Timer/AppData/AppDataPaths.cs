using Sprout;

using Windows.Storage;

namespace Rodel.Timer.AppData;

internal static class AppDataPaths
{
    private const string DataFileName = "Rodel.Timer.v1.json";

    public static string TimerDataFile()
    {
        if (PackageIdentity.IsPackaged)
        {
            return Path.Combine(ApplicationData.Current.LocalFolder.Path, DataFileName);
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Rodel.Timer",
            DataFileName);
    }
}
