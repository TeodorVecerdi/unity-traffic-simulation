using TrafficSimulation.Sim.Components;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace TrafficSimulation.Sim.Jobs;

[BurstCompile]
public struct UpdateTrafficLightGroupsJob : IJobParallelFor {
    public float DeltaTime;
    [ReadOnly] public NativeArray<TrafficLightGroupParameters> GroupParameters;
    public NativeArray<TrafficLightGroupState> GroupStates;

    public void Execute(int index) {
        Hint.Assume((uint)index < (uint)GroupStates.Length);
        Hint.Assume((uint)index < (uint)GroupParameters.Length);
        Hint.Assume(GroupStates.Length == GroupParameters.Length);

        var state = GroupStates[index];
        var parameters = GroupParameters[index];
        var total = parameters.TotalCycleSeconds;
        state.TimeInCycleSeconds += DeltaTime;
        if (total > 0.0f && state.TimeInCycleSeconds >= total) {
            state.TimeInCycleSeconds -= total * math.floor(state.TimeInCycleSeconds / total);
        }

        GroupStates[index] = state;
    }
}
