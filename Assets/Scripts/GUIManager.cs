﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class GUIManager : MonoBehaviour
{
    static public GUIManager instance;

    [HideInInspector]
    public enum GUIState { MainMenu, EndGame, Void}

    [Header("Ingame Menu")]
    public GameObject inGameTopPanel;
    public Text inGameCanLeft;
    public Text inGameTimeText;
    public Text inGameScoreText;

    [Header("Main Menu")]
    public GameObject mainMenu;
    public Text topText;
    public Text centerText;
    public Text bottomText;

    public Image ForceBar;

    [HideInInspector]
    public GUIState guiState;

    private DataManager dataManager;
    private GameManager gameManager;

    public void Awake()
    {
        instance = this; 
    }

    // Use this for initialization
    void Start()
    {
        dataManager = DataManager.instance;
        gameManager = GameManager.instance;

        ShowGUI(GUIState.MainMenu);
    }

    public void ShowGUI(GUIState state, bool HasWon = false)
    {
        switch (state)
        {
            case GUIState.MainMenu:
                // Time.timeScale = 0; // Stop the game
                inGameTopPanel.SetActive(false);
                mainMenu.SetActive(true);
                topText.enabled = true;
                centerText.enabled = false;
                bottomText.enabled = false;
                break;
            case GUIState.EndGame:
                // Time.timeScale = 0;
                inGameTopPanel.SetActive(false);
                // Show final score
                mainMenu.SetActive(true);
                topText.enabled = false;
                centerText.text = !HasWon ? "Game Over!\nFinal score: " + dataManager.GetScore() : "You Won!\nFinal score: " + dataManager.GetScore();
                centerText.enabled = true;
                bottomText.enabled = false;
                break;
            case GUIState.Void:
                inGameTimeText.enabled = gameManager.limitTime ? true : false;
                inGameCanLeft.enabled = gameManager.limitTime ? false : true;
                inGameTopPanel.SetActive(true);
                mainMenu.SetActive(false);
                Time.timeScale = 1;
                break;
        }

        guiState = state;
    }

    public void SetInGameTime(float time)
    {
        inGameTimeText.text = time.ToString("0.00");
    }

    public void SetInGameScore(int score)
    {
        inGameScoreText.text = score.ToString();
    }

    public void SetInGameCanNumber(int s)
    {
        inGameCanLeft.text = "Can left: " + s;
    }

    public void ShowBottomLine()
    {
        bottomText.enabled = true;
    }

    public void ShowForceBar(Vector3 canPosition, float force)
    {
        Debug.Log(canPosition);
        canPosition.y = ForceBar.rectTransform.position.y;
        canPosition.z = ForceBar.rectTransform.position.z;
        ForceBar.rectTransform.position = canPosition;
        
        ForceBar.GetComponent<Image>().fillAmount = force;
    }

    
}
