﻿using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;


namespace ServerSideMap
{
    [BepInPlugin("eu.mydayyy.plugins.serversidemap", "ServerSideMap", "1.1.3.0")]
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
                    peer.m_rpc.Register<ZPackage>("OnClientInitialDataPin", new Action<ZRpc, ZPackage>(InitialPinSync.OnClientInitialDataPin));
                    peer.m_rpc.Register<ZPackage>("OnClientAddPin", new Action<ZRpc, ZPackage>(PinSync.OnClientAddPin));
                    peer.m_rpc.Register<ZPackage>("OnClientRemovePin", new Action<ZRpc, ZPackage>(PinSync.OnClientRemovePin));
                }
                else
                {
                    Utility.Log("Registered Client Events");
                    
                    peer.m_rpc.Register<ZPackage>("OnReceiveMapData", new Action<ZRpc, ZPackage>(ExplorationDatabase.OnReceiveMapData));
                    peer.m_rpc.Register<ZPackage>("OnReceiveMapDataInitial", new Action<ZRpc, ZPackage>(InitialMapSync.OnReceiveMapDataInitial));
                    peer.m_rpc.Register<ZPackage>("OnReceiveInitialDataPin", new Action<ZRpc, ZPackage>(InitialPinSync.OnReceiveInitialDataPin));
                    peer.m_rpc.Register<ZPackage>("OnServerAddPin", new Action<ZRpc, ZPackage>(PinSync.OnServerAddPin));
                    peer.m_rpc.Register<ZPackage>("OnServerRemovePin", new Action<ZRpc, ZPackage>(PinSync.OnServerRemovePin));
                }
            }
        }
    }
}