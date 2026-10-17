using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZeroHour.Phase2.Editor
{
    public static class MaterialURPUpgrader
    {
        [MenuItem("ZeroHour/Fix Pink & Grey Materials (Upgrade to URP)")]
        public static void UpgradeAllMaterials()
        {
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                Debug.LogError("[ZeroHour] Universal Render Pipeline/Lit shader not found!");
                return;
            }

            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/SimplePoly City - Low Poly Assets/Materials" });
            int converted = 0;

            foreach (string guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    Texture mainTex = mat.mainTexture;
                    Color col = mat.HasProperty("_Color") ? mat.color : Color.white;

                    mat.shader = urpLitShader;
                    if (mainTex != null)
                    {
                        mat.SetTexture("_BaseMap", mainTex);
                    }
                    mat.SetColor("_BaseColor", col);
                    EditorUtility.SetDirty(mat);
                    converted++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=cyan>[ZeroHour] Başarıyla {converted} adet materyal URP Lit formatına yükseltildi! Fabrikalar ve binalar renklendi.</color>");
        }
    }
}
