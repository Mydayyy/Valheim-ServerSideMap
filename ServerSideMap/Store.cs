using BepInEx.Configuration;

namespace ServerSideMap
{
    public static class Store
    {
        public static ConfigEntry<bool> EnableMapShare;
        public static ConfigEntry<bool> EnablePinShare;

        public static bool ServerPinShare = false;

        public static bool IsSharingMap()
        {
            return EnableMapShare.Value;
        }

        public static bool IsSharingPin()
        {
            return (ServerPinShare || _ZNet.IsServer(_ZNet._instance)) && EnablePinShare.Value;
        }
    }
}