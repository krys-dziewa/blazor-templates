namespace KDSoftware.Blazor.Templates.Tests;

internal static class TemplatePaths
{
    public const string BlazorAppShortName = "kdsoftware-blazor-app";

    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    public static string BlazorAppTemplate { get; } =
        Path.Combine(RepositoryRoot, "src", "KDSoftware.Blazor.Templates", "content", "BlazorApp");

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "KDSoftware.Blazor.Templates.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"Could not locate the repository root above '{AppContext.BaseDirectory}'.");
    }
}