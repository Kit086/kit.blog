namespace Kit.Blog;

public sealed class Page
{
    public string Title { get; set; } = null!;
    public string H1 { get; set; } = null!;
    public string FilePath { get; set; } = null!;
}

public sealed class Page<T> where T : class
{
    public string Title { get; set; } = null!;
    public string H1 { get; set; } = null!;
    public T? ContentModel { get; set; }
    public string FilePath { get; set; } = null!;
}