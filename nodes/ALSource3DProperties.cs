namespace godot_openal;

public partial class ALSource3D : Node3D
{
    float _volume = 1;
    float _pitch = 1;
    float _maxDistance = 100;
    float _referenceDistance = 8;
    bool _looping = false;
    bool _relative = false;
    string _soundName;

    void UpdateProperty<T>(ref T field, T value, Action<T, ALSource> updateAction) where T : struct
    {
        if (!field.Equals(value))
        {
            field = value;

            if (updateAction != null)
                foreach (var s in sources)
                    updateAction.Invoke(value, s);
        }
    }

    [Export(PropertyHint.Range, "0.0,10.0")]
    public float Volume
    {
        get => _volume;
        set => UpdateProperty(ref _volume, value, (v, source) => source.SetGain(v));
    }

    [Export(PropertyHint.Range, "0.0,10.0")]
    public float Pitch
    {
        get => _pitch;
        set => UpdateProperty(ref _pitch, value, (v, source) => source.SetPitch(v));
    }

    [Export]
    public float MaxDistance
    {
        get => _maxDistance;
        set => UpdateProperty(ref _maxDistance, value, (v, source) => source.SetMaxDistance(v));
    }

    [Export]
    public float ReferenceDistance
    {
        get => _referenceDistance;
        set => UpdateProperty(ref _referenceDistance, value, (v, source) => source.SetReferenceDistance(v));
    }

    [Export]
    public bool Looping
    {
        get => _looping;
        set => UpdateProperty(ref _looping, value, (v, source) => source.SetLooping(v));
    }

    [Export]
    public bool Relative
    {
        get => _relative;
        set => UpdateProperty(ref _relative, value, (v, source) => source.SetRelative(v));
    }

    [Export]
    public string SoundName
    {
        get => _soundName;
        set
        {
            _soundName = value;
            UpdateConfigurationWarnings();
        }
    }
}
