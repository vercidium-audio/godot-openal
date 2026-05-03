using System.Threading.Tasks;

namespace godot_openal;

public class ALBuffer
{
    string fileName;
    Task loadingTask;

    SoundData data;
    uint handle;
    public int Duration => data.duration;
    public int SampleRate => data.sampleRate;
    public int Format => data.format;

    public ALBuffer(ALContext context, string fileName)
    {
        this.fileName = fileName;
        loadingTask = Task.Run(() => Load(context));
    }

    void Load(ALContext context)
    {
        if (SoundLoader.CancelLoadingSounds)
            return;

        data = SoundLoader.Load(fileName);

        // Bail if we're changing audio devices
        if (SoundLoader.CancelLoadingSounds)
            return;

        // Bail if it failed to load (corrupt or unsupported sound file)
        if (data == null || data.byteCount == 0)
        {
            LogWarning($"Cannot buffer data for {fileName} as no data was loaded from disk");
            return;
        }

        context.MakeCurrent();
        BufferOpenALData();
    }

    public void WaitForTask()
    {
        loadingTask?.Wait();
    }

    public unsafe void BufferOpenALData()
    {
        // Create a buffer and copy raw PCM data to it
        Debug.Assert(handle == 0);
        handle = AL.GenBuffer();

        if (data.byteData != null)
        {
            fixed (byte* bytePtr = data.byteData)
            {
                AL.BufferData(handle, data.format, (nint)bytePtr, data.byteCount, data.sampleRate);
            }
        }
        else
        {
            fixed (short* shortPtr = data.shortData)
            {
                AL.BufferData(handle, data.format, (nint)shortPtr, data.byteCount, data.sampleRate);
            }
        }

        // Free memory
        data.byteData = null;
        data.shortData = null;
    }

    public bool TryCreateSource(bool spatialised, out ALSource source)
    {
        // Bail if this buffer is still loading, or we failed to initialise OpenAL
        if (handle == 0)
        {
            source = null;
            return false;
        }

        var sourceID = AL.GenSource();

        // Out of memory
        if (sourceID == 0)
        {
            LogWarning($"Failed to create source - likely too many sources have been created");
            source = null;
            return false;
        }

        source = new ALSource(sourceID);
        source.SetBuffer(handle);
        source.SetRelative(!spatialised);
        source.SetSpatialise(spatialised);

        return true;
    }

    public void Dispose()
    {
        // Already disposed, or loading was cancelled
        if (handle == 0)
            return;

        AL.DeleteBuffer(handle);
        handle = 0;
    }
}