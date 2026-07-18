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
        if (storePanel != null)
            storePanel.SetActive(false);

        // Foolproof way to find and activate the MainMenu GameObject (even if inactive)
        MainMenu mainMenu = Object.FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
        if (mainMenu != null)
        {
            mainMenu.gameObject.SetActive(true);
        }
        else
        {
            // Fallback to searching under Canvas/SafeAreaPanel if script not found
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Transform menuTrans = canvas.transform.Find("MainMenu");
                if (menuTrans == null)
                {
                    menuTrans = canvas.transform.Find("SafeAreaPanel/MainMenu");
                }
                if (menuTrans != null)
                {
                    menuTrans.gameObject.SetActive(true);
                }
            }
        }
    }
}
