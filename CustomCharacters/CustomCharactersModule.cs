using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
namespace CustomCharacters
{
    public class CustomCharactersModule : ETGModule
    {
        public readonly static string version = "2.0.2";
        private static bool hasInitialized;
        public override void Start()
        {
            FakePrefabHooks.Init();
            Tools.Init();
            Hooks.Init();
            CharacterSwitcher.Init();

            Tools.Print("Did Start()", "00FF00");
        }

        public static void LateStart(Action<Foyer> orig, Foyer self)
        {
            orig(self);

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

        public override void Exit()
        {
        }

        public override void Init()
        {
        }
    }
}
