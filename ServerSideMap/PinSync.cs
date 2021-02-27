using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ServerSideMap
{
    public class PinSync
    {
        private static Minimap.PinData CurrentPin = null;
        
            
        public static void OnClientAddPin(ZRpc client, ZPackage pinData)
        {
            var znet =  Traverse.Create(typeof(ZNet)).Field("m_instance").GetValue() as ZNet;
            var mPeers = Traverse.Create((znet)).Field("m_peers").GetValue() as List<ZNetPeer>;

            var pin = ExplorationDatabase.UnpackPin(pinData);
            ExplorationDatabase.AddPin(pin);
            
            Utility.Log("Server received pin by client");
            
            foreach (var peer in mPeers)
            {
                if (peer.IsReady())
                {
                    if (peer.m_rpc == client)
                    {
                        continue;
                    }                    
                    peer.m_rpc.Invoke("OnServerAddPin", (object) ExplorationDatabase.PackPin(pin));
                }
            }
        }
        
        public static void OnServerAddPin(ZRpc client, ZPackage pinData)
        {
            var pin = ExplorationDatabase.UnpackPin(pinData);
            
            _Minimap.AddPin(_Minimap._instance, pin.Pos, pin.Type, pin.Name, false, pin.Checked);
            ExplorationDatabase.ClientPins.Add(pin);
            
            Utility.Log("Client received pin by server");

        }
        
        public static void OnClientRemovePin(ZRpc client, ZPackage pinData)
        {
            var znet =  Traverse.Create(typeof(ZNet)).Field("m_instance").GetValue() as ZNet;
            var mPeers = Traverse.Create((znet)).Field("m_peers").GetValue() as List<ZNetPeer>;

            var pin = ExplorationDatabase.UnpackPin(pinData);
            ExplorationDatabase.RemovePinSimilar(pin);
            
            Utility.Log("Server deleted pin by client");
            
            foreach (var peer in mPeers)
            {
                if (peer.IsReady())
                {
                    if (peer.m_rpc == client)
                    {
                        continue;
                    }                    
                    peer.m_rpc.Invoke("OnServerRemovePin", (object) ExplorationDatabase.PackPin(pin));
                }
            }
        }
        
        public static void OnServerRemovePin(ZRpc client, ZPackage pinData)
        {
            Utility.Log("Client deleted pin by server");

            
            var pin = ExplorationDatabase.UnpackPin(pinData);
            
            foreach (var clientPin in ExplorationDatabase.ClientPins)
            {
                if (ExplorationDatabase.ArePinsSimilar(clientPin, pin))
                {
                    ExplorationDatabase.ClientPins.Remove(clientPin);
                    break;
                }
            }

            var mapPin = GetMapPin(pin);

            if (mapPin == null)
            {
                return;
            }
            _Minimap.RemovePin(_Minimap._instance, mapPin);


        }
        
        [HarmonyPatch(typeof (Minimap), "ShowPinNameInput")]
        private class MinimapPatchShowPinNameInput
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(Minimap __instance, Minimap.PinData pin)
            {
                CurrentPin = pin;
            }
        }

        public static void SendPinToServer(Minimap.PinData pin)
        {
            var convertedPin = ExplorationDatabase.ConvertPin(pin);
            var data = ExplorationDatabase.PackPin(convertedPin);

            if (!_ZNet.IsServer(_ZNet._instance))
            {
                _ZNet.GetServerRPC(_ZNet._instance).Invoke("OnClientAddPin", data);
            }
            else
            {
                OnClientAddPin(null,  data);
            }
        }
        
        public static void RemovePinFromServer(PinData pin)
        {
            var convertedPin = pin;
            var data = ExplorationDatabase.PackPin(convertedPin);

            if (!_ZNet.IsServer(_ZNet._instance))
            {
                _ZNet.GetServerRPC(_ZNet._instance).Invoke("OnClientRemovePin", data);
            }
            else
            {
                OnClientAddPin(null,  data);
            }
        }
        
        // private Minimap.PinData GetClosestPin(Vector3 pos, float radius)
        // {
        //     Minimap.PinData pinData = (Minimap.PinData) null;
        //     float num1 = 999999f;
        //     foreach (Minimap.PinData pin in this.m_pins)
        //     {
        //         if (pin.m_save)
        //         {
        //             float num2 = Utils.DistanceXZ(pos, pin.m_pos);
        //             if ((double) num2 < (double) radius && ((double) num2 < (double) num1 || pinData == null))
        //             {
        //                 pinData = pin;
        //                 num1 = num2;
        //             }
        //         }
        //     }
        //     return pinData;
        // }

        public static bool ArePinsEqual(PinData pin1, Minimap.PinData pin2)
        {
            return pin1.Name == pin2.m_name && pin1.Type == pin2.m_type && pin1.Pos.Equals(pin2.m_pos);
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
        
        [HarmonyPatch(typeof (Minimap), "GetClosestPin", typeof(Vector3),  typeof(float))]
        private class MinimapPatchGetClosestPin
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(Minimap __instance, ref Minimap.PinData __result, Vector3 pos, float radius)
            {
                var pinData = (PinData) null;
                var num1 = 999999f;
                foreach (var p in ExplorationDatabase.ClientPins)
                {
                    var num2 = Utils.DistanceXZ(pos, p.Pos);
                    if ((double) num2 < (double) radius && ((double) num2 < (double) num1 || pinData == null))
                    {
                        pinData = p;
                        num1 = num2;
                    }
                }

                if (pinData == null) return;
                var pin = GetMapPin(pinData);
                if (__result == null)
                {
                    __result = pin;
                    return;
                }
                
                var distance = Utils.DistanceXZ(pos, __result.m_pos);
                if (distance > num1)
                {
                    __result = pin;
                }
            }
        }
        
        [HarmonyPatch(typeof (Minimap), "RemovePin")]
        private class MinimapPatchRemovePin
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(Minimap __instance, Minimap.PinData pin)
            {
                var clientPin = GetClientPin(pin);

                if (clientPin == null) return;
                
                RemovePinFromServer(clientPin);
            }
        }
        
        [HarmonyPatch(typeof (Minimap), "UpdateNameInput")]
        private class MinimapPatchUpdateNameInput
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(Minimap __instance, Minimap.PinData ___m_namePin)
            {
                if (CurrentPin == null) return;
                
                if (___m_namePin == null)
                {
                    SendPinToServer(CurrentPin);
                    CurrentPin = null;
                }
            }
        }
    }
}