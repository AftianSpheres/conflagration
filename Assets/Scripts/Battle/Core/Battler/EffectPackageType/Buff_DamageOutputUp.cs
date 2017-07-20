namespace CnfBattleSys
{
    public partial class Battler
    {
        /// <summary>
        /// Applies Damage Output Up buff based on the parameter of the given effect package.
        /// </summary>
        public void Buff_DamageOutputUp (BattleAction.Subaction.EffectPackage effect)
        {
            ApplyStatus(StatusType.TestBuff, StatusPacket_CancelationCondition.CancelWhenDurationZero, 0, effect.length_Byte, new Resistances_Raw(1), 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, effect.strength_Float, 1, 1, 1, 1, 1, 1, 1, 1);
        }
    }
}