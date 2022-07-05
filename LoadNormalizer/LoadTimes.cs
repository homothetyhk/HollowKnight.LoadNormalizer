using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoadNormalizer
{
    public static class LoadTimes
    {
        public static void Setup()
        {
            DefaultLoad = LoadNormalizer.GS.defaultLoadTime;
            if (DefaultLoad <= 0f)
            {
                LoadNormalizer.GS.defaultLoadTime = DefaultLoad = 0.6f;
            }
            DefaultBossLoad = LoadNormalizer.GS.defaultBossLoadTime;
            if (DefaultBossLoad <= 0f)
            {
                LoadNormalizer.GS.defaultBossLoadTime = DefaultBossLoad = 0.4f;
            }
        }

        public static float DefaultLoad { get; private set; }
        public static float DefaultBossLoad { get; private set; }
    }
}
