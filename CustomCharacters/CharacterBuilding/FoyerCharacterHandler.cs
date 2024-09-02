using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using GungeonAPI;
using MonoMod.RuntimeDetour;
using HutongCharacter = HutongGames.PlayMaker.Actions.ChangeToNewCharacter;

namespace CustomCharacters

{
    public static class FoyerCharacterHandler
    {
        private static FieldInfo m_isHighlighted = typeof(TalkDoerLite).GetField("m_isHighlighted", BindingFlags.NonPublic | BindingFlags.Instance);

        private static Vector3[] foyerPositions =
        {
            new Vector3(46.3f, 17.6f, 18.1f),
            new Vector3(46.7f, 21.1f, 21.6f),
            new Vector3(48.3f, 23.5f, 24.0f),
            new Vector3(53.9f, 23.5f, 24.0f),
            new Vector3(55.1f, 17f, 19.1f),
            new Vector3(61.1f, 18.4f, 18.9f),
            //new Vector3(62.9f, 23.3f, 23.8f),
            new Vector3(42.5f, 20.8f, 21.3f),
            new Vector3(47.6f, 35.5f, 36.0f),
            new Vector3(54.8f, 38.6f, 39.1f),

            new Vector3(30.6f, 34.6f, 35.1f),
            new Vector3(30.6f, 37.2f, 37.7f),
            new Vector3(30.6f, 39.8f, 40.3f),
            new Vector3(30.6f, 44.8f, 45.3f),
            new Vector3(30.6f, 47.4f, 47.9f),

            new Vector3(43.8f, 37.2f, 37.7f),
            new Vector3(43.8f, 39.8f, 40.3f),
            new Vector3(43.8f, 42.4f, 42.9f),
            new Vector3(43.8f, 44.8f, 45.3f),
            new Vector3(43.8f, 47.4f, 47.9f),

            new Vector3(24.3f, 57.7f, 58.2f),
            //new Vector3(31.6f, 57.4f, 57.9f),
            //new Vector3(43.4f, 57.6f, 58.1f),
            new Vector3(50.4f, 58.2f, 58.7f),
        };

        public static void AddCustomCharactersToFoyer(List<FoyerCharacterSelectFlag> sortedByX)
        {
            foreach (var character in CharacterBuilder.storedCharacters)
            {
                try
                {
                    Tools.Print($"Adding {character.Key} to the breach.");
                    var identity = character.Value.First.baseCharacter;
                    var selectCharacter = AddCharacterToFoyer(character.Key, GetFlagFromIdentity(identity, sortedByX).gameObject);
                    //This makes it so you can hover over them before choosing a character
                    //sortedByX.Insert(6, selectCharacter); 
                }
                catch (Exception e)
                {
                    Tools.PrintError($"An error occured while adding character {character.Key} to the breach.");
                    Tools.PrintException(e);
                }
            }
            foreach(var flag in sortedByX)
            {
                ResetToIdle(flag);
            }
        }

        public static FoyerCharacterSelectFlag GetFlagFromIdentity(PlayableCharacters character, List<FoyerCharacterSelectFlag> sortedByX)
        {
            string path;
            foreach (var flag in sortedByX)
            {
                path = flag.CharacterPrefabPath.ToLower();
                if (character == PlayableCharacters.Eevee && flag.IsEevee) return flag;
                if (character == PlayableCharacters.Gunslinger && flag.IsGunslinger) return flag;

                if (character == PlayableCharacters.Bullet && path.Contains("bullet")) return flag;
                if (character == PlayableCharacters.Convict && path.Contains("convict")) return flag;
                if (character == PlayableCharacters.Guide && path.Contains("guide")) return flag;
                if (character == PlayableCharacters.Soldier && path.Contains("marine")) return flag;
                if (character == PlayableCharacters.Robot && path.Contains("robot")) return flag;
                if (character == PlayableCharacters.Pilot && path.Contains("rogue")) return flag;
            }
            Tools.PrintError("Couldn't find foyer select flag for: " + character);
            Tools.PrintError("    Have you unlocked them yet?");
            return sortedByX[1];
        }

        private static FoyerCharacterSelectFlag AddCharacterToFoyer(string characterPath, GameObject selectFlagPrefab)
        {
            //Gather character data
            var customCharacter = CharacterBuilder.storedCharacters[characterPath.ToLower()];
            if (customCharacter.First.characterID >= foyerPositions.Length)
            {
                Tools.PrintError("Not enough room in the foyer for: " + customCharacter.First.nameShort);
                Tools.PrintError("    Use the character command instead.");
                Tools.PrintError("    Jeez, how many characters do you need?");
                return null;
            }
            Tools.Print("    Got custom character");

            //Create new object
            FoyerCharacterSelectFlag selectFlag = GameObject.Instantiate(selectFlagPrefab).GetComponent<FoyerCharacterSelectFlag>();
            selectFlag.transform.position = foyerPositions[customCharacter.First.characterID];
            selectFlag.CharacterPrefabPath = characterPath;
            selectFlag.name = characterPath + "_FoyerSelectFlag";
            Tools.Print("    Made select flag");

            //Replace sprites
            HandleSprites(selectFlag, customCharacter.Second.GetComponent<PlayerController>());
            Tools.Print("    Replaced sprites");

            var td = selectFlag.talkDoer;

            //Setup overhead card
            CreateOverheadCard(selectFlag, customCharacter.First);
            td.OverheadUIElementOnPreInteract = selectFlag.OverheadElement;
            Tools.Print("    Made Overhead Card");

            //Change the effect of talking to the character
            foreach (var state in selectFlag.playmakerFsm.Fsm.FsmComponent.FsmStates)
            {
                foreach (var action in state.Actions)
                {
                    if (action is HutongCharacter)
                    {
                        ((HutongCharacter)action).PlayerPrefabPath = characterPath;
                    }
                }
            }

            //Make interactable
            if (!Dungeonator.RoomHandler.unassignedInteractableObjects.Contains(td))
                Dungeonator.RoomHandler.unassignedInteractableObjects.Add(td);
            Tools.Print("    Adjusted Talk-Doer");

            //Player changed callback - Hides and shows player select object
            Foyer.Instance.OnPlayerCharacterChanged += (player) =>
            {
                OnPlayerCharacterChanged(player, selectFlag, characterPath);
            };
            Tools.Print("    Added callback");

            return selectFlag;
        }

        private static void HandleSprites(BraveBehaviour selectCharacter, BraveBehaviour player)
        {
            selectCharacter.spriteAnimator.Library = player.spriteAnimator.Library;
            selectCharacter.sprite.Collection = player.sprite.Collection;
            selectCharacter.renderer.material = new Material(selectCharacter.renderer.material);
            selectCharacter.sprite.ForceBuild();
            
            var idle = selectCharacter.GetComponent<CharacterSelectIdleDoer>().coreIdleAnimation;
            selectCharacter.spriteAnimator.Play(idle);
        }

        private static void ResetToIdle(BraveBehaviour idler)
        {
            SpriteOutlineManager.RemoveOutlineFromSprite(idler.sprite, true);
            SpriteOutlineManager.AddOutlineToSprite(idler.sprite, Color.black);

            //var idle = idler.GetComponent<CharacterSelectIdleDoer>().coreIdleAnimation;
            //idler.sprite.SetSprite(idler.spriteAnimator.GetClipByName(idle).frames[0].spriteId);
            //idler.talkDoer.OnExitRange(null); 
        }

        private static void CreateOverheadCard(FoyerCharacterSelectFlag selectCharacter, CustomCharacterData data)
        {
            //Create new card instance
            selectCharacter.ClearOverheadElement();
            selectCharacter.OverheadElement = FakePrefab.Clone(selectCharacter.OverheadElement);
            selectCharacter.OverheadElement.SetActive(true);

            string replaceKey = data.baseCharacter.ToString().ToUpper();
            if (data.baseCharacter == PlayableCharacters.Soldier)
                replaceKey = "MARINE";
            if (data.baseCharacter == PlayableCharacters.Pilot)
                replaceKey = "ROGUE";
            if (data.baseCharacter == PlayableCharacters.Eevee)
                replaceKey = "PARADOX";

            //Change text
            var infoPanel = selectCharacter.OverheadElement.GetComponent<FoyerInfoPanelController>();

            dfLabel nameLabel = infoPanel.textPanel.transform.Find("NameLabel").GetComponent<dfLabel>();
            nameLabel.Text = nameLabel.GetLocalizationKey().Replace(replaceKey, data.nameShort.ToUpper());

            dfLabel pastKilledLabel = infoPanel.textPanel.transform.Find("PastKilledLabel").GetComponent<dfLabel>();
            pastKilledLabel.Text = "(No Past)";

            infoPanel.itemsPanel.enabled = false;
            /*
            infoPanel.itemsPanel.ResolutionChangedPostLayout += (a, b, c) =>
            {
                infoPanel.itemsPanel.IsVisible = false;
                Tools.Print("called");
            };
            */

            //Swap out face sprites
            if (data.foyerCardSprites != null)
            {
                var facecard = selectCharacter.OverheadElement.GetComponentInChildren<CharacterSelectFacecardIdleDoer>();
                var orig = facecard.sprite.Collection;
                var copyCollection = GameObject.Instantiate(orig);

                tk2dSpriteDefinition[] copyDefinitions = new tk2dSpriteDefinition[orig.spriteDefinitions.Length];
                for (int i = 0; i < copyCollection.spriteDefinitions.Length; i++)
                {
                    copyDefinitions[i] = orig.spriteDefinitions[i].Copy();
                }
                copyCollection.spriteDefinitions = copyDefinitions;

                for (int i = 0; i < data.foyerCardSprites.Count; i++)
                {
                    var tex = data.foyerCardSprites[i];
                    var def = copyCollection.GetSpriteDefinition(tex.name);

					if (def != null) {
						def.ReplaceTexture(tex);
					}
                }
                facecard.sprite.Collection = copyCollection;

                facecard.spriteAnimator.Library = GameObject.Instantiate(facecard.spriteAnimator.Library);
                GameObject.DontDestroyOnLoad(facecard.spriteAnimator.Library);
                foreach (var clip in facecard.spriteAnimator.Library.clips)
                {
                    for (int i = 0; i < clip.frames.Length; i++)
                    {
                        clip.frames[i].spriteCollection = copyCollection;
                    }
                }
            }
            selectCharacter.OverheadElement.SetActive(false);
        }

        private static void OnPlayerCharacterChanged(PlayerController player, FoyerCharacterSelectFlag selectCharacter, string characterPath)
        {
            if (player.name.ToLower().Contains(characterPath))
            {
                Tools.Print("Selected: " + characterPath);
                if (selectCharacter.gameObject.activeSelf)
                {
                    selectCharacter.ClearOverheadElement();
                    selectCharacter.talkDoer.OnExitRange(null);
                    selectCharacter.gameObject.SetActive(false);
                    selectCharacter.GetComponent<SpeculativeRigidbody>().enabled = false;
                }
            }
            else if (!selectCharacter.gameObject.activeSelf)
            {
                selectCharacter.gameObject.SetActive(true);
                SpriteOutlineManager.RemoveOutlineFromSprite(selectCharacter.sprite, true);
                SpriteOutlineManager.AddOutlineToSprite(selectCharacter.sprite, Color.black);

                selectCharacter.specRigidbody.enabled = true;
                PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(selectCharacter.specRigidbody, null, false);

                CharacterSelectIdleDoer idleDoer = selectCharacter.GetComponent<CharacterSelectIdleDoer>();
                idleDoer.enabled = true;

            }
        }
    }
}
