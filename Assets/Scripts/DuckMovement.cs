using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DuckMovement : MonoBehaviour {

    [Header("Parameters")]
    public string PathName;
    public float Time;

    private DataManager dataManager;

	// Use this for initialization
	void Start()
    {
        dataManager = DataManager.instance;

        dataManager.AddDuckPosition(gameObject.name, new KeyValuePair<Vector3, Quaternion>(gameObject.transform.localPosition, gameObject.transform.localRotation));

        DefinePath();

        dataManager.DefineBoundaries(gameObject.transform.position);
    }

    public void DefinePath()
    {
        Vector3[] nodes = iTweenPath.GetPath(PathName);
        Vector3[] path = new Vector3[nodes.Length];
        int start = -1;

        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].x == transform.localPosition.x && nodes[i].z == transform.localPosition.z)
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

    public void StopPath()
    {
        iTween.Stop(gameObject);
    }
}
