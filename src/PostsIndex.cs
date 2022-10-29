namespace Kit.Blog;

public sealed class PostsIndex
{
    public IEnumerable<Post> AllPosts { get; set; } = null!;
}