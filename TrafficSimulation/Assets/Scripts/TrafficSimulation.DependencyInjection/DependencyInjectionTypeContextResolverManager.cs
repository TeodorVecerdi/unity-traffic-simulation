using TrafficSimulation.Core;
using TrafficSimulation.Sim;
using TrafficSimulation.UI;
using UnityEngine;
using Vecerdi.Extensions.DependencyInjection;
using Vecerdi.Extensions.DependencyInjection.Infrastructure;

namespace TrafficSimulation.DependencyInjection;

internal static class DependencyInjectionTypeContextResolverManager {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void InitializeTypeInjectorResolver() {
        ServiceManager.Resolver = new TypeInjectorResolverCombiner(
            new TrafficSimulationCoreTypeInjectorResolverContext(),
            new TrafficSimulationUITypeInjectorResolverContext(),
            new TrafficSimulationSimTypeInjectorResolverContext(),
            new ReflectionTypeInjectorResolver()
        );
    }
}
