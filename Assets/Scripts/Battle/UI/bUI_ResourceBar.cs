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
    public Image barPreview;
    public BattlerPuppet puppet;
    public TextMeshProUGUI uguiText;
    public Color depletedResourceColor;
    public Color fullResourceColor;
    public Color previewColor;
    public bool displayCurrentValueOverMaxValue;
    public bool scaleHorizontally;
    public bool scaleVertically;
    public bool graduateColorAsResourceDepletes;
    public float animationTime;
    private int approachingValue;
    private int previewValue;
    private int realValueAtLastUpdate;
    private Vector2 originalScale;
    private Vector2 originalPreviewScale;
    private Vector2 currentScaleMulti = Vector2.one;
    private Vector2 currentPreviewScaleMulti;
    private string scaleOverTimeTag { get { return GetInstanceID() + _scaleOverTimeTag; } }
    const string _scaleOverTimeTag = "_bUI_ResourceBar_scaleOverTime";
    private string colorOverTimeTag { get { return GetInstanceID() + _colorOverTimeTag; } }
    const string _colorOverTimeTag = "_bUI_ResourceBar_colorOverTime";
    private string updateValueTag { get { return GetInstanceID() + _updateValueTag; } }
    const string _updateValueTag = "_bUI_ResourceBar_HPOverTime";

    /// <summary>
    /// MonoBehaviour.Awake()
    /// </summary>
    void Awake ()
    {
        originalScale = barFill.rectTransform.sizeDelta;
        originalPreviewScale = barPreview.rectTransform.sizeDelta;
        SetPreviewSize(Vector2.zero);
    }

    /// <summary>
    /// Associates this HP bar with the specified BattlerPuppet.
    /// </summary>
    public void AttachBattlerPuppet (BattlerPuppet _puppet)
    {
        puppet = _puppet;
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
        int max = 0;
        switch (resourceType)
        {
            case ResourceType.HP:
                max = puppet.battler.stats.maxHP;
                break;
            case ResourceType.Stamina:
                max = puppet.battler.currentStance.maxStamina;
                break;
            case ResourceType.SubweaponsCharge:
                Util.Crash(new NotImplementedException());
                break;
            default:
                Util.Crash(new Exception("Invalid resource type on resource bar " + gameObject.name + ": " + resourceType.ToString()));
                break;
        }
        return max;
    }

    /// <summary>
    /// Gets the current value of the resource for the current battler,
    /// depending on ResourceType.
    /// </summary>
    private int GetResourceValue ()
    {
        int value = 0;
        switch (resourceType)
        {
            case ResourceType.HP:
                value = puppet.battler.currentHP;
                break;
            case ResourceType.Stamina:
                value = puppet.battler.currentStamina;
                break;
            case ResourceType.SubweaponsCharge:
                Util.Crash(new NotImplementedException());
                break;
            default:
                Util.Crash(new Exception("Invalid resource type on resource bar " + gameObject.name + ": " + resourceType.ToString()));
                break;
        }
        return value;
    }

    /// <summary>
    /// Preview the effect of using an action that will impact this resource.
    /// </summary>
    public void PreviewValue(int _previewValue)
    {
        previewValue = _previewValue;
        float ratio = previewValue / realValueAtLastUpdate;
        SetPreviewSize(currentScaleMulti * ratio);
        if (uguiText != null) SetGUITextBasedOnValue(realValueAtLastUpdate, true);
    }

    /// <summary>
    /// Cancel the resource value change preview.
    /// </summary>
    public void UnpreviewValue()
    {
        SetPreviewSize(Vector2.zero);
        if (uguiText != null) SetGUITextBasedOnValue(realValueAtLastUpdate);
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
        if (uguiText != null) SetGUITextBasedOnValue(realValueAtLastUpdate);
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
    private void SetBarSize (Vector2 newScaleMulti)
    {
        currentScaleMulti = newScaleMulti;
        barFill.rectTransform.sizeDelta = new Vector2(originalScale.x * currentScaleMulti.x, originalScale.y * currentScaleMulti.y);
    }

    /// <summary>
    /// Scales the bar preview based on the scale multiplier given.
    /// </summary>
    private void SetPreviewSize (Vector2 newScaleMulti)
    {
        currentPreviewScaleMulti = newScaleMulti;
        barPreview.rectTransform.sizeDelta = new Vector2(originalPreviewScale.x * currentPreviewScaleMulti.x, originalPreviewScale.x * currentPreviewScaleMulti.y);
    }

    /// <summary>
    /// Sets Gui text based on given value. Doesn't manage that, so keep that in mind.
    /// </summary>
    private void SetGUITextBasedOnValue (int value, bool forPreview = false)
    {
        int max = GetResourceMax();
        if (forPreview)
        {
            int diff = (value - previewValue);
            if (displayCurrentValueOverMaxValue) uguiText.SetText(value.ToString() + " " + diff + " / " + max);
            else uguiText.SetText(value.ToString() + " " + diff);
        }
        else
        {
            if (displayCurrentValueOverMaxValue) uguiText.SetText(value.ToString() + " / " + max);
            else uguiText.SetText(value.ToString());
        }

    }

    /// <summary>
    /// Immediately sync bar with resource value.
    /// </summary>
    private void UpdateValueImmediately ()
    {
        int max = GetResourceMax();
        int value = GetResourceValue();
        realValueAtLastUpdate = approachingValue = value;
        if (uguiText != null) SetGUITextBasedOnValue(realValueAtLastUpdate);
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
            if (uguiText != null) SetGUITextBasedOnValue(Mathf.RoundToInt(realValueAtLastUpdate));
            yield return 0;
        }
        realValueAtLastUpdate = approachingValue;
        if (uguiText != null) SetGUITextBasedOnValue(realValueAtLastUpdate);
    }
}
