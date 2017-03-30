namespace CnfBattleSys
{
    /// <summary>
    /// Defines the various models BattlerPuppets can load
    /// to represent units in battle.
    /// (Color variations, etc. are handled separately. these are just "base" models.
    /// (don't make this a short!!! you give it to a monobehaviour. the internal battle system
    /// objects can use the shorter integer lengths to keep the data tables more compact,
    /// but anything a monobehavious touches has to be enum : int.)
    /// </summary>
    public enum BattlerModelType
    {
        None,
        TestModelSimple,
        TestModelHumanoid
    }
}