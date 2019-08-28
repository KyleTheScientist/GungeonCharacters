using UnityEngine;
using System.Collections;
using System;
namespace CustomCharacters
{
    public static class CollectionDumper
    {
        //For debugging
        public static void DumpCollection(tk2dSpriteCollectionData collection)
        {
            string collectionName = string.IsNullOrEmpty(collection.name) ? collection.gameObject.name + "_Collection" : collection.name;

            tk2dSpriteDefinition def;
            string defName;
            Material material;
            Texture2D texture, output;
            int width, height, minX, minY, maxX, maxY, w, h;
            Vector2[] uvs;
            Color[] pixels;
            for (int i = 0; i < collection.spriteDefinitions.Length; i++)
            {
                def = collection.spriteDefinitions[i];
                if (def == null) continue;


                defName = string.IsNullOrEmpty(def.name) ? collectionName + "_" + i : def.name;
                material = def.material == null ? def.materialInst : def.material;
                if (material == null || material.mainTexture == null)
                {
                    Tools.PrintError($"Failed to dump {defName} in {collectionName}: No valid material");
                    continue;
                }

                texture = ((Texture2D)material.mainTexture).GetReadable();
                width = texture.width;
                height = texture.height;

                uvs = def.uvs;
                if (def.uvs == null || def.uvs.Length < 4)
                {
                    Tools.PrintError($"Failed to dump {defName} in {collectionName}: Invalid UV's");
                    continue;
                }

                minX = Mathf.RoundToInt(uvs[0].x * width);
                minY = Mathf.RoundToInt(uvs[0].y * height);
                maxX = Mathf.RoundToInt(uvs[3].x * width);
                maxY = Mathf.RoundToInt(uvs[3].y * height);

                w = maxX - minX;
                h = maxY - minY;
                if (w <= 0 || h <= 0)
                {
                    Tools.ExportTexture(new Texture2D(1, 1) { name = defName });
                    continue;
                };

                pixels = texture.GetPixels(minX, minY, w, h);

                output = new Texture2D(w, h);
                output.SetPixels(pixels);
                output.Apply();
                if (def.flipped == tk2dSpriteDefinition.FlipMode.Tk2d)
                {
                    output = output.Rotated().Flipped();
                }
                output.name = def.name;

                Tools.ExportTexture(output, "SpriteDump/" + collectionName);
            }
        }
    }

}
