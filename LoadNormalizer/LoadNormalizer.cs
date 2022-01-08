using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using UnityEngine;
using System.IO;
using MonoMod.RuntimeDetour;
using MonoMod;
using MonoMod.Cil;
using System.Collections;
using System.Reflection;
using Mono.Cecil.Cil;

namespace LoadNormalizer
{
    public class LoadNormalizer : Mod, IGlobalSettings<GlobalSettings>, ITogglableMod
    {
        public static GlobalSettings GS = new();

        public static LoadNormalizer instance;
        public static StreamWriter writer;
        public static string lastSceneLoaded;
        public static double lastSceneLoadStart;
        public static bool ignoreNextLoadFinish;

        public override void Initialize()
        {
            instance = this;
            LoadTimes.Setup();
            On.GameManager.BeginSceneTransition += GetLoadStart;
            On.GameManager.EnterHero += GetLoadEnd;
        }

        private void GetLoadEnd(On.GameManager.orig_EnterHero orig, GameManager self, bool additiveGateSearch)
        {
            if (IgnoreLoad() || ignoreNextLoadFinish)
            {
                ignoreNextLoadFinish = false;
                orig(self, additiveGateSearch);
                return;
            }

            LogLoad(lastSceneLoaded, Time.realtimeSinceStartup - lastSceneLoadStart);
            GameManager.instance.StartCoroutine(WaitForNormalizedLoad(lastSceneLoaded, () => orig(self, additiveGateSearch)));
        }

        private void GetLoadStart(On.GameManager.orig_BeginSceneTransition orig, GameManager self, GameManager.SceneLoadInfo info)
        {
            lastSceneLoaded = info.SceneName;
            if (!IgnoreLoad())
            {
                lastSceneLoadStart = Time.realtimeSinceStartup;
            }
            orig(self, info);
        }

        public override string GetVersion()
        {
            return LoadTimes.defaultLoad.ToString();
        }

        private bool IgnoreLoad()
        {
            bool hazardRespawningHero = (bool)ReflectionHelper.GetField<GameManager, bool>(GameManager.instance, "hazardRespawningHero");
            if (hazardRespawningHero)
            {
                return true;
            }
            if (GameManager.instance.RespawningHero || GameManager.instance.entryGateName == "dreamGate")
            {
                ignoreNextLoadFinish = true;
                return true;
            }
            return false;
        }

        public IEnumerator WaitForNormalizedLoad(string sceneName, Action finish)
        {
            if (Time.realtimeSinceStartup - lastSceneLoadStart > LoadTimes.defaultLoad)
            {
                LogWarn($"Load for {sceneName} exceded standard load time by {Time.realtimeSinceStartup - lastSceneLoadStart}");
            }

            yield return new WaitWhile(() => Time.realtimeSinceStartup - lastSceneLoadStart < LoadTimes.defaultLoad);
            finish();
        }

        public static void LogLoad(string sceneName, double loadTime)
        {
            if (writer == null)
            {
                if (new FileInfo(Path.Combine(Application.persistentDataPath, "loadTimes.txt")) is FileInfo info
                    && info.Exists && info.Length > 1000000)
                {
                    info.Delete();
                }

                writer = new StreamWriter(Path.Combine(Application.persistentDataPath, "loadTimes.txt"), append: true)
                {
                    AutoFlush = true
                };
            }
            writer.WriteLine($"{sceneName}: {loadTime}");
        }

        public void Unload()
        {
            On.GameManager.BeginSceneTransition -= GetLoadStart;
            On.GameManager.EnterHero -= GetLoadEnd;
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
