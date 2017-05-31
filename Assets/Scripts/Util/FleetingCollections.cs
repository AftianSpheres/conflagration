using System.Collections.Generic;
using CnfBattleSys;

/// <summary>
/// Like the sands of an hourglass, so are the
/// collections we use as buffers in order to dodge
/// object instantiation.
/// If you're using one of these, call Clear() before
/// touching it. And do not under any circumstances
/// assume you can put something in a FleetingCollection
/// one frame and retrieve that information
/// the next, because you'll just get whatever random-ass
/// data it last contained.
/// This is a tool for saying "I want an array RIGHT NOW but idk how large it is."
/// </summary>
public static class FleetingCollections
{
    public static List<Battler> battlerBuffer_0 { get; private set; }
    public static List<Battler> battlerBuffer_1 { get; private set; }
    public static List<Battler> battlerBuffer_2 { get; private set; }
    public static List<Battler> battlerBuffer_3 { get; private set; }
    public static List<int> intBuffer_0 { get; private set; }
    public static List<int> intBuffer_1 { get; private set; }
    public static List<int> intBuffer_2 { get; private set; }
    public static List<int> intBuffer_3 { get; private set; }
    public static List<int> intBuffer_4 { get; private set; }
    public static List<int> intBuffer_5 { get; private set; }
    public static List<int> intBuffer_6 { get; private set; }
    public static List<int> intBuffer_7 { get; private set; }
    /// <summary>
    /// Default size of these buffers.
    /// </summary>
    private const int buffersSize = 32;

    static FleetingCollections()
    {
        battlerBuffer_0 = new List<Battler>(buffersSize);
        battlerBuffer_1 = new List<Battler>(buffersSize);
        battlerBuffer_2 = new List<Battler>(buffersSize);
        battlerBuffer_3 = new List<Battler>(buffersSize);
        intBuffer_0 = new List<int>(buffersSize);
        intBuffer_1 = new List<int>(buffersSize);
        intBuffer_2 = new List<int>(buffersSize);
        intBuffer_3 = new List<int>(buffersSize);
        intBuffer_4 = new List<int>(buffersSize);
        intBuffer_5 = new List<int>(buffersSize);
        intBuffer_6 = new List<int>(buffersSize);
        intBuffer_7 = new List<int>(buffersSize);
    }
}