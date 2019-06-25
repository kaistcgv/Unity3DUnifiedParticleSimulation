using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUKernel
{
	public class KernelViscosity {
		public static float ComputeLaplacianConstant(float h)
		{
			return 45.0f / (Mathf.PI * Mathf.Pow(h, 6.0f));
		}

		public static float ComputeLaplacianVariable(float h, float r)
		{
			if(r < 0.0f || r > h) return 0.0f;

			return h - r;
		}

		public static float ComputeLaplacian(float h, float r)
		{
			return ComputeLaplacianConstant(h) * ComputeLaplacianVariable(h, r);
		}
	}	
}

