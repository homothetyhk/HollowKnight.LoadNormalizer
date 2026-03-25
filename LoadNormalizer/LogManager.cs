using UnityEngine;

namespace LoadNormalizer;

internal static class LogManager
{
    const string FileName = "loadTimes.yaml";

    private static StreamWriter Writer
    {
        get
        {
            if (field == null)
            {
                if (new FileInfo(Path.Combine(Application.persistentDataPath, FileName)) is FileInfo info
                    && info.Exists && info.Length > 1000000)
                {
                    info.Delete();
                }

                field = new StreamWriter(Path.Combine(Application.persistentDataPath, FileName), append: true)
                {
                    AutoFlush = true
                };
            }
            return field;
        }
    }

    public static void LogLoad(string sceneName, List<float> loadTimes, float normalization)
    {
        try
        {
            StreamWriter writer = Writer;
            writer.WriteLine("- ");
            writer.WriteLine($" sceneName: {sceneName}");
            for (int i = 0; i < loadTimes.Count; i++)
            {
                float time = loadTimes[i];
                writer.WriteLine($" {GlobalSettings.Keys[i]}: {time}");
            }
            writer.WriteLine($" Normalization: {normalization}");
        }
        catch (Exception e) { LoadNormalizer.instance.LogError($"Error reading load info:\n{e}"); }
    }
}
