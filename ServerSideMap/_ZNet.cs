using System;
using HarmonyLib;

namespace ServerSideMap
{
    // ReSharper disable once InconsistentNaming
    [HarmonyPatch]
    public class _ZNet
    {
        public static ZNet _instance;
        
        [HarmonyPatch(typeof(ZNet), "Awake")]
        public static class Awake
        {
            private static void Postfix(ZNet __instance)
            {
                _instance = __instance;
            }
        }
        
        
        // ReSharper disable once InconsistentNaming
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(ZNet), "GetServerRPC")]
        public static ZRpc GetServerRPC(ZNet instance)
        {
            throw new NotImplementedException();
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(ZNet), "IsServer")]
        public static bool IsServer(ZNet instance)
        {
            throw new NotImplementedException();
        }
    }
}