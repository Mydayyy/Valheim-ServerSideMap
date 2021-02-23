using HarmonyLib;
using UnityEngine;

namespace ServerSideMap
{
    public static class ExplorationDatabase
    {
        private static bool _dirty;
        public static byte[] MapData;
        public const int MapSize = 2048; // TODO: Find out where to retrieve this from

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
        
        public static void OnReceiveMapData(ZRpc client, ZPackage mapData)
        {
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