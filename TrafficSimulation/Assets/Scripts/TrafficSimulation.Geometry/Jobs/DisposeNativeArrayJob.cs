using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace TrafficSimulation.Geometry.Jobs;

[BurstCompile]
public struct DisposeNativeArrayJob<T> : IJob
    where T : unmanaged {
    [DeallocateOnJobCompletion, ReadOnly] public NativeArray<T> Array;
    public void Execute() { }
}

[BurstCompile]
public struct DisposeNativeArrayJob<T1, T2> : IJob
    where T1 : unmanaged
    where T2 : unmanaged {
    [DeallocateOnJobCompletion, ReadOnly] public NativeArray<T1> Array1;
    [DeallocateOnJobCompletion, ReadOnly] public NativeArray<T2> Array2;
    public void Execute() { }
}

[BurstCompile]
public struct DisposeNativeArrayJob<T1, T2, T3> : IJob
    where T1 : unmanaged
    where T2 : unmanaged
    where T3 : unmanaged {
    [DeallocateOnJobCompletion, ReadOnly] public NativeArray<T1> Array1;
    [DeallocateOnJobCompletion, ReadOnly] public NativeArray<T2> Array2;
    [DeallocateOnJobCompletion, ReadOnly] public NativeArray<T3> Array3;
    public void Execute() { }
}

[BurstCompile]
public struct DisposeNativeArrayJob<T1, T2, T3, T4> : IJob
    where T1 : unmanaged
    where T2 : unmanaged
    where T3 : unmanaged
    where T4 : unmanaged {
    [DeallocateOnJobCompletion, ReadOnly] public NativeArray<T1> Array1;
    [DeallocateOnJobCompletion, ReadOnly] public NativeArray<T2> Array2;
    [DeallocateOnJobCompletion, ReadOnly] public NativeArray<T3> Array3;
    [DeallocateOnJobCompletion, ReadOnly] public NativeArray<T4> Array4;
    public void Execute() { }
}
