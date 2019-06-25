using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderCameraToCubemap : MonoBehaviour {
    public RenderTexture rt;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void LateUpdate() {
        GetComponent<Camera>().RenderToCubemap(rt, 63);
    }
}
