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
                        Vector3 jitter = Random.insideUnitSphere * spacing * 0.1f;
                        simScript.AddParticle(transform.position + new Vector3(i, j , k) + jitter, dir, new Vector3(0.0f, 0.0f, 0.0f), new Vector4(0.0f, -1.0f, radius, mass));
                    }
                }
            }
            
            yield return new WaitForSecondsRealtime(seconds * Time.fixedDeltaTime / simScript.timestep);
        }
    }
}
