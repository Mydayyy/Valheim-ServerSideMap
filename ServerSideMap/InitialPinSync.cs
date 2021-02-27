using HarmonyLib;

namespace ServerSideMap
{
    public class InitialPinSync
    {
        public static void OnReceiveInitialDataPin(ZRpc client, ZPackage mapData)
        {
            Utility.Log("Client received initial pin data by server");
            
            SendPinsToServer(client);
        }

        public static void OnClientInitialDataPin(ZRpc client, ZPackage mapData)
        {
            Utility.Log("Server received initial pin data by client");

        }
        
        private static void SendPinsToClient(ZRpc client)
        {
            var z = new ZPackage();
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
            var z = new ZPackage();
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
                
                // else
                // {
                //     var znet =  Traverse.Create(typeof(ZNet)).Field("m_instance").GetValue() as ZNet;
                //     ZRpc server = _ZNet.GetServerRPC(znet);
                //     // server.Invoke("OnClientInitialData", (object) z);
                // }
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