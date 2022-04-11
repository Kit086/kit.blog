using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;
using RazorEngineCore;
using Kit.Blog;
using YamlDotNet.Serialization;
using System.Text.Json;
using System.Globalization;
using Microsoft.Extensions.Configuration;

// TODO: home 和 post 页面的 tag 展示

var blogConfig = new BlogConfig
{
    Title = "Kit Lau",
    Author = "Kit Lau",
};

var razorEngine = new RazorEngine();
MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions()
    .UseYamlFrontMatter()
    .Build();
IDeserializer yamlDeserializer = new DeserializerBuilder()
    .IgnoreUnmatchedProperties()
    .Build();

var cmdArgs = new ConfigurationBuilder()
    .AddCommandLine(args)
    .Build();

var cwd = (cmdArgs["cwd"] ?? Directory.GetCurrentDirectory()).TrimEnd('/').TrimEnd('\\');
var distDir = cmdArgs["dist"] ?? $"{cwd}/dist";
var postDir = cmdArgs["posts"] ?? $"{cwd}/posts";
var themeDir = cmdArgs["theme"] ?? $"{cwd}/theme";
var themeTemplateDir = $"{themeDir}/templates";

// Generate post pages
var posts = new List<PostViewModel>(16);
var themePostTemplateFilePath = $"{themeTemplateDir}/post.cshtml";
var themePostTemplateContent = File.ReadAllText(themePostTemplateFilePath);
var postFiles = Directory.GetFiles(postDir, "*", SearchOption.AllDirectories);

foreach (var path in postFiles.AsParallel())
{
    var newPath = path.Replace(postDir, $"{distDir}/posts");
    var postPageDir = Path.GetDirectoryName(newPath)!;
    if (!Directory.Exists(postPageDir))
        Directory.CreateDirectory(postPageDir);

    // Copy others files to dist, just like images
    if (!newPath.EndsWith(".md"))
    {
        File.Copy(path, newPath, true);
        Console.WriteLine("Generated: {0} (copyed)", newPath.Replace(distDir, ""));
        continue;
    }

    var htmlFile = newPath.Replace(".md", ".html");
    var htmlFileName = Path.GetFileName(htmlFile);
    var postRoute = Path.GetDirectoryName(htmlFile.Replace(distDir, ""))!;

    using var writer = new StringWriter();
    var renderer = new HtmlRenderer(writer);
    pipeline.Setup(renderer);
    var mdText = File.ReadAllText(path);
    var document = MarkdownParser.Parse(mdText, pipeline);
    var postFrontMatter = GetPostFrontMatter(document);
    renderer.Render(document);
    writer.Flush();
    var html = writer.ToString();

    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(newPath);
    var parentDir = Directory.GetParent(newPath)!;
    // var isPostPageDir = parentDir.FullName.EndsWith("/posts");
    // var newPostRoute = isPostPageDir ? postRoute + "/" + htmlFileName : postRoute;
    var newPostRoute = postRoute + "/" + htmlFileName;

    var postViewModel = new PostViewModel
    {
        BlogConfig = blogConfig,
        PostContent = html,
        PostRoute = newPostRoute,
        FrontMatter = postFrontMatter
    };

    // Console.WriteLine(JsonSerializer.Serialize(postViewModel));

    posts.Add(postViewModel);

    var template = await razorEngine.CompileAsync(themePostTemplateContent, option => {
        option.AddAssemblyReference(typeof(Util));
    });
    var result = template.Run(postViewModel);

    using StreamWriter swPost = File.CreateText(htmlFile);
    await swPost.WriteAsync(result);
    Console.WriteLine("Generated: {0}/{1}", postRoute, htmlFileName);
}

// Generate index.html
var homeViewModel = new
{
    BlogTitle = blogConfig.Title,
    Author = blogConfig.Author,
    Posts = posts.OrderByDescending(p => p.FrontMatter.LastUpdatedTime).Take(3)
};
await RenderRazorPageAsync($"{themeTemplateDir}/index.cshtml", $"{distDir}/index.html", homeViewModel);
Console.WriteLine("Generated: /index.html");

// Generate 404.html
await RenderRazorPageAsync($"{themeTemplateDir}/404.cshtml", $"{distDir}/404.html");
Console.WriteLine("Generated: /404.html");

// Generate /posts/index.html
var postsIndexViewModel = new {
    Posts = posts.OrderByDescending(p => p.FrontMatter.LastUpdatedTime)
};
await RenderRazorPageAsync($"{themeTemplateDir}/posts.cshtml", $"{distDir}/posts/index.html", postsIndexViewModel);
Console.WriteLine("Generated: /posts/index.html");

// Generate about.html
await RenderRazorPageAsync($"{themeTemplateDir}/about.cshtml", $"{distDir}/about.html");
Console.WriteLine("Generated: /posts/index.html");

// Generate tags pages
var mapTags = new Dictionary<string, IList<PostViewModel>>();
foreach (var post in posts)
{
    if (post.FrontMatter.Tags is null)
        continue;

    foreach (var tagName in post.FrontMatter.Tags)
    {
        if (!mapTags.ContainsKey(tagName))
            mapTags[tagName] = new List<PostViewModel> { post };
        else
            mapTags[tagName].Add(post);
    }
}

foreach (var(tagName, postsWithSameTag) in mapTags)
{
    var model = new
    {
        BlogConfig = blogConfig,
        TagName = tagName,
        Posts = postsWithSameTag
    };
    var newTagRoute = $"/tags/{Util.ReplaceWithspaceByLodash(tagName)}/index.html";
    await RenderRazorPageAsync($"{themeTemplateDir}/tag.cshtml",
        $"{distDir}{newTagRoute}", model);
    Console.WriteLine("Generated: {0}", newTagRoute);
}

// Copy other files in theme directory
var otherThemeFiles = Directory.GetFiles(themeDir, "*", SearchOption.AllDirectories);
foreach (var path in otherThemeFiles.AsParallel())
{
    // Do not copy any files in templates dir
    if (path.StartsWith(themeTemplateDir))
        continue;

    var newPath = path.Replace(themeDir, distDir);
    var fileDir = Path.GetDirectoryName(newPath) !;

    if (!Directory.Exists(fileDir))
        Directory.CreateDirectory(fileDir);

    File.Copy(path, newPath, overwrite : true);
    Console.WriteLine("Generated: {0} (copyed)", newPath.Replace(distDir, ""));
}


async Task RenderRazorPageAsync(string templatePath, string distPath, object? model = null)
{
    var dir = Path.GetDirectoryName(distPath) !;
    if (!Directory.Exists(dir))
        Directory.CreateDirectory(dir);

    var templateContent = File.ReadAllText(templatePath);
    var template = await razorEngine.CompileAsync(templateContent, optin =>
    {
        optin.AddAssemblyReference(typeof(Util));
    });
    var html = template.Run(model);
    using StreamWriter sw = File.CreateText(distPath);
    await sw.WriteAsync(html);
}

PostFrontMatterViewModel GetPostFrontMatter(MarkdownDocument document)
{
    var block = document
        .Descendants<YamlFrontMatterBlock>()
        .FirstOrDefault();

    if (block is null)
        throw new ArgumentNullException(nameof(block), "Post must have a front matter!");

    var yaml =
        block
        // this is not a mistake
        // we have to call .Lines 2x
        .Lines // StringLineGroup[]
        .Lines // StringLine[]
        .OrderByDescending(x => x.Line)
        .Select(x => $"{x}\n")
        .ToList()
        .Select(x => x.Replace("---", string.Empty))
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Aggregate((s, agg) => agg + s);

    var frontMatter = yamlDeserializer.Deserialize<PostFrontMatterViewModel>(yaml)!;

    if (string.IsNullOrEmpty(frontMatter.Title))
        throw new ArgumentNullException(nameof(frontMatter.Title),
            "Post `title` is required in front matter!");
    if (frontMatter.CreateTime == default)
        throw new ArgumentNullException(nameof(frontMatter.CreateTime),
            "Post `create_time` is required in front matter!");

    frontMatter.Tags = frontMatter.Tags ?? Array.Empty<string>();

    return frontMatter;
}

public class BlogConfig
{
    public string Title { get; set; } = null!;
    public string Author { get; set; } = null!;
}

public class PostViewModel
{
    public BlogConfig BlogConfig { get; set; } = null!;
    public string PostContent { get; set; } = null!;
    public string PostRoute { get; set; } = null!;
    public PostFrontMatterViewModel FrontMatter { get; set; } = null!;
    public string PostTitle => FrontMatter.Title;
}

public class PostFrontMatterViewModel
{
    [YamlMember(Alias = "title")]
    public string Title { get; set; } = null!;

    [YamlMember(Alias = "slug")]
    public string Slug { get; set; } = null!;

    [YamlMember(Alias = "create_time")]
    public DateTime CreateTime { get; set; }

    [YamlMember(Alias = "last_updated")]
    public DateTime LastUpdatedTime { get; set; }

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = null!;

    [YamlMember(Alias = "tags")]
    public string[] Tags { get; set; } = Array.Empty<string>();
}