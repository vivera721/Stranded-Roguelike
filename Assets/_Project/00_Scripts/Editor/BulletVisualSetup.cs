#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace StrandedRoguelike.Editor
{
    public static class BulletVisualSetup
    {
        private const string Root = "Assets/_Project";
        private const string ShaderGraphPath = Root + "/_ShaderGraph/Sojourn_Bullet.shadergraph";
        private const string MaterialFolder = Root + "/06_Materials/Bullets";
        private const string TextureFolder = Root + "/_Arts/Generated/Bullets";
        private const string PrefabFolder = Root + "/05_Prefabs/Projectiles";

        [InitializeOnLoadMethod]
        private static void CreateOnFirstImport()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                if (AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Sojourn_Bullet_Yellow.prefab") == null)
                {
                    CreateBulletVisuals();
                }
            };
        }

        [MenuItem("Tools/Stranded Roguelike/Create Sojourn Bullet Visuals")]
        public static void CreateBulletVisuals()
        {
            EnsureFolder(Root, "06_Materials");
            EnsureFolder(Root + "/06_Materials", "Bullets");
            EnsureFolder(Root, "_Arts");
            EnsureFolder(Root + "/_Arts", "Generated");
            EnsureFolder(Root + "/_Arts/Generated", "Bullets");
            EnsureFolder(Root, "05_Prefabs");
            EnsureFolder(Root + "/05_Prefabs", "Projectiles");

            Texture2D bulletTexture = CreateCircleTexture($"{TextureFolder}/Sojourn_Bullet_Core.png", 64, 0.95f, 0.18f, false);
            Texture2D shadowTexture = CreateCircleTexture($"{TextureFolder}/Sojourn_Bullet_Shadow.png", 64, 0.9f, 0.55f, true);

            Sprite bulletSprite = CreateSprite(bulletTexture, "Sojourn_Bullet_Core_Sprite");
            Sprite shadowSprite = CreateSprite(shadowTexture, "Sojourn_Bullet_Shadow_Sprite");

            Material yellow = CreateBulletMaterial(
                "Sojourn_Bullet_Yellow",
                new Color(1f, 0.48f, 0.05f, 1f),
                new Color(1f, 0.96f, 0.72f, 1f),
                2.8f);
            Material purple = CreateBulletMaterial(
                "Sojourn_Bullet_Purple",
                new Color(0.95f, 0.18f, 1f, 1f),
                new Color(1f, 0.82f, 1f, 1f),
                3.4f);
            Material shadow = CreateShadowMaterial();

            CreateBulletPrefab("Sojourn_Bullet_Yellow", bulletSprite, shadowSprite, yellow, shadow);
            CreateBulletPrefab("Sojourn_Bullet_Purple", bulletSprite, shadowSprite, purple, shadow);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Sojourn style bullet visuals created.");
        }

        private static Material CreateBulletMaterial(string name, Color rimColor, Color coreColor, float emission)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderGraphPath);
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            }

            Material material = new Material(shader)
            {
                name = name,
                enableInstancing = true
            };

            SetColorIfExists(material, "_Color", rimColor);
            SetColorIfExists(material, "_CoreColor", coreColor);
            SetFloatIfExists(material, "_Radius", 0.28f);
            SetFloatIfExists(material, "_Softness", 0.45f);
            SetFloatIfExists(material, "_Emission", emission);

            string path = $"{MaterialFolder}/{name}.mat";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material CreateShadowMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(shader)
            {
                name = "Sojourn_Bullet_Shadow"
            };

            string path = $"{MaterialFolder}/Sojourn_Bullet_Shadow.mat";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void CreateBulletPrefab(string name, Sprite bulletSprite, Sprite shadowSprite, Material bulletMaterial, Material shadowMaterial)
        {
            GameObject bullet = new GameObject(name);

            SpriteRenderer bulletRenderer = bullet.AddComponent<SpriteRenderer>();
            bulletRenderer.sprite = bulletSprite;
            bulletRenderer.sharedMaterial = bulletMaterial;
            bulletRenderer.sortingOrder = 30;

            CircleCollider2D collider = bullet.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.18f;

            bullet.AddComponent<EnemyProjectile>();

            GameObject shadow = new GameObject("Shadow");
            shadow.transform.SetParent(bullet.transform, false);
            shadow.transform.localPosition = new Vector3(0.12f, -0.18f, 0f);
            shadow.transform.localScale = new Vector3(1.25f, 0.45f, 1f);

            SpriteRenderer shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = shadowSprite;
            shadowRenderer.sharedMaterial = shadowMaterial;
            shadowRenderer.color = new Color(0f, 0f, 0f, 0.38f);
            shadowRenderer.sortingOrder = 20;

            string path = $"{PrefabFolder}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(bullet, path);
            Object.DestroyImmediate(bullet);
        }

        private static Texture2D CreateCircleTexture(string path, int size, float radius, float softness, bool shadow)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2(size - 1, size - 1) * 0.5f;
            float maxRadius = size * 0.5f * radius;
            float softRange = Mathf.Max(1f, size * 0.5f * softness);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - Mathf.InverseLerp(maxRadius - softRange, maxRadius, distance));
                    Color color = shadow ? new Color(0f, 0f, 0f, alpha) : new Color(1f, 1f, 1f, alpha);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();

            byte[] png = texture.EncodeToPNG();
            File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Sprite CreateSprite(Texture2D texture, string name)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            sprite.name = name;
            EditorUtility.SetDirty(sprite);
            return sprite;
        }

        private static void SetColorIfExists(Material material, string propertyName, Color value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
        }

        private static void SetFloatIfExists(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
#endif
