using UnityEngine;
using System.Collections;
using Rug.Osc;

public class FloorReceiver : ReceiveOscBehaviourBase {

    public GameObject chunk;
    bool spawned = false;

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
        int tot;

        CenterOfPressure(message, out x_value, out y_value, out tot);

        if (tot > 20000 && spawned == false)
        {
            var pos = GameObject.FindGameObjectWithTag("MainCamera").transform.localPosition;
            pos.x = Remap(x_value, 0, 2, -4, 4);
            var c = Instantiate(chunk, pos, Quaternion.identity) as GameObject;
            c.GetComponent<Rigidbody>().AddForce(new Vector3(0, 0.5f, 4f));
            c.GetComponent<Rigidbody>().AddTorque(new Vector3(10, 0, 0));

            spawned = true;
        }
        else if(tot < 15000)
        {
            spawned = false;
        }
    }

    private void CenterOfPressure(OscMessage message, out float x, out float y, out int tot)
    {
        tot = 0;

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

    private float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}
