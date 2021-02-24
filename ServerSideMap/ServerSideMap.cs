using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;


namespace ServerSideMap
{
    [BepInPlugin("eu.mydayyy.plugins.serversidemap", "ServerSideMap", "1.1.0.0")]
    public class ServerSideMap : BaseUnityPlugin
    {
        void Awake()
        {
            var harmony = new Harmony("eu.mydayyy.plugins.serversidemap");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), (string) null);
            
            
            // Random rnd = new Random();
            // var l = BepInEx.Logging.Logger.CreateLogSource("SSM");
            //
            // var size = 2048*2048;
            //
            // for (var k = 0; k < 10000; k++)
            // {
            //     var arr = new bool[size];
            //     for (var i = 0; i < size; i++)
            //     {
            //         arr[i] = rnd.NextDouble() > 0.5;
            //     }
            //
            //     var decompressed = ExplorationDatabase.UnpackBoolArray(ExplorationDatabase.PackBoolArray(arr));
            //     bool isEqual = Enumerable.SequenceEqual(arr, decompressed);
            //     l.LogInfo("equal: " + isEqual);
            // }

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
                    peer.m_rpc.Register<ZPackage>("OnClientInitialData", new Action<ZRpc, ZPackage>(InitialMapSync.OnClientInitialData));
                }
                else
                {
                    peer.m_rpc.Register<ZPackage>("OnReceiveMapData", new Action<ZRpc, ZPackage>(ExplorationDatabase.OnReceiveMapData));
                    peer.m_rpc.Register<ZPackage>("OnReceiveMapDataInitial", new Action<ZRpc, ZPackage>(InitialMapSync.OnReceiveMapDataInitial));
                }
            }
        }
    }
}