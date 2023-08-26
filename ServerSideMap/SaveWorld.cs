using System;
using System.IO;
using System.Runtime.InteropServices;
using HarmonyLib;

namespace ServerSideMap
{
    public class SaveWorld
    {
        public static void GenerateDefaultExploredFile(ZNet __instance)
        {
            // ReSharper disable once RedundantCast
            ExplorationDatabase.SetMapData(ExplorationDatabase.Default());
            Utility.Log("new explore file generated");
            __instance.Save(true, false);
            return;
        }
        
        [HarmonyPatch(typeof (ZNet), "LoadWorld")]
        private  class ZnetPatchLoadMap
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(ZNet __instance)
            {
                var world =  Traverse.Create(typeof(ZNet)).Field("m_world").GetValue() as World;
                var worldSavePath = System.IO.Path.ChangeExtension(world.GetDBPath(), null);
                Utility.Log("World .explored save path: " + worldSavePath);
                var exploredPath = worldSavePath + ".mod.serversidemap.explored";
                
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
                    GenerateDefaultExploredFile(__instance);
                    return;
                }
                
                BinaryReader reader = new BinaryReader(fileStream);
                // var data = reader.ReadBytes(int.MaxValue);
                var data = reader.ReadAllBytes();
                reader.Dispose();
                if (data.Length == 0)
                {
                    Utility.Log("Existing explored file is 0 bytes. Generating new one.");
                    GenerateDefaultExploredFile(__instance);
                    return;
                }
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
                var data = ExplorationDatabase.GetMapData().GetArray();
                if (data.Length == 0)
                {
                    Utility.Log("Explored data is zero bytes. Refusing to save.");
                    return;
                }
                var world =  Traverse.Create(typeof(ZNet)).Field("m_world").GetValue() as World;
                var worldSavePath = System.IO.Path.ChangeExtension(world.GetDBPath(), null);
                var exploredPath = worldSavePath + ".mod.serversidemap.explored";
                Utility.Log("World .explored save path: " + worldSavePath);
                FileStream fileStream = File.Create(exploredPath);
                BinaryWriter writer = new BinaryWriter(fileStream);
                writer.Write(data);
                writer.Flush();
                fileStream.Flush(true);
                fileStream.Close();
                fileStream.Dispose();
            }
        }
    }
}