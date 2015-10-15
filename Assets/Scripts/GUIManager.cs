using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GUIManager : MonoBehaviour
{
    static public GUIManager instance;

    [Header("Ingame Menu")]
    public GameObject inGameTopPanel;
    public Text inGameTimeText;

    float time = 30;

    public void Awake()
    {
        instance = this;
    }

    // Use this for initialization
    void Start()
    {
        // Hide everything except the main menu
        //inGameTopPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (time > 0)
        {
            time -= Time.deltaTime;
            SetInGameTime(time);
        }
    }

    public void SetInGameTime(float score)
    {
        inGameTimeText.text = score.ToString("0.00");
    }
}
