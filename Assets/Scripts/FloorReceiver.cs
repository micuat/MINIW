using UnityEngine;
using System.Collections;
using Rug.Osc;

public class FloorReceiver : ReceiveOscBehaviourBase {

    public GameObject chunk;
    bool[] spawened = new bool[4];

    public GameObject Voronoi;

    private float speed = -1.5f;
    private int limit = 4;
   
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha7))
        {
            var c = Instantiate(chunk, GameObject.FindGameObjectWithTag("MainCamera").transform.localPosition, Quaternion.identity) as GameObject;
            c.GetComponent<Rigidbody>().AddForce(new Vector3(0, 0.5f, 4f));
            c.GetComponent<Rigidbody>().AddTorque(new Vector3(10, 0, 0));
        }

        Vector3 moveDir = new Vector3(1, 0 ,0);

        if (Mathf.Abs(GameObject.FindGameObjectWithTag("Duck").transform.localPosition.x) >= limit)
        {
            speed *= -1;
            Debug.Log(limit + " " + speed);
        }
        GameObject.FindGameObjectWithTag("Duck").transform.localPosition += moveDir * speed * Time.deltaTime;

    }

    protected override void ReceiveMessage (OscMessage message) {

        if (message.Count != 16)
        {
            Debug.LogError(string.Format("Unexpected argument count {0}", message.Count));

            return;
        }

        float x_value = 0;
        float y_value = 0;

        CenterOfPressure(message, out x_value, out y_value);



        int[] fsrTile = new int[4];
        int[,] fsrRaw = new int[4, 4];
        for(int i = 0; i < 4; i++)
        {
            fsrTile[i] = 0;
            for(int j = 0; j < 4; j++)
            {
                fsrTile[i] += (int)message[i * 4 + j];
                fsrRaw[i, j] = (int)message[i * 4 + j];
            }
        }

        Debug.Log(fsrTile[0] + " " + fsrTile[1] + " " + fsrTile[2] + " " + fsrTile[3]);

        for (int i = 0; i < 4; i++) {
            Vector3 cmass = new Vector3();
            Vector3 tileCenter = new Vector3();
            switch(i) {
            case 0:
                tileCenter += new Vector3( 0.5f, 0, -0.5f);
                break;
            case 1:
                tileCenter += new Vector3( 0.5f, 0,  0.5f);
                break;
            case 2:
                tileCenter += new Vector3(-0.5f, 0, -0.5f);
                break;
            case 3:
                tileCenter += new Vector3(-0.5f, 0, 0.5f);
                break;
            }
            cmass += (new Vector3(-1, 0, -1) * 0.46f + tileCenter) * fsrRaw[i, 0];
            cmass += (new Vector3(-1, 0,  1) * 0.46f + tileCenter) * fsrRaw[i, 1];
            cmass += (new Vector3( 1, 0,  1) * 0.46f + tileCenter) * fsrRaw[i, 2];
            cmass += (new Vector3( 1, 0, -1) * 0.46f + tileCenter) * fsrRaw[i, 3];
            if (fsrTile [i] > 20000 && spawened[i] == false) {
                var c = Instantiate(chunk, new Vector3(0, 1, 0), Quaternion.identity) as GameObject;
                c.GetComponent<Rigidbody>().AddForce(new Vector3(0, 2, 0) + cmass * 0.00005f);
                spawened[i] = true;

                //Voronoi.SendMessage("CrackReceived", cmass);
            } else if (fsrTile [i] <= 20000) {
                spawened[i] = false;
            }
        }
    }

    private void CenterOfPressure(OscMessage message, out float x, out float y)
    {
        int tot = 0;

        int[] frontTiles = {1, 3};
        for(int i = 0; i < frontTiles.Length; i++)
        {
            for(int j = 0; j < 4; j++)
            {
                tot += (int)message[frontTiles[i] * 4 + j];
            }
        }
        x = ((float)(4 + (int)message[3 * 4 + 2] + (int)message[3 * 4 + 3] + (int)message[1 * 4 + 0] + (int)message[1 * 4 + 1] + 2 * ((int)message[1 * 4 + 2] + (int)message[1 * 4 + 3])) / (float)(8 + tot));
        y = ((float)(4 + (int)message[3 * 4 + 0] + (int)message[3 * 4 + 3] + (int)message[1 * 4 + 0] + (int)message[1 * 4 + 3]) / (float)(8 + tot));

        Debug.Log (x + " " + y + " " + tot);
    }
}
