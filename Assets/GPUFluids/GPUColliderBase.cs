using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUColliderBase : MonoBehaviour {
	static List<GPUColliderBase> s_instances;

	public static List<GPUColliderBase> GetInstances()
	{
		if(s_instances == null) s_instances = new List<GPUColliderBase>();
		return s_instances;
	}

	public static void UpdateAll()
	{
		GetInstances().ForEach((v) => {
			v.ActualUpdate();
		});
	}

	public GPUParticleSimulation[] m_targets;
	protected Transform m_trans;

	protected void EachTargets(System.Action<GPUParticleSimulation> a)
	{
		if(m_targets.Length == 0) { GPUParticleSimulation.GetInstances().ForEach(a); }
		else { foreach (var t in m_targets) { a(t); }}
	}

	void OnEnable()
	{
		GetInstances().Add(this);
		m_trans = GetComponent<Transform>();
	}

	void OnDisable() 
	{
		GetInstances().Remove(this);
	}

	public virtual void ActualUpdate()
	{

	}
}
