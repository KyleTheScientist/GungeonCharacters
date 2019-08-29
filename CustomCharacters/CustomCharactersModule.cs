using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Reflection;
using Logger = ModTheGungeon.Logger;
using UnityEngine;
using Semi;
namespace CustomCharacters
{
    //Main module
    public class CustomCharactersMod : Mod
    {
		public static Logger Logger = new Logger("CustomCharactersMod");

		public readonly static string version = "2.0.3";
        private static bool hasInitialized;
        public override void Loaded()
        {
            FakePrefabHooks.Init();
            Tools.Init();
            Hooks.Init();
            CharacterSwitcher.Init();

            Tools.Print("Did Start()", "00FF00");
        }

        //Creates characters late to prevent conflict with custom loadouts and stuff
        public override void RegisterContent()
        {
            Tools.Print("Late start called");
            if (hasInitialized) return;
            Tools.StartTimer("Initializing mod");
            Loader.Init();
            Tools.StopTimerAndReport("Initializing mod");
            hasInitialized = true;
            Tools.Print($"Custom Character Mod v{version} Initialized", "00FF00", true);
            Tools.Print("Custom Characters Available:", "00FF00", true);
            foreach (var character in CharacterBuilder.storedCharacters)
            {
                Tools.Print("    " + character.Value.First.nameShort, "00FF55", true);
            }
        }

		public override void InitializeContent()
		{
			// nothing
		}
	}
}
