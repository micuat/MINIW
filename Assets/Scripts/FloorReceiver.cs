using UnityEngine;
using System.Collections;
using Rug.Osc;

public class FloorReceiver : ReceiveOscBehaviourBase {

	public GameObject chunk;
	bool[] spawened = new bool[4];

	protected override void ReceiveMessage (OscMessage message) {

		if (message.Count != 16) 
		{
			Debug.LogError(string.Format("Unexpected argument count {0}", message.Count));  
			
			return; 
		}

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
				var c = Instantiate(chunk, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
				c.GetComponent<Rigidbody>().AddForce(new Vector3(0, 200, 0) + cmass * 0.005f);
				spawened[i] = true;
			} else if (fsrTile [i] <= 20000) {
				spawened[i] = false;
			}
		}
	}
}
