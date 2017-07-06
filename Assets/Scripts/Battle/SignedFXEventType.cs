using System;
using CnfBattleSys;

/// <summary>
/// An FXEventType that's had bit 31 conformed to its
/// scalable flag. This handles going back and forth between
/// those and FXEvent instances.
/// </summary>
public struct SignedFXEventType : IEquatable<FXEvent>, IEquatable<FXEventType>
{
    private int value;

    public SignedFXEventType(FXEvent fxEvent)
    {
        value = (int)fxEvent.fxEventType;
        if (value < -1) Util.Crash("What the ever-loving fuck are you doing??? You should not need more than 31 bits' worth of FX event types.");
        if (fxEvent.isScalable) value *= -1; // set sign bit
    }

    bool IEquatable<FXEvent>.Equals(FXEvent other)
    {
        int v = value;
        FXEvent.Flags comparison = FXEvent.Flags.None; // if not positive (ie sign bit unset) we want scalable flag unset
        if (value < 0) // factor out bit 31 + we do want the scalable flag set
        {
            v *= -1;
            comparison = FXEvent.Flags.Scalable;
        }
        return (int)other.fxEventType == v && (comparison & other.flags) == comparison;
    }

    bool IEquatable<FXEventType>.Equals(FXEventType other)
    {
        int v = value;
        if (v < 0) v *= -1;
        return (int)other == v;
    }
}