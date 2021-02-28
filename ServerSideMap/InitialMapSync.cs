using HarmonyLib;
using UnityEngine;
using Logger = BepInEx.Logging.Logger;


namespace ServerSideMap
{
    public class InitialMapSync
    {
        static int CHUNKS = 64; 

        public static void OnReceiveMapDataInitial(ZRpc client, ZPackage mapData)
        {
            if (!Store.IsSharingMap()) return;
            
            mapData.SetPos(0);
            
            var chunk = mapData.ReadInt();
            // Utility.Log("Client received initial map data by server chunk " + (chunk+1) + "/" + CHUNKS);
            
            var explored = ExplorationDatabase.UnpackBoolArray(mapData, ExplorationDatabase.MapSizeSquared / CHUNKS);
            var startIndex = chunk * (ExplorationDatabase.MapSizeSquared / CHUNKS);
            
            for (var index = 0; index < explored.Length; index++)
            {
                if (explored[index])
                {
                    _Minimap.Explore(_Minimap._instance, (startIndex+index) % ExplorationDatabase.MapSize, (startIndex+index) / ExplorationDatabase.MapSize);
                }
            }
            
            var fogTexture =  Traverse.Create((_Minimap._instance)).Field("m_fogTexture").GetValue() as Texture2D;
            fogTexture.Apply();
            
            SendChunkToServer(client, chunk);

            // var explored = ExplorationDatabase.UnpackBoolArray(mapData);
            //
            // for (var index = 0; index < explored.Length; index++)
            // {
            //     if (explored[index])
            //     {
            //         _Minimap.Explore(_Minimap._instance, index % ExplorationDatabase.MapSize, index / ExplorationDatabase.MapSize);
            //     }
            // }
            //
            // var fogTexture =  Traverse.Create((_Minimap._instance)).Field("m_fogTexture").GetValue() as Texture2D;
            // fogTexture.Apply();
            //
            // explored = Traverse.Create(_Minimap._instance).Field("m_explored").GetValue() as bool[];
            // var z = ExplorationDatabase.PackBoolArray(explored);
            // if (_ZNet.IsServer(_ZNet._instance))
            // {
            //     OnClientInitialData(null, z);
            // }
            // else
            // {
            //     var znet =  Traverse.Create(typeof(ZNet)).Field("m_instance").GetValue() as ZNet;
            //     ZRpc server = _ZNet.GetServerRPC(znet);
            //     server.Invoke("OnClientInitialData", (object) z);
            // }
        }
        
        public static void OnClientInitialData(ZRpc client,  ZPackage mapData)
        {
            if (!Store.IsSharingMap()) return;
            
            mapData.SetPos(0);
            
            var chunk = mapData.ReadInt();
            var startIndex = chunk * (ExplorationDatabase.MapSizeSquared / CHUNKS);
            var size = ExplorationDatabase.MapSizeSquared / CHUNKS;
            
            // Utility.Log("Server received initial map data by client chunk " + (chunk+1) + "/" + CHUNKS);
            ExplorationDatabase.MergeExplorationArray(ExplorationDatabase.UnpackBoolArray(mapData, size), startIndex, size);
            
            SendChunkToClient(client, chunk+1);
        }
        
        // ReSharper disable once InconsistentNaming
        [HarmonyPatch(typeof (ZNet), "RPC_PeerInfo")]
        private  class ZnetPatchRPC_PeerInfo
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(ZRpc rpc, ZNet __instance)
            {
                if (!Store.IsSharingMap()) return;
                
                if (__instance.IsServer())
                {
                    SendChunkToClient(rpc, 0);
                }
            }
        }

        private static void SendChunkToClient(ZRpc client, int chunk)
        {
            if(chunk >= CHUNKS) return;
            
            var size = ExplorationDatabase.MapSizeSquared / CHUNKS;
            var startIndex = chunk * (ExplorationDatabase.MapSizeSquared / CHUNKS);
            var z = ExplorationDatabase.PackBoolArray(ExplorationDatabase.GetExplorationArray(), chunk, startIndex, size);
            if (client == null)
            {
                OnReceiveMapDataInitial(null, z);
            }
            else
            {
                client.Invoke("OnReceiveMapDataInitial", (object) z);
            }
        }

        private static void SendChunkToServer(ZRpc client, int chunk)
        {
            if(chunk >= CHUNKS) return;
            
            var size = ExplorationDatabase.MapSizeSquared / CHUNKS;
            var startIndex = chunk * (ExplorationDatabase.MapSizeSquared / CHUNKS);
            var explored = Traverse.Create(_Minimap._instance).Field("m_explored").GetValue() as bool[];
            var z = ExplorationDatabase.PackBoolArray(explored, chunk, startIndex, size);
            if (client == null)
            {
                OnClientInitialData(null, z);
            }
            else
            {
                var znet =  Traverse.Create(typeof(ZNet)).Field("m_instance").GetValue() as ZNet;
                var server = _ZNet.GetServerRPC(znet);
                server.Invoke("OnClientInitialData", (object) z);
            }
        }
        
                
        [HarmonyPatch(typeof (Minimap), "SetMapData", typeof(byte[]))]
        private class MinimapPatchSetMapData
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(Minimap __instance)
            {
                if (!Store.IsSharingMap()) return;
                
                if (_ZNet.IsServer(_ZNet._instance))
                {
                    SendChunkToClient(null, 0);
                }
                
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