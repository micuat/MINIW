using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

public class DataManager : MonoBehaviour {

    static public DataManager instance;

    [Header("Game Parameters")]
    // Time that the game will last
    public float gameTime;
    // Points received for each hit duck
    public int duckPoints;
    // Points assigned for each can not used 
    public int canLeftPoints;
    /// Object that will be thrown at ducks
    public GameObject canObject;
    // Number of can available to the player
    public int canNumber;
    // Collection storing the can
    public List<GameObject> canList;
    // Left and right borders. The user will be able to throw a can in the area conained between those two objects
    public GameObject leftBorder;
    public GameObject rightBorder;
    // UI boundaries. The force (if used) will move between those two values.
    public float leftMostUIBorder { get; private set; }
    public float rightMostUIBorder { get; private set; }
    // Duck to be killed
    private int ducksInTheGame;
    // Score
    private float score;
    // Combo multiplier
    private int globalMultiplier;

    // Those variables are used during each session
    public float sessionGameTime { get; private set; }
    public int sessionDucksInTheGame { get; private set; }
    private int sessionCanNumber;
    public bool lastCan { get; private set; }

    // The following two collections are used to restart the game
    // List of hit duck during the current sessions game
    private List<GameObject> disabledDucks;
    // Dictionary storing location and rotation data regarding all the ducks contained in the game
    private Dictionary<String, KeyValuePair<Vector3, Quaternion>> ducksPositions;

    [Header("Xml Parser")]
    // Xml file name. This is the file in which to record all game data
    public string xmlFileName = "data";
    // Xml parser
    private XmlParser parser;

    private GUIManager guiManager;
    private GameManager gameManager;

    public void Awake()
    {
        instance = this;

        ducksPositions = new Dictionary<String, KeyValuePair<Vector3, Quaternion>>();
        disabledDucks = new List<GameObject>();
        ducksInTheGame = GameObject.FindGameObjectsWithTag("Duck").Length;

        lastCan = false;
        globalMultiplier = 1;
        leftMostUIBorder = 0;
        rightMostUIBorder = 0;

        parser = new XmlParser(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + xmlFileName + ".xml");

        // Instanciate all the can
        canList = new List<GameObject>();
        for(int i = 0; i < canNumber; i++)
        {
            // Instanciate a new object
            GameObject c = Instantiate(canObject, new Vector3(0, 10, 0), Quaternion.identity) as GameObject;
            // Set canID
            c.SendMessage("SetID", i);
            // Disable can
            c.SendMessage("SetCanActive", false);
            // Add the can to the collection
            canList.Add(c);
        }
    }

    public void Start()
    {
        // Get instances
        guiManager = GUIManager.instance;
        gameManager = GameManager.instance;

        // Variable inizialization
        score = 0;
        sessionGameTime = gameTime;
        sessionDucksInTheGame = ducksInTheGame;
        sessionCanNumber = canNumber;

        // Inizialize GUI
        guiManager.SetInGameCanNumber(sessionCanNumber);

        // Define game boundaries
        DefineBoundaries();
    }

    void Update()
    {
        if(sessionDucksInTheGame == 0 && guiManager.guiState == GUIManager.GUIState.Void)
        {
            score += sessionCanNumber * canLeftPoints;
            ResetParameters();
            gameManager.EndGame(true);
        }
    }

    void FixedUpdate()
    {
        if (gameManager.limitTime)
        {
            if (sessionGameTime > 0 && guiManager.guiState == GUIManager.GUIState.Void)
            {
                sessionGameTime -= Time.deltaTime;
                if (sessionGameTime < 0)
                {
                    sessionGameTime = 0;
                }
                guiManager.SetInGameTime(sessionGameTime);
            }
            else if (guiManager.guiState == GUIManager.GUIState.Void)
            {
                ResetParameters();
                gameManager.EndGame(false);
            }
        }
        else
        {
            if (sessionCanNumber == 0 && lastCan)   
            {
                ResetParameters();
                gameManager.EndGame(false);
            }
        }
    }

    public void AddToScore(bool doubleHit = false)
    {
        if (doubleHit)
        {
            score += (duckPoints * globalMultiplier)*2;
        }
        else
        {
            score += (duckPoints * globalMultiplier);
        }

        guiManager.SetInGameScore(Mathf.RoundToInt(score));
    }

    public void ResetMultiplier()
    {
        globalMultiplier = 1;
    }

    public void IncreaseMultiplier()
    {
        globalMultiplier++;
    }

    public int GetScore()
    {
        return Mathf.RoundToInt(score);
    }

    public void DuckHit()
    {
        sessionDucksInTheGame--;
    }

    public void SetScore(float f)
    {
        score = f;
        guiManager.SetInGameScore((int)score);
    }

    public void LastCan()
    {
        lastCan = true;
    }

    private void ResetParameters()
    {
        if (gameManager.floorType == FloorReceiver.FloorType.Normal)
        {
            parser.SaveSession(UtilityClass.GetTimestamp(DateTime.Now));
        }
        else
        {
            parser.SaveAdaptiveSession(UtilityClass.GetTimestamp(DateTime.Now));
        }

        // Notify server
        GameObject.FindGameObjectWithTag("NIW").GetComponent<FloorReceiver>().SendMessage("NotifyServer", "end");

        sessionDucksInTheGame = ducksInTheGame;
        sessionGameTime = gameTime;
        sessionCanNumber = canNumber;
        guiManager.SetInGameCanNumber(sessionCanNumber);
        lastCan = false;
        globalMultiplier = 1;

        if (gameManager.floorType == FloorReceiver.FloorType.Normal)
        {
            GameObject[] ducks = GameObject.FindGameObjectsWithTag("Duck");

            for (int i = 0; i < ducks.Length; i++)
            {
                ducks[i].GetComponent<DuckMovement>().StopPath();
            }

            for (int i = 0; i < disabledDucks.Count; i++)
            {
                disabledDucks[i].GetComponent<DuckMovement>().StopPath();
            } 
        }
    }

    public void AddDuckPosition(String name, KeyValuePair<Vector3, Quaternion> t)
    {
        ducksPositions[name] = t;
    }

    public void AddDisabledDuck(GameObject g)
    {
        disabledDucks.Add(g);
    }

    public GameObject UseCan(Vector3 startPosition)
    {
        // Get can
        GameObject c = canList[GetCurrentCanID()];
        // Reset its position
        c.transform.localPosition = startPosition;
        // Reset its rotation
        c.transform.localRotation = Quaternion.identity;
        // Reset its velocity
        c.GetComponent<Rigidbody>().velocity = Vector3.zero;
        // Activate can
        c.SendMessage("SetCanActive", true);
        // Update can counter
        sessionCanNumber--;
        // Update GUI
        guiManager.SetInGameCanNumber(sessionCanNumber);
        // Return game object
        return c;
    }

    public int CanLeft()
    {
        return sessionCanNumber;
    }

    public int GetCurrentCanID()
    {
        return (canNumber - sessionCanNumber);
    }

    public void ResetDucks()
    {
        GameObject[] ducks = GameObject.FindGameObjectsWithTag("Duck");

        for (int i = 0; i < ducks.Length; i++)
        {
           
            ducks[i].transform.localPosition = ducksPositions[ducks[i].name].Key;
            ducks[i].transform.localRotation = ducksPositions[ducks[i].name].Value;

            if (gameManager.floorType == FloorReceiver.FloorType.Normal)
            {
                ducks[i].GetComponent<DuckMovement>().DefinePath(); 
            }

            ducks[i].SetActive(true);
        }

        for (int i = 0; i < disabledDucks.Count; i++)
        {
            disabledDucks[i].transform.localPosition = ducksPositions[disabledDucks[i].name].Key;
            disabledDucks[i].transform.localRotation = ducksPositions[disabledDucks[i].name].Value;

            if (gameManager.floorType == FloorReceiver.FloorType.Normal)
            {
                disabledDucks[i].GetComponent<DuckMovement>().StopPath();
                disabledDucks[i].GetComponent<DuckMovement>().DefinePath(); 
            }

            disabledDucks[i].SetActive(true);
        }

        disabledDucks.Clear();
    }

    public bool isLastCan()
    {
        return sessionCanNumber == 0 ? true : false;
    }

    public void AddStep(int canID, string start_time)
    {
        parser.AddStep(canID, start_time);
    }

    public void AddStep(float x_value, float y_value, int tot, float force)
    {
        parser.AddStep(x_value, y_value, tot, force);
    }

    public void SaveStep(int canID, float x_value, float y_value, float force, string end_time)
    {
        parser.SaveStep(canID, x_value, y_value, force, end_time);
    }

    public void AddSession(string start_time, string mode)
    {
        parser.AddSession(start_time, mode);
    }

    public void RecordDuckHit(int id)
    {
        parser.SetDuckHit(id);
    }

    public void RecordDuckHit(int id, string name)
    {
        parser.SetDuckHit(id, name);
    }

    public void DefineBoundaries()
    {
        leftMostUIBorder = Camera.main.WorldToScreenPoint(leftBorder.transform.position).x;
        rightMostUIBorder = Camera.main.WorldToScreenPoint(rightBorder.transform.position).x;
    }

    public float GetLeftLimitXPoint()
    {
        return leftBorder.transform.localPosition.x;
    }

    public float GetRightLimitXPoint()
    {
        return rightBorder.transform.localPosition.x;
    }
}
