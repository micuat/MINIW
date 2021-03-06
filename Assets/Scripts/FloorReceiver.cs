﻿using UnityEngine;
using Rug.Osc;
using System.Collections.Generic;
using System;

public class FloorReceiver : MonoBehaviour
{
    public enum FloorType { Normal, BarIncreasing, Adaptive};

    #region define Osc parameters

    [Header("Osc Parameters")]
    public float lowestXValueNIW;
    public float highestXValueNIW;
    public float lowestForceValueNIW;
    public float highestForceValueNIW;
    public GameObject receiver;
    private OscReceiveController floorReceiverController;
    public GameObject sender;
    private OscSendController floorSenderController;
    private string serverAddress;
    // Dictionary storing all the callbacks used with the Vicon and Floor OscReceivers
    private Dictionary<KeyValuePair<OscReceiveController, string>, OscMessageEvent> callbacks;

    #endregion

    [Header("Haptic Parameters")]
    public int closerThreshold = 20000;
    public int fartherThreshold = 30000;
    public int spawnThreshold = 15000;
    public bool useInterpolation = false;
    public FloorType type;
    public static FloorType floorType;

    [Header("Game Parameters")]
    private float xForce = 0;
    private float yForce = 0;
    private float zForce = 0;
    private float torqueForce = 10;
    private bool spawned = false;
    private float lowForceValue = 2;
    private float highForceValue = 7;
    private Vector2 oldPosition;
    private float oldForce;
    private double movement_accumulator;
    private float force_accumulator;

    private GameManager gameManager;
    private DataManager dataManager;
    private GUIManager guiManager;

    // Struct storing all the data computed by the CenterOfPressure function
    public struct FloorData
    {
        public float x_value;
        public float y_value;
        public int tot;
    }

    void Awake()
    {
        if (receiver == null)
        {
            Debug.LogError("You must supply a ReceiveController");
            return;
        }

        if (!GetReceiveController(receiver, out floorReceiverController))
        {
            Debug.LogError(string.Format("The GameObject with the name '{0}' does not contain a OscReceiveController component", receiver.name));
            return;
        }
        else
        {
            callbacks = new Dictionary<KeyValuePair<OscReceiveController, string>, OscMessageEvent>();
            callbacks[new KeyValuePair<OscReceiveController, string>(floorReceiverController, "/niw/client/raw")] = ReceiveMessage;
            callbacks[new KeyValuePair<OscReceiveController, string>(floorReceiverController, "/niw/client/aggregator/floorcontact")] = FloorContact;
        }

        floorSenderController = sender.GetComponent<OscSendController>();
        if(floorSenderController == null)
        {
            Debug.LogError(string.Format("The GameObject with the name '{0}' does not contain a OscSendController component", sender.name));
            return;
        }

        floorType = type;
        serverAddress = "/niw/game/status";
    }

    private bool GetReceiveController(GameObject g, out OscReceiveController r)
    {
        r = g.GetComponent<OscReceiveController>();
        return r == null ? false : true;
    }

    public void Start()
    {
        #region init receiver

        // Attach from the OscAddressManager
        foreach (KeyValuePair<KeyValuePair<OscReceiveController, string>, OscMessageEvent> e in callbacks)
        {
            e.Key.Key.Manager.Attach(e.Key.Value, e.Value);
        }

        #endregion

        gameManager = GameManager.instance;
        guiManager = GUIManager.instance;
        dataManager = DataManager.instance;

        gameManager.SetFloorType(floorType);
    }

    void Update()
    {
        if(gameManager.isPlaying && Input.GetKeyDown(KeyCode.Alpha7))
        {
            var c = dataManager.UseCan(Camera.main.transform.localPosition);

            c.GetComponent<Rigidbody>().AddForce(new Vector3(0, 0.5f, 4f));
            c.GetComponent<Rigidbody>().AddTorque(new Vector3(10, 0, 0));
        }
    }

    private void FloorContact(OscMessage message)
    {
        // Check whether we can receive and compute the message
        if (gameManager.canReceive && dataManager.sessionGameTime > 0 && dataManager.sessionDucksInTheGame > 0)
        {
            // Check the type of the message received
            switch (message[0] as string)
            {
                // Add message: the user has started to push the tile
                case "add":
                    // If the player hasn't started the game yet ...
                    if (!gameManager.isPlaying)
                    {
                        /// ... Show the Void GUIState (that is, make the Main Menu disappear)
                        guiManager.ShowGUI(GUIManager.GUIState.Void);
                    }
                    // If the game is in Void mode and the player is playing ...
                    else if (guiManager.guiState == GUIManager.GUIState.Void && gameManager.isPlaying)
                    {
                        // ... Show force bar.
                        // It is essential to remap the x-axes value received since we want it to be in screen coordinate
                        guiManager.EnableForceBar(true, Utility.UtilityClass.Remap((float)message[2], lowestXValueNIW, highForceValue, dataManager.leftMostUIBorder, dataManager.rightMostUIBorder));
                        // Record detected step in the xml file
                        dataManager.AddStep(dataManager.GetCurrentCanID(), Utility.UtilityClass.GetTimestamp(DateTime.Now), (float)message[2], (float)message[3], (string)message[5]);
                        // Save position
                        oldPosition = new Vector2((float)message[2], (float)message[3]);
                        // Reset data
                        movement_accumulator = 0;
                        force_accumulator = 0;
                        oldForce = 0;

                        spawned = false;
                    }
                    break;
                case "update":
                    // If the game is in Void mode and the player is playing ...
                    if (guiManager.guiState == GUIManager.GUIState.Void && gameManager.isPlaying && !spawned)
                    {
                        // Update force bar
                        guiManager.UpdateForceBar(Utility.UtilityClass.Remap((float)message[2], lowestXValueNIW, highestXValueNIW, dataManager.leftMostUIBorder, dataManager.rightMostUIBorder), (float)message[4]);
                        // Update data
                        UpdateMovementAndForce((float)message[2], (float)message[3], (float)message[4]);
                    }
                    break;
                case "remove":
                    // The game will start only after the first remove message is received
                    if (guiManager.guiState == GUIManager.GUIState.Void && !gameManager.isPlaying)
                    {
                        // Start playing
                        gameManager.SetPlayingStatus(true);
                        // Add a new session in the xml file
                        dataManager.AddSession(Utility.UtilityClass.GetTimestamp(DateTime.Now), gameManager.GetModeGame());
                        // Notify server
                        Send(new OscMessage(serverAddress, "start"));
                    }
                    // If the game is in Void mode and the player is playing ...
                    else if (guiManager.guiState == GUIManager.GUIState.Void && gameManager.isPlaying)
                    {
                        // Update force bar
                        guiManager.UpdateForceBar(Utility.UtilityClass.Remap((float)message[2], lowestXValueNIW, highestXValueNIW, dataManager.leftMostUIBorder, dataManager.rightMostUIBorder), (float)message[4]);

                        // Define parameters to be used to throw the can
                        DefineCanParameters(0, 0.5f, (float)message[4], lowestForceValueNIW, highestForceValueNIW, lowForceValue, highForceValue);

                        // Instanciate and throw the can using the defined parameters
                        if (ThrowCan((float)message[2]))
                        {
                            // Update data before saving it
                            UpdateMovementAndForce((float)message[2], (float)message[3], (float)message[4]);

                            // Record detected step in the xml file
                            dataManager.SaveStep(dataManager.GetCurrentCanID() - 1, (float)message[2], (float)message[3], (float)message[4], movement_accumulator, force_accumulator, Utility.UtilityClass.GetTimestamp(DateTime.Now));

                            // Fade force bar
                            guiManager.FadeForceBar(true);

                            // Can has be spawned. The player has to lift his/her foot in order to be able to throw
                            // another can
                            spawned = true; 
                        }
                    }
                    break;
            }
        }
    }

    private void UpdateMovementAndForce(float x, float y, float force)
    {
        movement_accumulator += Vector2.Distance(new Vector2(x, y), oldPosition);
        force_accumulator += Mathf.Abs(force - oldForce);

        oldForce = force;
        oldPosition = new Vector2(x, y);
    }

    private void DefineCanParameters(float xforce, float yforce, float zforce)
    {
        xForce = xforce;
        yForce = yforce;
        zForce = zforce;
    }

    private void DefineCanParameters(float xforce, float yforce, float zforce, float from_old, float to_old, float from_new, float to_new)
    {
        xForce = xforce;
        yForce = yforce;
        zForce = Utility.UtilityClass.Remap(zforce, from_old, to_old, from_new, to_new);

        // zForce can be greater than highForceValue if we get an fsr value higher than furtherThreshold
        if (zForce > to_new)
        {
            zForce = to_new;
        }
    }

    private void ReceiveMessage (OscMessage message)
    {
        // Check whether we can receive and compute the message
        if (gameManager.canReceive && dataManager.sessionGameTime > 0 && dataManager.sessionDucksInTheGame > 0)
        {
            // If we don't receive all the parameters...
            if (message.Count != 16)
            {
                // ... Show an error and don't do anything else
                Debug.LogError(string.Format("Unexpected argument count {0}", message.Count));

                return;
            }

            // Compute center of pressure
            FloorData data = new FloorData();
            CenterOfPressure(message, out data);
            
            // If the player hasn't started the game yet ...
            if (!gameManager.isPlaying)
            {
                // ... and the total value of fsr sensors is above the defined threshold ...
                if (data.tot > closerThreshold)
                {
                    /// ... Zero the score, and show the Void GUIState (that is, make the Main Menu disappear)
                    dataManager.SetScore(0);
                    guiManager.ShowGUI(GUIManager.GUIState.Void);
                }
            }

            // If the game is in Void mode and the player is playing ...
            if (guiManager.guiState == GUIManager.GUIState.Void && gameManager.isPlaying)
            {
                // ... Check whether the current fsr sensors tot value is above a certain threshold
                // The spawed flag is necessary since fsr values increase/decrease as a ramp function, and as consequence
                // without this flag the player may continue to throw cans simply by continuing to press the tile, without 
                // lifting his/her foot. Using this flag, however, the player can throw only one can at a time. In order to
                // throw an other can, first he/she has to lift his/her foot from the tile, and then press on it again
                if (data.tot > closerThreshold && spawned == false)
                { 
                    // Do we want to use interpolation to compute can force?
                    if(useInterpolation )
                    {
                        // Force is comuted using a window. Look at DetectLane function for further details
                        // It is important to check whether we are throwing last can. Without this control,
                        // it would be possible to keep throwing cans if the user kept pressing on the tile
                        FloorData compute = new FloorData();
                        if (DetectLane(message, out compute) && !dataManager.isLastCan())
                        {
                            // Define parameters to be used to throw the can
                            DefineCanParameters(0, 0.5f, compute.tot, closerThreshold, fartherThreshold, lowForceValue, highForceValue);
                            
                            // Instanciate and throw the can using the defined parameters
                            if (ThrowCan(compute.x_value))
                            {
                                // Can has be spawned. The player has to lift his/her foot in order to be able to throw
                                // another can
                                spawned = true;

                                // Record detected step in the xml file
                                dataManager.AddStep(compute.x_value, compute.y_value, compute.tot, zForce); 
                            }
                        }
                    }
                    else // Use fixed parameters to throw can
                    {
                        FloorData compute = new FloorData();
                        if (DetectLane(message, out compute))
                        {
                            if (compute.tot > closerThreshold && compute.tot < fartherThreshold)
                            {
                                DefineCanParameters(0, 0.5f, 4.0f);

                                if (ThrowCan(compute.x_value))
                                {
                                    spawned = true;

                                    dataManager.AddStep(compute.x_value, compute.y_value, compute.tot, zForce); 
                                }
                            }
                            else if (compute.tot >= fartherThreshold)
                            {
                                DefineCanParameters(0, 0.5f, 5.5f);

                                if (ThrowCan(compute.x_value))
                                {
                                    spawned = true;

                                    dataManager.AddStep(compute.x_value, compute.y_value, compute.tot, zForce); 
                                }
                            }
                        } 
                    }
                }
                else if (data.tot < spawnThreshold)
                {
                    // Player has lifted his foot from the tile. He can now be able to throw another can
                    spawned = false;
                }
            }
            else if (guiManager.guiState == GUIManager.GUIState.Void && !gameManager.isPlaying)
            {
                // Start playing
                gameManager.SetPlayingStatus(true);
                // Add a new session in the xml file
                dataManager.AddSession(Utility.UtilityClass.GetTimestamp(DateTime.Now), gameManager.GetModeGame());
                // Notify server
                Send(new OscMessage(serverAddress, "start"));
            } 
        }
    }

    private bool ThrowCan(float x_value)
    {
        if (dataManager.CanLeft() > 0)
        {
            // Get camera position
            var pos = GameObject.FindGameObjectWithTag("MainCamera").transform.localPosition;
            // Remap x value coming from the floor
            pos.x = Utility.UtilityClass.Remap(x_value, lowestXValueNIW, highestXValueNIW, dataManager.GetLeftLimitXPoint(), dataManager.GetRightLimitXPoint());
            // Get a can
            GameObject c = dataManager.UseCan(pos);

            // Add a force and a torque to the can previously instanciated
            c.GetComponent<Rigidbody>().AddForce(new Vector3(xForce, yForce, zForce));
            c.GetComponent<Rigidbody>().AddTorque(new Vector3(torqueForce, 0, 0));

            // Can correctly instanciated
            return true;
        }

        // Imposible to instanciate can. User has alread thrown all the available can
        return false;
    }

    private int counter = 0;
    private static int windowSize = 5;
    private FloorData[] window = new FloorData[windowSize];
    private bool DetectLane(OscMessage message, out FloorData input)
    {
        // Calculate center of pressure
        CenterOfPressure(message, out window[counter]);

        // Upgrade the counter
        counter++;

        // Check if we get the desired number of values
        if(counter == windowSize)
        {
            int i = 0;
            input.x_value = 0;
            input.y_value = 0;
            input.tot = 0;
            for (; i < windowSize; i++)
            {
                if(window[i].tot > input.tot)
                {
                    input.x_value = window[i].x_value;
                    input.y_value = window[i].y_value;
                    input.tot = window[i].tot;
                }
            }

            counter = 0;

            return true;
        }

        input = new FloorData();
        return false;
    }

    /// <summary>
    /// Compute center of pressure.
    /// Top-Left Point = (0, 0)
    /// Bottom-Right Point = (2, 1)
    /// </summary>
    private void CenterOfPressure(OscMessage message, out FloorData data)
    {
        data.tot = 0;

        // We are interested in computing the center of pressure just for tiles 1 & 3
        // Tiles order:
        // |1|0|
        // |3|2|
        // Sensors order:
        // |2|3|
        // |1|0|
        int[] frontTiles = {1, 3};
        for(int i = 0; i < frontTiles.Length; i++)
        {
            for(int j = 0; j < 4; j++)
            {
                data.tot += (int)message[frontTiles[i] * 4 + j];
            }
        }

        // Compute weighted average
        data.x_value = ((float)(4 + (int)message[3 * 4 + 2] + (int)message[3 * 4 + 3] + (int)message[1 * 4 + 0] + (int)message[1 * 4 + 1] + 2 * ((int)message[1 * 4 + 2] + (int)message[1 * 4 + 3])) / (float)(8 + data.tot));
        data.y_value = ((float)(4 + (int)message[3 * 4 + 0] + (int)message[3 * 4 + 3] + (int)message[1 * 4 + 0] + (int)message[1 * 4 + 3]) / (float)(8 + data.tot));
    }

    public void OnDestroy()
    {
        // detach from the OscAddressManager
        foreach (KeyValuePair<KeyValuePair<OscReceiveController, string>, OscMessageEvent> e in callbacks)
        {
            e.Key.Key.Manager.Detach(e.Key.Value, e.Value);
        }

        // Notify server
        NotifyServer("end");
    }

    public FloorType GetFloorType()
    {
        return floorType;
    }

    private void Send(OscMessage msg)
    {
        if (floorSenderController != null)
        {
            // Send the message
            floorSenderController.Sender.Send(msg);
            Debug.Log(msg);
        }
    }

    private void NotifyServer(string status)
    {
        Send(new OscMessage(serverAddress, status));
    }
}
