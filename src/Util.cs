namespace Kit.Blog;

public static class Util
{
    public static string ReplaceWithspaceByLodash(string str)
    {
        return string.Join("", str.Select(c => char.IsWhiteSpace(c) ? '_' : c));
    }
}