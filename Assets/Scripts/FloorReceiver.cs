using UnityEngine;
using Rug.Osc;
using System.Collections.Generic;
using System;

public class FloorReceiver : MonoBehaviour
{
    public enum FloorType { Normal, BarIncreasing, Adaptive};

    #region define Osc parameters

    [Header("Osc Parameters")]
    public GameObject receiver;

    private OscReceiveController floorReceiverController;

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
    /// Object that will be thrown at ducks
    public GameObject chunk;
    private float xForce = 0;
    private float yForce = 0;
    private float zForce = 0;
    private float torqueForce = 10;
    private bool spawned = false;
    private float lowForceValue = 2;
    private float highForceValue = 7;

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

        floorType = type;
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
            var c = Instantiate(chunk, GameObject.FindGameObjectWithTag("MainCamera").transform.localPosition, Quaternion.identity) as GameObject;
            c.GetComponent<Rigidbody>().AddForce(new Vector3(0, 0.5f, 4f));
            c.GetComponent<Rigidbody>().AddTorque(new Vector3(10, 0, 0));
        }
    }

    private void FloorContact(OscMessage message)
    {
        Debug.Log(message[2]);
        // Check whether we can receive and compute the message
        if (gameManager.canReceive && dataManager.sessionGameTime > 0 && dataManager.sessionDucksInTheGame > 0)
        {
            Debug.Log(message[0] + " " + message[2]);
            // Check the type of the message received
            switch (message[0] as string)
            {
                // Add message: the user has started to push the tile
                case "add":
                    // If the player hasn't started the game yet ...
                    if (!gameManager.isPlaying)
                    {
                        /// ... Zero the score, and show the Void GUIState (that is, make the Main Menu disappear)
                        dataManager.SetScore(0);
                        guiManager.ShowGUI(GUIManager.GUIState.Void);
                    }
                    // If the game is in Void mode and the player is playing ...
                    else if (guiManager.guiState == GUIManager.GUIState.Void && gameManager.isPlaying)
                    {
                        // ... Show force bar
                        guiManager.ShowForceBar(true, Remap((float)message[2], 0, 2, 0, Screen.width));
                        // Record detected step in the xml file
                        dataManager.parser.AddStep(GetTimestamp(DateTime.Now));

                        spawned = false;
                    }
                    break;
                case "update":
                    // If the game is in Void mode and the player is playing ...
                    if (guiManager.guiState == GUIManager.GUIState.Void && gameManager.isPlaying && !spawned)
                    {
                        // Update force bar
                        guiManager.UpdateForceBar(Remap((float)message[2], 0, 2, 0, Screen.width), (float)message[4]);
                    }
                    break;
                case "remove":
                    // The game will start only after the first remove message is received
                    if (guiManager.guiState == GUIManager.GUIState.Void && !gameManager.isPlaying)
                    {
                        // Start playing
                        gameManager.SetPlayingStatus(true);
                        // Add a new session in the xml file
                        dataManager.parser.AddSession(GetTimestamp(DateTime.Now));
                    }
                    // If the game is in Void mode and the player is playing ...
                    else if (guiManager.guiState == GUIManager.GUIState.Void && gameManager.isPlaying)
                    {
                        // Update force bar
                        guiManager.UpdateForceBar(Remap((float)message[2], 0, 2, 0, Screen.width), (float)message[4]);

                        // Define parameters to be used to throw the can
                        DefineCanParameters(0, 0.5f, (float)message[4], 0, 1, lowForceValue, highForceValue);

                        // Instanciate and throw the can using the defined parameters
                        ThrowCan((float)message[2]);

                        // Record detected step in the xml file
                        dataManager.parser.SaveStep((float)message[2], (float)message[3], (float)message[4], GetTimestamp(DateTime.Now));

                        // Can has be spawned. The player has to lift his/her foot in order to be able to throw
                        // another can
                        spawned = true;
                    }
                    break;
            }

        }
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
        zForce = Remap(zforce, from_old, to_old, from_new, to_new);

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
                            ThrowCan(compute.x_value);

                            // Can has be spawned. The player has to lift his/her foot in order to be able to throw
                            // another can
                            spawned = true;

                            // Record detected step in the xml file
                            dataManager.parser.AddStep(compute.x_value, compute.y_value, compute.tot, zForce);
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

                                ThrowCan(compute.x_value);

                                spawned = true;

                                dataManager.parser.AddStep(compute.x_value, compute.y_value, compute.tot, zForce);
                            }
                            else if (compute.tot >= fartherThreshold)
                            {
                                DefineCanParameters(0, 0.5f, 5.5f);

                                ThrowCan(compute.x_value);

                                spawned = true;

                                dataManager.parser.AddStep(compute.x_value, compute.y_value, compute.tot, zForce);
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
                dataManager.parser.AddSession(GetTimestamp(DateTime.Now));
            } 
        }
    }

    private void ThrowCan(float x_value)
    {
        // Get camera position
        var pos = GameObject.FindGameObjectWithTag("MainCamera").transform.localPosition;
        // Remap x value coming from the floor
        pos.x = Remap(x_value, 0, 2, -6.5F, 6.5F);
        // Instanciate a new can
        var c = Instantiate(chunk, pos, Quaternion.identity) as GameObject;
        // Subtract the can from the total amunt
        dataManager.UseCan();

        // Add a force and a torque to the can previously instanciated
        c.GetComponent<Rigidbody>().AddForce(new Vector3(xForce, yForce, zForce));
        c.GetComponent<Rigidbody>().AddTorque(new Vector3(torqueForce, 0, 0));
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

    private float Remap(float value, float from1, float to1, float from2, float to2)
    {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    public void OnDestroy()
    {
        // detach from the OscAddressManager
        foreach (KeyValuePair<KeyValuePair<OscReceiveController, string>, OscMessageEvent> e in callbacks)
        {
            e.Key.Key.Manager.Detach(e.Key.Value, e.Value);
        }
    }
    
    private String GetTimestamp(this DateTime value)
    {
        return value.ToString("yyyy.MM.dd.HH.mm.ss.fff");
    }

    public FloorType GetFloorType()
    {
        return floorType;
    }
}
