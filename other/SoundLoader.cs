using NAudio.Wave;
using System.IO;

namespace godot_openal;

public static class SoundLoader
{
    public static bool CancelLoadingSounds;

    public static SoundData Load(string fileName)
    {
        if (CancelLoadingSounds)
            return null;

        try
        {
            // Open the file on this background thread
            using var stream = new GodotFileStream(Godot.FileAccess.Open(fileName, Godot.FileAccess.ModeFlags.Read));

            // Convert each format to PCM data
            var extension = fileName.Split('.')[^1];

            if (extension == "ogg")
            {
                return LoadOgg(stream);
            }
            else if (extension == "wav")
            {
                return LoadWav(stream);
            }
            else
            {
                return LoadMp3OrFlac(extension, stream);
            }
        }
        catch (Exception e)
        {
            Debug.Assert(false);
            LogError($"Failed to load sound: {fileName}. Error: {e}");

            return new SoundData();
        }
    }

    static SoundData LoadMp3OrFlac(string extension, GodotFileStream stream)
    {
        WaveStream reader;

        if (extension == "mp3")
            reader = new Mp3FileReader(stream);
        else
            reader = new NAudio.Flac.FlacReader(stream);

        // Mp3
        WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(reader);

        var channelCount = pcm.WaveFormat.Channels;
        var bitDepth = pcm.WaveFormat.BitsPerSample;
        var sampleRate = pcm.WaveFormat.SampleRate;

        var bytesPerSecond = channelCount * sampleRate * bitDepth / 8;
        var duration = (int)(pcm.Length / bytesPerSecond);

        byte[] data = new byte[pcm.Length];

        pcm.Read(data, 0, data.Length);

        return new SoundData()
        {
            format = AL.GetSoundFormat(channelCount, bitDepth),
            byteData = data,
            byteCount = data.Length,
            sampleRate = sampleRate,
            duration = duration,
        };
    }

    static SoundData LoadWav(GodotFileStream stream)
    {
        using BinaryReader reader = new(stream);

        // Read WAV file header fields
        var chunkID = reader.ReadBytes(4);
        var chunkSize = reader.ReadUInt32();
        var format = reader.ReadBytes(4);
        var formatChunkID = reader.ReadBytes(4);
        var formatChunkSize = reader.ReadUInt32();
        var audioFormat = reader.ReadUInt16();
        var channelCount = reader.ReadUInt16();
        var sampleRate = reader.ReadUInt32();
        var byteRate = reader.ReadUInt32();
        var blockAlign = reader.ReadUInt16();
        var bitDepth = reader.ReadUInt16();

        // Skip any extra bytes in the format chunk (e.g., extension size)
        if (formatChunkSize > 16)
        {
            reader.ReadBytes((int)(formatChunkSize - 16));
        }

        // Search for the data chunk
        byte[] datachunkID;
        uint datachunkSize;
        while (true)
        {
            datachunkID = reader.ReadBytes(4);
            datachunkSize = reader.ReadUInt32();

            // Check if this is the data chunk
            if (datachunkID[0] == 'd' && datachunkID[1] == 'a' &&
                datachunkID[2] == 't' && datachunkID[3] == 'a')
            {
                break;
            }

            // Skip this chunk and continue searching
            reader.ReadBytes((int)datachunkSize);
        }

        var chunkIDString = System.Text.Encoding.ASCII.GetString(chunkID);
        var formatString = System.Text.Encoding.ASCII.GetString(format);

        if (chunkIDString != "RIFF")
            throw new Exception($"Invalid chunk ID: {chunkIDString}. Expected: RIFF");

        if (formatString != "WAVE")
            throw new Exception($"Invalid format: {formatString}. Expected: WAVE");

        if (audioFormat != 1)
            throw new Exception($"Invalid audio format: {audioFormat}. Expected: 1 (PCM)");

        var data = reader.ReadBytes((int)datachunkSize);
        var sampleCount = datachunkSize / channelCount / (bitDepth / 8);

        // Duration is in milliseconds
        var duration = (int)(sampleCount / sampleRate) * 1000;

        return new SoundData()
        {
            format = AL.GetSoundFormat(channelCount, bitDepth),
            byteData = data,
            byteCount = data.Length,
            sampleRate = (int)sampleRate,
            duration = duration,
        };
    }

    static SoundData LoadOgg(GodotFileStream stream)
    {
        using var vorbis = new NVorbis.VorbisReader(stream);

        // Get the channels & sample rate
        var channels = vorbis.Channels;
        var sampleRate = vorbis.SampleRate;
        int bitDepth = 16;


        long totalSamplesLong = vorbis.TotalSamples * vorbis.Channels;

        // Overflow check: sound is too long to load
        if (totalSamplesLong > int.MaxValue)
        {
            Debug.Assert(false);
            return null;
        }


        // Convert OGG to normal samples
        var totalSamples = (int)totalSamplesLong;
        float[] readBuffer = new float[totalSamples];

        vorbis.ReadSamples(readBuffer, 0, totalSamples);

        // Convert floats to shorts
        var shortBuffer = new short[totalSamples];

        for (int i = 0; i < totalSamples; i++)
            shortBuffer[i] = (short)(readBuffer[i] * short.MaxValue);


        // Return all data about this sound
        var format = AL.GetSoundFormat(channels, bitDepth);
        var duration = (int)vorbis.TotalTime.TotalMilliseconds;

        return new SoundData()
        {
            format = format,
            shortData = shortBuffer,
            byteCount = totalSamples * 2,
            sampleRate = sampleRate,
            duration = duration,
        };
    }
}
