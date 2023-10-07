using System;
using HarmonyLib;
using UnityEngine;

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
                var l = BepInEx.Logging.Logger.CreateLogSource("ServerSideMap");
                l.LogInfo("Minimap awake");
            }
        }
        
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "Explore", typeof(int), typeof(int))]
        public static bool Explore(Minimap instance, int x, int y)
        {
            throw new NotImplementedException();
        }
        
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "AddPin", typeof(Vector3), typeof(Minimap.PinType), typeof(string), typeof(bool), typeof(bool), typeof(long), typeof(string))]
        public static Minimap.PinData AddPin(Minimap instance, Vector3 pos,
            Minimap.PinType type,
            string name,
            bool save,
            bool isChecked,
            long owner,
            string author)
        {
            throw new NotImplementedException();
        }
        
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "RemovePin", typeof(Minimap.PinData))]
        public static void RemovePin(Minimap instance, Minimap.PinData pin)
        {
            throw new NotImplementedException();
        }
    }
}