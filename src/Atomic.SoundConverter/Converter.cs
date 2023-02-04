using NAudio.Wave;

namespace Atomic.SoundConverter;

public class Converter
{
    public void Convert(string gameBaseDir, string outputDir, string[] onlyIncludeFileNames = null)
    {
        var format = new WaveFormat(44100, 1);
        var dir = @$"{gameBaseDir}\DATA\SOUND";
        
        foreach (var filePath in Directory
                     .GetFiles(dir).Where(f => Path.GetExtension(f) == ".RSS")
                     .Where(f => onlyIncludeFileNames == null || onlyIncludeFileNames.Contains(Path.GetFileName(f)) || onlyIncludeFileNames.Contains(Path.GetFileName(f).Replace(".RSS", ""))))
        {
            var info = new FileInfo(filePath);
            
            if(info.Length == 0)
                continue;
            
            var outFile = @$"{outputDir}\{info.Name.Replace(".RSS", ".ogg")}";
            using var s = new RawSourceWaveStream(File.OpenRead(filePath), format);
            
            OggVorbis.WriteToFile(StreamToByteArray(s), outFile);
        }
    }

    private static byte[] StreamToByteArray(Stream input)
    {
        var ms = new MemoryStream();
        input.CopyTo(ms);
        return ms.ToArray();
    }
    
}