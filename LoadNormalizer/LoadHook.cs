using Modding;
using UnityEngine;
using System.Collections;
using MonoMod.RuntimeDetour;

namespace LoadNormalizer;

internal static class LoadHook
{
    private static readonly List<IDisposable> hooks = [];
    private static readonly Dictionary<SceneLoad, (float?, float?)> activationBlockedTimes = new(ReferenceEqualityComparer<SceneLoad>.Instance);

    public static void Hook()
    {
        On.SceneLoad.BeginRoutine += OnBeginRoutine;
        if (LoadNormalizer.GS.AllowFetchBeforeCameraFade)
        {
            hooks.Add(new Hook(ReflectionHelper.GetPropertyInfo(typeof(SceneLoad), nameof(SceneLoad.IsActivationAllowed)).SetMethod, OnSetIsActivationAllowed));
        }
    }

    public static void Unhook()
    {
        On.SceneLoad.BeginRoutine -= OnBeginRoutine;
        foreach (IDisposable hook in hooks) hook.Dispose();
        hooks.Clear();
        activationBlockedTimes.Clear();
    }

    private static void OnSetIsActivationAllowed(Action<SceneLoad, bool> orig, SceneLoad load, bool value)
    {
        if (!value)
        {
            activationBlockedTimes[load] = (Time.realtimeSinceStartup, null);
        }
        else if (activationBlockedTimes.TryGetValue(load, out (float?, float?) p))
        {
            activationBlockedTimes[load] = (p.Item1, Time.realtimeSinceStartup);
        }
        orig(load, value);
    }

    private static IEnumerator OnBeginRoutine(On.SceneLoad.orig_BeginRoutine orig, SceneLoad self)
    {
        if (self.TargetSceneName == "Opening_Sequence" // breaks due to the different type of load in
            || LoadNormalizer.GS.IgnoreLoadsInto.Contains(self.TargetSceneName)
            || LoadNormalizer.GS.IgnoreLoadsFrom.Contains(GameManager.instance.sceneName)) 
        {
            yield return orig(self);
            yield break;
        }

        self.IsFetchAllowed = true;
        IEnumerator enumerator = orig(self);
        SceneLoad.FinishDelegate del = ReflectionHelper.GetField<SceneLoad, SceneLoad.FinishDelegate>(self, nameof(SceneLoad.Finish));
        ReflectionHelper.SetField<SceneLoad, SceneLoad.FinishDelegate>(self, nameof(SceneLoad.Finish), null!);

        while (enumerator.MoveNext()) yield return enumerator.Current;

        float timeScale = Time.timeScale;
        Time.timeScale = 0f;

        string targetScene = self.TargetSceneName;
        float interphaseTime = Time.realtimeSinceStartup - self.BeginTime!.Value;
        float normalization = 0f;
        List<float> times = [];
        float? activationDisallowedDuration = activationBlockedTimes.TryGetValue(self, out var p) ? p.Item2 - p.Item1 : null;
        activationBlockedTimes.Remove(self);

        for (SceneLoad.Phases phase = SceneLoad.Phases.FetchBlocked; phase <= SceneLoad.Phases.LoadBoss; phase++)
        {
            if (self.GetDuration(phase) is not float duration)
            {
                times.Add(float.NaN);
                continue;
            }
            interphaseTime -= duration;

            switch (phase)
            {
                case SceneLoad.Phases.Fetch when activationDisallowedDuration is not null:
                    times.Add(duration);
                    HandleFetchThreshold(targetScene, duration, activationDisallowedDuration.Value, ref normalization);
                    break;
                // if unload is not required, then the duration is 0, and should not be normalized
                // we use NaN to also omit from the logged data.
                case SceneLoad.Phases.UnloadUnusedAssets when !self.IsUnloadAssetsRequired:
                    times.Add(float.NaN); 
                    break;
                default:
                    times.Add(duration);
                    HandleThreshold(targetScene, (int)phase, duration, ref normalization);
                    break;
                /*
                    Remarks:
                    - IsUnloadAssetsRequired is true when loading in from the menu, respawning, warping to bench, etc.
                    - IsUnloadAssetsRequired may also be true if a very large amount of memory is in use (2202009600 bytes for area transitions, 2621440000 for ordinary transitions).
                      - In practice, it does not seem like this memory threshold is reached in ordinary play.
                    - IsGarbageCollectRequired is true on desktop. There are some platform-specific checks that could allow it to be false on other platforms.
                */
            }
        }

        times.Add(interphaseTime);
        HandleThreshold(targetScene, 8, interphaseTime, ref normalization);

        times.Add(activationDisallowedDuration ?? float.NaN);

        yield return new WaitForSecondsRealtime(normalization);
        Time.timeScale = timeScale;

        LogManager.LogLoad(targetScene, times, normalization);

        try
        {
            del?.Invoke();
        }
        catch (Exception e)
        {
            LoadNormalizer.instance.LogError("Exception in responders to SceneLoad.Finish:\n" + e);
        }
    }

    private static void HandleFetchThreshold(string scene, float fetchDuration, float activationDisallowedDuration, ref float normalization)
    {
        const int phase = (int)SceneLoad.Phases.Fetch;

        float fetchThreshold = GetThreshold(phase);
        if (float.IsNaN(fetchThreshold)) return;

        float undershoot = fetchThreshold - fetchDuration;
        if (undershoot < -0.05f)
        {
            Warn(scene, phase, -undershoot);
        }
        if (undershoot < 0f) return;

        // we assume that FT and ADD are the same for all players. FD varies between players, but is always less than FT.
        // if FT < ADD, all players are normalized to ADD by the game regardless of FD, so no further normalization is needed.
        // if FD < ADD < FT, the game normalizes the player to ADD, so we should add FT-ADD to reach FT.
        // if ADD < FD < FT, then the game does not normalize the player at all, so we should add FT - FD.
        // in all 3 cases, the amount we add is max(ADD, FT) - max(ADD, FD).

        normalization += Math.Max(activationDisallowedDuration, fetchThreshold) - Math.Max(activationDisallowedDuration, fetchDuration);
    }

    private static void HandleThreshold(string scene, int phase, float duration, ref float normalization)
    {
        float threshold = GetThreshold(phase);
        if (float.IsNaN(threshold)) return;

        float undershoot = threshold - duration;
        if (undershoot < -0.05f)
        {
            Warn(scene, phase, -undershoot);
        }
        if (undershoot > 0f)
        {
            normalization += undershoot;
        }
    }

    private static float GetThreshold(int phase)
    {
        return LoadNormalizer.GS.GetThreshold(phase);
    }

    private static void Warn(string sceneName, int phase, float overshoot)
    {
        LoadNormalizer.instance.LogWarn($"{GlobalSettings.Keys[phase]} of load into {sceneName} exceeded threshold by {overshoot}.");
    }
}


file class ReferenceEqualityComparer<T> : EqualityComparer<T> where T : class
{
    public static ReferenceEqualityComparer<T> Instance { get; } = new();

    public override bool Equals(T x, T y)
    {
        return ReferenceEquals(x, y);
    }

    public override int GetHashCode(T obj)
    {
        return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}