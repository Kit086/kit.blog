using YamlDotNet.Serialization;

namespace Kit.Blog;

public sealed class PostFrontMatter
{
    [YamlMember(Alias = "title")] public string Title { get; set; } = null!;

    [YamlMember(Alias = "slug")] public string Slug { get; set; } = null!;

    [YamlMember(Alias = "create_time")] public DateTime CreateTime { get; set; }

    [YamlMember(Alias = "last_updated")] public DateTime LastUpdatedTime { get; set; }

    [YamlMember(Alias = "description")] public string Description { get; set; } = null!;

    [YamlMember(Alias = "tags")] public string[] Tags { get; set; } = Array.Empty<string>();
}