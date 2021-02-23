using System;
using HarmonyLib;

namespace ServerSideMap
{
    [HarmonyPatch]
    // ReSharper disable once InconsistentNaming
    public class _Minimap
    {
            [HarmonyReversePatch]
            [HarmonyPatch(typeof(Minimap), "Explore", typeof(int), typeof(int))]
            public static bool Explore(Minimap instance, int x, int y)
            {
                throw new NotImplementedException();
        }
    }
}