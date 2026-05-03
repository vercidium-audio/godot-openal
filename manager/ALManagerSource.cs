namespace godot_openal;

public unsafe partial class ALManager
{
    HashSet<string> loggedWarnings = [];

    public virtual bool TryCreateSource(string name, bool spatialised, out ALSource source)
    {
        if (!GlobalSoundStorage.TryGetValue(name, out var sound))
        {
            // Trying to play a sound we never loaded from disk
            if (!loggedWarnings.Contains(name))
            {
                LogWarning($"Unable to play sound {name} as it does not exist.");
                loggedWarnings.Add(name);
            }

            source = null;
            return false;
        }

        return sound.TryCreateSource(spatialised, out source);
    }
}