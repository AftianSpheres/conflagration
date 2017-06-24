namespace CnfBattleSys
{
    /// <summary>
    /// Specifies the available categories of battle action.
    /// This is, right now: damage-dealing, healing, buffs, debuffs.
    /// This isn't actually used in executing actions - it's for
    /// AI and (potentially) UI to quickly identify a given action as
    /// being "an attack" or "a heal" or whatever without trying to
    /// determine that by actually going over what each subaction is supposed to do.
    /// An action can potentially be multiple categories at once - a damage-dealing
    /// attack that also inflicts a debuff, say. This doesn't mean that every
    /// damage-dealing attack that inflicts a debuff _needs_ to be considered
    /// attack+debuff, though!
    /// In a nutshell: these flags communicate an action's "selling points."
    /// A strong fire attack that has a low chance of inflicting a weak burn debuff isn't
    /// really going to be on the table if "debuff" is what you want to do.
    /// If that attack inflicts a really nasty speed down debuff, though, that's
    /// more of a selling point, so you can say "this is an attack and also a debuff"
    /// and the AI won't ignore that very potent debuff just because it deals damage,
    /// or vice versa.
    /// </summary>
    [System.Flags]
    public enum BattleActionCategoryFlags
    {
        None = 0,
        Attack = 1,
        Heal = 1 << 1,
        Buff = 1 << 2,
        Debuff = 1 << 3
    }
}