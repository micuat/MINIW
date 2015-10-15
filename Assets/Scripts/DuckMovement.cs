using UnityEngine;
using System.Collections;

public class DuckMovement : MonoBehaviour {

    [Header("Parameters")]
    public string PathName;
    public float Time;


	// Use this for initialization
	void Start () {
        Vector3[] nodes = iTweenPath.GetPath(PathName);
        Vector3[] path = new Vector3[nodes.Length];
        int start = -1;
        
        for(int i = 0; i < nodes.Length; i++)
        {
            if(nodes[i].x == transform.localPosition.x && nodes[i].z == transform.localPosition.z)
            {
                start = i;
                break;
            }
        }
        
        int j = 0;
        for (int i = start; i < nodes.Length; i++, j++)
        {
            path[j] = nodes[i];
        }
        for (int i = 1; i <= start; i++, j++)
        {
            path[j] = nodes[i];
        }

        iTween.MoveTo(gameObject, iTween.Hash("path", path, "time", Time, "looptype", iTween.LoopType.loop, "easetype", iTween.EaseType.linear, "movetopath", true, "orienttopath", true));
    }
}
