using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;


namespace ServerSideMap
{
    [BepInPlugin("eu.mydayyy.plugins.serversidemap", "ServerSideMap", "1.1.2.0")]
    public class ServerSideMap : BaseUnityPlugin
    {
        void Awake()
        {
            var harmony = new Harmony("eu.mydayyy.plugins.serversidemap");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), (string) null);
        }

        [HarmonyPatch(typeof (ZNet), "OnNewConnection")]
        private  class ZnetPatchOnNewConnection
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(ZNetPeer peer, ZNet __instance)
            {
                if (__instance.IsServer())
                {
                    Utility.Log("Registered Server Events");

                    peer.m_rpc.Register<int, int>("OnClientExplore", new Action<ZRpc, int, int>(ExplorationMapSync.OnClientExplore));
                    peer.m_rpc.Register<ZPackage>("OnClientInitialData", new Action<ZRpc, ZPackage>(InitialMapSync.OnClientInitialData));
                }
                else
                {
                    Utility.Log("Registered Client Events");
                    
                    peer.m_rpc.Register<ZPackage>("OnReceiveMapData", new Action<ZRpc, ZPackage>(ExplorationDatabase.OnReceiveMapData));
                    peer.m_rpc.Register<ZPackage>("OnReceiveMapDataInitial", new Action<ZRpc, ZPackage>(InitialMapSync.OnReceiveMapDataInitial));
                }
            }
        }
    }
}