using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using HarmonyLib;

namespace ServerSideMap
{
    public static class Utility
    {
        private static ManualLogSource _logInstance = Logger.CreateLogSource("ServerSideMap");
        
        public static void Log(object data)
        {
            _logInstance.Log(LogLevel.Info, data);
        }
        
        public static byte[] ReadAllBytes(this BinaryReader reader)
        {
            const int bufferSize = 4096;
            using (var ms = new MemoryStream())
            {
                byte[] buffer = new byte[bufferSize];
                int count;
                while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
                    ms.Write(buffer, 0, count);
                return ms.ToArray();
            }
        }
    }
}