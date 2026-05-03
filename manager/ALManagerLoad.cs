using System.Linq;

namespace godot_openal;

public unsafe partial class ALManager
{
    Dictionary<string, GodotSound> GlobalSoundStorage = [];

    List<string> allFileNames;

    void LoadAudioFiles()
    {
        Debug.Assert(allFileNames == null);
        allFileNames = [];
        LoadAudioFilesRecursive(Constants.AudioPath);
        GroupAndLoadSounds();
    }

    void GroupAndLoadSounds()
    {
        var soundGroups = new Dictionary<string, List<string>>();

        foreach (var path in allFileNames)
        {
            // Get name without digits
            var baseName = RemoveDigits(path[Constants.AudioPath.Length..].TrimStart('/'));

            if (!soundGroups.TryGetValue(baseName, out var list))
                soundGroups[baseName] = list = [];

            list.Add(path);
        }

        allFileNames = null;

        foreach (var kv in soundGroups)
        {
            StoreSound(kv.Key, kv.Value);
        }
    }

    static string RemoveDigits(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return new string([..input.Where(c => !char.IsDigit(c))]);
    }

    void LoadAudioFilesRecursive(string directoryPath)
    {
        using var dir = DirAccess.Open(directoryPath);

        if (dir == null)
        {
            LogError($"Failed to load audio files from folder: {directoryPath}");
            return;
        }

        dir.ListDirBegin();
        string fileOrDirName = dir.GetNext();

        while (fileOrDirName != "")
        {
            if (fileOrDirName == "." || fileOrDirName == "..")
            {
                fileOrDirName = dir.GetNext();
                continue;
            }

            var fullPath = $"{directoryPath}/{fileOrDirName}";

            if (dir.CurrentIsDir())
            {
                // Recursively process subdirectory
                LoadAudioFilesRecursive(fullPath);
            }
            else if (fileOrDirName.EndsWith(".ogg") || fileOrDirName.EndsWith(".wav") || fileOrDirName.EndsWith(".mp3") || fileOrDirName.EndsWith(".flac"))
            {
                if (!Godot.FileAccess.FileExists(fullPath))
                {
                    LogError($"Failed to load sound: {fullPath}");
                    fileOrDirName = dir.GetNext();
                    continue;
                }

                allFileNames.Add(fullPath);
            }

            fileOrDirName = dir.GetNext();
        }

        dir.ListDirEnd();
    }

    void StoreSound(string name, List<string> fileNames)
    {
        // Create the base sound
        var sound = new GodotSound();

        foreach (var fileName in fileNames)
        {
            // Buffers will load PCM data from GodotFileStreams and buffer it to OpenAL on background threads
            var buffer = new ALBuffer(ALContext, fileName);
            sound.buffers.Add(buffer);
        }

        // Store the sound in the global dictionary
        GlobalSoundStorage[name] = sound;
    }
}