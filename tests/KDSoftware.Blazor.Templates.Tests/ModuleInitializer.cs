using System.Runtime.CompilerServices;

using EmptyFiles;

namespace KDSoftware.Blazor.Templates.Tests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Verify compares unknown extensions byte by byte. 'dotnet sln' writes platform-specific line endings while git
        // may check snapshots out with LF, so .slnx files must be compared as text (which normalizes line endings).
        FileExtensions.AddTextExtension("slnx");
    }
}