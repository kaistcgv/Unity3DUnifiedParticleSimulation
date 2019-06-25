using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUCapsuleColliderBase : GPUColliderBase {

	public enum Direction
    {
        X, Y, Z
    }

    public float m_radius = 0.5f;
    public float m_height = 2.0f;
    public Direction m_direction = Direction.Y;
    GPUCapsuleCollider m_collider_data;

    public override void ActualUpdate()
    {
        ColliderImplementation.BuildCapsuleCollider(ref m_collider_data, m_trans, m_radius, m_height, (int)m_direction);
        EachTargets((t) => { t.AddCapsuleCollider(ref m_collider_data); });
    }

}
