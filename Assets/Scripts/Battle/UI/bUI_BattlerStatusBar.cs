using System.Collections.Generic;
using UnityEngine;
using CnfBattleSys;
using MovementEffects;

/// <summary>
/// The bar what contains status icons and updates them based on
/// the battler state.
/// </summary>
public class bUI_BattlerStatusBar : MonoBehaviour
{
    private BattlerPuppet puppet;
    private bUI_StatusIcon iconsPrototype;
    private bUI_StatusIcon[] icons;
    private RectTransform rectTransform;
    private StatusPacket[][] statusSets;
    private string thisTag;
    private float currentSetDisplayTime;
    private int setIndex;
    private const float setLifetime = 1.0f;

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        rectTransform = GetComponent<RectTransform>();
        iconsPrototype = GetComponentInChildren<bUI_StatusIcon>();
        thisTag = gameObject.GetInstanceID().ToString();
        GenerateIcons(); // surprising fact: if you don't call this the status bar doesn't work
    }

    /// <summary>
    /// MonoBehaviour.OnDestroy ()
    /// </summary>
    void OnDestroy()
    {
        if (puppet != null) puppet.onStatusPacketsChanged -= OnStatusPacketsModified;
    }

    /// <summary>
    /// Pairs with the puppet and attaches to the 
    /// OnStatusPacketsModified linked list.
    /// </summary>
    public void PairWithPuppet (BattlerPuppet _puppet)
    {
        puppet = _puppet;
        puppet.onStatusPacketsChanged += OnStatusPacketsModified;
    }

    /// <summary>
    /// Display the given set, conforming icons
    /// to it.
    /// </summary>
    private void Display (StatusPacket[] set)
    {
        Timing.KillCoroutines(thisTag);
        for (int i = 0; i < icons.Length; i++)
        {
            if (i >= set.Length) icons[i].Hide();
            else icons[i].ConformToStatus(set[i]);
        }
    }

    /// <summary>
    /// Generate the icon gameobjects that are to be slaved to this statusbar.
    /// </summary>
    private void GenerateIcons ()
    {
        RectTransform iconRt = iconsPrototype.gameObject.GetComponent<RectTransform>();
        float iconLen = iconRt.sizeDelta.x;
        int iconsCount = Mathf.FloorToInt(rectTransform.sizeDelta.x / iconLen);
        icons = new bUI_StatusIcon[iconsCount];
        for (int i = 0; i < iconsCount; i++)
        {
            bUI_StatusIcon newIcon = Instantiate(iconsPrototype, rectTransform);
            newIcon.gameObject.name = "Status Icon " + i;
            iconRt = newIcon.GetComponent<RectTransform>();
            iconRt.anchoredPosition = new Vector2(iconLen * i, iconRt.anchoredPosition.y);
            newIcon.Hide();
            icons[i] = newIcon;
        }
        iconsPrototype.gameObject.SetActive(false);
        Destroy(iconsPrototype.gameObject);
    }

    /// <summary>
    /// Returns an array of arrays of status packets.
    /// Each array contains a single "set" of status packets,
    /// equal to or fewer than the number of icons.
    /// </summary>
    private StatusPacket[][] GetStatusSets()
    {
        StatusType[] statusTypes = new StatusType[puppet.battler.statusPackets.Keys.Count];
        puppet.battler.statusPackets.Keys.CopyTo(statusTypes, 0);
        int numberOfSets = Mathf.CeilToInt(statusTypes.Length / icons.Length); // If there are more statuses than there are icons, we
        StatusPacket[][] _statusSets = new StatusPacket[numberOfSets][];
        int statusTypeIndex = 0;
        for (int s = 0; s < _statusSets.Length; s++)
        {
            if (statusTypes.Length - statusTypeIndex < icons.Length) _statusSets[s] = new StatusPacket[statusTypes.Length - statusTypeIndex];
            else _statusSets[s] = new StatusPacket[icons.Length];
            for (int i = 0; i < _statusSets[s].Length; i++)
            {
                _statusSets[s][i] = puppet.battler.statusPackets[statusTypes[statusTypeIndex]];
                statusTypeIndex++;
            }
        }
        return _statusSets;
    }

    /// <summary>
    /// Called when something has modified the battler's status
    /// packet dict.
    /// </summary>
    private void OnStatusPacketsModified()
    {
        statusSets = GetStatusSets();
        if (statusSets.Length == 0) Display(new StatusPacket[0]);
        else if (statusSets.Length == 1) Display(statusSets[0]);
        else
        {
            Timing.KillCoroutines(thisTag);
            Timing.RunCoroutine(_FlashStatusIconsWithSets(), thisTag);
        }
    }

    /// <summary>
    /// Coroutine: Juggles the active status sets.
    /// This never returns. If you want to start a new 
    /// instance of this coroutine, make sure to kill
    /// the last one.
    /// </summary>
    private IEnumerator<float> _FlashStatusIconsWithSets ()
    {
        if (setIndex >= statusSets.Length) setIndex = 0;
        currentSetDisplayTime = 0;
        Display(statusSets[setIndex]);
        while (true)
        {
            currentSetDisplayTime += Timing.DeltaTime;
            if (currentSetDisplayTime > setLifetime)
            {
                setIndex++;
                if (setIndex >= statusSets.Length) setIndex = 0;
                Display(statusSets[setIndex]);
            }
            yield return 0;
        }
    }
}
