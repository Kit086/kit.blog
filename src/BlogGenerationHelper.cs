using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;
using Microsoft.Extensions.Configuration;
using RazorEngineCore;
using YamlDotNet.Serialization;
using MdFilePath_NewPathPairDict = System.Collections.Generic.Dictionary<string, string>;

namespace Kit.Blog;

public static class BlogGenerationHelper
{
    public static Argument GetArguments(IConfigurationRoot cmdArgs)
    {
        Argument argument = new()
        {
            OutputDir = GetDirFromCmdArgs(cmdArgs, "output-dir"),
            PostsDir = GetDirFromCmdArgs(cmdArgs, "posts-dir"),
            TemplatesDir = GetDirFromCmdArgs(cmdArgs, "templates-dir"),
            StaticFilesDir = GetDirFromCmdArgs(cmdArgs, "staticfiles-dir")
        };

        return argument;
    }

    private static string GetDirFromCmdArgs(IConfigurationRoot cmdArgs, string argName)
    {
        string arg = cmdArgs[argName] ?? throw new ArgumentNullException($"The argument --{argName} is required.");
        
        if (!arg.EndsWith(Path.DirectorySeparatorChar))
        {
            arg += Path.DirectorySeparatorChar;
        }

        return arg;
    }

    public static void CopyStaticFiles(string fromDir, string outputDir)
    {
        string[] filePaths = Directory.GetFiles(fromDir, "*", SearchOption.AllDirectories);

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        foreach (var filePath in filePaths)
        {
            string newPath = filePath.Replace(fromDir, outputDir);
            string newDir = Path.GetDirectoryName(newPath)!;
            
            if (!Directory.Exists(newDir))
            {
                Directory.CreateDirectory(newDir);
            }

            File.Copy(filePath, newPath, true);
            Console.WriteLine("Generated: {0} (copied)", newPath.Replace(outputDir, ""));
        }
    }
    
    public static async Task RenderRazorPageAsync(Argument argument, string templatePath, string distPath, object? model = null)
    {
        RazorEngine razorEngine = new();
        
        string dir = Path.GetDirectoryName(distPath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string templateContent = GetTemplate(argument, templatePath);
        IRazorEngineCompiledTemplate template = await razorEngine.CompileAsync(templateContent, optin =>
        {
            optin.AddAssemblyReference(typeof(Util));
        });
        string html = template.Run(model);
        html = html.Replace("class=\"language-c#\"", "class=\"language-csharp\"");
        using StreamWriter sw = File.CreateText(distPath);
        await sw.WriteAsync(html);
    }
    
    private static string GetTemplate(Argument argument, string templatePath)
    {
        string headTemplatePath = Path.Combine(argument.TemplatesDir, "Head.cshtml");
        string headerTemplatePath = Path.Combine(argument.TemplatesDir, "Header.cshtml");
        string footerTemplatePath = Path.Combine(argument.TemplatesDir, "Footer.cshtml");
        string scriptTemplatePath = Path.Combine(argument.TemplatesDir, "Script.cshtml");
        
        string templateContent = File.ReadAllText(templatePath);
        templateContent = templateContent.Replace("$HEAD_PLACEHOLDER", File.ReadAllText(headTemplatePath));
        templateContent = templateContent.Replace("$HEADER_PLACEHOLDER", File.ReadAllText(headerTemplatePath));
        templateContent = templateContent.Replace("$FOOTER_PLACEHOLDER", File.ReadAllText(footerTemplatePath));
        templateContent = templateContent.Replace("$SCRIPT_PLACEHOLDER", File.ReadAllText(scriptTemplatePath));

        return templateContent;
    }
    
    public static MdFilePath_NewPathPairDict CopyPostAssetsFiles(string dir, string outputDir)
    {
        string[] filesPath = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        MdFilePath_NewPathPairDict mdPairsDict = new();

        foreach (var filePath in filesPath)
        {
            string newPath = filePath.Replace(dir, outputDir);
            string newDir = Path.GetDirectoryName(newPath)!;

            if (!Directory.Exists(newDir))
            {
                Directory.CreateDirectory(newDir);
            }

            if (!newPath.EndsWith(".md"))
            {
                File.Copy(filePath, newPath, true);
                Console.WriteLine("Generated: {0} (copied)", newPath.Replace(outputDir, ""));
                continue;
            }

            if (!mdPairsDict.ContainsKey(filePath))
            {
                mdPairsDict.Add(filePath, newPath);
            }
            else
            {
                throw new ArgumentException("The file path must be unique!");
            }
        }
    
        return mdPairsDict;
    }
    
    public static (IList<Page<Post>>, IList<Post>) GetPostPagesModel(Argument argument, MdFilePath_NewPathPairDict mdFilePathAndNewPathPairDict)
    {
        MarkdownPipeline mdPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseYamlFrontMatter()
            .UseTableOfContent()
            .Build();
        
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

            string newHtmlPath = Path.Combine(newDir, "index.html");

            string newAbsoluteRoute = Path.DirectorySeparatorChar + newHtmlPath.Replace(argument.OutputDir, "");
            string newRoute = newAbsoluteRoute.Substring(0, newAbsoluteRoute.LastIndexOf("index.html", StringComparison.Ordinal));

            Post post = new()
            {
                Content = postHtmlContent,
                Route = newRoute, // /符号开头的相对路径
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
    
    private static PostFrontMatter GetPostFrontMatterModel(MarkdownDocument mdDocument)
    {
        YamlFrontMatterBlock? block = mdDocument
            .Descendants<YamlFrontMatterBlock>()
            .FirstOrDefault();
        
        IDeserializer yamlDeserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();

        if (block is null)
        {
            throw new ArgumentNullException(nameof(block), "Post must have a front matter!");
        }

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
        {
            throw new ArgumentNullException(nameof(frontMatter.Title),
                "Post `title` is required in front matter!");
        }

        if (frontMatter.CreateTime == default)
        {
            throw new ArgumentNullException(nameof(frontMatter.CreateTime),
                "Post `create_time` is required in front matter!");
        }

        if (frontMatter.LastUpdatedTime == default)
        {
            frontMatter.LastUpdatedTime = frontMatter.CreateTime;
        }

        frontMatter.Tags ??= Array.Empty<string>();

        return frontMatter;
    }
}