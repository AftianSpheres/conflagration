using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CnfBattleSys;
using TMPro;

/// <summary>
/// Controller for one of the buttons attached to the action wheel.
/// </summary>
public class bUI_ActionWheelButton : MonoBehaviour, IPointerClickHandler
{
    /// <summary>
    /// What sorts of things is the button attached to?
    /// </summary>
    private enum SelectionType
    {
        None,
        FromCommands,
        FromActions
    }
    /// <summary>
    /// Valid action wheel button states.
    /// </summary>
    private enum State
    {
        None,
        Disabled,
        Locked,
        Available,
        Available_ButAttackWillBreakStance,
        Selected,
        Selected_ButAttackWillBreakStance
    }
    public Animator animator;
    public Image buttonBG;
    public Image commandIcon;
    public TextMeshProUGUI guiText_Label;
    public int indexOnWheel { get; private set; }
    private bUI_ActionWheel wheel;
    private static TextBank actionsCommonBank;
    private static TextBank commandsBank;
    private SelectionType selectionType;
    private State state;
    private bool forbidden;
    readonly static int confirmAnimHash = Animator.StringToHash("Base Layer.Confirm");
    readonly static int failedConfirmAnimHash = Animator.StringToHash("Base Layer.FailedConfirm");
    readonly static int idleAnimHash = Animator.StringToHash("Base Layer.Idle");
    readonly static int lockedAnimHash = Animator.StringToHash("Base Layer.Locked");
    readonly static int selectedAnimHash = Animator.StringToHash("Base Layer.Selected");
    const string actionIconsResourcePath = "Battle/2D/UI/AWIcon/Action/";
    const string commandIconsResourcePath = "Battle/2D/UI/AWIcon/Command/";

    /// <summary>
    /// IPointerClickHandler.OnPointerClick (PointerEventData eventData)
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (wheel.allowInput)
        {
            if (wheel.selectedButton == this) wheel.ConfirmSelection();
            else wheel.RotateToButton(this);
        }
        else Debug.Log("input forbidden");
    }

    /// <summary>
    /// Confirms the action wheel button to the battle action.
    /// </summary>
    public void ConformToBattleAction (BattleAction action)
    {
        gameObject.SetActive(true);
        selectionType = SelectionType.FromActions;
        DetermineStateForBattleAction(action);
        if (actionsCommonBank == null) actionsCommonBank = TextBankManager.Instance.GetCommonTextBank(typeof(ActionType));
        TextBank.Page thisPage = actionsCommonBank.GetPage(action.actionID);
        if (thisPage.isValid) guiText_Label.text = thisPage.text;
        else guiText_Label.text = commandsBank.GetPage("error").text;
        SetIconForBattleAction(action);
    }

    /// <summary>
    /// Conforms the action wheel button to the UI command.
    /// </summary>
    public void ConformToUICommand (bUI_BattleUIController.Command command)
    {
        gameObject.SetActive(true);
        selectionType = SelectionType.FromCommands;
        DetermineStateForCommand(command);
        if (commandsBank == null) commandsBank = TextBankManager.Instance.GetTextBank("Battle/ActionWheel");
        TextBank.Page thisPage = commandsBank.GetPage("cmd_" + command.ToString());
        if (thisPage.isValid) guiText_Label.text = thisPage.text;
        else guiText_Label.text = commandsBank.GetPage("error").text;
        SetIconForCommand(command);
    }

    /// <summary>
    /// Disables the action wheel button - use this to hide 
    /// </summary>
    public void Disable()
    {
        gameObject.SetActive(false);
        selectionType = SelectionType.None;
        state = State.Disabled;
    }

    /// <summary>
    /// Called on all ActionWheelButtons when a selection is confirmed.
    /// </summary>
    public void OnWheelConfirm ()
    {
        switch (state)
        {
            case State.Selected:
            case State.Selected_ButAttackWillBreakStance:
                animator.Play(confirmAnimHash);
                wheel.WaitOnAnimator(animator);
                break;
            case State.Locked:
                animator.Play(failedConfirmAnimHash);
                wheel.WaitOnAnimator(animator);
                break;
        }
        ConformAnimatorToState();
    }

    /// <summary>
    /// Called on all ActionWheelButtons when the wheel rotates.
    /// Determines which state this button should be in for current wheel position.
    /// </summary>
    public void OnWheelMove ()
    {
        if (selectionType == SelectionType.FromActions) DetermineStateForBattleAction(wheel.GetActionForButton(this));
        else if (selectionType == SelectionType.FromCommands) DetermineStateForCommand(wheel.GetCommandForButton(this));
        ConformAnimatorToState();
    }

    /// <summary>
    /// Pairs the button with the wheel and disables it.
    /// Call this when generating these in the first place, basically.
    /// </summary>
    public void PairWithWheelAndDisable(bUI_ActionWheel _wheel, int _index)
    {
        wheel = _wheel;
        indexOnWheel = _index;
        Disable();
    }

    /// <summary>
    /// Conforms button's animator to button state.
    /// </summary>
    private void ConformAnimatorToState ()
    {
        int hash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        switch (state)
        {
            case State.Available:
                if (hash != idleAnimHash) animator.Play(idleAnimHash);
                break;
            case State.Locked:
                if (hash != lockedAnimHash && hash != failedConfirmAnimHash) animator.Play(lockedAnimHash);
                break;
            case State.Selected:
                if (hash != selectedAnimHash && hash != confirmAnimHash) animator.Play(selectedAnimHash);
                break;
            default:
                throw new Exception("Can't conform animator to button state: " + state.ToString());
        }
    }

    /// <summary>
    /// Determines non-inactive button state, based on the given battle action and the current state of the wheel.
    /// </summary>
    private void DetermineStateForBattleAction (BattleAction action)
    {
        if (BattleOverseer.currentTurnBattler == null) DetermineStateForCommand(bUI_BattleUIController.Command.Attack);
        else if (!BattleOverseer.currentTurnBattler.CanExecuteAction(action)) state = State.Locked;
        else if (wheel.selectedButton == this)
        {
            if (BattleOverseer.currentTurnBattler.CalcActionStaminaCost(action.baseSPCost) >= BattleOverseer.currentTurnBattler.currentStamina) state = State.Selected_ButAttackWillBreakStance;
            else state = State.Selected;
        }
        else
        {
            if (BattleOverseer.currentTurnBattler.CalcActionStaminaCost(action.baseSPCost) >= BattleOverseer.currentTurnBattler.currentStamina) state = State.Available_ButAttackWillBreakStance;
            else state = State.Available;
        }
    }

    /// <summary>
    /// Determines non-inactive button state based on given command and current wheel state.
    /// </summary>
    private void DetermineStateForCommand (bUI_BattleUIController.Command command)
    {
        if (wheel.CommandLocked(command)) state = State.Locked;
        else if (wheel.selectedButton == this) state = State.Selected;
        else state = State.Available;
    }

    /// <summary>
    /// Loads icon sprite for given battle action and sets icon to that sprite.
    /// </summary>
    private void SetIconForBattleAction (BattleAction action)
    {
        Sprite iconSprite = Resources.Load<Sprite>(actionIconsResourcePath + action.actionID.ToString());
        if (iconSprite == null) iconSprite = Resources.Load<Sprite>(actionIconsResourcePath + ActionType.InvalidAction.ToString());
        if (iconSprite == null) throw new Exception("Couldn't get invalid action icon placeholder");
        commandIcon.sprite = iconSprite;
    }

    /// <summary>
    /// Loads icon sprite for given command and sets icon to that sprite.
    /// </summary>
    private void SetIconForCommand (bUI_BattleUIController.Command command)
    {
        Sprite iconSprite = Resources.Load<Sprite>(commandIconsResourcePath + command.ToString());
        if (iconSprite == null) iconSprite = Resources.Load<Sprite>(commandIconsResourcePath + "Invalid");
        if (iconSprite == null) throw new Exception("Couldn't get invalid command icon placeholder");
        commandIcon.sprite = iconSprite;
    }

}
