using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Text;

namespace LoadNormalizer;

public class GlobalSettings
{
    public static ReadOnlyCollection<string> Keys = new(
    [
        nameof(SceneLoad.Phases.FetchBlocked),
        nameof(SceneLoad.Phases.Fetch),
        nameof(SceneLoad.Phases.ActivationBlocked),
        nameof(SceneLoad.Phases.Activation),
        nameof(SceneLoad.Phases.UnloadUnusedAssets),
        nameof(SceneLoad.Phases.GarbageCollect),
        nameof(SceneLoad.Phases.StartCall),
        nameof(SceneLoad.Phases.LoadBoss),
        nameof(Interphase),
        "ActivationDisallowed",
    ]);

    public static GlobalSettings Default { get; } = new()
    {
        AutoUpdate = new() { Version = AutoUpdate.CurrentVersion, UpdateSettingsOnNewVersion = true },
        AllowFetchBeforeCameraFade = true,
        Fetch = 0.6f,
        Activation = 0.05f,
        UnloadUnusedAssets = 0.3f,
        GarbageCollect = 0.15f,
        StartCall = 0.3f,
        LoadBoss = 0.4f,
        Interphase = 0.05f,
        IgnoreLoadsFrom = ["Room_Mender_House"],
        IgnoreLoadsInto = ["Room_Mender_House"],
    };

    public AutoUpdate AutoUpdate { get; init; }

    public bool AllowFetchBeforeCameraFade { get; init; }
    [JsonIgnore] public float FetchBlocked { get => float.NaN; }
    public float Fetch { get; init; }
    [JsonIgnore] public float ActivationBlocked { get => float.NaN; }
    public float Activation { get; init; }
    public float UnloadUnusedAssets { get; init; }
    public float GarbageCollect { get; init; }
    public float StartCall { get; init; }
    public float LoadBoss { get; init; }
    public float Interphase { get; init; }
    public HashSet<string> IgnoreLoadsInto { get; init; } = [];
    public HashSet<string> IgnoreLoadsFrom { get; init; } = [];

    public float GetThreshold(int phase) => phase switch
    {
        0 => FetchBlocked,
        1 => Fetch,
        2 => ActivationBlocked,
        3 => Activation,
        4 => UnloadUnusedAssets,
        5 => GarbageCollect,
        6 => StartCall,
        7 => LoadBoss,
        8 => Interphase,
        _ => throw new NotImplementedException(),
    };

    public byte[] GetHashableData()
    {
        List<byte> data = [];
        data.AddRange(BitConverter.GetBytes(AllowFetchBeforeCameraFade));
        float[] fields = [Fetch, Activation, UnloadUnusedAssets, GarbageCollect, StartCall, LoadBoss, Interphase];
        data.AddRange(fields.SelectMany(BitConverter.GetBytes));
        data.AddRange(IgnoreLoadsFrom.OrderBy(s => s).SelectMany(Encoding.UTF8.GetBytes));
        data.AddRange(IgnoreLoadsInto.OrderBy(s => s).SelectMany(Encoding.UTF8.GetBytes));
        return [.. data];
    }
}
