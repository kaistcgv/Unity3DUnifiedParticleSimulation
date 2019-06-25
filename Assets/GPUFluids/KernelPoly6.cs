using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUKernel
{
		
	public class KernelPoly6 {
		public static float ComputeKernelConstant(float h)
		{
			return 315.0f / (64.0f * Mathf.PI * Mathf.Pow(h, 9.0f));
		}

		public static float ComputeKernelVariable(float h, float r)
		{
			if(r < 0.0f || r > h) return 0.0f;
			float tmp = Mathf.Pow(h, 2.0f) - Mathf.Pow(r, 2.0f);
			return tmp * tmp * tmp;
		}

		public static float ComputeKernel(float h, float r)
		{
			return ComputeKernelConstant(h) * ComputeKernelVariable(h, r);
		}
	}

}
