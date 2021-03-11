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

        public static bool LocalPinIsDupe(Minimap.PinData pin)
        {
            foreach(var serverpin in  ExplorationDatabase.ClientPins)
            {
                if (ExplorationDatabase.ArePinsDupes(pin, serverpin, Store.GetDuplicatePinRadius()))
                    return true;
            }

            return false;
        }

        public static void UploadAllPins(bool removeDupes = false)
        {
            var pins = Traverse.Create(_Minimap._instance).Field("m_pins").GetValue() as List<Minimap.PinData>;

            foreach (var pin in pins)
            {
                if (!pin.m_save) continue;
                if(!removeDupes || !LocalPinIsDupe(pin))
                    PinSync.SendPinToServer(pin, false);
            }
        }

        public static void DeleteLocalPins()
        {
            var pins = Traverse.Create(_Minimap._instance).Field("m_pins").GetValue() as List<Minimap.PinData>;

            foreach (var pin in pins)
            {
                Utility.Log("Save: " + pin.m_save);
                if(pin.m_save)
                    _Minimap.RemovePin(_Minimap._instance, pin);
            }
        }
    }
}