using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUKernel
{
	public class KernelSpiky {
		public static float ComputeGradientConstant(float h)
		{
			return -45.0f / (Mathf.PI * Mathf.Pow(h, 6.0f));
		}

		public static float ComputeGradientVariable(float h, float r)
		{
			if(r < 0 || r > h) return 0.0f;

			float tmp = h - r;
			return Mathf.Pow(tmp, 2.0f);
		}

		public static float ComputeGradient(float h, float r)
		{
			return ComputeGradientConstant(h) * ComputeGradientVariable(h, r);
		}
	}	
}

