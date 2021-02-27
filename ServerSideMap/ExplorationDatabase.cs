using System;
using HarmonyLib;
using UnityEngine;
using BepInEx.Logging;

namespace ServerSideMap
{
    public static class ExplorationDatabase
    {
        private static bool _dirty;
        public static bool[] Explored;
        public const int MapSize = 2048; // TODO: Find out where to retrieve this from
        public const int MapSizeSquared = MapSize*MapSize;

        public static ZPackage Default()
        {
            var z = new ZPackage();
            z.Write((int) 3);
            z.Write( MapSize);
            for (var i = 0; i < MapSizeSquared; i++)
            {
                z.Write(false);
            }
            z.Write((int) 0);
            z.SetPos(0);
            return z;
        }
        
        public static void SetMapData(ZPackage mapData)
        {
            var version = mapData.ReadInt();
            var mapSize = mapData.ReadInt();
            
            var explored = new bool[mapSize * mapSize];
            for (var i = 0; i < mapSize * mapSize; i++)
            {
                explored[i] = mapData.ReadBool();
            }

            var pinCount = mapData.ReadInt();

            Explored = explored;
        }

        public static ZPackage GetMapData()
        {
            var z = new ZPackage();

            z.Write((int) 3);
            z.Write(Explored.Length);

            foreach (var t in Explored)
            {
                z.Write(t);
            }
            
            z.Write((int) 0);
            
            return z;
        }
        
        public static void SetExplored(int x, int y)
        {
            Explored[(y * MapSize + x)] = true;
        }
        
        public static bool GetExplored(int x, int y)
        {
            return Explored[(y * MapSize + x)];
        }
        
        public static bool GetExplored(int idx)
        {
            return  Explored[idx];
        }

        // TODO: Optimize
        public static bool[] GetExplorationArray()
        {
            return Explored;
        }

        public static void MergeExplorationArray(bool[] arr, int startIndex, int size)
        {
            for (var i = 0; i < size; i++)
            {
                Explored[startIndex + i] = arr[i] || Explored[startIndex + i];
            }
        }
        
        public static ZPackage PackBoolArray(bool[] arr, int chunkId, int startIndex, int size)
        {
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
            
            Utility.Log("Compressed exploration array:  " + size + ":" + z.Size());

            return z;
        }

        public static bool[] UnpackBoolArray(ZPackage z, int length)
        {
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

            Utility.Log("Decompressed exploration array:  " + z.Size() + ":" + arr.Length);
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