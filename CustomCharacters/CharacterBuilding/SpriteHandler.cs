using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

namespace CustomCharacters
{
    /*
     * Handles adding sprites to the appropriate collections,
     * creating animations, and loading them when necessary
     */

    public static class SpriteHandler
    {
        private static FieldInfo spriteNameLookup =
            typeof(tk2dSpriteCollectionData).GetField("spriteNameLookupDict",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static FieldInfo m_playerMarkers = typeof(Minimap).GetField("m_playerMarkers", BindingFlags.NonPublic | BindingFlags.Instance);
        public static dfAtlas uiAtlas;
        public static List<dfAtlas.ItemInfo> uiFaceCards = new List<dfAtlas.ItemInfo>();
        public static List<dfAtlas.ItemInfo> punchoutFaceCards = new List<dfAtlas.ItemInfo>();

        private static readonly Rect uiFacecardBounds = new Rect(0, 1235, 2048, 813);
        private static readonly Rect punchoutFacecardBounds = new Rect(128, 71, 128, 186);
        private static readonly Vector2 faceCardSizeInPixels = new Vector2(34, 34);

        private static Dictionary<string, Material[]> usedMaterialDictionary = new Dictionary<string, Material[]>();

        public static void HandleSprites(PlayerController player, CustomCharacterData data)
        {
            if (data.minimapIcon != null)
                HandleMinimapIcons(player, data);

            if (data.bossCard != null)
                HandleBossCards(player, data);

            if (data.sprites != null || data.playerSheet != null)
                HandleAnimations(player, data);

            //face card stuff
            uiAtlas = GameUIRoot.Instance.ConversationBar.portraitSprite.Atlas;
            if (data.faceCard != null)
                HandleFacecards(player, data);

            if (data.punchoutFaceCards != null && data.punchoutFaceCards.Count > 0)
                HandlePunchoutFaceCards(data);
        }

        public static void HandleAnimations(PlayerController player, CustomCharacterData data)
        {
            var orig = player.sprite.Collection;
            var copyCollection = GameObject.Instantiate(orig);
            GameObject.DontDestroyOnLoad(copyCollection);

            tk2dSpriteDefinition[] copyDefinitions = new tk2dSpriteDefinition[orig.spriteDefinitions.Length];
            for (int i = 0; i < copyCollection.spriteDefinitions.Length; i++)
            {
                copyDefinitions[i] = orig.spriteDefinitions[i].Copy();
            }
            copyCollection.spriteDefinitions = copyDefinitions;

            if (data.playerSheet != null)
            {
                Tools.Print("        Using sprite sheet replacement.", "FFBB00");
                var materialsToCopy = orig.materials;
                copyCollection.materials = new Material[orig.materials.Length];
                for (int i = 0; i < copyCollection.materials.Length; i++)
                {
                    if (materialsToCopy[i] == null) continue;
                    var mat = new Material(materialsToCopy[i]);
                    GameObject.DontDestroyOnLoad(mat);
                    mat.mainTexture = data.playerSheet;
                    mat.name = materialsToCopy[i].name;
                    copyCollection.materials[i] = mat;
                }

                for (int i = 0; i < copyCollection.spriteDefinitions.Length; i++)
                {
                    foreach (var mat in copyCollection.materials)
                    {
                        if (mat != null && copyDefinitions[i].material.name.Equals(mat.name))
                        {
                            copyDefinitions[i].material = mat;
                            copyDefinitions[i].materialInst = new Material(mat);
                        }
                    }
                }
            }
            else if (data.sprites != null)
            {
                Tools.Print("        Using individual sprite replacement.", "FFBB00");
                bool notSlinger = data.baseCharacter != PlayableCharacters.Gunslinger;

                RuntimeAtlasPage page = new RuntimeAtlasPage();
                for (int i = 0; i < data.sprites.Count; i++)
                {
                    var tex = data.sprites[i];

                    float nw = (tex.width) / 16f;
                    float nh = (tex.height) / 16f;

                    var def = copyCollection.GetSpriteDefinition(tex.name);
                    if (def != null)
                    {

                        if (notSlinger && def.boundsDataCenter != Vector3.zero)
                        {
                            var ras = page.Pack(tex);
                            def.materialInst.mainTexture = ras.texture;
                            def.uvs = ras.uvs;
                            def.extractRegion = true;
                            def.position0 = new Vector3(0, 0, 0);
                            def.position1 = new Vector3(nw, 0, 0);
                            def.position2 = new Vector3(0, nh, 0);
                            def.position3 = new Vector3(nw, nh, 0);

                            def.boundsDataCenter = new Vector2(nw / 2, nh / 2);
                            def.untrimmedBoundsDataCenter = def.boundsDataCenter;

                            def.boundsDataExtents = new Vector2(nw, nh);
                            def.untrimmedBoundsDataExtents = def.boundsDataExtents;
                        }
                        else
                        {
                            def.ReplaceTexture(tex);
                        }
                    }
                }
                page.Apply();
            }
            else
            {
                Tools.Print("        Not replacing sprites.", "FFFF00");
            }

            player.spriteAnimator.Library = GameObject.Instantiate(player.spriteAnimator.Library);
            GameObject.DontDestroyOnLoad(player.spriteAnimator.Library);

            foreach (var clip in player.spriteAnimator.Library.clips)
            {
                for (int i = 0; i < clip.frames.Length; i++)
                {
                    clip.frames[i].spriteCollection = copyCollection;
                }
            }



            copyCollection.name = player.OverrideDisplayName;

            player.primaryHand.sprite.Collection = copyCollection;
            player.secondaryHand.sprite.Collection = copyCollection;
            player.sprite.Collection = copyCollection;
        }

        public static Vector2[] GetMarginUVS(Texture2D orig, Texture2D margined)
        {
            int padding = TextureStitcher.padding;

            //float xOff = 0;
            //float yOff = 0;
            float xOff = (float)(padding) / (margined.width);
            float yOff = (float)(padding) / (margined.height);

            float w = (float)(orig.width) / (margined.width);
            float h = (float)(orig.height) / (margined.height);

            return new Vector2[]
            {
                new Vector2(xOff, yOff),
                new Vector2(xOff + w, yOff),
                new Vector2(xOff, yOff + h),
                new Vector2(xOff + w, yOff + h)
            };
        }

        public static Vector2[] defaultUVS = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };


        public static void HandleMinimapIcons(PlayerController player, CustomCharacterData data)
        {
            player.minimapIconPrefab = GameObject.Instantiate(player.minimapIconPrefab);
            var minimapSprite = player.minimapIconPrefab.GetComponent<tk2dSprite>();
            GameObject.DontDestroyOnLoad(minimapSprite);

            string iconName = "Player_" + player.name + "_001";
            int id = minimapSprite.Collection.GetSpriteIdByName(iconName, -1); //return -1 if not found

            if (id < 0)
            {
                var spriteDef = minimapSprite.GetCurrentSpriteDef();
                var copy = spriteDef.Copy();
                copy.ReplaceTexture(data.minimapIcon);
                copy.name = iconName;
                id = AddSpriteToCollection(copy, minimapSprite.Collection);
            }
            else
            {
                Tools.Print("Minimap icon for " + iconName + " already found, not generating a new one");
            }

            //SetMinimapIconSpriteID(minimapSprite.spriteId, id);
            minimapSprite.SetSprite(id);
        }

        public static void HandleBossCards(PlayerController player, CustomCharacterData data)
        {
            int count = player.BosscardSprites.Count;
            player.BosscardSprites = new List<Texture2D>();
            for (int i = 0; i < count; i++)
            {
                player.BosscardSprites.Add(data.bossCard);
            }
        }

        public static void HandleFacecards(PlayerController player, CustomCharacterData data)
        {
            var atlas = uiAtlas;
            var atlasTex = atlas.Texture;

            dfAtlas.ItemInfo info = new dfAtlas.ItemInfo();
            info.name = player.name + "_facecard";
            info.region = TextureStitcher.AddFaceCardToAtlas(data.faceCard, atlasTex, uiFaceCards.Count, uiFacecardBounds);
            info.sizeInPixels = faceCardSizeInPixels;

            atlas.AddItem(info);

            if (atlas.Replacement)
            {
                atlas.Replacement.Material.mainTexture = atlasTex;
            }

            uiFaceCards.Add(info);
        }

        public static void HandlePunchoutSprites(PunchoutPlayerController player, CustomCharacterData data)
        {
            var primaryPlayer = GameManager.Instance.PrimaryPlayer;
            player.PlayerUiSprite.Atlas = uiAtlas;

            if (data != null)
            {
                if (data.punchoutSprites != null && player.sprite.Collection.name != (data.nameShort + " Punchout Collection"))
                    HandlePunchoutAnimations(player, data);

                if (data.faceCard != null)
                {
                    player.PlayerUiSprite.SpriteName = data.nameInternal + "_punchout_facecard1";
                }

            }
        }

        public static void HandlePunchoutFaceCards(CustomCharacterData data)
        {
            var atlas = uiAtlas;
            var atlasTex = atlas.Texture;
            if (data.punchoutFaceCards != null)
            {
                Tools.Print("Adding punchout facecards");
                int count = Mathf.Min(data.punchoutFaceCards.Count, 3);
                for (int i = 0; i < count; i++)
                {
                    dfAtlas.ItemInfo info = new dfAtlas.ItemInfo();
                    info.name = data.nameInternal + "_punchout_facecard" + (i + 1);
                    info.region = TextureStitcher.AddFaceCardToAtlas(data.punchoutFaceCards[i], atlasTex, uiFaceCards.Count, uiFacecardBounds);
                    info.sizeInPixels = faceCardSizeInPixels;

                    atlas.AddItem(info);
                    uiFaceCards.Add(info);
                }
            }
        }

        public static void HandlePunchoutAnimations(PunchoutPlayerController player, CustomCharacterData data)
        {
            Tools.Print("Replacing punchout sprites...");

            var orig = player.sprite.Collection;
            var copyCollection = GameObject.Instantiate(orig);

            GameObject.DontDestroyOnLoad(copyCollection);

            tk2dSpriteDefinition[] copyDefinitions = new tk2dSpriteDefinition[orig.spriteDefinitions.Length];
            for (int i = 0; i < copyCollection.spriteDefinitions.Length; i++)
            {
                copyDefinitions[i] = orig.spriteDefinitions[i].Copy();
            }
            copyCollection.spriteDefinitions = copyDefinitions;

            foreach (var tex in data.punchoutSprites)
            {
                var def = copyCollection.GetSpriteDefinition(tex.name);
                if (def != null)
                {
                    def.ReplaceTexture(tex.CropWhiteSpace());
                }
            }

            player.spriteAnimator.Library = GameObject.Instantiate(player.spriteAnimator.Library);
            GameObject.DontDestroyOnLoad(player.spriteAnimator.Library);

            foreach (var clip in player.spriteAnimator.Library.clips)
            {
                for (int i = 0; i < clip.frames.Length; i++)
                {
                    clip.frames[i].spriteCollection = copyCollection;
                }
            }

            copyCollection.name = data.nameShort + " Punchout Collection";
            //CharacterBuilder.storedCollections.Add(data.nameInternal, copyCollection);
            player.sprite.Collection = copyCollection;
            Tools.Print("Punchout sprites successfully replaced");
        }

        public static void SetMinimapIconSpriteID(int key, int value)
        {
            if (Minimap.HasInstance)
            {
                var playerMarkers = (List<Tuple<Transform, Renderer>>)m_playerMarkers.GetValue(Minimap.Instance);
                foreach (var marker in playerMarkers)
                {
                    var sprite = marker.First.gameObject.GetComponent<tk2dSprite>();
                    if (sprite != null && sprite.spriteId == key)
                    {
                        sprite.SetSprite(value);
                    }
                }
            }
        }

        public static tk2dSpriteDefinition Copy(this tk2dSpriteDefinition orig)
        {
            tk2dSpriteDefinition copy = new tk2dSpriteDefinition()
            {
                boundsDataCenter = orig.boundsDataCenter,
                boundsDataExtents = orig.boundsDataExtents,
                colliderConvex = orig.colliderConvex,
                colliderSmoothSphereCollisions = orig.colliderSmoothSphereCollisions,
                colliderType = orig.colliderType,
                colliderVertices = orig.colliderVertices,
                collisionLayer = orig.collisionLayer,
                complexGeometry = orig.complexGeometry,
                extractRegion = orig.extractRegion,
                flipped = orig.flipped,
                indices = orig.indices,
                material = new Material(orig.material),
                materialId = orig.materialId,
                materialInst = new Material(orig.materialInst),
                metadata = orig.metadata,
                name = orig.name,
                normals = orig.normals,
                physicsEngine = orig.physicsEngine,
                position0 = orig.position0,
                position1 = orig.position1,
                position2 = orig.position2,
                position3 = orig.position3,
                regionH = orig.regionH,
                regionW = orig.regionW,
                regionX = orig.regionX,
                regionY = orig.regionY,
                tangents = orig.tangents,
                texelSize = orig.texelSize,
                untrimmedBoundsDataCenter = orig.untrimmedBoundsDataCenter,
                untrimmedBoundsDataExtents = orig.untrimmedBoundsDataExtents,
                uvs = orig.uvs
            };
            return copy;
        }

        public static tk2dSpriteAnimationClip CopyOf(tk2dSpriteAnimationClip orig)
        {
            return new tk2dSpriteAnimationClip(orig);
        }

        public static int AddSpriteToCollection(tk2dSpriteDefinition spriteDefinition, tk2dSpriteCollectionData collection)
        {
            //Add definition to collection
            var defs = collection.spriteDefinitions;
            var newDefs = defs.Concat(new tk2dSpriteDefinition[] { spriteDefinition }).ToArray();
            collection.spriteDefinitions = newDefs;

            //Reset lookup dictionary
            spriteNameLookup.SetValue(collection, null);  //Set dictionary to null
            collection.InitDictionary(); //InitDictionary only runs if the dictionary is null
            return newDefs.Length - 1;
        }


        public class ReplacedCharacterData
        {
            public PlayableCharacters baseCharacter;
            public int origMapIconID = -1;
            public int replaceMapIconID = -1;
            public tk2dSpriteCollectionData origPlayerCollection;
            public tk2dSpriteCollectionData replacePlayerCollection;
        }
    }
}
