using System;
using HarmonyLib;
using UnityEngine;

namespace ServerSideMap
{
    public static class Hotkeys
    {
        [HarmonyPatch(typeof (Player), "Update")]
        private class PlayerInputPatch
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(Player __instance)
            {
                if (Store.HasKeyConvertAll() && Input.GetKeyDown(Store.GetKeyConvertAll()))
                {
                    Utility.Log("Hotkey: GetKeyConvertAll");
                    UtilityPin.UploadAllPins(false);
                }

                if (Store.HasKeyConvertIgnoreDupes() && Input.GetKeyDown(Store.GetKeyConvertIgnoreDupes()))
                {
                    Utility.Log("Hotkey: GetKeyConvertIgnoreDupes");
                    UtilityPin.UploadAllPins(true);
                }
            }
        }
    }
}