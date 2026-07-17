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

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Transform mainMenu = canvas.transform.Find("MainMenu");
            if (mainMenu != null)
                mainMenu.gameObject.SetActive(true);
        }
    }
}
