using Microsoft.Extensions.Configuration;
using Kit.Blog;
using Index = Kit.Blog.Index;
using MdFilePath_NewPathPairDict = System.Collections.Generic.Dictionary<string, string>;

IConfigurationRoot cmdArgs = new ConfigurationBuilder()
    .AddCommandLine(args)
    .Build();

Argument argument = BlogGenerationHelper.GetArguments(cmdArgs);

BlogGenerationHelper.CopyStaticFiles(argument.StaticFilesDir, argument.OutputDir);

MdFilePath_NewPathPairDict mdFilePathAndNewPathPairDict = BlogGenerationHelper.CopyPostAssetsFiles(argument.PostsDir, argument.PostsOutputDir);

(IList<Page<Post>> postPages, IList<Post> posts) = BlogGenerationHelper.GetPostPagesModel(argument, mdFilePathAndNewPathPairDict);

await RenderIndexPageAsync(argument.OutputDir, posts.OrderByDescending(p => p.FrontMatter.LastUpdatedTime).Take(3));
await RenderPostsIndexPageAsync(argument.PostsOutputDir, posts.OrderByDescending(p => p.FrontMatter.LastUpdatedTime));
await RenderAboutPageAsync(argument.OutputDir);
await Render404PageAsync(argument.OutputDir);
await RenderPostsPagesAsync(postPages);

async Task Render404PageAsync(string outputDir)
{
    string templatePath = Path.Combine(argument.TemplatesDir, "404.cshtml");
    string outputPath = Path.Combine(outputDir, "404.html");
    
    Page page = new()
    {
        Title = "404 | Kit Lau's Blog",
        H1 = "Kit Lau's Blog 😿",
        FilePath = outputPath
    };
    
    await BlogGenerationHelper.RenderRazorPageAsync(argument, templatePath, outputPath, page);
}

async Task RenderPostsPagesAsync(IEnumerable<Page<Post>> allPostsPages)
{
    string templatePath = Path.Combine(argument.TemplatesDir, "Post.cshtml");
    
    foreach (var postPage in allPostsPages)
    {
        await BlogGenerationHelper.RenderRazorPageAsync(argument, templatePath, postPage.FilePath, postPage);
        Console.WriteLine("Generated: {0}", postPage.FilePath.Replace(argument.OutputDir, ""));
    }
}

async Task RenderAboutPageAsync(string outputDir)
{
    string templatePath = Path.Combine(argument.TemplatesDir, "About.cshtml");
    string outputPath = Path.Combine(outputDir, "about", "index.html");
    
    Page page = new()
    {
        Title = "About | Kit Lau's Blog",
        H1 = "About Kit Lau",
        FilePath = outputPath
    };
    await BlogGenerationHelper.RenderRazorPageAsync(argument, templatePath, outputPath, page);
    Console.WriteLine("Generated: {0}", outputPath.Replace(argument.OutputDir, ""));
}

async Task RenderPostsIndexPageAsync(string outputDir, IEnumerable<Post> allPosts)
{
    string templatePath = Path.Combine(argument.TemplatesDir, "PostsIndex.cshtml");
    string outputPath = Path.Combine(outputDir, "index.html");
    
    Page<PostsIndex> page = new()
    {
        Title = "Posts | Kit Lau's Blog",
        H1 = "Kit Lau's Posts",
        FilePath = outputPath,
        ContentModel = new PostsIndex
        {
            AllPosts = allPosts
        }
    };
    await BlogGenerationHelper.RenderRazorPageAsync(argument, templatePath, outputPath, page);
    Console.WriteLine("Generated: {0}", outputPath.Replace(argument.OutputDir, ""));
}

async Task RenderIndexPageAsync(string outputDir, IEnumerable<Post> indexPagePosts)
{
    string templatePath = Path.Combine(argument.TemplatesDir, "Index.cshtml");
    string outputPath = Path.Combine(outputDir, "index.html");
    
    Page<Index> page = new()
    {
        Title = "Kit Lau's Blog",
        H1 = "Kit Lau's Blog",
        FilePath = outputPath,
        ContentModel = new Index { IndexPagePosts = indexPagePosts }
    };

    await BlogGenerationHelper.RenderRazorPageAsync(argument, templatePath, outputPath, page);
    Console.WriteLine("Generated: {0}", outputPath.Replace(argument.OutputDir, ""));
}