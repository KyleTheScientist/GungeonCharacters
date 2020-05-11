using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using GungeonAPI;

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
        private bool failedToFindData = false;
        private List<int> infiniteGunIDs = new List<int>();

        void Start()
        {
            GetData();
            GameManager.Instance.OnNewLevelFullyLoaded += StartInfiniteGunCheck;
            if (!GameManager.Instance.IsFoyer)
            {
                StartInfiniteGunCheck();
            }
        }

        void GetData()
        {
            try
            {
                var gameobjName = this.gameObject.name.ToLower().Replace("(clone)", "").Trim();
                foreach (var cc in CharacterBuilder.storedCharacters.Keys)
                {
                    if (cc.ToLower().Equals(gameobjName))
                        data = CharacterBuilder.storedCharacters[cc].First;
                }
            }
            catch
            {
                failedToFindData = true;
            }
            if (data == null) failedToFindData = true;
        }

        public void StartInfiniteGunCheck()
        {
            StartCoroutine("CheckInfiniteGuns");
        }

        public IEnumerator CheckInfiniteGuns()
        {
            while (!checkedGuns)
            {
                Tools.Print("    Data check");
                if (data == null)
                {
                    Tools.PrintError("Couldn't find a character data object for this player!");
                    yield return new WaitForSeconds(.1f);
                }

                Tools.Print("    Loadout check");
                var loadout = data.loadout;
                if (loadout == null)
                {
                    checkedGuns = true;
                    yield break;
                }

                var player = GetComponent<PlayerController>();
                if (player?.inventory?.AllGuns == null)
                {
                    Tools.PrintError("Player or inventory not found");
                    yield return new WaitForSeconds(.1f);
                }
                Tools.Print($"Doing infinite gun check on {player.name}");

                this.infiniteGunIDs = GetInfiniteGunIDs();
                Tools.Print("    Gun check");
                foreach (var gun in player.inventory.AllGuns)
                {
                    if (infiniteGunIDs.Contains(gun.PickupObjectId))
                    {
                        if (!Hooks.gunBackups.ContainsKey(gun.PickupObjectId))
                        {
                            var backup = new Hooks.GunBackupData()
                            {
                                InfiniteAmmo = gun.InfiniteAmmo,
                                PreventStartingOwnerFromDropping = gun.PreventStartingOwnerFromDropping,
                                CanBeDropped = gun.CanBeDropped,
                                PersistsOnDeath = gun.PersistsOnDeath
                            };
                            Hooks.gunBackups.Add(gun.PickupObjectId, backup);
                            var prefab = PickupObjectDatabase.GetById(gun.PickupObjectId) as Gun;
                            prefab.InfiniteAmmo = true;
                            prefab.PersistsOnDeath = true;
                            prefab.CanBeDropped = false;
                            prefab.PreventStartingOwnerFromDropping = true;
                        }

                        gun.InfiniteAmmo = true;
                        gun.PersistsOnDeath = true;
                        gun.CanBeDropped = false;
                        gun.PreventStartingOwnerFromDropping = true;
                        Tools.Print($"        {gun.name} is infinite now.");
                    }
                }
                checkedGuns = true;
                yield break;
            }
        }

        public List<int> GetInfiniteGunIDs()
        {
            var infiniteGunIDs = new List<int>();
            if (data == null) GetData();
            if (data == null || failedToFindData) return infiniteGunIDs;
            foreach (var item in data.loadout)
            {
                var g = item?.First?.GetComponent<Gun>();
                if (g && item.Second)
                    infiniteGunIDs.Add(g.PickupObjectId);
            }
            return infiniteGunIDs;
        }

        //This handles the dueling laser problem
        void FixedUpdate()
        {
            if (GameManager.Instance.IsLoadingLevel || GameManager.Instance.IsPaused) return;
            if (data == null) return;

            foreach (var gun in GetComponent<PlayerController>().inventory.AllGuns)
            {
                if (gun.InfiniteAmmo && infiniteGunIDs.Contains(gun.PickupObjectId))
                {
                    gun.ammo = gun.AdjustedMaxAmmo;
                    gun.RequiresFundsToShoot = false;

                    if (gun.UsesRechargeLikeActiveItem)
                    {
                        if (gun.RemainingActiveCooldownAmount > 0)
                        {
                            gun.RemainingActiveCooldownAmount = Mathf.Max(0f, gun.RemainingActiveCooldownAmount - 25f * BraveTime.DeltaTime);
                        }
                    }

                }
            }
        }

        void OnDestroy()
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= StartInfiniteGunCheck;
        }
    }
}
