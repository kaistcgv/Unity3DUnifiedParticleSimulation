#ifndef Kernel
#define Kernel

float ComputeCubicSplineKernelVariable(float q)
{
    if(q < 0 || q >= 2) return 0;
    float tmp = 2.0f - q;
    float tmp2 = 1.0f - q;
    if(q < 1) return tmp * tmp * tmp - 4.0f * tmp2 * tmp2 * tmp2;
    return tmp * tmp * tmp;
}

float ComputeCubicSplineGradientVariable(float q)
{
    if(q < 0 || q >= 2) return 0;
    float tmp = 2.0f - q;
    float tmp2 = 1.0f - q;
    if(q < 1) return -3.0f * tmp * tmp + 12.0f * tmp2 * tmp2;
    return -3.0f * tmp * tmp;
}

float ComputeM4KernelVariable(float smoothing_len, float r_len)
{
     float q = r_len / smoothing_len;

    if (r_len < 0.0f || r_len >= smoothing_len) return 0.0f;
    
    if (q < 0.5f) return 1.0f - 6.0f * q * q + 6.0f * q * q * q;
        
    float tmp = 1.0f - q; 
    return 2.0f * tmp * tmp * tmp; 
}

float ComputePoly6KernelVariable(float smoothing_len_sq, float r_len_sq)
{
    float tmp = smoothing_len_sq - r_len_sq;
    return tmp * tmp * tmp;
}

float ComputeSpikyGradientVariable(float smoothing_len, float r_len)
{
    float diff = smoothing_len - r_len;
    return (1.0f / r_len) * diff * diff;
}

float ComputeViscosityLaplacianVariable(float smoothing_len, float r_len)
{
    return smoothing_len - r_len;
}

#endif