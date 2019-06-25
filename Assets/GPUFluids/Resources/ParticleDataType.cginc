#ifndef ParticleDataType
#define ParticleDataType

struct SimulationParameters
{
    int maxParticles;
    int numParticles;
    float timestep;
    float smoothingLength;
    float smoothingLengthSq;
    float invSmoothingLength;
    float restDensity;
    float artificialViscosity;
    float velocityLimit;
    float spacing;
    float3 worldOrigin;
    float3 cellSize;
    uint3 gridSize;
    float3 boxSize;
    int enableBox;
    uint numCells;
    int numSphereColliders;
    int numCapsuleColliders;
    int numBoxColliders;
    float coeffWeightedVolume;
    float coeffDensity;
    float coeffPressure;
    float coeffViscosity;
    float coeffCSKernel;
    float coeffCSGradient;
};

struct SortData
{
    uint key;
    uint index;
};

struct Cell
{
    uint begin;
    uint end;
};

struct AABB
{
    float3 center;
    float3 extents;
};

struct Sphere
{
    float3 center;
    float radius;
};

struct Capsule
{
    float3 pos1;
    float3 pos2;
    float radius;
};

struct Plane
{
    float3 normal;
    float distance;
};

struct Box
{
    float3 center;
    Plane planes[6];
};

struct SphereCollider
{
    AABB aabb;
    Sphere shape;
};

struct CapsuleCollider
{
    AABB aabb;
    Capsule shape;
};

struct BoxCollider
{
    AABB aabb;
    Box shape;
};

#endif // ParticleDataType