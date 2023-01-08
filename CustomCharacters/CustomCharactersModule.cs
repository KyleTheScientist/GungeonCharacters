using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using BepInEx;
using GungeonAPI;

namespace CustomCharacters
{
    //Main module
    [BepInDependency(ETGModMainBehaviour.GUID)]
    [BepInPlugin(GUID, "Custom Characters Mod", version)]
    public class CustomCharactersModule : BaseUnityPlugin
    {
        public const string version = "2.2.1";
        public const string GUID = "kyle.etg.CCM";
        private static bool hasInitialized;
        public void Start()
        {
            ETGModMainBehaviour.WaitForGameManagerStart(GMStart);
        }
        public void GMStart(GameManager self)
        {
            FakePrefabHooks.Init();
            Tools.Init();
            Hooks.Init();
            CharacterSwitcher.Init();
            ShrineFactory.Init();
            Tools.Print("Did Start()", "00FF00");
        }
        //Creates characters late to prevent conflict with custom loadouts and stuff
        public static void LateStart(Action<Foyer> orig, Foyer self)
        {
            orig(self);

            Tools.Print("Late start called");
            if (hasInitialized) return;
            Tools.StartTimer("Initializing mod");
            Loader.Init();
            Franseis.Add();
            RandomShrine.Add();
            Tools.StopTimerAndReport("Initializing mod");
            hasInitialized = true;
            Tools.Print($"Custom Character Mod v{version} Initialized", "00FF00", true);
            Tools.Print("Custom Characters Available:", "00FF00", true);
            foreach (var character in CharacterBuilder.storedCharacters)
            {
                Tools.Print("    " + character.Value.First.nameShort, "00FF55", true);
            }
            ShrineFactory.PlaceBreachShrines();
        }
    }
}
