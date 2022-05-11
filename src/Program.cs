using RazorEngineCore;
using YamlDotNet.Serialization;
using Markdig;
using Microsoft.Extensions.Configuration;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Parsers;
using Markdig.Extensions.Yaml;
using Kit.Blog;

using MdFilePathAndNewPathPairDict = System.Collections.Generic.Dictionary<string, string>;

IConfigurationRoot cmdArgs = new ConfigurationBuilder()
    .AddCommandLine(args)
    .Build();

string outputDir = cmdArgs["output-dir"] ?? throw new ArgumentNullException("The argument --output-dir is required.");
string postsDir = cmdArgs["posts-dir"] ?? throw new ArgumentNullException("The argument --posts-dir is required.");
string templatesDir = cmdArgs["templates-dir"] ?? throw new ArgumentNullException("The argument --templates-dir is required.");
string staticfilesDir = cmdArgs["staticfiles-dir"] ?? throw new ArgumentNullException("The argument --staticfiles-dir is required.");

outputDir += Path.DirectorySeparatorChar;
postsDir += Path.DirectorySeparatorChar;
templatesDir += Path.DirectorySeparatorChar;
staticfilesDir += Path.DirectorySeparatorChar;

string postsOutputDir = Path.Combine(outputDir, "posts") + Path.DirectorySeparatorChar;

string indexTemplatePath = Path.Combine(templatesDir, "Index.cshtml");
string postIndexTemplatePath = Path.Combine(templatesDir, "PostsIndex.cshtml");
string aboutTemplatePath = Path.Combine(templatesDir, "About.cshtml");
string postTemplatePath = Path.Combine(templatesDir, "Post.cshtml");
string notFoundTemplatePath = Path.Combine(templatesDir, "404.cshtml");

string headTemplatePath = Path.Combine(templatesDir, "Head.cshtml");
string headerTemplatePath = Path.Combine(templatesDir, "Header.cshtml");
string footerTemplatePath = Path.Combine(templatesDir, "Footer.cshtml");
string scriptTemplatePath = Path.Combine(templatesDir, "Script.cshtml");

RazorEngine razorEngine = new();
MarkdownPipeline mdPipeline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions()
    .UseYamlFrontMatter()
    .UseTableOfContent()
    .Build();
IDeserializer yamlDeserializer = new DeserializerBuilder()
    .IgnoreUnmatchedProperties()
    .Build();

CopyStaticFiles(staticfilesDir, outputDir);
MdFilePathAndNewPathPairDict mdFilePathAndNewPathPairDict = CopyAssetFilesAndGetMdFilesPathDict(postsDir, postsOutputDir);
(IList<Page<Post>> postPages, IList<Post> posts) = GeneratePostsPageModel(mdFilePathAndNewPathPairDict);
await RenderIndexPageAsync(indexTemplatePath, outputDir, posts.OrderByDescending(p => p.FrontMatter.LastUpdatedTime).Take(3));
await RenderPostsIndexPageAsync(postIndexTemplatePath, postsOutputDir, posts);
await RenderAboutPageAsync(aboutTemplatePath, outputDir);
await Render404PageAsync(notFoundTemplatePath, outputDir);
await RenderPostsPagesAsync(postTemplatePath, postPages);


async Task Render404PageAsync(string templatePath, string distPath)
{
    string notFoundFilePath = Path.Combine(distPath, "404.html");
    Page<object> page = new()
    {
        Title = "404 | Kit Lau's Blog",
        H1 = "Kit Lau's Blog 😿",
        FilePath = notFoundFilePath
    };
    await RenderRazorPageAsync(templatePath, notFoundFilePath, page);
}

async Task RenderPostsPagesAsync(string templatePath, IList<Page<Post>> allPostsPages)
{
    foreach (var postPage in allPostsPages)
    {
        await RenderRazorPageAsync(templatePath, postPage.FilePath, postPage);
        Console.WriteLine("Generated: {0}", postPage.FilePath.Replace(outputDir, ""));
    }
}

async Task RenderAboutPageAsync(string templatePath, string distPath)
{
    string aboutFilePath = Path.Combine(distPath, "about.html");
    Page<object> page = new()
    {
        Title = "About | Kit Lau's Blog",
        H1 = "About Kit Lau",
        FilePath = aboutFilePath
    };
    await RenderRazorPageAsync(templatePath, aboutFilePath, page);
    Console.WriteLine("Generated: {0}", aboutFilePath.Replace(outputDir, ""));
}

async Task RenderPostsIndexPageAsync(string templatePath, string distPath, IEnumerable<Post> allPosts)
{
    string postsIndexFilePath = Path.Combine(distPath, "index.html");
    Page<PostsIndex> page = new()
    {
        Title = "Posts | Kit Lau's Blog",
        H1 = "Kit Lau's Posts",
        FilePath = postsIndexFilePath,
        ContentModel = new PostsIndex
        {
            AllPosts = allPosts
        }
    };
    await RenderRazorPageAsync(templatePath, postsIndexFilePath, page);
    Console.WriteLine("Generated: {0}", postsIndexFilePath.Replace(outputDir, ""));
}

async Task RenderIndexPageAsync(string templatePath, string distPath, IEnumerable<Post> indexPagePosts)
{
    string indexFilePath = Path.Combine(distPath, "index.html");
    Page<Index> page = new()
    {
        Title = "Kit Lau's Blog",
        H1 = "Kit Lau's Blog",
        FilePath = indexFilePath,
        ContentModel = new Index { IndexPagePosts = indexPagePosts }
    };

    await RenderRazorPageAsync(templatePath, indexFilePath, page);
    Console.WriteLine("Generated: {0}", indexFilePath.Replace(outputDir, ""));
}

async Task RenderRazorPageAsync(string templatePath, string distPath, object? model = null)
{
    string dir = Path.GetDirectoryName(distPath)!;
    if (!Directory.Exists(dir))
        Directory.CreateDirectory(dir);

    string templateContent = GetTemplate(templatePath);
    IRazorEngineCompiledTemplate template = await razorEngine.CompileAsync(templateContent, optin =>
    {
        optin.AddAssemblyReference(typeof(Util));
    });
    string html = template.Run(model);
    html = html.Replace("class=\"language-c#\"", "class=\"language-csharp\"");
    using StreamWriter sw = File.CreateText(distPath);
    await sw.WriteAsync(html);
}

(IList<Page<Post>>, IList<Post>) GeneratePostsPageModel(MdFilePathAndNewPathPairDict mdFilePathAndNewPathPairDict)
{
    List<Page<Post>> pages = new(mdFilePathAndNewPathPairDict.Count);
    List<Post> posts = new(mdFilePathAndNewPathPairDict.Count);
    foreach ((string oldPath, string newPath) in mdFilePathAndNewPathPairDict)
    {
        string newDir = Path.GetDirectoryName(newPath)!;
        using StringWriter writer = new();
        HtmlRenderer renderer = new(writer);
        mdPipeline.Setup(renderer);
        string mdText = File.ReadAllText(oldPath);
        MarkdownDocument mdDocument = MarkdownParser.Parse(mdText, mdPipeline);

        PostFrontMatter postFrontMatter = GetPostFrontMatterModel(mdDocument);

        renderer.Render(mdDocument);
        writer.Flush();
        string postHtmlContent = writer.ToString();

        string newHtmlPath = Path.Combine(newDir, $"{postFrontMatter.Slug}.html");

        Post post = new()
        {
            Content = postHtmlContent,
            Route = newHtmlPath.Replace(outputDir, ""),
            FrontMatter = postFrontMatter
        };

        Page<Post> page = new()
        {
            Title = $"{post.FrontMatter.Title} | Kit Lau's Blog",
            H1 = post.FrontMatter.Title,
            ContentModel = post,
            FilePath = newHtmlPath
        };

        pages.Add(page);
        posts.Add(post);
    }
    return (pages, posts);
}

PostFrontMatter GetPostFrontMatterModel(MarkdownDocument mdDocument)
{
    YamlFrontMatterBlock? block = mdDocument
        .Descendants<YamlFrontMatterBlock>()
        .FirstOrDefault();

    if (block is null)
        throw new ArgumentNullException(nameof(block), "Post must have a front matter!");

    string yaml = block
        .Lines // StringLineGroup[]
        .Lines // StringLine[]
        .OrderByDescending(x => x.Line)
        .Select(x => $"{x}\n")
        .ToList()
        .Select(x => x.Replace("---", string.Empty))
        .Where(x => !string.IsNullOrEmpty(x))
        .Aggregate((s, agg) => agg + s);

    PostFrontMatter frontMatter = yamlDeserializer.Deserialize<PostFrontMatter>(yaml);

    if (string.IsNullOrEmpty(frontMatter.Title))
        throw new ArgumentNullException(nameof(frontMatter.Title),
            "Post `title` is required in front matter!");
    if (frontMatter.CreateTime == default)
        throw new ArgumentNullException(nameof(frontMatter.CreateTime),
            "Post `create_time` is required in front matter!");
    if (frontMatter.LastUpdatedTime == default)
        frontMatter.LastUpdatedTime = frontMatter.CreateTime;

    frontMatter.Tags ??= Array.Empty<string>();

    return frontMatter;
}


MdFilePathAndNewPathPairDict CopyAssetFilesAndGetMdFilesPathDict(string dir, string outputDir)
{
    string[] filesPath = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
    if (!Directory.Exists(outputDir))
        Directory.CreateDirectory(outputDir);

    MdFilePathAndNewPathPairDict mdPairsDict = new();

    foreach (var filePath in filesPath)
    {
        string newPath = filePath.Replace(dir, outputDir);
        string newDir = Path.GetDirectoryName(newPath)!;

        if (!Directory.Exists(newDir))
            Directory.CreateDirectory(newDir);

        if (!newPath.EndsWith(".md"))
        {
            File.Copy(filePath, newPath, true);
            Console.WriteLine("Generated: {0} (copyed)", newPath.Replace(outputDir, ""));
            continue;
        }

        if (!mdPairsDict.TryGetValue(filePath, out string? newOne))
            mdPairsDict.Add(filePath, newPath);
        else
            throw new ArgumentException("The file path must be unique!");
    }
    return mdPairsDict;
}

void CopyStaticFiles(string dir, string outputDir)
{
    string[] filesPath = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
    if (!Directory.Exists(outputDir))
        Directory.CreateDirectory(outputDir);
    foreach (var filePath in filesPath)
    {
        string newPath = filePath.Replace(dir, outputDir);
        string newDir = Path.GetDirectoryName(newPath)!;
        if (!Directory.Exists(newDir))
            Directory.CreateDirectory(newDir);

        File.Copy(filePath, newPath, true);
        Console.WriteLine("Generated: {0} (copyed)", newPath.Replace(outputDir, ""));
    }
}

string GetTemplate(string templatePath)
{
    string templateContent = File.ReadAllText(templatePath);
    templateContent = templateContent.Replace("$HEAD_PLACEHOLDER", File.ReadAllText(headTemplatePath));
    templateContent = templateContent.Replace("$HEADER_PLACEHOLDER", File.ReadAllText(headerTemplatePath));
    templateContent = templateContent.Replace("$FOOTER_PLACEHOLDER", File.ReadAllText(footerTemplatePath));
    templateContent = templateContent.Replace("$SCRIPT_PLACEHOLDER", File.ReadAllText(scriptTemplatePath));

    return templateContent;
}

public class Page<T> where T : class
{
    public string Title { get; set; } = null!;
    public string H1 { get; set; } = null!;
    public T? ContentModel { get; set; }
    public string FilePath { get; set; } = null!;
}

public class PostsIndex
{
    public IEnumerable<Post> AllPosts { get; set; } = null!;
}

public class Index
{
    public IEnumerable<Post> IndexPagePosts { get; set; } = null!;
}

public class Post
{
    public string Content { get; set; } = null!;
    public string Route { get; set; } = null!;
    public PostFrontMatter FrontMatter { get; set; } = null!;
}

public class PostFrontMatter
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