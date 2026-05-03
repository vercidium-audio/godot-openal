namespace godot_openal;

public class GodotSound
{
    public List<ALBuffer> buffers = [];

    public void WaitForTasks()
    {
        foreach (var v in buffers)
            v.WaitForTask();
    }

    public void Dispose()
    {
        foreach (var v in buffers)
            v.Dispose();
    }

    static Random random = new();
    
    // TODO - round-robin selection
    ALBuffer RandomBuffer => buffers[random.Next(buffers.Count)];
    
    public bool TryCreateSource(bool spatialised, out ALSource source) => RandomBuffer.TryCreateSource(spatialised, out source);
    public bool TryCreateSource(bool spatialised, int index, out ALSource source) => buffers[index].TryCreateSource(spatialised, out source);
}