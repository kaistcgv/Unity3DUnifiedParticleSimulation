using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUBoxColliderBase : GPUColliderBase {

	public Vector3 m_size = Vector3.one;
    GPUBoxCollider m_collider_data;

    public override void ActualUpdate()
    {
        ColliderImplementation.BuildBoxCollider(ref m_collider_data, m_trans, m_size);
        EachTargets((t) => { t.AddBoxCollider(ref m_collider_data); });
    }
}
