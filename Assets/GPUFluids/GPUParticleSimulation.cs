using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GPUParticleSimulation : MonoBehaviour 
{
    static List<GPUParticleSimulation> sInstances;

    [Header("SPH Parameters")]
    public int iterations = 10;
    public float timestep = 0.05f;
    public int maxParticles = 100000;
    public float smoothingLength = 0.228f;
    public float restDensity = 1000.0f;
    public float artificialViscosity = 1.0f;
    public float velocityLimit = 20.0f;
    private int numParticles = 0;

    [Header("Grid Information")]
    public Vector3 worldOrigin = new Vector3(0.0f, 0.0f, 0.0f);
    public UVector3 gridSize = new UVector3(64, 64, 64);

    [Header("Collider Parameters")]
    public int maxSphereColliders = 256;
    public int maxCapsuleColliders = 256;
    public int maxBoxColliders = 256;

    [Header("Box Container")]
    public Vector3 boxSize = new Vector3(5.0f, 5.0f, 5.0f);
    public Boolean useBox;

    // Simulation Data
    public SimulationParameters[] simulationParameters = new SimulationParameters[1];

    private List<Vector4> addPosPress = new List<Vector4>();
    private List<Vector4> addVelRho = new List<Vector4>();
    private List<Vector4> addForceVol = new List<Vector4>();
    private List<Vector4> addTGRM = new List<Vector4>(); // type, group, radius, mass;
    
    private List<GPUSphereCollider> sphereColliders = new List<GPUSphereCollider>();
    private List<GPUCapsuleCollider> capsuleColliders = new List<GPUCapsuleCollider>();
    private List<GPUBoxCollider> boxColliders = new List<GPUBoxCollider>();

    // Kernel index
    private int kAddParticles;
    private int kComputeWeightedVolume;
    private int kComputeDensityPressureWCSPH;
    private int kComputeForceWCSPH;
    private int kComputeForceBoundary;
    private int kIntegration;

    [Header("Compute Shaders")]
    // Compute shader 
    public ComputeShader simulationShader;
    public ComputeShader computeGridShader;
    public ComputeShader bitonicSortShader;
    ComputeBuffer bufSimulationParameters;
    ComputeBuffer[] bufSortData = new ComputeBuffer[2];
    ComputeBuffer[] bufPosPress = new ComputeBuffer[2];
    ComputeBuffer[] bufVelRho = new ComputeBuffer[2];
    ComputeBuffer[] bufForceVol = new ComputeBuffer[2];
    ComputeBuffer[] bufTGRM = new ComputeBuffer[2];
    ComputeBuffer bufVelVerlet;

    ComputeBuffer bufAddPosPress;
    ComputeBuffer bufAddVelRho;
    ComputeBuffer bufAddForceVol;
    ComputeBuffer bufAddTGRM;

    ComputeBuffer bufSphereColliders;
    ComputeBuffer bufCapsuleColliders;
    ComputeBuffer bufBoxColliders;

    ComputeBuffer bufCells;
    GPUSort bitonicSort;

    // Getters
    public ComputeBuffer GetPosPressBuffer() { return bufPosPress[0]; }
    public ComputeBuffer GetTGRMBuffer() { return bufTGRM[0]; }
    public int GetParticleNum() { return simulationParameters[0].numParticles; }

    // Colliders
    public void AddSphereCollider(ref GPUSphereCollider v) { if(enabled) sphereColliders.Add(v); }
    public void AddCapsuleCollider(ref GPUCapsuleCollider v) { if(enabled) capsuleColliders.Add(v); }
    public void AddBoxCollider(ref GPUBoxCollider v) { if(enabled) boxColliders.Add(v); }

    public delegate void SimulationHandler(List<GPUColliderBase> colliders);
    public SimulationHandler handler;

    const int BLOCK_SIZE = 512;

    public static List<GPUParticleSimulation> GetInstances()
    {
        if(sInstances == null) { sInstances = new List<GPUParticleSimulation>();}
        return sInstances;
    }

    #if UNITY_EDITOR
    void Reset()
    {
        simulationShader = AssetDatabase.LoadAssetAtPath("Assets/GPUFluids/Resources/ParticleSimulation.compute", typeof(ComputeShader)) as ComputeShader;
        computeGridShader = AssetDatabase.LoadAssetAtPath("Assets/GPUFluids/Resources/HashGrid.compute", typeof(ComputeShader)) as ComputeShader;
        bitonicSortShader = AssetDatabase.LoadAssetAtPath("Assets/GPUBitonicSort/BitonicSortCS/BitonicSort.compute", typeof(ComputeShader)) as ComputeShader;
    }
    #endif

    void Awake()
    {
        if(!SystemInfo.supportsComputeShaders)
        {
            Debug.Log("ComputeShader is not available on this system");
            gameObject.SetActive(false);
            return;
        }

        kAddParticles = simulationShader.FindKernel("AddParticles");
        kComputeWeightedVolume = simulationShader.FindKernel("ComputeWeightedVolume");
        kComputeDensityPressureWCSPH = simulationShader.FindKernel("ComputeDensityPressureWCSPH");
        kComputeForceWCSPH = simulationShader.FindKernel("ComputeForceWCSPH");
        kComputeForceBoundary = simulationShader.FindKernel("ComputeForceBoundary");
        kIntegration = simulationShader.FindKernel("Integration");
    }

    void OnEnable()
    {
        GetInstances().Add(this);
        SettingSimulationParameters();

        int roundUpPowerOf2 = (int) Mathf.Pow(2, Mathf.Ceil(Mathf.Log(simulationParameters[0].maxParticles)/Mathf.Log(2.0f)));

        bufSimulationParameters = new ComputeBuffer(1, Marshal.SizeOf(typeof(SimulationParameters)));
        for(int i = 0; i < 2; i++)
        {
            bufPosPress[i] = new ComputeBuffer(maxParticles, Marshal.SizeOf(typeof(Vector4)));
            bufVelRho[i] = new ComputeBuffer(maxParticles, Marshal.SizeOf(typeof(Vector4)));
            bufForceVol[i] = new ComputeBuffer(maxParticles, Marshal.SizeOf(typeof(Vector4)));
            bufTGRM[i] = new ComputeBuffer(maxParticles, Marshal.SizeOf(typeof(Vector4)));

            bufSortData[i] = new ComputeBuffer(roundUpPowerOf2, Marshal.SizeOf(typeof(SortData)));
        }

        bufVelVerlet = new ComputeBuffer(maxParticles, Marshal.SizeOf(typeof(Vector4)));
        bufSphereColliders = new ComputeBuffer(maxSphereColliders, Marshal.SizeOf(typeof(GPUSphereCollider)));
        bufCapsuleColliders = new ComputeBuffer(maxCapsuleColliders, Marshal.SizeOf(typeof(GPUCapsuleCollider)));
        bufBoxColliders = new ComputeBuffer(maxBoxColliders, Marshal.SizeOf(typeof(GPUBoxCollider)));

        bufAddPosPress = new ComputeBuffer(maxParticles, Marshal.SizeOf(typeof(Vector4)));
        bufAddVelRho = new ComputeBuffer(maxParticles, Marshal.SizeOf(typeof(Vector4)));
        bufAddForceVol = new ComputeBuffer(maxParticles, Marshal.SizeOf(typeof(Vector4)));
        bufAddTGRM = new ComputeBuffer(maxParticles, Marshal.SizeOf(typeof(Vector4)));
        bufCells = new ComputeBuffer((int)simulationParameters[0].numCells, Marshal.SizeOf(typeof(Cell)));

        bitonicSort = new GPUSort();
        bitonicSort.Initialize(bitonicSortShader);

        bufSimulationParameters.SetData(simulationParameters);
    }

    void OnDisable()
    {
        GetInstances().Remove(this);

        if(bufSimulationParameters != null) { bufSimulationParameters.Release(); }
        if(bufCells != null) { bufCells.Release(); }
        if(bufAddPosPress != null) { bufAddPosPress.Release(); }
        if(bufAddVelRho != null) { bufAddVelRho.Release(); }
        if(bufAddForceVol != null) { bufAddForceVol.Release(); }
        if(bufAddTGRM != null) { bufAddTGRM.Release(); }
        if(bufVelVerlet != null) { bufVelVerlet.Release(); }
        if(bufSphereColliders != null) { bufSphereColliders.Release(); }
        if(bufCapsuleColliders != null) { bufCapsuleColliders.Release(); }
        if(bufBoxColliders != null) { bufBoxColliders.Release(); }
        if(bitonicSort != null) { bitonicSort.Release(); }
        for(int i = 0; i < 2; i++)
        {
            if(bufPosPress[i] != null) { bufPosPress[i].Release(); }
            if(bufVelRho[i] != null) { bufVelRho[i].Release(); }
            if(bufForceVol[i] != null) { bufForceVol[i].Release(); }
            if(bufTGRM[i] != null) { bufTGRM[i].Release(); }
            if(bufSortData[i] != null) { bufSortData[i].Release(); }
        }
    }

    void OnValidate()
    {
        SettingSimulationParameters();
    }

    private void SettingSimulationParameters()
    {
        simulationParameters[0].maxParticles = maxParticles;
        simulationParameters[0].numParticles = numParticles;
        simulationParameters[0].timestep = timestep;
        simulationParameters[0].smoothingLength = smoothingLength;
        simulationParameters[0].smoothingLengthSq = smoothingLength * smoothingLength;
        simulationParameters[0].invSmoothingLength = 1.0f / smoothingLength;
        simulationParameters[0].restDensity = restDensity;
        simulationParameters[0].artificialViscosity = artificialViscosity;
        simulationParameters[0].velocityLimit = velocityLimit;
        simulationParameters[0].worldOrigin = worldOrigin;
        simulationParameters[0].gridSize = gridSize;
        simulationParameters[0].boxSize = boxSize;
        simulationParameters[0].enableBox = useBox ? 1 : 0;
        simulationParameters[0].numCells = gridSize.x * gridSize.y * gridSize.z;
        float cellWidth = 2.0f * smoothingLength;
        simulationParameters[0].cellSize = new Vector3(cellWidth, cellWidth, cellWidth);
        simulationParameters[0].coeffWeightedVolume = GPUKernel.KernelM4.ComputeKernelConstant(simulationParameters[0].smoothingLength);
        simulationParameters[0].coeffDensity = GPUKernel.KernelPoly6.ComputeKernelConstant(simulationParameters[0].smoothingLength);
        simulationParameters[0].coeffPressure = GPUKernel.KernelSpiky.ComputeGradientConstant(simulationParameters[0].smoothingLength);
        simulationParameters[0].coeffViscosity = GPUKernel.KernelViscosity.ComputeLaplacianConstant(simulationParameters[0].smoothingLength);
        
        simulationParameters[0].coeffCSKernel = GPUKernel.KernelCubicSpline.ComputeKernelConstant(simulationParameters[0].smoothingLength);
        simulationParameters[0].coeffCSGradient = GPUKernel.KernelCubicSpline.ComputeGradientConstant(simulationParameters[0].smoothingLength);
    }

    public void AddParticle(Vector3 pos, Vector3 vel, Vector3 force, Vector4 tgrm)
    {
        addPosPress.Add(new Vector4(pos.x, pos.y, pos.z, 0.0f));
        addVelRho.Add(new Vector4(vel.x, vel.y, vel.z, 0.0f));
        addForceVol.Add(new Vector4(force.x, force.y, force.z, 0.0f));
        addTGRM.Add(tgrm);
    }

    private void ProcessAddParticles()
    {
        if(addPosPress.Count > 0)
        {
            bufAddPosPress.SetData(addPosPress.ToArray());
            bufAddVelRho.SetData(addVelRho.ToArray());
            bufAddForceVol.SetData(addForceVol.ToArray());
            bufAddTGRM.SetData(addTGRM.ToArray());

            simulationShader.SetInt("num_additional_particles", addPosPress.Count);
            simulationShader.SetBuffer(kAddParticles, "parameters", bufSimulationParameters);
            // Current particle buffer
            simulationShader.SetBuffer(kAddParticles, "pos_press", bufPosPress[0]);
            simulationShader.SetBuffer(kAddParticles, "vel_rho", bufVelRho[0]);
            simulationShader.SetBuffer(kAddParticles, "force_vol", bufForceVol[0]);
            simulationShader.SetBuffer(kAddParticles, "tgrm", bufTGRM[0]);
            // Additional particles
            simulationShader.SetBuffer(kAddParticles, "add_pos_press", bufAddPosPress);
            simulationShader.SetBuffer(kAddParticles, "add_vel_rho", bufAddVelRho);
            simulationShader.SetBuffer(kAddParticles, "add_force_vol", bufAddForceVol);
            simulationShader.SetBuffer(kAddParticles, "add_tgrm", bufAddTGRM);
            simulationShader.Dispatch(kAddParticles, addPosPress.Count / BLOCK_SIZE + 1, 1, 1);
            simulationParameters[0].numParticles += addPosPress.Count;
            numParticles = simulationParameters[0].numParticles;

            addPosPress.Clear();
            addVelRho.Clear();
            addForceVol.Clear();
            addTGRM.Clear();
        }
    }

    private void ProcessColliders()
    {
        GPUColliderBase.UpdateAll();
        simulationParameters[0].numSphereColliders = sphereColliders.Count;
        simulationParameters[0].numCapsuleColliders = capsuleColliders.Count;
        simulationParameters[0].numBoxColliders = boxColliders.Count;

        bufSphereColliders.SetData(sphereColliders.ToArray());
        bufCapsuleColliders.SetData(capsuleColliders.ToArray());
        bufBoxColliders.SetData(boxColliders.ToArray());

        sphereColliders.Clear();
        capsuleColliders.Clear();
        boxColliders.Clear();
    }

    private void ComputeGridHash()
    {
        // Clear cells
        int kernel = 0;
        int numBlocks = (int) ComputeGroupSize(simulationParameters[0].numCells, BLOCK_SIZE);
        computeGridShader.SetBuffer(kernel, "cells_rw", bufCells);
        computeGridShader.Dispatch(kernel, numBlocks, 1, 1);

        // Compute (hash, index)
        kernel = 1;
        numBlocks = (int) ComputeGroupSize((uint) simulationParameters[0].maxParticles, BLOCK_SIZE);
        computeGridShader.SetBuffer(kernel, "parameters", bufSimulationParameters);
        computeGridShader.SetBuffer(kernel, "pos_press", bufPosPress[0]);
        computeGridShader.SetBuffer(kernel, "vel_rho", bufVelRho[0]);
        computeGridShader.SetBuffer(kernel, "force_vol", bufForceVol[0]);
        computeGridShader.SetBuffer(kernel, "tgrm", bufTGRM[0]);
        computeGridShader.SetBuffer(kernel, "sort_data_rw", bufSortData[0]);
        computeGridShader.Dispatch(kernel, numBlocks, 1, 1);

        // Sort (hash-index) using bitonic sort
        int roundUpPowerOf2 = (int) Mathf.Pow(2, Mathf.Ceil(Mathf.Log(simulationParameters[0].maxParticles) / Mathf.Log(2.0f)));

        bitonicSort.BitonicSort(bufSortData[0], bufSortData[1], (uint) roundUpPowerOf2);

        // Reorder particle data and compute cell data
        kernel = 2;
        numBlocks = (int) ComputeGroupSize((uint) simulationParameters[0].numParticles, BLOCK_SIZE);
        computeGridShader.SetBuffer(kernel, "parameters", bufSimulationParameters);
        computeGridShader.SetBuffer(kernel, "pos_press", bufPosPress[0]);
        computeGridShader.SetBuffer(kernel, "vel_rho", bufVelRho[0]);
        computeGridShader.SetBuffer(kernel, "force_vol", bufForceVol[0]);
        computeGridShader.SetBuffer(kernel, "tgrm", bufTGRM[0]);
        computeGridShader.SetBuffer(kernel, "pos_press_rw", bufPosPress[1]);
        computeGridShader.SetBuffer(kernel, "vel_rho_rw", bufVelRho[1]);
        computeGridShader.SetBuffer(kernel, "force_vol_rw", bufForceVol[1]);
        computeGridShader.SetBuffer(kernel, "tgrm_rw", bufTGRM[1]);
        computeGridShader.SetBuffer(kernel, "sort_data", bufSortData[0]);
        computeGridShader.SetBuffer(kernel, "cells_rw", bufCells);
        computeGridShader.Dispatch(kernel, numBlocks, 1, 1);
    }

    private void ComputeWCSPH()
    {
        // Compute weighted volume for particles
        int numBlocks = (int) ComputeGroupSize((uint)simulationParameters[0].numParticles, BLOCK_SIZE);

        simulationShader.SetBuffer(kComputeWeightedVolume, "parameters", bufSimulationParameters);
        simulationShader.SetBuffer(kComputeWeightedVolume, "pos_press", bufPosPress[1]);
        simulationShader.SetBuffer(kComputeWeightedVolume, "tgrm", bufTGRM[1]);
        simulationShader.SetBuffer(kComputeWeightedVolume, "force_vol", bufForceVol[1]);
        simulationShader.SetBuffer(kComputeWeightedVolume, "cells", bufCells);
        simulationShader.Dispatch(kComputeWeightedVolume, numBlocks, 1, 1);

        // Compute density and pressure for particles
        simulationShader.SetBuffer(kComputeDensityPressureWCSPH, "parameters", bufSimulationParameters);
        simulationShader.SetBuffer(kComputeDensityPressureWCSPH, "cells", bufCells);
        simulationShader.SetBuffer(kComputeDensityPressureWCSPH, "pos_press", bufPosPress[1]);
        simulationShader.SetBuffer(kComputeDensityPressureWCSPH, "vel_rho", bufVelRho[1]);
        simulationShader.SetBuffer(kComputeDensityPressureWCSPH, "force_vol", bufForceVol[1]);
        simulationShader.SetBuffer(kComputeDensityPressureWCSPH, "tgrm", bufTGRM[1]);
        simulationShader.Dispatch(kComputeDensityPressureWCSPH, numBlocks, 1, 1);

        // Solve Navier-Stokes
        simulationShader.SetBuffer(kComputeForceWCSPH, "parameters", bufSimulationParameters);
        simulationShader.SetBuffer(kComputeForceWCSPH, "cells", bufCells);
        simulationShader.SetBuffer(kComputeForceWCSPH, "pos_press", bufPosPress[1]);
        simulationShader.SetBuffer(kComputeForceWCSPH, "vel_rho", bufVelRho[1]);
        simulationShader.SetBuffer(kComputeForceWCSPH, "force_vol", bufForceVol[1]);
        simulationShader.SetBuffer(kComputeForceWCSPH, "tgrm", bufTGRM[1]);
        simulationShader.SetBuffer(kComputeForceWCSPH, "vel_verlet", bufVelVerlet);
        simulationShader.Dispatch(kComputeForceWCSPH, numBlocks, 1, 1);

        // Add boundary force from virtual boundary and colliders
        

        // Integration
        simulationShader.SetBuffer(kIntegration, "parameters", bufSimulationParameters);
        simulationShader.SetBuffer(kIntegration, "sort_data", bufSortData[0]);
        simulationShader.SetBuffer(kIntegration, "pos_press", bufPosPress[1]);
        simulationShader.SetBuffer(kIntegration, "vel_rho", bufVelRho[1]);
        simulationShader.SetBuffer(kIntegration, "force_vol", bufForceVol[1]);
        simulationShader.SetBuffer(kIntegration, "tgrm", bufTGRM[1]);
        simulationShader.SetBuffer(kIntegration, "vel_verlet", bufVelVerlet);
        simulationShader.SetBuffer(kIntegration, "pos_press_dest", bufPosPress[0]);
        simulationShader.SetBuffer(kIntegration, "vel_rho_dest", bufVelRho[0]);
        simulationShader.SetBuffer(kIntegration, "force_vol_dest", bufForceVol[0]);
        simulationShader.SetBuffer(kIntegration, "tgrm_dest", bufTGRM[0]);
        simulationShader.SetBuffer(kIntegration, "sphere_colliders", bufSphereColliders);
        simulationShader.SetBuffer(kIntegration, "capsule_colliders", bufCapsuleColliders);
        simulationShader.SetBuffer(kIntegration, "box_colliders", bufBoxColliders);
        simulationShader.Dispatch(kIntegration, numBlocks, 1, 1);

    }

    uint ComputeGroupSize(uint a, uint b)
    {
        return (a % b != 0) ? (a / b + 1) : (a / b);
    }

    void Step()
    {
        ProcessColliders();

        // New particles to buffer
        ProcessAddParticles();

        SettingSimulationParameters();
        bufSimulationParameters.SetData(simulationParameters);

        // Update particle dynamics
        if(simulationParameters[0].numParticles > 0)
        {
            ComputeGridHash();
            ComputeWCSPH();
        }
    }

    private void FixedUpdate() {
        float dt = Time.fixedDeltaTime / iterations;
        timestep = dt;
        for (int i = 0; i < iterations; i++) {
            Step();
        }
    }
}
