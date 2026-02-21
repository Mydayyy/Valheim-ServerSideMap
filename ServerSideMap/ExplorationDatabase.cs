using System;
using System.Collections.Generic;
using System.Linq;
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
        
        private static List<PinData> ServerPins = new List<PinData>();
        public static List<PinData> ClientPins = new List<PinData>();

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

        public static ZPackage PackPins(List<PinData> pins)
        {
            var z = new ZPackage();
            z.Write((int) pins.Count);
            foreach (var pin in pins)
            {
                z.Write(pin.Name);
                z.Write(pin.Pos);
                z.Write((int) pin.Type);
                z.Write(pin.Checked);
            }
            z.SetPos(0);
            Utility.Log("Packing pins: " + pins.Count);
            return z;
        }

        public static List<PinData> UnpackPins(ZPackage z)
        {
            var pins = new List<PinData>();
            
            var pinCount = z.ReadInt();

            for (var i = 0; i < pinCount; i++)
            {
                var pin = new PinData
                {
                    Name = z.ReadString(),
                    Pos = z.ReadVector3(),
                    Type = (Minimap.PinType) z.ReadInt(),
                    Checked = z.ReadBool()
                };
                pins.Add(pin);
            }

            return pins;
        }
        
        public static ZPackage PackPin(PinData pin, bool skipSetPos = false)
        {
            var z = new ZPackage();
            z.Write(pin.Name);
            z.Write(pin.Pos);
            z.Write((int) pin.Type);
            z.Write(pin.Checked);
            if(!skipSetPos) 
                z.SetPos(0);
            return z;
        }

        public static PinData UnpackPin(ZPackage z)
        {
            var pin = new PinData
            {
                Name = z.ReadString(),
                Pos = z.ReadVector3(),
                Type = (Minimap.PinType) z.ReadInt(),
                Checked = z.ReadBool()
            };
            return pin;
        }

        public static List<PinData> GetPins()
        {
            return ServerPins;
        }

        public static void AddPin(PinData pin)
        {
            ServerPins.Add(pin);
        }
        
        public static void RemovePinEqual(PinData needle)
        {
            foreach (var pin in ServerPins.ToList())
            {
                if (UtilityPin.ArePinsEqual(pin, needle))
                {
                    ServerPins.Remove(pin);
                }
            }
        }

        public static void SetPinState(PinData needle, bool state)
        {
            foreach (var pin in ServerPins.ToList())
            {
                if (UtilityPin.ArePinsEqual(pin, needle))
                {
                    pin.Checked = state;
                }
            }
        }
        
        public static void SetMapData(ZPackage mapData)
        {
            ServerPins.Clear();
            
            var version = mapData.ReadInt();
            var mapSize = mapData.ReadInt();
            
            var explored = new bool[mapSize * mapSize];
            for (var i = 0; i < mapSize * mapSize; i++)
            {
                explored[i] = mapData.ReadBool();
            }

            var pinCount = mapData.ReadInt();

            for (var i = 0; i < pinCount; i++)
            {
                var pin = new PinData
                {
                    Name = mapData.ReadString(),
                    Pos = mapData.ReadVector3(),
                    Type = (Minimap.PinType) mapData.ReadInt(),
                    Checked = mapData.ReadBool()
                };
                ServerPins.Add(pin);
            }

            Explored = explored;
        }

        public static ZPackage GetMapData()
        {
            var z = new ZPackage();

            z.Write((int) 3);
            z.Write(MapSize);

            foreach (var t in Explored)
            {
                z.Write(t);
            }
            
            z.Write((int) ServerPins.Count);

            Utility.Log("Map saved. Pin Count: " + ServerPins.Count);
            
            foreach (var pin in ServerPins)
            {
                z.Write(pin.Name);
                z.Write(pin.Pos);
                z.Write((int) pin.Type);
                z.Write(pin.Checked);
            }
            
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
            
            // Utility.Log("Compressed exploration array:  " + size + ":" + z.Size());

            return z;
        }

        public static bool[] UnpackBoolArray(ZPackage z, int length)
        {
            var arr = new bool[length];
            
            for (var i = 0; i < length; i += 8)
            {          
                var b = z.ReadByte();
                
                for (var bit = 0; bit < 8 && i + bit < length; bit++)
                {
                    arr[i + bit] = (b & (1 << bit)) != 0;
                }
            }

            return arr;
        }
        
        // TODO: Move to ExplorationMapSync.cs
        public static void OnReceiveMapData(ZRpc client, ZPackage mapData)
        {
            mapData.SetPos(0);
            
            var x = mapData.ReadInt();
            var y = mapData.ReadInt();
            
            var m =  Traverse.Create(typeof(Minimap)).Field("m_instance").GetValue() as Minimap;
            if (m == null) return;
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
                fogTexture?.Apply();
                _dirty = false;
            }
        }
    }
    
    public class PinData
    {
        public string Name;
        public Minimap.PinType Type;
        public Vector3 Pos;
        public bool Checked;
    }
}