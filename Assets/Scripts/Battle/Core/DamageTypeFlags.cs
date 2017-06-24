namespace CnfBattleSys
{
    /// <summary>
    /// Bitflags corresponding to damage types. Single subactions can be multiple damage types at once.
    /// </summary>
    [System.Flags]
    public enum DamageTypeFlags
    {
        None = 0,
        Magic = 1,
        Strike = 1 << 1,
        Slash = 1 << 2,
        Thrust = 1 << 3,
        Fire = 1 << 4,
        Earth = 1 << 5,
        Air = 1 << 6,
        Water = 1 << 7,
        Light = 1 << 8,
        Dark = 1 << 9,
        Bio = 1 << 10,
        Sound = 1 << 11,
        Psyche = 1 << 12,
        Reality = 1 << 13,
        Time = 1 << 14,
        Space = 1 << 15,
        Electric = 1 << 16,
        Ice = 1 << 17,
        Spirit = 1 << 18
    }
}