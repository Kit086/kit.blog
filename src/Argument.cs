using System.ComponentModel.DataAnnotations;

namespace Kit.Blog;

public sealed class Argument
{
    public string OutputDir { get; set; } = null!;
    
    public string PostsDir { get; set; } = null!;
    
    public string TemplatesDir { get; set; } = null!;
    
    public string StaticFilesDir { get; set; } = null!;

    public string PostsOutputDir => Path.Combine(OutputDir, "posts") + Path.DirectorySeparatorChar;
}