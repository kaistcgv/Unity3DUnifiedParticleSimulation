using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEmitter : MonoBehaviour {

    public Vector3 dir = new Vector3(0.0f, 0.0f, 0.0f);
    public float radius = 0.001f;
    public float mass = 0.05f;
    public GPUParticleSimulation simScript;
    public float seconds = 1.0f;

	// Use this for initialization
	void OnEnable () 
    {
        StartCoroutine(Pipe());            
	}

    IEnumerator Pipe()
    {
        while(true)
        {
            float spacing = 0.25f;
            for(float i = -0.5f; i < 0.5f; i += spacing)
            {
                for(float j = 0.0f; j < 0.2f; j += spacing)
                {
                    for(float k = -0.5f; k < 0.5f; k += spacing)
                    {
                        simScript.AddParticle(transform.position + new Vector3(i, j , k), dir, new Vector3(0.0f, 0.0f, 0.0f), new Vector4(0.0f, -1.0f, radius, mass));
                    }
                }
            }
            
            yield return new WaitForSecondsRealtime(seconds);
        }
    }
}
