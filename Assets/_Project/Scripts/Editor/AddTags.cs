using UnityEditor;
using UnityEngine;

public class AddTags
{
    public static void Execute()
    {
        // Add "Obstacle" tag
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        string[] tagsToAdd = { "Obstacle", "Coin", "TrackSegment" };

        foreach (string tag in tagsToAdd)
        {
            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                Debug.Log($"[MundialRunner] Tag '{tag}' añadido.");
            }
        }

        tagManager.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log("[MundialRunner] Tags configurados.");
    }
}
