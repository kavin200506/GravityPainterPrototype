using System;
using UnityEngine;

[Serializable]
public class MainMenuButtonLayout
{
    [Tooltip("Child name under Canvas > MainMenu.")]
    public string buttonName = "Play";

    public Vector2 anchoredPosition = new Vector2(0f, 0f);
    public Vector2 sizeDelta = new Vector2(520f, 120f);
    public Vector3 localScale = Vector3.one;

    public MainMenuButtonLayout()
    {
    }

    public MainMenuButtonLayout(string name, Vector2 position, Vector2 size)
    {
        buttonName = name;
        anchoredPosition = position;
        sizeDelta = size;
    }

    public MainMenuButtonLayout(string name, Vector2 position, Vector2 size, Vector3 scale)
    {
        buttonName = name;
        anchoredPosition = position;
        sizeDelta = size;
        localScale = scale;
    }
}
