using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;

namespace ServerSideMap
{
    public static class ExplorationMapSync
    {
        private static bool _blockExplore = false;
        
        public static void OnClientExplore(ZRpc client, int  x, int y)
        {
            if (!Store.IsSharingMap()) return;
            
            ExplorationDatabase.SetExplored(x, y);
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
                    var z = new ZPackage();
                    z.Write(x);
                    z.Write(y);
                    peer.m_rpc.Invoke("OnReceiveMapData", (object) z);
                }
            }

            var zz = new ZPackage();
            zz.Write(x);
            zz.Write(y);
            ExplorationDatabase.OnReceiveMapData(null, zz);
        }


        
        [HarmonyPatch(typeof (Minimap), "Explore", typeof(int), typeof(int))]
        private class MinimapPatchExplore
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(int x, int y, bool __result)
            {
                if (__result == false)
                {
                    return;
                }

                if (_blockExplore) return;

                if (!Store.IsSharingMap()) return;
                if (_ZNet.IsServer(_ZNet._instance))
                {
                    OnClientExplore(null, x, y);
                }
                else
                {
                    var znet =  Traverse.Create(typeof(ZNet)).Field("m_instance").GetValue() as ZNet;
                    ZRpc server = _ZNet.GetServerRPC(znet);
                    server.Invoke("OnClientExplore", (object) x, (object) y);
                }
            }
        }
        
        
        [HarmonyPatch(typeof (Minimap), "SetMapData", typeof(byte[]))]
        private class MinimapPatchSetMapData
        {
            private static void Prefix()
            {
                _blockExplore = true;
            }

            // ReSharper disable once InconsistentNaming
            private static void Postfix(Minimap __instance)
            {
                _blockExplore = false;
            }
        }
    }
}