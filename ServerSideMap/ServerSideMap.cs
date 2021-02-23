using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;


namespace ServerSideMap
{
    [BepInPlugin("eu.mydayyy.plugins.serversidemap", "ServerSideMap", "1.0.0.0")]
    public class ServerSideMap : BaseUnityPlugin
    {
        private static System.Timers.Timer debounce = new System.Timers.Timer();

        private static bool dirty = false;
        private static byte[] mapData;
        private static bool[] explored;
        private static int mapSize = 2048; // TODO: Find out where to retrieve this from
        void Awake()
        {
            var harmony = new Harmony("eu.mydayyy.plugins.serversidemap");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), (string) null);
        }
        
        [HarmonyPatch(typeof (ZNet), "LoadWorld")]
        private  class ZnetPatch
        {
            private static void Postfix(ZNet __instance)
            {
                var l = BepInEx.Logging.Logger.CreateLogSource("ServerSideMap");
                
                var world =  Traverse.Create(typeof(ZNet)).Field("m_world").GetValue() as World;
                var m_worldSavePath =  Traverse.Create(world).Field("m_worldSavePath").GetValue() as String;
                string exploredPath = m_worldSavePath + "/" + world.m_name + ".mod.serversidemap.explored";
                
                FileStream fileStream;
                try
                {
                    fileStream = File.OpenRead(exploredPath);
                }
                catch
                {
                    var z = new ZPackage();
                    z.Write((int) 3);
                    z.Write(mapSize);
                    for (var i = 0; i < mapSize*mapSize; i++)
                    {
                        z.Write(false);
                    }
                    z.Write(0);
                    mapData = z.GetArray();
                    l.LogInfo("new explore file generated");
                    __instance.Save(true);

                    return;
                }
                BinaryReader reader = new BinaryReader((Stream) fileStream);
                mapData = reader.ReadBytes(int.MaxValue);
                l.LogInfo("loaded from existing explore file");
            }
        }



        [HarmonyPatch(typeof (ZNet), "SaveWorldThread")]
        private  class ZnetPatchSaveWorldThread
        {
            private static void Postfix(ZNet __instance)
            {
                var world =  Traverse.Create(typeof(ZNet)).Field("m_world").GetValue() as World;
                var m_worldSavePath =  Traverse.Create(world).Field("m_worldSavePath").GetValue() as String;
                string exploredPath = m_worldSavePath + "/" + world.m_name + ".mod.serversidemap.explored";
        
                FileStream fileStream = File.Create(exploredPath);
                BinaryWriter writer = new BinaryWriter((Stream) fileStream);
                writer.Write(mapData);
                writer.Flush();
                fileStream.Flush(true);
                fileStream.Close();
                fileStream.Dispose();
            }
        }

        private static void setExplored(int x, int y)
        {
            mapData[(y * mapSize + x) + 8] = 0x01;
        }
        
        private static bool getExplored(int x, int y)
        {
            return mapData[(y * mapSize + x) + 8] != 0;
        }
        
        private static bool getExplored(int idx)
        {
            return  mapData[idx + 8] != 0;
        }
        
        private static void onClientExplore(ZRpc client, int  x, int y)
        {
            setExplored(x, y);
            var znet =  Traverse.Create(typeof(ZNet)).Field("m_instance").GetValue() as ZNet;
            var m_peers = Traverse.Create((znet)).Field("m_peers").GetValue() as List<ZNetPeer>;
            foreach (ZNetPeer peer in m_peers)
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
        
        private static void onReceiveMapData(ZRpc client, ZPackage mapData)
        {
            int x = mapData.ReadInt();
            int y = mapData.ReadInt();
            
            var m =  Traverse.Create(typeof(Minimap)).Field("m_instance").GetValue() as Minimap;
            bool flag = _Minimap.Explore(m, x, y);
            dirty = flag || dirty;
        }
        
        [HarmonyPatch(typeof (ZNet), "RPC_PeerInfo")]
        private  class ZnetPatchRPC_PeerInfo
        {
            private static void Postfix(ZRpc rpc, ZPackage pkg, ZNet __instance)
            {
                if (__instance.IsServer())
                {
                    for (var i = 0; i < mapSize*mapSize; i++)
                    {
                        if (getExplored(i))
                        {
                            var z = new ZPackage();
                            z.Write(i % mapSize );
                            z.Write(i / mapSize);
                            rpc.Invoke("OnReceiveMapData", (object) z);
                        }
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof (ZNet), "OnNewConnection")]
        private  class ZnetPatchOnNewConnection
        {
            private static void Postfix(ZNetPeer peer, ZNet __instance)
            {
                if (__instance.IsServer())
                {
                    peer.m_rpc.Register<int, int>("OnClientExplore", new Action<ZRpc, int, int>(onClientExplore));
                }
                else
                {
                    peer.m_rpc.Register<ZPackage>("OnReceiveMapData", new Action<ZRpc, ZPackage>(onReceiveMapData));
                }
            }
        }
        
        
        [HarmonyPatch(typeof (Minimap), "Explore", new Type[] { typeof(int), typeof(int) })]
        private  class ZnetPatchExplore
        {
            private static void Postfix(int x, int y, ZNet __instance, bool __result)
            {
                if (__result == false)
                {
                    return;
                }

                if (__instance.IsServer())
                {
                    onClientExplore(null, x, y);
                }
                else
                {
                    var znet =  Traverse.Create(typeof(ZNet)).Field("m_instance").GetValue() as ZNet;
                    ZRpc server = _ZNet.GetServerRPC(znet);
                    server.Invoke("OnClientExplore", (object) x, (object) y);
                }
            }
        }
        
        [HarmonyPatch(typeof (Minimap), "Explore", new Type[] { typeof(Vector3), typeof(float) })]
        private  class ZnetPatchExploreInterval
        {
            private static void Postfix(ZNet __instance)
            {
                if (dirty)
                {
                    var m =  Traverse.Create(typeof(Minimap)).Field("m_instance").GetValue() as Minimap;
                    var m_fogTexture =  Traverse.Create((m)).Field("m_fogTexture").GetValue() as Texture2D;
                    m_fogTexture.Apply();
                }
                 
            }
        }
        
        [HarmonyPatch]
        public class _ZNet
        {
            [HarmonyReversePatch]
            [HarmonyPatch(typeof(ZNet), "GetServerRPC")]
            public static ZRpc GetServerRPC(ZNet instance)
            {
                throw new NotImplementedException();
            }
        }
        
        [HarmonyPatch]
        public class _Minimap
        {
            [HarmonyReversePatch(HarmonyReversePatchType.Original)]
            [HarmonyPatch(typeof(Minimap), "Explore", new Type[] { typeof(int), typeof(int) })]
            public static bool Explore(Minimap instance, int x, int y)
            {
                throw new NotImplementedException();
            }
        }
    }
}