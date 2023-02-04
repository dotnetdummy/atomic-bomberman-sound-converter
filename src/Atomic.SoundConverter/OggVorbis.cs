using OggVorbisEncoder;

namespace Atomic.SoundConverter;

public static class OggVorbis
{
    private const int WriteBufferSize = 512;

    private enum PcmSample
    {
        EightBit = 1,
        SixteenBit = 2
    }

    public static void WriteToFile(byte[] bytes, string filePath)
    {
        var oggBytes = ConvertRawPcmFile(44100, 1, bytes, PcmSample.SixteenBit, 44100, 1);
        File.WriteAllBytes(filePath, oggBytes);
    }
    
    private static byte[] ConvertRawPcmFile(int outputSampleRate, int outputChannels, IReadOnlyList<byte> pcmSamples, PcmSample pcmSampleSize, int pcmSampleRate, int pcmChannels)
    {
        var numPcmSamples = (pcmSamples.Count / (int)pcmSampleSize / pcmChannels);
        var pcmDuration = numPcmSamples / (float)pcmSampleRate;

        var numOutputSamples = (int)(pcmDuration * outputSampleRate);
        numOutputSamples = (numOutputSamples / WriteBufferSize) * WriteBufferSize;

        var outSamples = new float[outputChannels][];

        for (var ch = 0; ch < outputChannels; ch++)
        {
            outSamples[ch] = new float[numOutputSamples];
        }

        for (var sampleNumber = 0; sampleNumber < numOutputSamples; sampleNumber++)
        {
            var rawSample = 0.0f;

            for (var ch = 0; ch < outputChannels; ch++)
            {
                var sampleIndex = (sampleNumber * pcmChannels) * (int)pcmSampleSize;

                if (ch < pcmChannels) sampleIndex += (ch * (int)pcmSampleSize);

                rawSample = pcmSampleSize switch
                {
                    PcmSample.EightBit => ByteToSample(pcmSamples[sampleIndex]),
                    PcmSample.SixteenBit => ShortToSample((short) (pcmSamples[sampleIndex + 1] << 8 |
                                                                   pcmSamples[sampleIndex])),
                    _ => rawSample
                };

                outSamples[ch][sampleNumber] = rawSample;
            }
        }

        return GenerateFile(outSamples, outputSampleRate, outputChannels);
    }
    
    private static float ByteToSample(short pcmValue)
    {
        return pcmValue / 128f;
    }

    private static float ShortToSample(short pcmValue)
    {
        return pcmValue / 32768f;
    }

    private static byte[] GenerateFile(float[][] floatSamples, int sampleRate, int channels)
    {
        using var outputData = new MemoryStream();
        var info = VorbisInfo.InitVariableBitRate(channels, sampleRate, 0.5f);
        var serial = new Random().Next();
        var oggStream = new OggStream(serial);

        // =========================================================
        // HEADER
        // =========================================================
        // Vorbis streams begin with three headers; the initial header (with
        // most of the codec setup parameters) which is mandated by the Ogg
        // bitstream spec.  The second header holds any comment fields.  The
        // third header holds the bitstream codebook.

        var comments = new Comments();
        //comments.AddTag("ARTIST", "TEST");

        var infoPacket = HeaderPacketBuilder.BuildInfoPacket(info);
        var commentsPacket = HeaderPacketBuilder.BuildCommentsPacket(comments);
        var booksPacket = HeaderPacketBuilder.BuildBooksPacket(info);

        oggStream.PacketIn(infoPacket);
        oggStream.PacketIn(commentsPacket);
        oggStream.PacketIn(booksPacket);

        // Flush to force audio data onto its own page per the spec
        FlushPages(oggStream, outputData, true);

        // =========================================================
        // BODY (Audio Data)
        // =========================================================
        var processingState = ProcessingState.Create(info);

        for (var readIndex = 0; readIndex <= floatSamples[0].Length; readIndex += WriteBufferSize)
        {
            if (readIndex == floatSamples[0].Length)
            {
                processingState.WriteEndOfStream();
            }
            else
            {
                processingState.WriteData(floatSamples, WriteBufferSize, readIndex);
            }

            while (!oggStream.Finished && processingState.PacketOut(out var packet))
            {
                oggStream.PacketIn(packet);

                FlushPages(oggStream, outputData, false);
            }
        }

        FlushPages(oggStream, outputData, true);

        return outputData.ToArray();
    }
    
    private static void FlushPages(OggStream oggStream, Stream output, bool force)
    {
        while (oggStream.PageOut(out var page, force))
        {
            output.Write(page.Header, 0, page.Header.Length);
            output.Write(page.Body, 0, page.Body.Length);
        }
    }
}