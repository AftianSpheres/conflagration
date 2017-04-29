using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;
using MovementEffects;

/// <summary>
/// A resource bar in the battle UI.
/// </summary>
public class bUI_ResourceBar : MonoBehaviour
{
    public enum ResourceType
    {
        None,
        HP,
        Stamina,
        SubweaponsCharge
    }

    public ResourceType resourceType;
    public Image barFill;
    public BattlerPuppet puppet;
    new public TextMeshProUGUI guiText;
    public Color fullResourceColor;
    public Color depletedResourceColor;
    public bool displayCurrentValueOverMaxValue;
    public bool scaleHorizontally;
    public bool scaleVertically;
    public bool graduateColorAsResourceDepletes;
    public float animationTime;
    private int realValueAtLastUpdate;
    private int approachingValue;
    private uint thisInstance;
    private Vector2 originalScale;
    private Vector2 currentScaleMulti = Vector2.one;
    private string scaleOverTimeTag { get { return thisInstance.ToString() + _scaleOverTimeTag; } }
    const string _scaleOverTimeTag = "_bUI_ResourceBar_scaleOverTime";
    private string colorOverTimeTag { get { return thisInstance.ToString() + _colorOverTimeTag; } }
    const string _colorOverTimeTag = "_bUI_ResourceBar_colorOverTime";
    private string updateValueTag { get { return thisInstance.ToString() + _updateValueTag; } }
    const string _updateValueTag = "_bUI_ResourceBar_HPOverTime";

    /// <summary>
    /// We use this to allow individual instances of UI_ResourceBar to identify coroutine instances that belong to them.
    /// The instanceCounter increments each time a ResourceBar is loaded, and each ResourceBar identifies itself using the
    /// state of instanceCounter at the time it loaded in.
    /// </summary>
    static uint instanceCounter = uint.MinValue;

    /// <summary>
    /// MonoBehaviour.Awake()
    /// </summary>
    void Awake ()
    {
        originalScale = barFill.rectTransform.sizeDelta;
        thisInstance = instanceCounter;
        instanceCounter++;
    }

    /// <summary>
    /// Associates this HP bar with the specified BattlerPuppet.
    /// </summary>
    public void AttachBattlerPuppet (BattlerPuppet _puppet)
    {
        puppet = _puppet;
        switch (resourceType)
        {
            case ResourceType.HP:
                puppet.AttachHPBar(this);
                break;
            case ResourceType.Stamina:
                puppet.AttachStaminaBar(this);
                break;
            case ResourceType.SubweaponsCharge:
                throw new NotImplementedException();
            default:
                throw new Exception("Invalid resource type on resource bar " + gameObject.name + ": " + resourceType.ToString());
        }
        UpdateValueImmediately();
    }

    /// <summary>
    /// BattlerPuppet can call this to prompt the ResourceBar to sync its state with the battler's current state
    /// and update its UI elements accordingly.
    /// </summary>
    public void HandleValueChanges ()
    {
        if (animationTime > 0) UpdateValueOverTime(animationTime);
        else UpdateValueImmediately();
    }

    /// <summary>
    /// Changes to given color over duration seconds.
    /// </summary>
    private void ChangeColorOverTime(Color finalColor, float duration)
    {
        Timing.KillCoroutines(colorOverTimeTag);
        Timing.RunCoroutine(_ChangeColorOverTime(finalColor, duration).CancelWith(gameObject), colorOverTimeTag);
    }

    /// <summary>
    /// Gets the maximum value of the resource for the current battler,
    /// depending on ResourceType.
    /// </summary>
    /// <returns></returns>
    private int GetResourceMax ()
    {
        int max;
        switch (resourceType)
        {
            case ResourceType.HP:
                max = puppet.battler.stats.maxHP;
                break;
            case ResourceType.Stamina:
                max = puppet.battler.currentStance.maxStamina;
                break;
            case ResourceType.SubweaponsCharge:
                throw new NotImplementedException();
            default:
                throw new Exception("Invalid resource type on resource bar " + gameObject.name + ": " + resourceType.ToString());
        }
        return max;
    }

    /// <summary>
    /// Gets the current value of the resource for the current battler,
    /// depending on ResourceType.
    /// </summary>
    private int GetResourceValue ()
    {
        int value;
        switch (resourceType)
        {
            case ResourceType.HP:
                value = puppet.battler.currentHP;
                break;
            case ResourceType.Stamina:
                value = puppet.battler.currentStamina;
                break;
            case ResourceType.SubweaponsCharge:
                throw new NotImplementedException();
            default:
                throw new Exception("Invalid resource type on resource bar " + gameObject.name + ": " + resourceType.ToString());
        }
        return value;
    }

    /// <summary>
    /// Coroutine: Gradually change color to given value over duration seconds.
    /// </summary>
    private IEnumerator<float> _ChangeColorOverTime(Color finalColor, float duration)
    {
        float elapsedTime = 0;
        Color startTimeColor = barFill.color;
        while (elapsedTime < duration)
        {
            elapsedTime += Timing.DeltaTime;
            float r = elapsedTime / duration;
            if (r > 1) r = 1;
            barFill.color = Color.Lerp(startTimeColor, finalColor, r);
            yield return 0;
        }
        barFill.color = finalColor;
    }

    /// <summary>
    /// Kills a running gradual-HP-value-update coroutine if it exists and updates the realHPValue variable and the GUI text.
    /// </summary>
    private void NormalizeValue()
    {
        Timing.KillCoroutines(updateValueTag);
        realValueAtLastUpdate = approachingValue;
        if (guiText != null) SetGUITextBasedOnValue(realValueAtLastUpdate);
    }

    /// <summary>
    /// Scales bar to given degree over duration seconds.
    /// </summary>
    private void ScaleBarOverTime(Vector2 finalScaleMulti, float duration)
    {
        Timing.KillCoroutines(scaleOverTimeTag);
        Timing.RunCoroutine(_ScaleBarOverTime(finalScaleMulti, duration).CancelWith(gameObject), scaleOverTimeTag);
    }

    /// <summary>
    /// Coroutine: Gradually fill/unfill bar to the given multiplier over duration seconds.
    /// </summary>
    private IEnumerator<float> _ScaleBarOverTime(Vector2 finalScaleMulti, float duration)
    {
        float elapsedTime = 0;
        Vector2 startTimeScaleMulti = currentScaleMulti;
        while (elapsedTime < duration)
        {
            elapsedTime += Timing.DeltaTime;
            float r = elapsedTime / duration;
            if (r > 1) r = 1;
            SetBarSize(Vector2.Lerp(startTimeScaleMulti, finalScaleMulti, r));
            yield return 0;
        }
        SetBarSize(finalScaleMulti);
    }

    /// <summary>
    /// Scales the bar based on the scale multiplier given.
    /// </summary>
    private void SetBarSize(Vector2 newScaleMulti)
    {
        currentScaleMulti = newScaleMulti;
        barFill.rectTransform.sizeDelta = new Vector2(originalScale.x * currentScaleMulti.x, originalScale.y * currentScaleMulti.y);
    }

    /// <summary>
    /// Sets Gui text based on given value. Doesn't manage that, so keep that in mind.
    /// </summary>
    private void SetGUITextBasedOnValue (int value)
    {
        int max = GetResourceMax();
        if (displayCurrentValueOverMaxValue) guiText.SetText(value.ToString() + " / " + max);
        else guiText.SetText(value.ToString());
    }

    /// <summary>
    /// Immediately sync bar with resource value.
    /// </summary>
    private void UpdateValueImmediately ()
    {
        int max = GetResourceMax();
        int value = GetResourceValue();
        realValueAtLastUpdate = approachingValue = value;
        if (guiText != null) SetGUITextBasedOnValue(realValueAtLastUpdate);
        float ratio = (float)value / max;
        if (ratio > 1) ratio = 1;
        else if (ratio < 0) ratio = 0;
        if (graduateColorAsResourceDepletes) barFill.color = Color.Lerp(depletedResourceColor, fullResourceColor, ratio);
        float x = 1;
        if (scaleHorizontally) x = ratio;
        float y = 1;
        if (scaleVertically) y = ratio;
        SetBarSize(new Vector2(x, y));
    }

    /// <summary>
    /// Update the resource value to reflect the current state of the associated battler over duration seconds,
    /// and with it the GUI widgets.
    /// </summary>
    private void UpdateValueOverTime(float duration)
    {
        int max = GetResourceMax();
        int value = GetResourceValue();
        float ratio = (float)value / max;
        if (ratio > 1) ratio = 1;
        else if (ratio < 0) ratio = 0;
        if (graduateColorAsResourceDepletes) ChangeColorOverTime(Color.Lerp(depletedResourceColor, fullResourceColor, ratio), duration);
        float x = 1;
        if (scaleHorizontally) x = ratio;
        float y = 1;
        if (scaleVertically) y = ratio;
        ScaleBarOverTime(new Vector2(x, y), duration);
        NormalizeValue();
        Timing.RunCoroutine(_UpdateValueOverTime(duration).CancelWith(gameObject), updateValueTag);
    }

    /// <summary>
    /// Updates internal resource value and GUI text over time.
    /// </summary>
    private IEnumerator<float> _UpdateValueOverTime (float duration)
    {
        float elapsedTime = 0;
        float fakeValue = realValueAtLastUpdate;
        approachingValue = GetResourceValue();
        int originalValue = realValueAtLastUpdate;
        bool valueRising = approachingValue > originalValue;
        while (elapsedTime < duration)
        {
            fakeValue = Mathf.Lerp(approachingValue, realValueAtLastUpdate, Timing.DeltaTime / duration);
            if ((valueRising && fakeValue > approachingValue) || (!valueRising && fakeValue < approachingValue)) fakeValue = approachingValue;
            realValueAtLastUpdate = Mathf.RoundToInt(fakeValue);
            if (guiText != null) SetGUITextBasedOnValue(Mathf.RoundToInt(realValueAtLastUpdate));
            yield return 0;
        }
        realValueAtLastUpdate = approachingValue;
        if (guiText != null) SetGUITextBasedOnValue(realValueAtLastUpdate);
    }
}
