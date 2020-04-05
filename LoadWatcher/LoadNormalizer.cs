using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using UnityEngine;
using System.IO;
using MonoMod.RuntimeDetour;
using System.Collections;
using System.Reflection;

namespace LoadNormalizer
{
    public class LoadNormalizer : Mod, ITogglableMod
    {
        public static LoadNormalizer instance;
        public static StreamWriter writer;
        public static string lastSceneLoaded;
        public static double lastSceneLoadStart;

        public override void Initialize()
        {
            instance = this;
            On.GameManager.BeginSceneTransition += GetLoadStart;
            On.GameManager.EnterHero += GetLoadEnd;
        }

        private void GetLoadEnd(On.GameManager.orig_EnterHero orig, GameManager self, bool additiveGateSearch)
        {
            bool hazardRespawn;
            try
            {
                hazardRespawn = (bool)typeof(GameManager).GetField("hazardRespawningHero", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GameManager.instance);
            }
            catch (Exception e)
            {
                LogError("Unable to read hazard respawn bool:\n" + e);
                hazardRespawn = false;
            }

            if (!hazardRespawn)
            {
                Log(lastSceneLoaded, Time.realtimeSinceStartup - lastSceneLoadStart); 
                GameManager.instance.StartCoroutine(WaitForNormalizedLoad(lastSceneLoaded, () => orig(self, additiveGateSearch)));
            }
            else
            {
                orig(self, additiveGateSearch);
            }
        }

        private void GetLoadStart(On.GameManager.orig_BeginSceneTransition orig, GameManager self, GameManager.SceneLoadInfo info)
        {
            lastSceneLoaded = info.SceneName;
            lastSceneLoadStart = Time.realtimeSinceStartup;
            orig(self, info);
        }

        public override string GetVersion()
        {
            return LoadTimes.loadTimes.GetHashCode().ToString();
        }

        public IEnumerator WaitForNormalizedLoad(string sceneName, Action finish)
        {
            double loadTime;
            if (!LoadTimes.loadTimes.TryGetValue(sceneName, out loadTime))
            {
                loadTime = LoadTimes.defaultLoad;
            }

            if (Time.realtimeSinceStartup - lastSceneLoadStart > loadTime)
            {
                LogWarn($"Load for {sceneName} exceded standard load time by {Time.realtimeSinceStartup - lastSceneLoadStart > loadTime}");
            }

            yield return new WaitWhile(() => Time.realtimeSinceStartup - lastSceneLoadStart < loadTime);
            finish();
        }

        public static void Log(string sceneName, double loadTime)
        {
            if (writer == null)
            {
                if (new FileInfo(Path.Combine(Application.persistentDataPath, "loadTimes.txt")) is FileInfo info
                    && info.Exists && info.Length > 1000000)
                {
                    info.Delete();
                }

                writer = new StreamWriter(Path.Combine(Application.persistentDataPath, "loadTimes.txt"), append: true);
                writer.AutoFlush = true;
            }
            writer.WriteLine($"{sceneName}: {loadTime}");
        }

        public void Unload()
        {
            On.GameManager.BeginSceneTransition -= GetLoadStart;
            On.GameManager.EnterHero -= GetLoadEnd;
            if (writer != null) writer.Dispose();
        }
    }
}
