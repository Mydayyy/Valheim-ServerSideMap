using System;
using HarmonyLib;

namespace ServerSideMap
{
    [HarmonyPatch]
    // ReSharper disable once InconsistentNaming
    public class _Minimap
    {
        public static Minimap _instance;
        
        [HarmonyPatch(typeof(Minimap), "Awake")]
        public static class Awake
        {
            private static void Postfix(Minimap __instance)
            {
                _instance = __instance;
            }
        }
        
            [HarmonyReversePatch]
            [HarmonyPatch(typeof(Minimap), "Explore", typeof(int), typeof(int))]
            public static bool Explore(Minimap instance, int x, int y)
            {
                throw new NotImplementedException();
        }
    }
}