using System.Collections;
using UnityEngine;
using Rug.Osc;

public abstract class ReceiveOscBehaviourBase : MonoBehaviour {

	private OscReceiveController m_ReceiveController; 

	public GameObject ReceiveControllerObject; 

	public string OscAddress = "/test";

	public void Awake () {
		
		m_ReceiveController = null; 
		
		if (ReceiveControllerObject == null) {
			Debug.LogError("You must supply a ReceiverControllerObject"); 
			return; 
		}

		OscReceiveController controller = ReceiveControllerObject.GetComponent<OscReceiveController> (); 
		
		if (controller == null) { 
			Debug.LogError(string.Format("The GameObject with the name '{0}' does not contain a OscReceiveController component", ReceiveControllerObject.name)); 
			return; 
		}
		
		m_ReceiveController = controller; 
	}

	// Use this for initialization
	public virtual void Start () {

		if (m_ReceiveController != null) {

			m_ReceiveController.Manager.Attach (OscAddress, ReceiveMessage); 
		}
	}

	public virtual void OnDestroy () {

		// detach from the OscAddressManager
		if (m_ReceiveController != null) {
			m_ReceiveController.Manager.Detach (OscAddress, ReceiveMessage);
		}
	}

	protected abstract void ReceiveMessage (OscMessage message);
}
