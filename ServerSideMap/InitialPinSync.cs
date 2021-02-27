using System.Collections.Generic;
using HarmonyLib;

namespace ServerSideMap
{
    public class InitialPinSync
    {
        public static void OnReceiveInitialDataPin(ZRpc client, ZPackage pinData)
        {
            
            // SendPinsToServer(client);

            var pins = ExplorationDatabase.UnpackPins(pinData);

            Utility.Log("Client received initial pin data by server. Pins: " + pins.Count);

            ExplorationDatabase.ClientPins = pins;
            ClientAppendPins();
        }

        private static void ClientAppendPins()
        {
            foreach (var pin in ExplorationDatabase.ClientPins)
            {
                _Minimap.AddPin(_Minimap._instance, pin.Pos, pin.Type, pin.Name, false, pin.Checked);
            }
        }

        public static void OnClientInitialDataPin(ZRpc client, ZPackage pinData)
        {
            Utility.Log("Server received initial pin data by client");

        }
        
        private static void SendPinsToClient(ZRpc client)
        {
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
                    filteredPins.Add(ExplorationDatabase.ConvertPin(pin));
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