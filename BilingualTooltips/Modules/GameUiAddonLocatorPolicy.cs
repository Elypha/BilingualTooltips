namespace BilingualTooltips.Modules;

internal enum GameUiAddonLocatorPolicy
{
    // Locate a singleton-like top-level addon by addon name.
    TopLevelAddonByName,

    // Locate a child addon by matching both parent addon id and child addon name.
    ChildAddonByParentIdAndName,
}
