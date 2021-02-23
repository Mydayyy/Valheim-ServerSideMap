using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;


namespace ServerSideMap
{
    [BepInPlugin("eu.mydayyy.plugins.serversidemap", "ServerSideMap", "1.0.0.0")]
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
                    peer.m_rpc.Register<int, int>("OnClientExplore", new Action<ZRpc, int, int>(ExplorationMapSync.OnClientExplore));
                }
                else
                {
                    peer.m_rpc.Register<ZPackage>("OnReceiveMapData", new Action<ZRpc, ZPackage>(ExplorationDatabase.OnReceiveMapData));
                }
            }
        }
    }
}