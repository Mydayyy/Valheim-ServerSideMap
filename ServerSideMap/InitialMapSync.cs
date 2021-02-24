using HarmonyLib;
using UnityEngine;
using Logger = BepInEx.Logging.Logger;

namespace ServerSideMap
{
    public class InitialMapSync
    {
        public static void OnReceiveMapDataInitial(ZRpc client, ZPackage mapData)
        {
            var l = BepInEx.Logging.Logger.CreateLogSource("ServerSideMap");
            l.LogInfo("Client received initial map data by server");

            var explored = ExplorationDatabase.UnpackBoolArray(mapData);

            for (var index = 0; index < explored.Length; index++)
            {
                if (explored[index])
                {
                    _Minimap.Explore(_Minimap._instance, index % ExplorationDatabase.MapSize, index / ExplorationDatabase.MapSize);
                }
            }
            
            var fogTexture =  Traverse.Create((_Minimap._instance)).Field("m_fogTexture").GetValue() as Texture2D;
            fogTexture.Apply();
            
            explored = Traverse.Create(_Minimap._instance).Field("m_explored").GetValue() as bool[];
            var z = ExplorationDatabase.PackBoolArray(explored);
            if (_ZNet.IsServer(_ZNet._instance))
            {
                OnClientInitialData(null, z);
            }
            else
            {
                var znet =  Traverse.Create(typeof(ZNet)).Field("m_instance").GetValue() as ZNet;
                ZRpc server = _ZNet.GetServerRPC(znet);
                server.Invoke("OnClientInitialData", (object) z);
            }
        }
        
        public static void OnClientInitialData(ZRpc client,  ZPackage mapData)
        {
            var l = Logger.CreateLogSource("ServerSideMap");
            l.LogInfo("Server received initial map data by client");
            ExplorationDatabase.MergeExplorationArray(ExplorationDatabase.UnpackBoolArray(mapData));
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
                    var z = ExplorationDatabase.PackBoolArray(ExplorationDatabase.GetExplorationArray());
                    rpc.Invoke("OnReceiveMapDataInitial", (object) z);
                }
                else
                {
                    var l = Logger.CreateLogSource("ServerSideMap");
                }
            }
        }
        
                
        [HarmonyPatch(typeof (Minimap), "SetMapData", typeof(byte[]))]
        private class MinimapPatchSetMapData
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(Minimap __instance)
            {
                // var explored = Traverse.Create(__instance).Field("m_explored").GetValue() as bool[];
                // var z = ExplorationDatabase.PackBoolArray(explored);
                // if (_ZNet.IsServer(_ZNet._instance))
                // {
                //     OnClientInitialData(null, z);
                // }
                // else
                // {
                //     var znet =  Traverse.Create(typeof(ZNet)).Field("m_instance").GetValue() as ZNet;
                //     ZRpc server = _ZNet.GetServerRPC(znet);
                //     // server.Invoke("OnClientInitialData", (object) z);
                // }
            }
        }
    }
}