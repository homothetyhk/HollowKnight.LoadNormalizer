using Modding;
using System.Security.Cryptography;

namespace LoadNormalizer
{
    public class LoadNormalizer : Mod, IGlobalSettings<GlobalSettings>, ITogglableMod
    {
        public static GlobalSettings GS = GlobalSettings.Default;

        public static LoadNormalizer instance;

        public LoadNormalizer() : base("LoadNormalizer-EXPERIMENTAL")
        {
        }

        public override void Initialize()
        {
            instance = this;
            LoadHook.Hook();
        }

        public void Unload()
        {
            LoadHook.Unhook();
        }

        public override string GetVersion()
        {
            var sha1 = SHA1.Create();
            byte[] settings = GS.GetHashableData();
            byte[] hash = sha1.ComputeHash(settings);
            string code = BitConverter.ToString(hash).Substring(0, 5).Replace("-", "");
            return $"2.1.4+{code}";
        }

        void IGlobalSettings<GlobalSettings>.OnLoadGlobal(GlobalSettings s)
        {
            if (s is null || s.AutoUpdate is null || s.AutoUpdate.UpdateSettingsOnNewVersion && s.AutoUpdate.Version != AutoUpdate.CurrentVersion)
            {
                LogWarn("Auto-updating settings to new defaults.");
                GS = GlobalSettings.Default;
            }
            else
            {
                GS = s;
            }
        }

        GlobalSettings IGlobalSettings<GlobalSettings>.OnSaveGlobal()
        {
            return GS;
        }
    }
}
