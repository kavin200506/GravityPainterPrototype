using UnityEngine;
using UnityEngine.UI;

public class StoreSceneLoader : MonoBehaviour
{
    public Button backButton;
    public GameObject storePanel;

    private void Awake()
    {
        if (backButton == null)
            backButton = GetComponentInChildren<Button>(true);

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(CloseStore);

            Image img = backButton.GetComponent<Image>();
            if (img != null)
            {
                if (img.sprite == null)
                    img.sprite = Resources.Load<Sprite>("UI/Store_Page/LevelsBackButton");
                img.preserveAspect = true;
            }
        }
    }

    public void CloseStore()
    {
        Debug.Log("[StoreSceneLoader] CloseStore called!");

        // Find the MainMenu script component in the scene (even if inactive)
        MainMenu mainMenu = Object.FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
        if (mainMenu != null)
        {
            Debug.Log("[StoreSceneLoader] Found MainMenu script component. Redirecting to mainMenu.CloseStore()");
            mainMenu.CloseStore();
        }
        else
        {
            Debug.LogWarning("[StoreSceneLoader] MainMenu script NOT found in scene! Falling back to local close.");
            if (storePanel != null)
            {
                storePanel.SetActive(false);
            }
        }
    }
}
