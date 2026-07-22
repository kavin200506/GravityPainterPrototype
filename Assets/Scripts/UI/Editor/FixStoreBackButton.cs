using UnityEngine;
using UnityEditor;

public class FixStoreBackButton
{
    [MenuItem("Gravity Painter/Fix Store Back Button")]
    [InitializeOnLoadMethod]
    public static void FixButton()
    {
        // Find all GameObjects named "BackButton"
        GameObject[] backButtons = Resources.FindObjectsOfTypeAll<GameObject>();
        bool changed = false;

        foreach (GameObject go in backButtons)
        {
            if (go.name == "BackButton" && go.transform.parent != null && go.transform.parent.name == "StorePanel")
            {
                RectTransform rect = go.GetComponent<RectTransform>();
                if (rect != null)
                {
                    Undo.RecordObject(rect, "Fix Store Back Button Position");
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition3D = new Vector3(-539.4f, 989.4f, 639f);
                    rect.sizeDelta = new Vector2(540f, 275f);
                    rect.localRotation = Quaternion.identity;
                    rect.localScale = Vector3.one;
                    
                    EditorUtility.SetDirty(rect);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            Debug.Log("Successfully fixed Store Back Button positions!");
        }
    }
}
