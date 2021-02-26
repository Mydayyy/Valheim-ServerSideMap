using System;
using HarmonyLib;
using UnityEngine;
using BepInEx.Logging;

namespace ServerSideMap
{
    public static class ExplorationDatabase
    {
        private static bool _dirty;
        public static byte[] MapData;
        public const int MapSize = 2048; // TODO: Find out where to retrieve this from
        public const int MapSizeSquared = 2048*2048; // TODO: Find out where to retrieve this from

        public static void SetExplored(int x, int y)
        {
            MapData[(y * MapSize + x) + 8] = 0x01;
        }
        
        public static bool GetExplored(int x, int y)
        {
            return MapData[(y * MapSize + x) + 8] != 0;
        }
        
        public static bool GetExplored(int idx)
        {
            return  MapData[idx + 8] != 0;
        }

        // TODO: Optimize
        public static bool[] GetExplorationArray()
        {
            var size = MapSize * MapSize;
            var arr = new bool[size];

            for (var i = 0; i < (size); i++)
            {
                arr[i] =  Convert.ToBoolean(MapData[i + 8]);
            }
            return arr;
        }

        public static void MergeExplorationArray(bool[] arr, int startIndex, int size)
        {
            for (var i = 0; i < size; i++)
            {
                MapData[startIndex + i + 8] = arr[i] ? (byte) 0x01: MapData[i + 8];
            }
        }
        
        public static ZPackage PackBoolArray(bool[] arr, int chunkId, int startIndex, int size)
        {
            var l = BepInEx.Logging.Logger.CreateLogSource("ServerSideMap");
            
            ZPackage z = new ZPackage();
            
            z.Write(chunkId);
            
            byte currentByte = 0;
            int currentIndex = 0;

            for (var i = startIndex; i < startIndex+size; i++)
            {
                var value = arr[i];
                if (value)
                {
                    byte mask = (byte)(1 << currentIndex);
                    currentByte |= mask;
                }

                currentIndex += 1;
                if (currentIndex >= 8)
                {
                    z.Write(currentByte);
                    currentByte = 0;
                    currentIndex = 0;
                }
            }
            if (currentIndex > 0)
            {
                z.Write(currentByte);
            }
            
            l.LogInfo("Compressed exploration array:  " + size + ":" + z.Size());

            return z;
        }

        public static bool[] UnpackBoolArray(ZPackage z, int length)
        {
            var l = BepInEx.Logging.Logger.CreateLogSource("ServerSideMap");
            
            var arr = new bool[length];
            for (var i = 0; i < length; i += 8)
            {          

                var b = z.ReadByte();
                
                arr[i] = (b & (1 << 0)) != 0;
                arr[i+1] = (b & (1 << 1)) != 0;
                arr[i+2] = (b & (1 << 2)) != 0;
                arr[i+3] = (b & (1 << 3)) != 0;
                arr[i+4] = (b & (1 << 4)) != 0;
                arr[i+5] = (b & (1 << 5)) != 0;
                arr[i+6] = (b & (1 << 6)) != 0;
                arr[i+7] = (b & (1 << 7)) != 0;
            }

            l.LogInfo("Decompressed exploration array:  " + z.Size() + ":" + arr.Length);
            return arr;
        }
        
        // TODO: Move to ExplorationMapSync.cs
        public static void OnReceiveMapData(ZRpc client, ZPackage mapData)
        {
            mapData.SetPos(0);
            
            var x = mapData.ReadInt();
            var y = mapData.ReadInt();
            
            var m =  Traverse.Create(typeof(Minimap)).Field("m_instance").GetValue() as Minimap;
            var flag = _Minimap.Explore(m, x, y);
            _dirty = flag || _dirty;
        }
        

        
        [HarmonyPatch(typeof (Minimap), "Explore", typeof(Vector3), typeof(float))]
        private  class MinimapPatchExploreInterval
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(Minimap __instance)
            {
                if (!_dirty) return;
                var fogTexture =  Traverse.Create((__instance)).Field("m_fogTexture").GetValue() as Texture2D;
                fogTexture.Apply();
                _dirty = false;
            }
        }
    }
}