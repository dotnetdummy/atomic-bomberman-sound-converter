using System.IO;
using System.Linq;
using Xunit;

namespace Atomic.SoundConverter.Tests;

public class ConverterShould
{
    [Fact]
    public void ConvertMenuSoundsToOgg()
    {
        var converter = new Converter();
        var gameBaseDir = @"D:\Games\Atomic Bomberman";
        var outputDir = "menu-sounds";
        var menuSounds = new[] { "MENU.RSS", "MENUEXIT.RSS" };
        
        if(Directory.Exists(outputDir))
            Directory.Delete(outputDir, true);

        Directory.CreateDirectory(outputDir);
        
        converter.Convert(gameBaseDir, outputDir, menuSounds);

        var files = Directory.GetFiles(outputDir).Where(f => Path.GetExtension(f) == ".ogg");
        
        Assert.Equal(2, files.Count());
    }
    
    [Fact]
    public void ConvertAllSoundFilesInGameDir()
    {
        var converter = new Converter();
        var gameBaseDir = @"D:\Games\Atomic Bomberman";
        var outputDir = "all-sounds";
        
        if(Directory.Exists(outputDir))
            Directory.Delete(outputDir, true);
        
        Directory.CreateDirectory(outputDir);
        
        converter.Convert(gameBaseDir, outputDir);

        var files = Directory.GetFiles(outputDir).Where(f => Path.GetExtension(f) == ".ogg");
        
        Assert.NotEmpty(files);
    }
}