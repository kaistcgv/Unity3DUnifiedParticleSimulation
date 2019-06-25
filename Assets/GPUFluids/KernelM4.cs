using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUKernel
{
		
	public class KernelM4 {
		public static float ComputeKernelConstant(float h)
		{
			return 8.0f / Mathf.PI;
		}

		public static float ComputeKernelVariable(float h, float r)
		{
			float q = r / (2 * h);
			if(q < 0 || q > 1) return 0;
			if(q <= 0.5f) return 1.0f - 6.0f * q * q + 6.0f * q * q * q;

			float tmp = 1.0f - q;
			return 2.0f * tmp * tmp * tmp;
		}

		public static float ComputeKernel(float h, float r)
		{
			return ComputeKernelConstant(h) * ComputeKernelVariable(h, r);
		}
	}
}