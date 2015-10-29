using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

public class DataManager : MonoBehaviour {

    static public DataManager instance;

    [Header("Game Parameters")]
    public float gameTime;
    public int duckPoints;
    public int canLeftPoints;
    public int canNumber;
    public string xmlFileName = "data";
    private int ducksInTheGame;
    private float score;

    private int globalMultiplier;
    public float sessionGameTime { get; private set; }
    public int sessionDucksInTheGame { get; private set; }
    private int sessionCanNumber;
    public bool lastCan { get; private set; }
    private List<GameObject> disabledDucks;
    private Dictionary<String, KeyValuePair<Vector3, Quaternion>> ducksPositions;

    [HideInInspector]
    public XmlParser parser;

    public float leftMostDuck { get; private set; }
    public float rightMostDuck { get; private set; }

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

        rightMostDuck = 0;
        leftMostDuck = Screen.width;
        
        parser = new XmlParser(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + xmlFileName + ".xml");
    }

    public void Start()
    {
        guiManager = GUIManager.instance;
        gameManager = GameManager.instance;

        score = 0;

        sessionGameTime = gameTime;
        sessionDucksInTheGame = ducksInTheGame;
        sessionCanNumber = canNumber;

        guiManager.SetInGameCanNumber(sessionCanNumber);
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
        Debug.Log(globalMultiplier);
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
        // score = 0;
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

        if (gameManager.floorType == FloorReceiver.FloorType.Normal)
        {
            parser.SaveSession(GetTimestamp(DateTime.Now)); 
        }
        else
        {
            parser.SaveAdaptiveSession(GetTimestamp(DateTime.Now));
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

    public void UseCan()
    {
        sessionCanNumber--;
        guiManager.SetInGameCanNumber(sessionCanNumber);
    }

    public int CanLeft()
    {
        return sessionCanNumber;
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

    private String GetTimestamp(this DateTime value)
    {
        return value.ToString("yyyy.MM.dd.HH.mm.ss.fff");
    }

    public void RecordDuckHit(int id)
    {
        parser.SetDuckHit(id);
    }

    public void RecordDuckHit(int id, string name)
    {
        parser.SetDuckHit(id, name);
    }

    public void DefineBoundaries(Vector3 worldPosition)
    {
        Vector3 n = new Vector3(worldPosition.x * 1.272727F, worldPosition.y, worldPosition.z);

        Vector3 v = Camera.main.WorldToScreenPoint(n);

        if(v.x > rightMostDuck)
        {
            rightMostDuck = v.x;
        }

        if(v.x < leftMostDuck)
        {
            leftMostDuck = v.x;
        }
    }
}
