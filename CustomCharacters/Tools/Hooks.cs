using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;
using Object = UnityEngine.Object;
using IEnumerator = System.Collections.IEnumerator;


namespace CustomCharacters
{
    public static class Hooks
    {
        public static void Init()
        {

			//Hook lateStartHook = new Hook(
			//    typeof(Foyer).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance),
			//    typeof(CustomCharactersMod).GetMethod("LateStart")
			//);

            Hook punchoutUIHook = new Hook(
                typeof(PunchoutPlayerController).GetMethod("UpdateUI", BindingFlags.Public | BindingFlags.Instance),
                typeof(Hooks).GetMethod("PunchoutUpdateUI")
            );

            Hook foyerCallbacksHook = new Hook(
                typeof(Foyer).GetMethod("SetUpCharacterCallbacks", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(Hooks).GetMethod("FoyerCallbacks")
            );
            Hook languageManagerHook = new Hook(
                typeof(dfControl).GetMethod("getLocalizedValue", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(Hooks).GetMethod("DFGetLocalizedValue")
            );

            var braveSETypes = new Type[]
            {
                typeof(string),
                typeof(string),
            };
            Hook braveLoad = new Hook(
                typeof(BraveResources).GetMethod("Load", BindingFlags.Public | BindingFlags.Static, null, braveSETypes, null),
                typeof(Hooks).GetMethod("BraveLoadObject")
            );

            Hook playerSwitchHook = new Hook(
                typeof(Foyer).GetMethod("PlayerCharacterChanged", BindingFlags.Public | BindingFlags.Instance),
                typeof(Hooks).GetMethod("OnPlayerChanged")
            );

            Hook clearPlayerHook = new Hook(
                typeof(GameManager).GetMethod("ClearPrimaryPlayer", BindingFlags.Public | BindingFlags.Instance),
                typeof(Hooks).GetMethod("OnPlayerCleared")
            );
        }

        //Hook for Punchout UI being updated (called when UI updates)
        public static void PunchoutUpdateUI(Action<PunchoutPlayerController> orig, PunchoutPlayerController self)
        {
            orig(self);
            var customChar = GameManager.Instance.PrimaryPlayer.GetComponent<CustomCharacter>();
            if (customChar != null)
            {
                char index = self.PlayerUiSprite.SpriteName.Last();
                SpriteHandler.HandlePunchoutSprites(self, customChar.data);
                if (customChar.data.punchoutFaceCards != null)
                {
                    self.PlayerUiSprite.SpriteName = customChar.data.nameInternal + "_punchout_facecard" + index;
                    Tools.Print(self.PlayerUiSprite.SpriteName);
                }
            }
        }

        //Triggers FoyerCharacterHandler (called from Foyer.SetUpCharacterCallbacks)
        public static List<FoyerCharacterSelectFlag> FoyerCallbacks(Func<Foyer, List<FoyerCharacterSelectFlag>> orig, Foyer self)
        {
            var sortedByX = orig(self);

            FoyerCharacterHandler.AddCustomCharactersToFoyer(sortedByX);

            return sortedByX;
        }

        //Used to add in strings 
        public static string DFGetLocalizedValue(Func<dfControl, string, string> orig, dfControl self, string key)
        {
            foreach (var pair in StringHandler.customStringDictionary)
            {
                if (pair.Key.ToLower() == key.ToLower())
                {
                    return pair.Value;
                }
            }
            return orig(self, key);
        }

        //Used to set fake player prefabs to active on instantiation (hook doesn't work on this call)
        public static Object BraveLoadObject(Func<string, string, Object> orig, string path, string extension = ".prefab")
        {
            var value = orig(path, extension);
            if (value == null)
            {
                path = path.ToLower();
                if (CharacterBuilder.storedCharacters.ContainsKey(path))
                {
                    var character = CharacterBuilder.storedCharacters[path].Second;
                    return character;
                }
            }
            return value;
        }

        public static void OnPlayerChanged(Action<Foyer, PlayerController> orig, Foyer self, PlayerController player)
        {
            ResetCustomCharacters();
            orig(self, player);
        }

        public static void OnPlayerCleared(Action<GameManager> orig, GameManager self)
        {
            ResetCustomCharacters();
            orig(self);
        }

        //Resets all the character-specific infinite guns 
        public static void ResetCustomCharacters()
        {
            foreach (var gun in CharacterBuilder.guns)
            {
                gun.InfiniteAmmo = false;
            }
        }
    }
}
