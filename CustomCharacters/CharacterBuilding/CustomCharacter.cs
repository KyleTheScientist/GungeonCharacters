using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CustomCharacters
{
    public class CustomCharacterData
    {
        public PlayableCharacters baseCharacter = PlayableCharacters.Pilot;
        public string name, nameShort, nickname, nameInternal;
        public Dictionary<PlayerStats.StatType, float> stats;
        public List<Texture2D> sprites, foyerCardSprites, punchoutSprites, punchoutFaceCards;
        public Texture2D playerSheet, minimapIcon, bossCard;
        public Texture2D faceCard;
        public List<Tuple<PickupObject, bool>> loadout;
        public int characterID;
        public float health = 3, armor = 0;
    }

    public class CustomCharacter : MonoBehaviour
    {
        public CustomCharacterData data;
        private bool checkedGuns = false;
        private List<int> infiniteGunIDs = new List<int>();

        void Start()
        {
            GameManager.Instance.OnNewLevelFullyLoaded += CheckInfiniteGuns;
        }

        public void CheckInfiniteGuns()
        {
            Tools.Print("Doing infinite gun check...");
            try
            {
                Tools.Print("    Data check");
                if (data == null)
                {
                    var gameobjName = this.gameObject.name.ToLower().Replace("(clone)", "");
                    foreach (var cc in CharacterBuilder.storedCharacters.Keys)
                    {
                        if (cc.ToLower().Contains(gameobjName))
                            data = CharacterBuilder.storedCharacters[cc].First;
                    }
                }

                if (data == null)
                {
                    Tools.PrintError("Couldn't find a character data object for this player!");
                    return;
                }

                Tools.Print("    Loadout check");
                var loadout = data.loadout;
                if (loadout == null)
                {
                    checkedGuns = true;
                    return;
                }
                var player = GetComponent<PlayerController>();
                if (player?.inventory?.AllGuns == null)
                {
                    Tools.PrintError("Player or inventory not found");
                    return;
                }

                CollectInfiniteGunIDs(); //Store all infinite gun PickupObject IDs in "infiniteGunIDs"
                Tools.Print("    Gun check");
                foreach (var gun in player.inventory.AllGuns)
                {
                    if (infiniteGunIDs.Contains(gun.PickupObjectId))
                    {
                        gun.InfiniteAmmo = true;
                    }
                }
                checkedGuns = true;
            }
            catch (Exception e)
            {
                Tools.PrintError("Infinite gun check failed");
                //Tools.PrintException(e);
            }
        }

        void CollectInfiniteGunIDs()
        {
            infiniteGunIDs = new List<int>();
            foreach (var item in data.loadout)
            {
                var g = item?.First?.GetComponent<Gun>();
                if (g && item.Second)
                    infiniteGunIDs.Add(g.PickupObjectId);
            }
        }

        //I was having some issues with guns marked "Infinite Ammo" not actually having infinite ammo.
        //This just ensures that doesn't happen
        void FixedUpdate()
        {
            if (GameManager.Instance.IsLoadingLevel || GameManager.Instance.IsPaused) return;
            if (!checkedGuns)
                CheckInfiniteGuns();
            if (data == null) return;

            foreach (var gun in GetComponent<PlayerController>().inventory.AllGuns)
            {
                if (gun.InfiniteAmmo && infiniteGunIDs.Contains(gun.PickupObjectId))
                {
                    gun.ammo = gun.AdjustedMaxAmmo;
                    gun.RequiresFundsToShoot = false;

                    if (gun.UsesRechargeLikeActiveItem)
                    {
                        gun.OnPostFired += (PlayerController, Gun) =>
                        {
                            gun.RemainingActiveCooldownAmount = 0;
                        };
                    }
                    if (gun.RemainingActiveCooldownAmount > 0)
                    {
                        gun.RemainingActiveCooldownAmount -= 2;
                    }
                }
            }
        }
    }
}
