using Modding;
using System.Collections;
using UnityEngine;

namespace LoadNormalizer
{
    public class LoadNormalizer : Mod, IGlobalSettings<GlobalSettings>, ITogglableMod
    {
        public static GlobalSettings GS = new();

        public static LoadNormalizer instance;
        public static StreamWriter writer;
        public static float? loadStart;
        public static float? LoadTime { get => loadStart.HasValue ? Time.realtimeSinceStartup - loadStart.Value : null; }
        private static bool ignoreNextGMLoadedBoss;

        public override void Initialize()
        {
            instance = this;
            LoadTimes.Setup();
            On.SceneLoad.RecordBeginTime += RecordBeginFetchTime;
            On.SceneLoad.BeginRoutine += NormalizeSceneFetchTime;
            On.SceneAdditiveLoadConditional.LoadAll += NormalizeBossLoads;
            On.GameManager.LoadedBoss += InterceptOnLoadedBoss;
        }

        public void Unload()
        {
            On.SceneLoad.RecordBeginTime -= RecordBeginFetchTime;
            On.SceneLoad.BeginRoutine -= NormalizeSceneFetchTime;
            On.SceneAdditiveLoadConditional.LoadAll -= NormalizeBossLoads;
            On.GameManager.LoadedBoss -= InterceptOnLoadedBoss;
        }

        public static void LogLoad(string sceneName, SceneLoad load)
        {
            try
            {
                if (writer == null)
                {
                    if (new FileInfo(Path.Combine(Application.persistentDataPath, "loadTimes.yaml")) is FileInfo info
                        && info.Exists && info.Length > 1000000)
                    {
                        info.Delete();
                    }

                    writer = new StreamWriter(Path.Combine(Application.persistentDataPath, "loadTimes.yaml"), append: true)
                    {
                        AutoFlush = true
                    };
                }
                writer.WriteLine("- ");
                writer.WriteLine($" sceneName: {sceneName}");
                for (SceneLoad.Phases p = 0; p <= SceneLoad.Phases.LoadBoss; p++)
                {
                    float? d = load.GetDuration(p);
                    if (d.HasValue)
                        writer.WriteLine($" {p}: {d.Value}");
                }
            }
            catch (Exception e) { instance.LogError($"Error loading load info:\n{e}"); }
        }

        private static IEnumerator NormalizeBossLoads(On.SceneAdditiveLoadConditional.orig_LoadAll orig)
        {
            float bossLoadStart = Time.realtimeSinceStartup;
            yield return orig();
            float loadTime = Time.realtimeSinceStartup - bossLoadStart;
            if (loadTime < LoadTimes.DefaultBossLoad)
            {
                GameManager.instance.LoadedBoss(); // prevent weird effects from delaying the GameManager.OnLoadedBoss event.
                ignoreNextGMLoadedBoss = true;
                yield return new WaitForSecondsRealtime(LoadTimes.DefaultBossLoad - loadTime);
            }
            else
            {
                float diff = loadTime - LoadTimes.DefaultBossLoad;
                if (diff > 0.05f)
                {
                    instance.LogWarn($"Boss load exceeded standard load time by {diff}.");
                }
            }
        }

        private static void RecordBeginFetchTime(On.SceneLoad.orig_RecordBeginTime orig, SceneLoad self, SceneLoad.Phases phase)
        {
            if (phase == SceneLoad.Phases.Fetch)
            {
                loadStart = Time.realtimeSinceStartup;
            }
            orig(self, phase);
        }

        private static IEnumerator NormalizeSceneFetchTime(On.SceneLoad.orig_BeginRoutine orig, SceneLoad self)
        {
            loadStart = null;
            self.Finish += () =>
            {
                LogLoad(self.TargetSceneName, self);
                float diff = self.GetDuration(SceneLoad.Phases.Fetch).Value - LoadTimes.DefaultLoad;
                if (diff > 0.05f)
                {
                    instance.LogWarn($"Load for {self.TargetSceneName} exceded standard load time by {diff}.");
                };
            };

            IEnumerator routine = orig(self);
            bool MoveNext()
            {
                self.IsActivationAllowed = LoadTime.HasValue && LoadTime > LoadTimes.DefaultLoad;
                return routine.MoveNext();
            }
            while (MoveNext()) yield return routine.Current;
        }

        private void InterceptOnLoadedBoss(On.GameManager.orig_LoadedBoss orig, GameManager self)
        {
            if (ignoreNextGMLoadedBoss)
            {
                ignoreNextGMLoadedBoss = false;
                return;
            }
            orig(self);
        }

        public override string GetVersion()
        {
            return $"{LoadTimes.DefaultLoad.ToString("G", System.Globalization.CultureInfo.InvariantCulture)}; {LoadTimes.DefaultBossLoad.ToString("G", System.Globalization.CultureInfo.InvariantCulture)}";
        }

        void IGlobalSettings<GlobalSettings>.OnLoadGlobal(GlobalSettings s)
        {
            GS = s;
        }

        GlobalSettings IGlobalSettings<GlobalSettings>.OnSaveGlobal()
        {
            return GS;
        }
    }
}
