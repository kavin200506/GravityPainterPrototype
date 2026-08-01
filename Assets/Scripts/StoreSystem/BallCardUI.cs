using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BallCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image cardBg;
    public Image ballImage;
    public TextMeshProUGUI ballNameText;
    public GameObject lockIcon;

    [Header("Status & Button Controls")]
    public Button actionButton;
    public Image actionButtonImage;
    public TextMeshProUGUI actionText;
    public Image coinIconImage;
    public GameObject coinIconObj;

    [Header("Sprites")]
    public Sprite pricePanelSprite;
    public Sprite ownedSprite;
    public Sprite equipSprite;
    public Sprite coinSprite;
    public Sprite defaultBallSprite;

    private BallData currentBall;
    private Action<BallData> onClickCallback;

    private void Awake()
    {
        LoadSpritesIfMissing();
    }

    private void LoadSpritesIfMissing()
    {
        if (pricePanelSprite == null)
            pricePanelSprite = Resources.Load<Sprite>("UI/Store_Page/price_panel");
        if (ownedSprite == null)
            ownedSprite = Resources.Load<Sprite>("UI/Store_Page/owned");
        if (equipSprite == null)
            equipSprite = Resources.Load<Sprite>("UI/Store_Page/equip");
        if (coinSprite == null)
            coinSprite = Resources.Load<Sprite>("UI/Store_Page/coin_icon_32");
        if (defaultBallSprite == null)
            defaultBallSprite = Resources.Load<Sprite>("UI/Store_Page/White_ball");
    }

    public void Setup(BallData data, Action<BallData> onClick)
    {
        LoadSpritesIfMissing();
        currentBall = data;
        onClickCallback = onClick;

        if (actionButton != null)
        {
            actionButton.transition = Selectable.Transition.None;

            Navigation nav = actionButton.navigation;
            nav.mode = Navigation.Mode.None;
            actionButton.navigation = nav;

            ColorBlock cb = actionButton.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.pressedColor = Color.white;
            cb.selectedColor = Color.white;
            cb.disabledColor = Color.white;
            actionButton.colors = cb;

            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() =>
            {
                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(null);

                onClickCallback?.Invoke(currentBall);
            });
        }

        EnsureRectTransformLayout();
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        EnsureRectTransformLayout();
        LoadSpritesIfMissing();
        if (currentBall == null) return;

        if (ballNameText != null)
        {
            ballNameText.text = currentBall.ballName.ToUpper();
            ballNameText.raycastTarget = false;
        }

        if (ballImage != null)
        {
            if (currentBall.icon != null)
            {
                ballImage.sprite = currentBall.icon;
            }
            else if (defaultBallSprite != null)
            {
                ballImage.sprite = defaultBallSprite;
            }

            ballImage.color = Color.white;
            ballImage.enabled = ballImage.sprite != null;
            ballImage.preserveAspect = true;
            ballImage.raycastTarget = false;
        }

        if (coinIconImage != null && coinSprite != null)
        {
            coinIconImage.sprite = coinSprite;
            coinIconImage.color = Color.white;
            coinIconImage.preserveAspect = true;
            coinIconImage.raycastTarget = false;
        }

        bool isOwned = InventoryManager.Instance != null && InventoryManager.Instance.IsOwned(currentBall.ballId, currentBall.isDefault);
        bool isEquipped = InventoryManager.Instance != null && InventoryManager.Instance.IsEquipped(currentBall.ballId);

        // Lock icon display
        if (lockIcon != null)
        {
            lockIcon.SetActive(!isOwned);
            foreach (Graphic g in lockIcon.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;
        }

        // State Pill/Button formatting using exact visual sprites
        if (isEquipped)
        {
            if (coinIconObj != null) coinIconObj.SetActive(false);
            if (actionText != null) actionText.text = "EQUIPPED";
            if (actionButtonImage != null)
            {
                if (equipSprite != null)
                {
                    actionButtonImage.sprite = equipSprite;
                    actionButtonImage.color = Color.white;
                }
                else
                {
                    actionButtonImage.color = new Color(0.15f, 0.7f, 0.25f, 1f);
                }
            }
        }
        else if (isOwned)
        {
            if (coinIconObj != null) coinIconObj.SetActive(false);
            if (actionText != null) actionText.text = "OWNED";
            if (actionButtonImage != null)
            {
                if (ownedSprite != null)
                {
                    actionButtonImage.sprite = ownedSprite;
                    actionButtonImage.color = Color.white;
                }
                else if (pricePanelSprite != null)
                {
                    actionButtonImage.sprite = pricePanelSprite;
                    actionButtonImage.color = Color.white;
                }
                else
                {
                    actionButtonImage.color = new Color(0.2f, 0.25f, 0.35f, 1f);
                }
            }
        }
        else
        {
            if (coinIconObj != null) coinIconObj.SetActive(true);
            if (actionText != null) actionText.text = currentBall.price.ToString();
            if (actionButtonImage != null)
            {
                if (pricePanelSprite != null)
                {
                    actionButtonImage.sprite = pricePanelSprite;
                    actionButtonImage.color = Color.white;
                }
                else
                {
                    actionButtonImage.color = new Color(0.15f, 0.45f, 0.75f, 1f);
                }
            }
        }

        // Center alignment of text when coin icon is hidden
        if (actionText != null)
        {
            actionText.alignment = TextAlignmentOptions.Center;
            actionText.raycastTarget = false;
            if (coinIconObj != null && coinIconObj.activeSelf)
            {
                actionText.rectTransform.offsetMin = new Vector2(40f, 0f);
                actionText.rectTransform.offsetMax = Vector2.zero;
            }
            else
            {
                actionText.rectTransform.offsetMin = Vector2.zero;
                actionText.rectTransform.offsetMax = Vector2.zero;
            }
        }
    }

    private void EnsureRectTransformLayout()
    {
        if (actionButton != null)
        {
            RectTransform btnRt = actionButton.GetComponent<RectTransform>();
            if (btnRt != null)
            {
                btnRt.anchorMin = new Vector2(0.5f, 0f);
                btnRt.anchorMax = new Vector2(0.5f, 0f);
                btnRt.pivot = new Vector2(0.5f, 0f);
                btnRt.anchoredPosition = new Vector2(0f, 2.936005f);
                btnRt.sizeDelta = new Vector2(235f, 70f);
                btnRt.localScale = new Vector3(0.8146776f, 1.4875f, 1f);
                btnRt.localRotation = Quaternion.identity;
            }
        }

        if (ballImage != null)
        {
            RectTransform bRt = ballImage.GetComponent<RectTransform>();
            if (bRt != null)
            {
                bRt.anchorMin = new Vector2(0.5f, 0.5f);
                bRt.anchorMax = new Vector2(0.5f, 0.5f);
                bRt.pivot = new Vector2(0.5f, 0.5f);
                bRt.anchoredPosition = new Vector2(0f, 15f);
                bRt.sizeDelta = new Vector2(170f, 170f);
                bRt.localScale = Vector3.one;
                bRt.localRotation = Quaternion.identity;
            }
        }
    }
}
