namespace Kit.Blog;

public sealed class Index
{
    public IEnumerable<Post> IndexPagePosts { get; set; } = null!;
}