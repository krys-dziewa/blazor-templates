namespace KDSoftware.Blazor.Templates.Tests;

internal static class TemplatePaths
{
    public const string BlazorBffShortName = "kd-blazor-bff";

    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    public static string BlazorBffTemplate { get; } =
        Path.Combine(RepositoryRoot, "src", "KDSoftware.Blazor.Templates", "content", "BlazorBff");

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