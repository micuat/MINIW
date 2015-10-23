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
    public int canNumber;
    public string xmlFileName = "data";
    private int ducksInTheGame;
    private float score;

    public float sessionGameTime { get; private set; }
    public int sessionDucksInTheGame { get; private set; }
    private int sessionCanNumber;
    public bool lastCan { get; private set; }
    private List<GameObject> disabledDucks;
    private Dictionary<String, KeyValuePair<Vector3, Quaternion>> ducksPositions;

    [HideInInspector]
    public XmlParser parser;

    private GUIManager guiManager;
    private GameManager gameManager;

    public void Awake()
    {
        instance = this;

        ducksPositions = new Dictionary<String, KeyValuePair<Vector3, Quaternion>>();
        disabledDucks = new List<GameObject>();
        ducksInTheGame = GameObject.FindGameObjectsWithTag("Duck").Length;

        lastCan = false;
        
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
    }

    void Update()
    {
        if(ducksInTheGame == 0 && guiManager.guiState == GUIManager.GUIState.Void)
        {
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

    public void AddToScore()
    {
        score += (duckPoints * sessionGameTime);
        guiManager.SetInGameScore(Mathf.RoundToInt(score));
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

        GameObject[] ducks = GameObject.FindGameObjectsWithTag("Duck");
        
        for(int i = 0; i < ducks.Length; i++)
        {
            ducks[i].GetComponent<DuckMovement>().StopPath();
        }

        for (int i = 0; i < disabledDucks.Count; i++)
        {
            disabledDucks[i].GetComponent<DuckMovement>().StopPath();
        }

        parser.SaveSession(GetTimestamp(DateTime.Now));
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

    public void ResetDucks()
    {
        GameObject[] ducks = GameObject.FindGameObjectsWithTag("Duck");

        for (int i = 0; i < ducks.Length; i++)
        { 
            ducks[i].transform.localPosition = ducksPositions[ducks[i].name].Key;
            ducks[i].transform.localRotation = ducksPositions[ducks[i].name].Value;
            ducks[i].GetComponent<DuckMovement>().DefinePath();
            ducks[i].SetActive(true);
        }

        for (int i = 0; i < disabledDucks.Count; i++)
        {
            disabledDucks[i].GetComponent<DuckMovement>().StopPath();
            disabledDucks[i].transform.localPosition = ducksPositions[disabledDucks[i].name].Key;
            disabledDucks[i].transform.localRotation = ducksPositions[disabledDucks[i].name].Value;
            disabledDucks[i].GetComponent<DuckMovement>().DefinePath();
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
}