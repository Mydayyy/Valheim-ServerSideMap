using System;
using BepInEx.Configuration;
using UnityEngine;

namespace ServerSideMap
{
    public static class Store
    {
        public static ConfigEntry<bool> EnableMapShare;
        public static ConfigEntry<bool> EnablePinShare;
        public static ConfigEntry<float> DuplicatePinRadius;
        
        public static ConfigEntry<String> sKeyConvertAll;
        private static bool hasKeyConvertAll;
        private static KeyCode KeyConvertAll;
        
        public static ConfigEntry<String> sKeyConvertIgnoreDupes;
        private static bool hasKeyConvertIgnoreDupes;
        private static KeyCode KeyConvertIgnoreDupes;
        
        public static bool ServerPinShare = false;

        public static bool IsSharingMap()
        {
            return EnableMapShare.Value;
        }

        public static bool IsSharingPin()
        {
            return (ServerPinShare || _ZNet.IsServer(_ZNet._instance)) && EnablePinShare.Value;
        }

        public static float GetDuplicatePinRadius()
        {
            return DuplicatePinRadius.Value;
        }

        public static void InitHotkeys()
        {
            hasKeyConvertAll = Enum.TryParse(sKeyConvertAll.Value, out KeyConvertAll);
            hasKeyConvertIgnoreDupes = Enum.TryParse(sKeyConvertIgnoreDupes.Value, out KeyConvertIgnoreDupes);
        }
        public static bool HasKeyConvertAll()
        {
            return hasKeyConvertAll;
        } 
        public static KeyCode GetKeyConvertAll()
        {
            return KeyConvertAll;
        }
        
        public static bool HasKeyConvertIgnoreDupes()
        {
            return hasKeyConvertIgnoreDupes;
        } 
        public static KeyCode GetKeyConvertIgnoreDupes()
        {
            return KeyConvertIgnoreDupes;
        }
    }
}