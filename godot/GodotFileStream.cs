using System.IO;

namespace godot_openal;

public class GodotFileStream : Stream
{
    Godot.FileAccess file;

    public GodotFileStream(Godot.FileAccess file)
    {
        this.file = file;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => (long)file.GetLength();
    public override long Position
    {
        get => (long)file.GetPosition();
        set => file.Seek((ulong)value);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var data = file.GetBuffer(count);
        Array.Copy(data, 0, buffer, offset, data.Length);
        return data.Length;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                file.Seek((ulong)offset);
                break;
            case SeekOrigin.Current:
                file.Seek(file.GetPosition() + (ulong)offset);
                break;
            case SeekOrigin.End:
                file.Seek(file.GetLength() - (ulong)offset);
                break;
        }
        return (long)file.GetPosition();
    }

    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            file?.Dispose();

        base.Dispose(disposing);
    }
}