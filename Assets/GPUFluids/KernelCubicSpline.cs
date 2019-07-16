using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUKernel
{
		
	public class KernelCubicSpline {
		public static float ComputeKernelConstant(float h)
		{
			return 1.0f / (4.0f * Mathf.PI * Mathf.Pow(h, 3));
		}

		public static float ComputeKernelVariable(float h, float r)
		{
			float q = r / h;
            if(q < 0 || q >= 2) return 0;
            float tmp = 2.0f - q;
            float tmp2 = 1.0f - q;
            if(q < 1) return Mathf.Pow(tmp, 3) - 4.0f * Mathf.Pow(tmp2, 3);
            return Mathf.Pow(tmp, 3);
		}

		public static float ComputeKernel(float h, float r)
		{
			return ComputeKernelConstant(h) * ComputeKernelVariable(h, r);
		}

        public static float ComputeGradientConstant(float h)
        {
            return 1.0f / (4.0f * Mathf.PI * Mathf.Pow(h, 4));
        }

        public static float ComputeGradientVariable(float h, float r)
        {
            float q = r / h;
            if(q < 0 || q >= 2) return 0;
            float tmp = 2.0f - q;
            float tmp2 = 1.0f - q;
            if(q < 1) return -3.0f * Mathf.Pow(tmp, 2) + 12.0f * Mathf.Pow(tmp2, 2);
            return -3.0f * Mathf.Pow(tmp, 2);
        }

        public static float ComputeGradient(float h, float r)
        {
            return ComputeGradientConstant(h) * ComputeGradientVariable(h, r);
        }
	}
}