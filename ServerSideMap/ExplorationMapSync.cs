using System.Collections.Generic;
using HarmonyLib;

namespace ServerSideMap
{
    public static class ExplorationMapSync
    {
        public static void OnClientExplore(ZRpc client, int  x, int y)
        {
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
    }
}