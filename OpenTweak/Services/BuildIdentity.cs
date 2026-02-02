
using System.Reflection;

namespace OpenTweak.Services;

/// <summary>
/// Provides information about the current build type (Official vs Community).
/// </summary>
public static class BuildIdentity
{
    /// <summary>
    /// Gets a value indicating whether this is an official build distributed by the author.
    /// </summary>
    public static bool IsOfficialBuild
    {
        get
        {
#if OFFICIAL_BUILD
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// Gets the user-facing build type string.
    /// </summary>
    public static string BuildTypeString => IsOfficialBuild ? "Official Release" : "Community Build";

    /// <summary>
    /// Gets the assembly version.
    /// </summary>
    public static string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
}
