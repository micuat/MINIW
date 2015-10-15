using UnityEngine;
using System.Collections;
using Rug.Osc;

public class FloorReceiver : ReceiveOscBehaviourBase {

    [Header("Haptic Parameters")]
    public int CloserThreshold = 20000;
    public int FartherThreshold = 30000;
    public int SpawnThreshold = 15000;

    [Header("Game Parameters")]
    public GameObject chunk;
    private bool spawned = false;
    public float Speed = -1f;
    private int limit = 4;

    float angle = 0;
    float speed = (Mathf.PI) / 2; //2*PI in degress is 360, so you get 5 seconds to complete a circle
    float radius = 4;

    public Material waterMaterial;
    int waterCounter = 0;

    public override void Start()
    {
        base.Start();
        GameObject water = GameObject.Find ("WaterProDaytime");
        waterMaterial = water.GetComponent<Renderer>().sharedMaterial;
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha7))
        {
            var c = Instantiate(chunk, GameObject.FindGameObjectWithTag("MainCamera").transform.localPosition, Quaternion.identity) as GameObject;
            c.GetComponent<Rigidbody>().AddForce(new Vector3(0, 0.5f, 4f));
            c.GetComponent<Rigidbody>().AddTorque(new Vector3(10, 0, 0));

            Vector4 impactShader = new Vector4 ();
            impactShader.x = Random.Range(-2.4f, 2.4f);
            impactShader.y = 0;
            impactShader.z = Random.Range(-2.4f, 2.4f);
            impactShader.w = Time.time;
            waterMaterial.SetVector(System.String.Concat("_Center", System.Convert.ToString(waterCounter)), impactShader);
            waterCounter = (waterCounter + 1) % 4;
        }

        Vector3 moveDir = new Vector3(1, 0, 0);

        //if (Mathf.Abs(GameObject.FindGameObjectWithTag("Duck").transform.localPosition.x) >= limit)
        //{
        //    Speed *= -1;
        //}
        //GameObject.FindGameObjectWithTag("Duck").transform.localPosition += moveDir * Speed * Time.deltaTime;







        
        if(Mathf.Abs(GameObject.FindGameObjectWithTag("Duck").transform.localPosition.x) < (limit - 1))
        {
            angle = 0;
            GameObject.FindGameObjectWithTag("Duck").transform.localPosition += moveDir * Speed * Time.deltaTime;
        }
        if (Mathf.Abs(GameObject.FindGameObjectWithTag("Duck").transform.localPosition.x) >= (limit - 1) && angle < 90)
        {
            angle -= speed * Time.deltaTime; //if you want to switch direction, use -= instead of +=
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            Vector3 v = new Vector3(x, 0, y);
            GameObject.FindGameObjectWithTag("Duck").transform.localPosition += moveDir;
        }
        if(angle >= 90)
        {
            Speed *= -1;
        }
        // GameObject.FindGameObjectWithTag("Duck").transform.localPosition += moveDir * Speed * Time.deltaTime;
       






        //angle -= speed * Time.deltaTime; //if you want to switch direction, use -= instead of +=
        //float x = Mathf.Cos(angle) * radius;
        //float y = Mathf.Sin(angle) * radius;
        //Vector3 moveDir = new Vector3(x, 0, y);

        //if (Mathf.Abs(GameObject.FindGameObjectWithTag("Duck").transform.localPosition.x) >= limit)
        //{
        //    Speed *= -1;
        //}
        //// GameObject.FindGameObjectWithTag("Duck").transform.localPosition += moveDir * Speed * Time.deltaTime;
        //GameObject.FindGameObjectWithTag("Duck").transform.localPosition = moveDir;

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

        if (tot > CloserThreshold && spawned == false)
        {
            var pos = GameObject.FindGameObjectWithTag("MainCamera").transform.localPosition;
            pos.x = Remap(x_value, 0, 2, -4, 4);
            var c = Instantiate(chunk, pos, Quaternion.identity) as GameObject;
            c.GetComponent<Rigidbody>().AddForce(new Vector3(0, 0.5f, 4f));
            c.GetComponent<Rigidbody>().AddTorque(new Vector3(10, 0, 0));

            spawned = true;
        }
        else if(tot < SpawnThreshold)
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
