namespace Sia_Asset_Examples;

using System.IO;
using System.Text;
using Sia;
using Sia.Asset;

public static partial class Example1_TextAsset
{
    public record struct TextLoadOptions(Encoding Encoding)
    {
        public static readonly TextLoadOptions UTF8 = new(Encoding.UTF8);
    }

    public record RText
        : AssetRecord
        , ILoadable<RText>
        , ILoadable<RText, TextLoadOptions>
    {
        public required string Content { get; init; }

        public static RText Load(Stream stream, string? name = null)
            => Load(stream, TextLoadOptions.UTF8, name);

        public static RText Load(Stream stream, TextLoadOptions options, string? name = null)
            => new() {
                Name = name,
                Content = new StreamReader(stream, options.Encoding).ReadToEnd()
            };
        
        public static implicit operator string(RText text) => text.Content;
    }

    public static void Run(World world)
    {
        var text = EmbeddedLoader.LoadInternal(TypedPath<RText>.From("example1.txt"));
        Console.WriteLine(text);
    }
}