using System;
using System.IO;
using HarmonyLib;

namespace ServerSideMap
{
    public class SaveWorld
    {
        [HarmonyPatch(typeof (ZNet), "LoadWorld")]
        private  class ZnetPatchLoadMap
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(ZNet __instance)
            {
                var l = BepInEx.Logging.Logger.CreateLogSource("ServerSideMap");
                
                var world =  Traverse.Create(typeof(ZNet)).Field("m_world").GetValue() as World;
                var worldSavePath =  Traverse.Create(world).Field("m_worldSavePath").GetValue() as String;
                var exploredPath = worldSavePath + "/" + world.m_name + ".mod.serversidemap.explored";
                
                FileStream fileStream;
                try
                {
                    fileStream = File.OpenRead(exploredPath);
                }
                catch
                {
                    var z = new ZPackage();
                    // ReSharper disable once RedundantCast
                    z.Write((int) 3);
                    z.Write(ExplorationDatabase.MapSize);
                    for (var i = 0; i < ExplorationDatabase.MapSize*ExplorationDatabase.MapSize; i++)
                    {
                        z.Write(false);
                    }
                    z.Write(0);
                    ExplorationDatabase.MapData = z.GetArray();
                    l.LogInfo("new explore file generated");
                    __instance.Save(true);
                    return;
                }
                BinaryReader reader = new BinaryReader(fileStream);
                ExplorationDatabase.MapData = reader.ReadBytes(int.MaxValue);
                l.LogInfo("loaded from existing explore file");
            }
        }

        [HarmonyPatch(typeof (ZNet), "SaveWorldThread")]
        private  class ZnetPatchSaveWorldThread
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix()
            {
                var world =  Traverse.Create(typeof(ZNet)).Field("m_world").GetValue() as World;
                var worldSavePath =  Traverse.Create(world).Field("m_worldSavePath").GetValue() as String;
                var exploredPath = worldSavePath + "/" + world.m_name + ".mod.serversidemap.explored";
        
                FileStream fileStream = File.Create(exploredPath);
                BinaryWriter writer = new BinaryWriter(fileStream);
                writer.Write(ExplorationDatabase.MapData);
                writer.Flush();
                fileStream.Flush(true);
                fileStream.Close();
                fileStream.Dispose();
            }
        }
    }
}