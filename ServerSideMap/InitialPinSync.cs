using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using Splatform;

namespace ServerSideMap
{
    public class InitialPinSync
    {
        public static void OnReceiveInitialDataPin(ZRpc client, ZPackage pinData)
        {
            // SendPinsToServer(client);

            Store.ServerPinShare = true;
            
            if (!Store.IsSharingPin()) return;

            var pins = ExplorationDatabase.UnpackPins(pinData);

            Utility.Log("Client received initial pin data by server. Pins: " + pins.Count);

            ExplorationDatabase.ClientPins = pins;
            ClientAppendPins();
        }

        private static void ClientAppendPins()
        {
            if (!Store.IsSharingPin()) return;
            
            Utility.Log("ClientAppendPins " + ExplorationDatabase.ClientPins.Count);
            foreach (var pin in ExplorationDatabase.ClientPins)
            {
                var mapPin = UtilityPin.GetMapPin(pin);
                if (mapPin != null)
                {
                    _Minimap.RemovePin(_Minimap._instance, mapPin);
                }
                _Minimap.AddPin(_Minimap._instance, pin.Pos, pin.Type, pin.Name, false, pin.Checked, 0, new PlatformUserID(""));
            }
        }

        public static void OnClientInitialDataPin(ZRpc client, ZPackage pinData)
        {
            Utility.Log("Server received initial pin data by client");
        }
        
        private static void SendPinsToClient(ZRpc client)
        {
            if (!Store.IsSharingPin()) return;
            
            var z = ExplorationDatabase.PackPins(ExplorationDatabase.GetPins());
            
            if (client == null)
            {
                OnReceiveInitialDataPin(null, z);
            }
            else
            {
                client.Invoke("OnReceiveInitialDataPin", (object) z);
            }
        }

        private static void SendPinsToServer(ZRpc client)
        {
            var pins = Traverse.Create(_Minimap._instance).Field("m_pins").GetValue() as List<Minimap.PinData>;
            var filteredPins = new List<PinData>();

            foreach (var pin in pins)
            {
                if (pin.m_save)
                {
                    filteredPins.Add(UtilityPin.ConvertPin(pin));
                }
            }

            var z = ExplorationDatabase.PackPins(filteredPins);

            if (client == null)
            {
                OnClientInitialDataPin(null, z);
            }
            else
            {
                var znet =  Traverse.Create(typeof(ZNet)).Field("m_instance").GetValue() as ZNet;
                var server = _ZNet.GetServerRPC(znet);
                server.Invoke("OnClientInitialDataPin", (object) z);
            }
        }
        
        [HarmonyPatch(typeof (Minimap), "ClearPins")]
        private class MinimapPatchClearPins
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(Minimap __instance)
            {
                ClientAppendPins();
            }
        }
        
        [HarmonyPatch(typeof (ZNet), "Shutdown")]
        private class ZNetPatchShutdown
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(ZNet __instance)
            {
                ExplorationDatabase.ClientPins.Clear();
            }
        }
        
        [HarmonyPatch(typeof (Minimap), "SetMapData", typeof(byte[]))]
        private class MinimapPatchSetMapData
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(Minimap __instance)
            {
                if (_ZNet.IsServer(_ZNet._instance))
                {
                    SendPinsToClient(null);
                }
            }
        }

        // ReSharper disable once InconsistentNaming
        [HarmonyPatch(typeof (ZNet), "RPC_PeerInfo")]
        private  class ZnetPatchRPC_PeerInfo
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(ZRpc rpc, ZNet __instance)
            {
                if (__instance.IsServer())
                {
                    SendPinsToClient(rpc);
                }
            }
        }
        
    }
}