namespace Kit.Blog;

public sealed class Post
{
    public string Content { get; set; } = null!;
    public string Route { get; set; } = null!;
    public PostFrontMatter FrontMatter { get; set; } = null!;
}