using System;
using HarmonyLib;

namespace ServerSideMap
{
    public class InitialMapSync
    {
        // ReSharper disable once InconsistentNaming
        [HarmonyPatch(typeof (ZNet), "RPC_PeerInfo")]
        private  class ZnetPatchRPC_PeerInfo
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(ZRpc rpc, ZNet __instance)
            {
                if (__instance.IsServer())
                {
                    for (var i = 0; i < ExplorationDatabase.MapSize*ExplorationDatabase.MapSize; i++)
                    {
                        if (ExplorationDatabase.GetExplored(i))
                        {
                            var z = new ZPackage();
                            z.Write(i % ExplorationDatabase.MapSize );
                            z.Write(i / ExplorationDatabase.MapSize);
                            rpc.Invoke("OnReceiveMapData", (object) z);
                        }
                    }
                }
            }
        }
    }
}