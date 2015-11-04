using UnityEngine;
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

    [Header("Force Bar")]
    public Image ForceBar;
    public Sprite adaptiveForceBarImage;
    public Sprite staticForceBarImage;

    [Header("Various")]
    public Text hitPointsText;
    private bool fadePoints;

    [HideInInspector]
    public GUIState guiState;
    private string originalBottomText;

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
        originalBottomText = bottomText.text;
        bottomText.text = originalBottomText + "\nStandard Mode";
        SetHitPointsActive(false);

        UpdateForceBarSprite();
    }

    private void Update()
    {
        if(guiState == GUIState.MainMenu && Input.GetKeyDown(KeyCode.Alpha1))
        {
            bottomText.text = originalBottomText + "\nStandard Mode";
            gameManager.PlayStandardMode(true);
        }
        else if (guiState == GUIState.MainMenu && Input.GetKeyDown(KeyCode.Alpha2))
        {
            bottomText.text = originalBottomText + "\nExtreme Mode";
            gameManager.PlayStandardMode(false);
        }
    }

    private void FixedUpdate()
    {
        if (fadePoints)
        {
            UpdateAlfa();
            if (hitPointsText.color.a <= 0)
            {
                SetHitPointsActive(false);
                UpdateAlfa(true);
            }
        }
    }

    private void UpdateAlfa(bool reset = false)
    {
        Color color = hitPointsText.color;
        if (reset)
        {
            color.a = 1;
        }

        else
        {
            color.a -= 0.0125f;
        }
        hitPointsText.color = color;
    }

    public void SetHitPointsActive(bool value)
    {
        hitPointsText.enabled = value;
        fadePoints = value;
    }

    public void UpdateForceBarSprite()
    {
        // Change sprite
        ForceBar.GetComponent<Image>().sprite = gameManager.updateForceBar ? adaptiveForceBarImage : staticForceBarImage;
    }

    public void ShowGUI(GUIState state, bool HasWon = false)
    {
        switch (state)
        {
            case GUIState.MainMenu:
                inGameTopPanel.SetActive(false);
                mainMenu.SetActive(true);
                topText.enabled = true;
                centerText.enabled = false;
                bottomText.enabled = false;
                ForceBar.enabled = false;
                break;
            case GUIState.EndGame:
                inGameTopPanel.SetActive(false);
                // Show final score
                mainMenu.SetActive(true);
                topText.enabled = false;
                centerText.text = !HasWon ? "Game Over!\nFinal score: " + dataManager.GetScore() : "You Win!\nFinal score: " + dataManager.GetScore();
                centerText.enabled = true;
                bottomText.enabled = false;
                break;
            case GUIState.Void:
                inGameTimeText.enabled = gameManager.limitTime ? true : false;
                inGameCanLeft.enabled = gameManager.limitTime ? false : true;
                inGameTopPanel.SetActive(true);
                mainMenu.SetActive(false);
                dataManager.SetScore(0);
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

    public void ShowPointsOnDuck(float points, GameObject hitDuck)
    {
        Vector3 v = Camera.main.WorldToScreenPoint(hitDuck.transform.position);
        v.y = v.y * 1.2F;
        hitPointsText.rectTransform.position = v;
        hitPointsText.text = "+" + points.ToString();
        UpdateAlfa(true);
        SetHitPointsActive(true);
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
        canPosition.y = ForceBar.rectTransform.position.y;
        canPosition.z = ForceBar.rectTransform.position.z;
        ForceBar.rectTransform.position = canPosition;
        
        ForceBar.GetComponent<Image>().fillAmount = force;
    }

    public void EnableForceBar(bool value, float startPosition = 0)
    {
        // Show/Hide force bar
        ForceBar.enabled = value;
        // Reset its value
        ForceBar.GetComponent<Image>().fillAmount = 0;

        // If the bar has been enabling ...
        if (value)
        {
            // ... Inizialize its position
            Vector3 v = new Vector3();
            v.x = startPosition;
            v.y = ForceBar.rectTransform.position.y;
            v.z = ForceBar.rectTransform.position.z;
            ForceBar.rectTransform.position = v;
        }
    }

    public void UpdateForceBar(float xPosition, float length)
    {
        ForceBar.GetComponent<Image>().fillAmount = gameManager.updateForceBar ? length : 1;

        if (Mathf.Abs(ForceBar.rectTransform.position.x - xPosition) > 5)
        {
            Vector3 v = new Vector3();
            v.x = xPosition;
            v.y = ForceBar.rectTransform.position.y;
            v.z = ForceBar.rectTransform.position.z;
            ForceBar.rectTransform.position = v;
        }
    }
}
