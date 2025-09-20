namespace TrafficSimulation.Core.Editor;

public sealed class EditorResource<T>(string path) where T : Object {
    public T? Value => OrNull(ref field) ??= EditorResources.Load<T>(path);
}
