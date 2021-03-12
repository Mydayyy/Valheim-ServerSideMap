using System.Collections.Generic;
using HarmonyLib;

namespace ServerSideMap
{
    public class UtilityPin
    {
        public static bool ArePinsEqual(PinData pin1, Minimap.PinData pin2)
        {
            return pin1.Name == pin2.m_name && pin1.Type == pin2.m_type && pin1.Pos.Equals(pin2.m_pos);
        }
        
        public static bool ArePinsEqual(PinData pin1, PinData pin2)
        {
            return pin1.Name == pin2.Name && pin1.Type == pin2.Type && pin1.Pos.Equals(pin2.Pos);
        }

        public static bool ArePinsDupes(Minimap.PinData pin1, PinData pin2, float radius)
        {
            return Utils.DistanceXZ(pin1.m_pos, pin2.Pos) < radius;
        }
        
        public static bool LocalPinIsDupe(Minimap.PinData pin)
        {
            foreach(var serverpin in  ExplorationDatabase.ClientPins)
            {
                if (ArePinsDupes(pin, serverpin, Store.GetDuplicatePinRadius()))
                    return true;
            }
            return false;
        }
        
        public static Minimap.PinData GetMapPin(PinData needle)
        {
            var pins = Traverse.Create(_Minimap._instance).Field("m_pins").GetValue() as List<Minimap.PinData>;

            foreach (var pin in pins)
            {
                if (ArePinsEqual(needle, pin))
                {
                    return pin;
                }
            }
            return null;
        }
        
        public static PinData GetClientPin(Minimap.PinData needle)
        {
            foreach (var pin in ExplorationDatabase.ClientPins)
            {
                if (ArePinsEqual(pin, needle))
                {
                    return pin;
                }
            }
            return null;
        }
        
        public static PinData ConvertPin(Minimap.PinData pin)
        {
            return new PinData {
                Name = pin.m_name,
                Pos = pin.m_pos,
                Type = pin.m_type,
                Checked =  pin.m_checked
            };
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