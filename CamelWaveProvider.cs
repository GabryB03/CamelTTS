using NAudio.Dsp;
using NAudio.Wave;

public class CamelWaveProvider : ISampleProvider
{
    private readonly ISampleProvider sourceProvider;
    public WaveFormat WaveFormat => sourceProvider.WaveFormat;
    private BiQuadFilter _filter = BiQuadFilter.HighPassFilter(22050, 120, 1.0F);
    private bool doThings;

    public CamelWaveProvider(ISampleProvider sourceProvider, bool doIt)
    {
        this.sourceProvider = sourceProvider;
        this.doThings = doIt;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = sourceProvider.Read(buffer, offset, count);

        if (doThings)
        {
            for (int i = 0; i < samplesRead; i++)
            {
                buffer[i] = _filter.Transform(buffer[i]);
            }
        }

        return samplesRead;
    }
}