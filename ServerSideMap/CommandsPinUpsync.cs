using HarmonyLib;
using UnityEngine.UI;

namespace ServerSideMap
{
    public static class CommandsPinUpsync
    {
        [HarmonyPatch(typeof (Chat), "InputText")]
        private class ChatPatchInputText
        {
            // ReSharper disable once InconsistentNaming
            private static bool Prefix(Chat __instance, InputField ___m_input)
            {
                string text = ___m_input.text;
            
                Utility.Log("Received Text: " + text);

                if (text.ToLower().Equals("/convertpins removelocaldupes"))
                {
                    Utility.UploadAllPins(true);
                    return false;
                }
                if (text.ToLower().Equals("/convertpins"))
                {
                    Utility.UploadAllPins();
                    return false;
                }
                if (text.ToLower().Equals("/deletealllocalpins"))
                {
                    Utility.DeleteLocalPins();
                    return false;
                }
                return true;
            }
        }
    }
}