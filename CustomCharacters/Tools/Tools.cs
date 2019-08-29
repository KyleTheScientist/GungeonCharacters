using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

using UnityEngine;
using Dungeonator;
using MonoMod.RuntimeDetour;
using Semi;
using Logger = ModTheGungeon.Logger;

namespace CustomCharacters
{
    //Utility methods
    class Tools
    {
        public static bool verbose = false;
		public static string resourcesDir = Path.Combine(FileHierarchy.GameFolder, "Resources");
		private static string defaultLog = Path.Combine(resourcesDir, "customCharacterLog.txt");

        public static AssetBundle sharedAuto1 = ResourceManager.LoadAssetBundle("shared_auto_001");
        public static AssetBundle sharedAuto2 = ResourceManager.LoadAssetBundle("shared_auto_002");
        public static string modID = "CC";

        private static Dictionary<string, float> timers = new Dictionary<string, float>();

        public static void Init()
        {
            if (File.Exists(defaultLog)) File.Delete(defaultLog);
        }

        public static void StartTimer(string name)
        {
            string key = name.ToLower();
            if (timers.ContainsKey(key))
            {
                PrintError($"Timer {name} already exists.");
                return;
            }
            timers.Add(key, Time.realtimeSinceStartup);
        }

        public static void StopTimerAndReport(string name)
        {
            string key = name.ToLower();
            if (!timers.ContainsKey(key))
            {
                Tools.PrintError($"Could not stop timer {name}, no such timer exists");
                return;
            }
            float timerStart = timers[key];
            int elapsed = (int)((Time.realtimeSinceStartup - timerStart) * 1000);
            timers.Remove(key);
            Tools.Print($"{name} finished in " + elapsed + "ms");
        }

        public static void Print<T>(T obj, string color = "FFFFFF", bool force = false)
        {
			CustomCharactersMod.Logger.Info($"{modID}: {obj.ToString()}");

            //if (verbose || force)
            //    ETGModConsole.Log($"<color=#{color}>{modID}: {obj.ToString()}</color>");

            //Log(obj.ToString());
        }

        public static void PrintRaw<T>(T obj, bool force = false)
        {
			CustomCharactersMod.Logger.Info(obj.ToString());

            //if (verbose || force)
            //    ETGModConsole.Log(obj.ToString());

            //Log(obj.ToString());
        }

        public static void PrintError<T>(T obj, string color = "FF0000")
        {
			CustomCharactersMod.Logger.Error($"{modID}: {obj.ToString()}");

            //ETGModConsole.Log($"<color=#{color}>{modID}: {obj.ToString()}</color>");

            //Log(obj.ToString());
        }

        public static void PrintException(Exception e, string color = "FF0000")
        {
			CustomCharactersMod.Logger.ErrorPretty($"{modID}: {e.Message}\n{e.StackTrace}");

            //ETGModConsole.Log($"<color=#{color}>{modID}: {e.Message}</color>");
            //ETGModConsole.Log(e.StackTrace);

            //Log(e.Message);
            //Log("\t" + e.StackTrace);
        }

        public static void Log<T>(T obj)
        {
			using (StreamWriter writer = new StreamWriter(Path.Combine(resourcesDir, defaultLog), true))
            {
                writer.WriteLine(obj.ToString());
            }
        }

        public static void Log<T>(T obj, string fileName)
        {
            if (!verbose) return;
            using (StreamWriter writer = new StreamWriter(Path.Combine(resourcesDir, fileName), true))
            {
                writer.WriteLine(obj.ToString());
            }
        }

        public static void ExportTexture(Texture texture, string folder = "")
        {
            string path = Path.Combine(resourcesDir, folder);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            File.WriteAllBytes(Path.Combine(path, texture.name + ".png"), ((Texture2D)texture).EncodeToPNG());
        }

		public static RuntimeAtlasPacker Packer = new RuntimeAtlasPacker(2048, 2048);
		private readonly static Vector2[] _DefaultUVs = {
			new Vector2(0f, 0f),
			new Vector2(1f, 0f),
			new Vector2(0f, 1f),
			new Vector2(1f, 1f)
		};

		public static void ReplaceTexture(tk2dSpriteDefinition frame, Texture2D replacement, bool pack = true) {
			frame.flipped = tk2dSpriteDefinition.FlipMode.None;
			frame.materialInst = new Material(frame.material);
			frame.texelSize = replacement.texelSize;
			frame.extractRegion = true;
			if (pack)
			{
				RuntimeAtlasSegment segment = Packer.Pack(replacement, 0, 0, replacement.width, replacement.height, true);
				frame.materialInst.mainTexture = segment.texture;
				frame.uvs = segment.uvs;
			}
			else
			{
				frame.materialInst.mainTexture = replacement;
				frame.uvs = _DefaultUVs;
			}
		}

    }
}
