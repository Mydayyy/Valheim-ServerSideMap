using HarmonyLib;
using UnityEngine.UI;
using TMPro;

namespace ServerSideMap
{
    public static class CommandsPinUpsync
    {
        [HarmonyPatch(typeof (Chat), "InputText")]
        private class ChatPatchInputText
        {
            // ReSharper disable once InconsistentNaming
            private static bool Prefix(Chat __instance, TMP_InputField ___m_input)
            {
                string text = ___m_input.text;
            
                if (text.ToLower().Equals("/convertpins ignorelocaldupes"))
                {
                    UtilityPin.UploadAllPins(true);
                    return false;
                }
                if (text.ToLower().Equals("/convertpins"))
                {
                    UtilityPin.UploadAllPins();
                    return false;
                }
                if (text.ToLower().Equals("/deletealllocalpins"))
                {
                    UtilityPin.DeleteLocalPins();
                    return false;
                }
                if (text.ToLower().Equals("/downloadpins"))
                {
                    UtilityPin.DownloadPins();
                    return false;
                }
                return true;
            }
        }
    }
}