using BepInEx.Logging;

namespace ServerSideMap
{
    public static class Utility
    {
        private static ManualLogSource _logInstance = Logger.CreateLogSource("ServerSideMap");
        
        public static void Log(object data)
        {
            _logInstance.Log(LogLevel.Info, data);
        }
    }
}