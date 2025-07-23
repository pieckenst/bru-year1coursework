using System;

namespace SuperNova;

public class Static
{
    public static bool IsBrowser = OperatingSystem.IsBrowser();

    public static bool ForceSingleView => false;

    public static bool SupportsWindowing { get; } = OperatingSystem.IsWindows() ||
                                             OperatingSystem.IsLinux() ||
                                             OperatingSystem.IsMacOS();

    public static bool SingleView { get; set; }

    public static MainView MainView { get; set; } = null!;

    public static MainViewViewModel RootViewModel { get; set; } = null!;
}