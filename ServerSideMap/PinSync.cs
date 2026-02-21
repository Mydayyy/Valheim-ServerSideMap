using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Splatform;
using UnityEngine;

namespace ServerSideMap
{
    public class PinSync
    {
        private static Minimap.PinData LatestClosestPin = null;
        
            
        public static void OnClientAddPin(ZRpc client, ZPackage pinData)
        {
            if (!Store.IsSharingPin()) return;
            pinData.SetPos(0);
            
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

            if (client != null)
            {
                OnServerAddPin(null, ExplorationDatabase.PackPin(pin));
            }
        }
        
        public static void OnServerAddPin(ZRpc client, ZPackage pinData)
        {
            if (!Store.IsSharingPin()) return;
            pinData.SetPos(0);
            
            var pin = ExplorationDatabase.UnpackPin(pinData);
            
            _Minimap.AddPin(_Minimap._instance, pin.Pos, pin.Type, pin.Name, false, pin.Checked, 0, new PlatformUserID(""));
            ExplorationDatabase.ClientPins.Add(pin);
            
            Utility.Log("Client received pin by server");

        }
        
        public static void OnClientRemovePin(ZRpc client, ZPackage pinData)
        {
            if (!Store.IsSharingPin()) return;
            pinData.SetPos(0);
            
            var znet =  Traverse.Create(typeof(ZNet)).Field("m_instance").GetValue() as ZNet;
            var mPeers = Traverse.Create((znet)).Field("m_peers").GetValue() as List<ZNetPeer>;

            var pin = ExplorationDatabase.UnpackPin(pinData);
            ExplorationDatabase.RemovePinEqual(pin);
            
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
            
            if (client != null)
            {
                OnServerRemovePin(null, ExplorationDatabase.PackPin(pin));
            }
        }
        
        public static void OnServerRemovePin(ZRpc _, ZPackage pinData)
        {
            if (!Store.IsSharingPin()) return;
            pinData.SetPos(0);
            
            Utility.Log("Client deleted pin by server");
            
            var pin = ExplorationDatabase.UnpackPin(pinData);
            
            foreach (var clientPin in ExplorationDatabase.ClientPins.ToList())
            {
                if (UtilityPin.ArePinsEqual(clientPin, pin))
                {
                    ExplorationDatabase.ClientPins.Remove(clientPin);
                }
            }

            var mapPin = UtilityPin.GetMapPin(pin);

            if (mapPin == null)
            {
                return;
            }
            _Minimap.RemovePin(_Minimap._instance, mapPin);
        }
        
        public static void OnClientCheckPin(ZRpc client, ZPackage data)
        {
            if (!Store.IsSharingPin()) return;
            data.SetPos(0);
            
            Utility.Log("Server checked pin by client");

            var pin = ExplorationDatabase.UnpackPin(data);
            var state = data.ReadBool();
            
            ExplorationDatabase.SetPinState(pin, state);
            
            var znet =  Traverse.Create(typeof(ZNet)).Field("m_instance").GetValue() as ZNet;
            var mPeers = Traverse.Create((znet)).Field("m_peers").GetValue() as List<ZNetPeer>;
            foreach (var peer in mPeers)
            {
                if (peer.IsReady())
                {
                    if (peer.m_rpc == client)
                    {
                        continue;
                    }
                    var z = ExplorationDatabase.PackPin(pin, true);
                    z.Write(state);
                    peer.m_rpc.Invoke("OnServerCheckPin", (object) z);
                }
            }
            
            if (client != null)
            {
                var z = ExplorationDatabase.PackPin(pin, true);
                z.Write(state);
                OnServerCheckPin(null, z);
            }
        }
        
        public static void OnServerCheckPin(ZRpc client, ZPackage data)
        {
            if (!Store.IsSharingPin()) return;
            data.SetPos(0);
            
            Utility.Log("Client checked pin by server");
            
            var pin = ExplorationDatabase.UnpackPin(data);
            var state = data.ReadBool();
            
            foreach (var clientPin in ExplorationDatabase.ClientPins.ToList())
            {
                if (UtilityPin.ArePinsEqual(clientPin, pin))
                {
                    clientPin.Checked = state;
                    var mapPin = UtilityPin.GetMapPin(clientPin);
                    if (mapPin != null)
                    {
                        mapPin.m_checked = state;
                    }
                }
            }
        }
        
        public static void SendPinToServer(Minimap.PinData pin, bool deletePin = true)
        {
            if (!Store.IsSharingPin()) return;
            
            var convertedPin = UtilityPin.ConvertPin(pin);
            var data = ExplorationDatabase.PackPin(convertedPin);

            pin.m_save = !deletePin && pin.m_save;
            ExplorationDatabase.ClientPins.Add(convertedPin);

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
            if (!Store.IsSharingPin()) return;
            
            var data = ExplorationDatabase.PackPin(pin);

            ExplorationDatabase.ClientPins.Remove(pin);
 
            if (!_ZNet.IsServer(_ZNet._instance))
            {
                _ZNet.GetServerRPC(_ZNet._instance).Invoke("OnClientRemovePin", data);
            }
            else
            {
                OnClientRemovePin(null,  data);
            }
        }

        public static void CheckPinOnServer(PinData pin, bool state)
        {
            if (!Store.IsSharingPin()) return;
            
            var data = ExplorationDatabase.PackPin(pin, true);
            data.Write(state);
            data.SetPos(0);

            if (!_ZNet.IsServer(_ZNet._instance))
            {
                _ZNet.GetServerRPC(_ZNet._instance).Invoke("OnClientCheckPin", data);
            }
            else
            {
                OnClientCheckPin(null,  data);
            }
        }
        
        [HarmonyPatch(typeof (Minimap), "OnMapLeftClick")]
        private class MinimapPatchOnMapLeftClick
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(Minimap __instance)
            {
                if (LatestClosestPin == null) return;

                var clientPin = UtilityPin.GetClientPin(LatestClosestPin);

                if (clientPin == null) return;

                clientPin.Checked = LatestClosestPin.m_checked;
                
                CheckPinOnServer(clientPin, clientPin.Checked);
            }
        }
        
        [HarmonyPatch(typeof (Minimap), "OnMapRightClick")]
        private class MinimapPatchOnMapRightClick
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(Minimap __instance)
            {
                if (LatestClosestPin == null) return;

                var clientPin = UtilityPin.GetClientPin(LatestClosestPin);

                if (clientPin == null) return;

                RemovePinFromServer(clientPin);
            }
        }
        
        [HarmonyPatch(typeof (Minimap), "GetClosestPin", typeof(Vector3),  typeof(float), typeof(bool))]
        private class MinimapPatchGetClosestPin
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(Minimap __instance, ref Minimap.PinData __result, Vector3 pos, float radius, bool mustBeVisible = true)
            {
                if (!Store.IsSharingPin()) return;
                
                LatestClosestPin = __result;
                
                var pinData = (PinData) null;
                var num1 = float.MaxValue;
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
                
                var pin = UtilityPin.GetMapPin(pinData);
                if (__result == null)
                {
                    __result = pin;
                    LatestClosestPin = pin;
                    return;
                }
                
                var distance = Utils.DistanceXZ(pos, __result.m_pos);
                if (distance > num1)
                {
                    __result = pin;
                    LatestClosestPin = pin;
                }
            }
        }
        
        [HarmonyPatch(typeof (Minimap), "OnPinTextEntered")]
        private class MinimapPatchOnPinTextEntered
        {
            static void Prefix(Minimap __instance, out Minimap.PinData __state, Minimap.PinData ___m_namePin)
            {
                __state = null;
                if (___m_namePin == null) return;
                __state = ___m_namePin;
            }
            
            // ReSharper disable once InconsistentNaming
            private static void Postfix(Minimap __instance, Minimap.PinData __state)
            {
                if (__state == null) return;
                SendPinToServer(__state);
            }
        }
    }
}