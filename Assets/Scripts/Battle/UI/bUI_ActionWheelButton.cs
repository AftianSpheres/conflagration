using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CnfBattleSys;
using CnfBattleSys.AI;
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
    private Battler decidingBattler { get { return AIModule_PlayerSide_ManualControl.waitingBattler; } }
    private static TextBank actionsCommonBank;
    private static TextBank commandsBank;
    private static TextBank stancesCommonBank;
    private SelectionType selectionType;
    private State state;
    private bool forbidden;
    readonly static int confirmTrigger = Animator.StringToHash("confirm");
    readonly static int idleBool = Animator.StringToHash("idle");
    readonly static int idlePathHash = Animator.StringToHash("Base Layer.Idle");
    readonly static int lockedBool = Animator.StringToHash("locked");
    readonly static int lockedPathHash = Animator.StringToHash("Base Layer.Locked");
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

    /// <summary>
    /// Conforms the action wheel button to the UI command.
    /// </summary>
    public void ConformToUICommand (bUI_BattleUIController.Command command)
    {
        gameObject.SetActive(true);
        selectionType = SelectionType.FromCommands;
        DetermineStateForCommand(command);
        if (commandsBank == null) commandsBank = TextBankManager.Instance.GetTextBank("Battle/ActionWheel");
        TextBank.Page thisPage;
        if (command == bUI_BattleUIController.Command.AttackPrimary)
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
    /// Disables the action wheel button - use this to hide 
    /// </summary>
    public void Disable()
    {
        gameObject.SetActive(false);
        selectionType = SelectionType.None;
        state = State.Disabled;
    }

    /// <summary>
    /// Check if the button is in an animation that counts as a transition.
    /// </summary>
    public bool InTransitionAnimation ()
    {
        bool returnVal = false;
        switch (state)
        {
            case State.Available:
            case State.Available_ButAttackWillBreakStance:
                returnVal = animator.GetCurrentAnimatorStateInfo(0).fullPathHash != idlePathHash;
                break;
            case State.Selected:
            case State.Selected_ButAttackWillBreakStance:
                returnVal = animator.GetCurrentAnimatorStateInfo(0).fullPathHash != selectedPathHash;
                break;
            case State.Locked:
                returnVal = animator.GetCurrentAnimatorStateInfo(0).fullPathHash != lockedPathHash;
                break;
            default:
                Util.Crash("Tried to find out if action wheel button " + indexOnWheel + " was in a transition animation, but that's not possible from state " + state);
                break;
        }
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
        animator.SetBool(lockedBool, state == State.Locked);
        animator.SetBool(selectedBool, state == State.Selected | state == State.Selected_ButAttackWillBreakStance);
        animator.SetBool(idleBool, state == State.Available | state == State.Available_ButAttackWillBreakStance);
    }

    /// <summary>
    /// Determines non-inactive button state, based on the given battle action and the current state of the wheel.
    /// </summary>
    private void DetermineStateForBattleAction (BattleAction action)
    {
        if (decidingBattler == null) DetermineStateForCommand(bUI_BattleUIController.Command.AttackPrimary);
        else if (!decidingBattler.CanExecuteAction(action)) state = State.Locked;
        else if (wheel.selectedButton == this)
        {
            if (decidingBattler.CalcActionStaminaCost(action.baseSPCost) >= decidingBattler.currentStamina) state = State.Selected_ButAttackWillBreakStance;
            else state = State.Selected;
        }
        else
        {
            if (decidingBattler.CalcActionStaminaCost(action.baseSPCost) >= decidingBattler.currentStamina) state = State.Available_ButAttackWillBreakStance;
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
        commandIcon.sprite = ActionDatabase.GetIconForActionID(action.actionID);
    }

    /// <summary>
    /// Loads icon sprite for given command and sets icon to that sprite.
    /// </summary>
    private void SetIconForCommand (bUI_BattleUIController.Command command)
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
