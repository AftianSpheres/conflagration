﻿using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CnfBattleSys;
using CnfBattleSys.AI;

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
        FromActions,
        FromStances
    }
    public Animator animator;
    public Image buttonBG;
    public Image commandIcon;
    public Text guiText_Label;
    public bool disabled { get; private set; }
    public bool locked { get; private set; }
    public bool selected { get; private set; }
    public bool willBreakStance { get; private set; }
    public int indexOnWheel { get; private set; }
    private bUI_ActionWheel wheel;
    private Battler decidingBattler { get { return AIModule_PlayerSide_ManualControl.waitingBattler; } }
    private static TextBank actionsCommonBank;
    private static TextBank commandsBank;
    private static TextBank stancesCommonBank;
    private SelectionType selectionType;
    private bool forbidden;
    readonly static int closeTrigger = Animator.StringToHash("close");
    readonly static int confirmTrigger = Animator.StringToHash("confirm");
    readonly static int idleBool = Animator.StringToHash("idle");
    readonly static int idlePathHash = Animator.StringToHash("Base Layer.Idle");
    readonly static int lockedBool = Animator.StringToHash("locked");
    readonly static int lockedPathHash = Animator.StringToHash("Base Layer.Locked");
    readonly static int lockedSelectedPathHash = Animator.StringToHash("Base Layer.Locked+Selected");
    readonly static int selectedBool = Animator.StringToHash("selected");
    readonly static int selectedPathHash = Animator.StringToHash("Base Layer.Selected");
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
        ConformAnimatorToState();
    }

    /// <summary>
    /// Called on all ActionWheelButtons when the wheel rotates.
    /// Determines which state this button should be in for current wheel position.
    /// </summary>
    public void ConformStateToWheelPosition()
    {
        if (selectionType == SelectionType.FromActions) DetermineStateForBattleAction(wheel.GetActionForButton(this));
        else if (selectionType == SelectionType.FromCommands) DetermineStateForCommand(wheel.GetCommandForButton(this));
        switch (selectionType)
        {
            case SelectionType.FromActions:
                DetermineStateForBattleAction(wheel.GetActionForButton(this));
                break;
            case SelectionType.FromCommands:
                DetermineStateForCommand(wheel.GetCommandForButton(this));
                break;
            case SelectionType.FromStances:
                DetermineStateForStance(wheel.GetStanceForButton(this));
                break;
            default:
                Util.Crash("Bad selection type on action wheel button " + indexOnWheel + " : " + selectionType);
                break;
        }
        ConformAnimatorToState();
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

    public void ConformToStance (BattleStance stance)
    {
        gameObject.SetActive(true);
        selectionType = SelectionType.FromStances;
        DetermineStateForStance(stance);
        if (stancesCommonBank == null) stancesCommonBank = TextBankManager.Instance.GetCommonTextBank(typeof(StanceType));
        TextBank.Page thisPage = stancesCommonBank.GetPage(stance.stanceID);
        if (thisPage.isValid) guiText_Label.text = thisPage.text;
        else guiText_Label.text = commandsBank.GetPage("error").text;
        SetIconForStance(stance);
    }

    /// <summary>
    /// Conforms the action wheel button to the UI command.
    /// </summary>
    public void ConformToUICommand (bUI_Command command)
    {
        gameObject.SetActive(true);
        selectionType = SelectionType.FromCommands;
        DetermineStateForCommand(command);
        if (commandsBank == null) commandsBank = TextBankManager.Instance.GetTextBank("Battle/ActionWheel");
        TextBank.Page thisPage;
        if (command == bUI_Command.Decide_AttackPrimary)
        {
            if (stancesCommonBank == null) stancesCommonBank = TextBankManager.Instance.GetCommonTextBank(typeof(StanceType));
            thisPage = stancesCommonBank.GetPage(decidingBattler.currentStance.stanceID);
            SetIconForStance(decidingBattler.currentStance);
        }
        else
        {
            thisPage = commandsBank.GetPage("cmd_" + command.ToString());
            SetIconForCommand(command);
        }
        if (thisPage.isValid) guiText_Label.text = thisPage.text;
        else guiText_Label.text = commandsBank.GetPage("error").text;
    }

    /// <summary>
    /// Disables the action wheel button - use this to hide it when not in use.
    /// </summary>
    public void Disable()
    {
        gameObject.SetActive(false);
        selectionType = SelectionType.None;
        disabled = true;
    }

    /// <summary>
    /// Check if the button is in an animation that counts as a transition.
    /// </summary>
    public bool InTransitionAnimation ()
    {
        bool returnVal = false;
        Action onIdle = () =>
        {
            returnVal = animator.GetCurrentAnimatorStateInfo(0).fullPathHash != idlePathHash;
        };
        Action onLocked = () =>
        {
            returnVal = animator.GetCurrentAnimatorStateInfo(0).fullPathHash != lockedPathHash;
        };
        Action onLockedSelected = () =>
        {
            returnVal = animator.GetCurrentAnimatorStateInfo(0).fullPathHash != lockedSelectedPathHash;
        };
        Action onSelected = () =>
        {
            returnVal = animator.GetCurrentAnimatorStateInfo(0).fullPathHash != selectedPathHash;
        };

        if (selected)
        {
            if (locked) onLockedSelected();
            else onSelected();
        }
        else if (locked) onLocked();
        else if (!disabled) onIdle();
        return returnVal;
    }

    /// <summary>
    /// Called on all ActionWheelButtons when a selection is confirmed.
    /// </summary>
    public void OnWheelConfirm ()
    {
        if (wheel.selectedButton == this) animator.SetTrigger(confirmTrigger);
    }

    /// <summary>
    /// Called on all active buttons when wheel is closed.
    /// </summary>
    public void OnWheelClose ()
    {
        animator.SetTrigger(closeTrigger);
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
        animator.SetBool(lockedBool, locked);
        animator.SetBool(selectedBool, selected);
        animator.SetBool(idleBool, !locked && !selected);
    }

    /// <summary>
    /// Determines non-inactive button state, based on the given battle action and the current state of the wheel.
    /// </summary>
    private void DetermineStateForBattleAction (BattleAction action)
    {
        if (decidingBattler == null) DetermineStateForCommand(bUI_Command.Decide_AttackPrimary);
        else
        {
            locked = !decidingBattler.CanExecuteAction(action);
            selected = wheel.selectedButton == this;
            disabled = !wheel.ButtonShouldBeActive(this);
            willBreakStance = decidingBattler.CalcActionStaminaCost(action.baseSPCost) >= decidingBattler.currentStamina;
        }
    }

    /// <summary>
    /// Determines non-inactive button state based on given command and current wheel state.
    /// </summary>
    private void DetermineStateForCommand (bUI_Command command)
    {
        locked = wheel.CommandLocked(command);
        selected = wheel.selectedButton == this;
        disabled = !wheel.ButtonShouldBeActive(this);
        willBreakStance = false;
    }

    /// <summary>
    /// Determines non-inactive button state based on given stance and current wheel state.
    /// </summary>
    private void DetermineStateForStance (BattleStance stance)
    {
        locked = wheel.StanceLocked(stance);
        selected = wheel.selectedButton == this;
        disabled = !wheel.ButtonShouldBeActive(this);
        willBreakStance = false;
    }

    /// <summary>
    /// Loads icon sprite for given battle action and sets icon to that sprite.
    /// </summary>
    private void SetIconForBattleAction (BattleAction action)
    {
        commandIcon.sprite = ActionDatabase.GetIconForActionID(action.actionID);
    }

    /// <summary>
    /// Loads icon sprite for given command and sets icon to that sprite.
    /// </summary>
    private void SetIconForCommand (bUI_Command command)
    {
        Sprite iconSprite = Resources.Load<Sprite>(commandIconsResourcePath + command.ToString());
        if (iconSprite == null) iconSprite = Resources.Load<Sprite>(commandIconsResourcePath + "Invalid");
        if (iconSprite == null) Util.Crash(new Exception("Couldn't get invalid command icon placeholder"));
        commandIcon.sprite = iconSprite;
    }

    /// <summary>
    /// Loads icon sprite for given stance and sets icon to that sprite.
    /// </summary>
    private void SetIconForStance (BattleStance stance)
    {
        commandIcon.sprite = StanceDatabase.GetIconForStanceID(stance.stanceID);
    }

}
