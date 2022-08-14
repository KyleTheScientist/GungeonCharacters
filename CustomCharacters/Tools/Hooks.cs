using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;
using Object = UnityEngine.Object;
using IEnumerator = System.Collections.IEnumerator;
using GungeonAPI; 

namespace CustomCharacters
{
    public static class Hooks
    {
        public static void Init()
        {
            try
            {
                On.StringTableManager.GetTalkingPlayerNick += StringTableManager_GetTalkingPlayerNick;
                On.StringTableManager.GetTalkingPlayerName += StringTableManager_GetTalkingPlayerName;
                On.dfLanguageManager.GetValue += DfLanguageManager_GetValue;
                On.Foyer.Awake += CustomCharactersModule.LateStart;
                On.PunchoutPlayerController.UpdateUI += PunchoutPlayerController_UpdateUI;
                On.Foyer.SetUpCharacterCallbacks += Foyer_SetUpCharacterCallbacks;
                On.dfControl.getLocalizedValue += DfControl_getLocalizedValue;
                On.BraveResources.Load_string_string += BraveResources_Load_string_string;
                On.Foyer.PlayerCharacterChanged += Foyer_PlayerCharacterChanged;
                On.GameManager.ClearSecondaryPlayer += GameManager_ClearSecondaryPlayer;

                Hook clearP1Hook = new Hook(
                    typeof(ETGModConsole).GetMethod("SwitchCharacter", BindingFlags.Public | BindingFlags.Static),
                    typeof(Hooks).GetMethod("PrimaryPlayerSwitched")
                );

            }
            catch (Exception e)
            {
                Tools.PrintException(e);
            }
        }
        private static void GameManager_ClearSecondaryPlayer(On.GameManager.orig_ClearSecondaryPlayer orig, GameManager self)
        {
            orig(self);
            ResetInfiniteGuns();
        }

        private static void Foyer_PlayerCharacterChanged(On.Foyer.orig_PlayerCharacterChanged orig, Foyer self, PlayerController newCharacter)
        {
            ResetInfiniteGuns();
            orig(self, newCharacter);
        }

        private static object BraveResources_Load_string_string(On.BraveResources.orig_Load_string_string orig, string path, string extension)
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

        private static string DfControl_getLocalizedValue(On.dfControl.orig_getLocalizedValue orig, dfControl self, string key)
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

        private static List<FoyerCharacterSelectFlag> Foyer_SetUpCharacterCallbacks(On.Foyer.orig_SetUpCharacterCallbacks orig, Foyer self)
        {
            var sortedByX = orig(self);

            RandomShrine.BuildCharacterList(sortedByX);
            FoyerCharacterHandler.AddCustomCharactersToFoyer(sortedByX);

            return sortedByX;
        }

        private static void PunchoutPlayerController_UpdateUI(On.PunchoutPlayerController.orig_UpdateUI orig, PunchoutPlayerController self)
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

        private static string DfLanguageManager_GetValue(On.dfLanguageManager.orig_GetValue orig, dfLanguageManager self, string key)
        {
            if (characterDeathNames.Contains(key))
            {
                if (GameManager.Instance.PrimaryPlayer != null && GameManager.Instance.PrimaryPlayer.GetComponent<CustomCharacter>() != null && GameManager.Instance.PrimaryPlayer.GetComponent<CustomCharacter>().data != null)
                {
                    return GameManager.Instance.PrimaryPlayer.GetComponent<CustomCharacter>().data.name;
                }
            }
            return orig(self, key);
        }

        private static string StringTableManager_GetTalkingPlayerName(On.StringTableManager.orig_GetTalkingPlayerName orig)
        {
            PlayerController talkingPlayer = Hooks.GetTalkingPlayer();
            if (talkingPlayer.IsThief)
            {
                return "#THIEF_NAME";
            }
            if (talkingPlayer.GetComponent<CustomCharacter>() != null)
            {
                if (talkingPlayer.GetComponent<CustomCharacter>().data != null)
                {
                    return "#PLAYER_NAME_" + talkingPlayer.GetComponent<CustomCharacter>().data.nameShort.ToUpper();
                }
            }
            if (talkingPlayer.characterIdentity == PlayableCharacters.Eevee)
            {
                return "#PLAYER_NAME_RANDOM";
            }
            if (talkingPlayer.characterIdentity == PlayableCharacters.Gunslinger)
            {
                return "#PLAYER_NAME_GUNSLINGER";
            }
            return "#PLAYER_NAME_" + talkingPlayer.characterIdentity.ToString().ToUpperInvariant();
        }

        private static string StringTableManager_GetTalkingPlayerNick(On.StringTableManager.orig_GetTalkingPlayerNick orig)
        {
            PlayerController talkingPlayer = Hooks.GetTalkingPlayer();
            if (talkingPlayer.IsThief)
            {
                return "#THIEF_NAME";
            }
            if (talkingPlayer.GetComponent<CustomCharacter>() != null)
            {
                if (talkingPlayer.GetComponent<CustomCharacter>().data != null)
                {
                    return "#PLAYER_NICK_" + talkingPlayer.GetComponent<CustomCharacter>().data.nameShort.ToUpper();
                }
            }
            if (talkingPlayer.characterIdentity == PlayableCharacters.Eevee)
            {
                return "#PLAYER_NICK_RANDOM";
            }
            if (talkingPlayer.characterIdentity == PlayableCharacters.Gunslinger)
            {
                return "#PLAYER_NICK_GUNSLINGER";
            }
            return "#PLAYER_NICK_" + talkingPlayer.characterIdentity.ToString().ToUpperInvariant();
        }

        private static PlayerController GetTalkingPlayer()
        {
            List<TalkDoerLite> allNpcs = StaticReferenceManager.AllNpcs;
            for (int i = 0; i < allNpcs.Count; i++)
            {
                if (allNpcs[i])
                {
                    if (!allNpcs[i].IsTalking || !allNpcs[i].TalkingPlayer || GameManager.Instance.HasPlayer(allNpcs[i].TalkingPlayer))
                    {
                        if (allNpcs[i].IsTalking && allNpcs[i].TalkingPlayer)
                        {
                            return allNpcs[i].TalkingPlayer;
                        }
                    }
                }
            }
            return GameManager.Instance.PrimaryPlayer;
        }


        public static void PrimaryPlayerSwitched(Action< string[]> orig, string[] args)
        {
            try
            {
                orig(args);
            }
            catch { }
            ResetInfiniteGuns();
        }

        //Resets all the character-specific infinite guns 
        public static Dictionary<int, GunBackupData> gunBackups = new Dictionary<int, GunBackupData>();
        public static void ResetInfiniteGuns()
        {
            var player1 = GameManager.Instance?.PrimaryPlayer?.GetComponent<CustomCharacter>();
            var player2 = GameManager.Instance?.SecondaryPlayer?.GetComponent<CustomCharacter>();
            List<int> removables = new List<int>();
            foreach (var entry in gunBackups)
            {
                if ((player1 && player1.GetInfiniteGunIDs().Contains(entry.Key)) || (player2 && player2.GetInfiniteGunIDs().Contains(entry.Key))) continue;
                var gun = PickupObjectDatabase.GetById(entry.Key) as Gun;
                gun.InfiniteAmmo = entry.Value.InfiniteAmmo;
                gun.CanBeDropped = entry.Value.CanBeDropped;
                gun.PersistsOnDeath = entry.Value.PersistsOnDeath;
                gun.PreventStartingOwnerFromDropping = entry.Value.PreventStartingOwnerFromDropping;
                removables.Add(entry.Key);
                Tools.Print($"Reset {gun.EncounterNameOrDisplayName} to infinite = {gun.InfiniteAmmo}");
            }
            foreach (var id in removables)
                gunBackups.Remove(id);

        }

        public static List<string> characterDeathNames = new List<string>
        {
            "#CHAR_ROGUE_SHORT",
            "#CHAR_CONVICT_SHORT",
            "#CHAR_ROBOT_SHORT",
            "#CHAR_MARINE_SHORT",
            "#CHAR_GUIDE_SHORT",
            "#CHAR_CULTIST_SHORT",
            "#CHAR_BULLET_SHORT",
            "#CHAR_PARADOX_SHORT",
            "#CHAR_GUNSLINGER_SHORT"
        };

        public struct GunBackupData
        {
            public bool InfiniteAmmo,
                CanBeDropped,
                PersistsOnDeath,
                PreventStartingOwnerFromDropping;
        }
    }
}
