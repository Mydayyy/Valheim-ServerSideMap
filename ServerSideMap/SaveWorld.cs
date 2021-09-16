using System;
using System.IO;
using System.Runtime.InteropServices;
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
                var world =  Traverse.Create(typeof(ZNet)).Field("m_world").GetValue() as World;
                var worldSavePath =  Traverse.Create(world).Field("m_worldSavePath").GetValue() as String;
                var exploredPath = worldSavePath + "/" + world.m_name + ".mod.serversidemap.explored";
                
                // Make one time backup before hs
                var backupPath = exploredPath + ".beforehs";
                if (File.Exists(exploredPath) && !File.Exists(backupPath))
                {
                    File.Copy(exploredPath, backupPath);
                }
                //

                FileStream fileStream;
                try
                {
                    fileStream = File.OpenRead(exploredPath);
                }
                catch
                {
                    // ReSharper disable once RedundantCast
                    ExplorationDatabase.SetMapData(ExplorationDatabase.Default());
                    Utility.Log("new explore file generated");
                    __instance.Save(true);
                    return;
                }
                
                BinaryReader reader = new BinaryReader(fileStream);
                // var data = reader.ReadBytes(int.MaxValue);
                var data = reader.ReadAllBytes();
                var z = new ZPackage(data);
                ExplorationDatabase.SetMapData(z);
                Utility.Log("loaded from existing explore file");
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
                writer.Write(ExplorationDatabase.GetMapData().GetArray());
                writer.Flush();
                fileStream.Flush(true);
                fileStream.Close();
                fileStream.Dispose();
            }
        }
    }
}