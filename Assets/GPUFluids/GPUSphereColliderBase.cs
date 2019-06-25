using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSphereColliderBase : GPUColliderBase {

	public float m_radius = 0.5f;
	GPUSphereCollider m_collider_data;

	public override void ActualUpdate()
	{
		ColliderImplementation.BuildSphereCollider(ref m_collider_data, m_trans, m_radius);
		EachTargets((t) => { t.AddSphereCollider(ref m_collider_data); });
	}
}
