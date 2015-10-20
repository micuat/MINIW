using UnityEngine;
using Rug.Osc;
using System.Collections.Generic;

public class FloorReceiver : MonoBehaviour
{
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

    [Header("Game Parameters")]
    public GameObject chunk;
    private bool spawned = false;

    private GameManager gameManager;
    private DataManager dataManager;
    private GUIManager guiManager;

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
        }
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

    private void ReceiveMessage (OscMessage message) {

        if (gameManager.canReceive && dataManager.sessionGameTime > 0 && dataManager.sessionDuckNumber > 0)
        {
            if (message.Count != 16)
            {
                Debug.LogError(string.Format("Unexpected argument count {0}", message.Count));

                return;
            }

            float x_value = 0;
            float y_value = 0;
            int tot = 0;
            CenterOfPressure(message, out x_value, out y_value, out tot);

            if (!gameManager.isPlaying)
            {
                if (tot > fartherThreshold)
                {
                    dataManager.SetScore(0);
                    guiManager.ShowGUI(GUIManager.GUIState.Void);
                }
            }

             
            if (guiManager.guiState == GUIManager.GUIState.Void && gameManager.isPlaying)
            {
                if (tot > closerThreshold && spawned == false)
                {
                    FloorData compute = new FloorData();
                    if (DetectLane(message, out compute))
                    {
                        Debug.Log(compute.tot);
                        if (compute.tot > closerThreshold && compute.tot < fartherThreshold)
                        {
                            // Get camera position
                            var pos = GameObject.FindGameObjectWithTag("MainCamera").transform.localPosition;
                            // Remap x value coming from the floor
                            pos.x = Remap(compute.x_value, 0, 2, -4, 4);
                            // Instanciate a new can
                            var c = Instantiate(chunk, pos, Quaternion.identity) as GameObject;
                            // Add a force and a torque to the can previously instanciated
                            c.GetComponent<Rigidbody>().AddForce(new Vector3(0, 0.5f, 4f));
                            c.GetComponent<Rigidbody>().AddTorque(new Vector3(10, 0, 0));

                            spawned = true; 
                        } 
                        else if(compute.tot >= fartherThreshold)
                        {
                            // Get camera position
                            var pos = GameObject.FindGameObjectWithTag("MainCamera").transform.localPosition;
                            // Remap x value coming from the floor
                            pos.x = Remap(compute.x_value, 0, 2, -4, 4);
                            // Instanciate a new can
                            var c = Instantiate(chunk, pos, Quaternion.identity) as GameObject;
                            // Add a force and a torque to the can previously instanciated
                            c.GetComponent<Rigidbody>().AddForce(new Vector3(0, 0.5f, 5.5f));
                            c.GetComponent<Rigidbody>().AddTorque(new Vector3(10, 0, 0));

                            spawned = true;
                        }
                    }
                }
                else if (tot < spawnThreshold)
                {
                    spawned = false;
                }
            }
            else if (guiManager.guiState == GUIManager.GUIState.Void && !gameManager.isPlaying)
            {
                gameManager.SetPlayingStatus(true);
            } 
        }
    }

    private int counter = 0;
    private static int windowSize = 5;
    public struct FloorData
    {
        public float x_value;
        public float y_value;
        public int tot;
    }
    private FloorData[] window = new FloorData[windowSize];
    private bool DetectLane(OscMessage message, out FloorData input)
    {
        float x, y;
        int tot;

        // Calculate center of pressure
        CenterOfPressure(message, out x, out y, out tot);

        // Store data in the window array
        window[counter].x_value = x;
        window[counter].y_value = y;
        window[counter].tot = tot;

        // Upgrade the counter
        counter++;

        // Check if we get the desired nomber of values
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
    private void CenterOfPressure(OscMessage message, out float x, out float y, out int tot)
    {
        tot = 0;

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
                tot += (int)message[frontTiles[i] * 4 + j];
            }
        }

        // Compute weighted average
        x = ((float)(4 + (int)message[3 * 4 + 2] + (int)message[3 * 4 + 3] + (int)message[1 * 4 + 0] + (int)message[1 * 4 + 1] + 2 * ((int)message[1 * 4 + 2] + (int)message[1 * 4 + 3])) / (float)(8 + tot));
        y = ((float)(4 + (int)message[3 * 4 + 0] + (int)message[3 * 4 + 3] + (int)message[1 * 4 + 0] + (int)message[1 * 4 + 3]) / (float)(8 + tot));
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
}
