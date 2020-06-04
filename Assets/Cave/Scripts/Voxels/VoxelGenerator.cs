using Unity.Jobs;

public static class VoxelGeneratorFactory
{
    public static VoxelGenerator CreateInstance(VoxelSettings settings)
    {
        return new VoxelGeneratorNoise(settings);
    }
}

public class VoxelGenerator : System.IDisposable
{
    public VoxelVolume Volume { get; protected set; }
    public bool IsCompleted => jobHandle.IsCompleted;

    protected VoxelSettings settings;
    protected JobHandle jobHandle;

    public VoxelGenerator(VoxelSettings settings)
    {
        this.settings = settings;
        Volume = new VoxelVolume(settings);
    }

    public virtual void Schedule()
    {
    }

    public virtual void Complete()
    {
        jobHandle.Complete();
    }

    public virtual void Dispose()
    {
        Volume.Dispose();
    }
}
