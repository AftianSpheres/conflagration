/// <summary>
/// Each scene inhabits one "ring."
/// Loading/unloading behavior differs based on which ring a scene inhabits.
/// For example: global scenes are things like the loading screen, which have very long
/// lifespans. Venue scenes refer specifically to battle system venues, and they're
/// loaded as needed and unloaded as soon as the battle ends.
/// System scenes are menus/etc. We can load a system scene on top of a venue scene,
/// but we never expect to load more than one system scene at a time.
/// </summary>
[System.Flags]
public enum SceneRing
{
    None = 0,
    GlobalScenes = 1,
    SystemScenes = 1 << 1,
    VenueScenes = 1 << 2,
    WorldScenes = 1 << 3,
}