using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using CnfBattleSys;
using MovementEffects;
using TMPro;

public class bUI_ActionWheel : MonoBehaviour
{
    /// <summary>
    /// Models a decision the action wheel can make.
    /// </summary>
    protected class Decision
    {
        public readonly BattleStance baseStance;
        public readonly bUI_Command[] commands;
        public readonly BattleAction[] decideableActions;
        public readonly BattleStance[] decideableStances;
        public readonly BattleStance lockedStance;
        public readonly DecisionType decisionType;
        public int optionCount { get { if (commands != null) return commands.Length; else if (decideableActions != null) return decideableActions.Length; else return decideableStances.Length; } }
        public int selectedOptionIndex = 0;
        private readonly bUI_ActionWheel wheel;
        private readonly bUI_Command[] lockedCommands;

        /// <summary>
        /// Constructor for a decision between bui controller commands.
        /// </summary>
        public Decision (bUI_Command[] _commands, bUI_Command[] _lockedCommands, bUI_ActionWheel _wheel)
        {
            wheel = _wheel;
            decisionType = DecisionType.BattleUI;
            commands = _commands;
            lockedCommands = _lockedCommands;
            decideableActions = null;
            decideableStances = null;
        }

        /// <summary>
        /// Constructor for a decision between actions in a stance.
        /// </summary>
        public Decision (BattleStance _baseStance, bUI_ActionWheel _wheel)
        {
            wheel = _wheel;
            baseStance = _baseStance;
            decideableActions = baseStance.actionSet;
            decisionType = DecisionType.ActionSelect;
            commands = null;
            decideableStances = null;
        }

        /// <summary>
        /// Constructor for a decision between stances.
        /// </summary>
        public Decision (BattleStance[] _stances, BattleStance _lockedStance, bUI_ActionWheel _wheel)
        {
            wheel = _wheel;
            decideableStances = _stances;
            lockedStance = _lockedStance;
            for (int i = 0; i < decideableStances.Length; i++)
            {
                if (decideableStances[i] == lockedStance)
                {
                    selectedOptionIndex = i; // The default selection should always be the stance you broke
                    break;
                }
            }
            decisionType = DecisionType.StanceSelect;
            commands = null;
            decideableActions = null;
        }

        /// <summary>
        /// Submits the appropriate commands to the UI controller.
        /// </summary>
        public void Submit ()
        {
            switch (decisionType)
            {
                case DecisionType.BattleUI:
                    bUI_BattleUIController.instance.SubmitCommand(commands[selectedOptionIndex]);
                    break;
                case DecisionType.ActionSelect:
                    bUI_BattleUIController.instance.SubmitBattleAction(decideableActions[selectedOptionIndex]);
                    bUI_BattleUIController.instance.SubmitCommand(bUI_Command.GetTargets);
                    break;
                case DecisionType.StanceSelect:
                    bUI_BattleUIController.instance.SubmitBattleStance(decideableStances[selectedOptionIndex]);
                    bUI_BattleUIController.instance.SubmitCommand(bUI_Command.WheelFromTopLevel);
                    break;
                default:
                    Util.Crash(new Exception("Invalid action wheel decision type: " + decisionType.ToString()));
                    break;
            }
            if (wheel.currentDecision == this) Timing.RunCoroutine(wheel._CallOnceAnimatorsFinish(wheel.Close), thisTag);
        }

        /// <summary>
        /// Returns true if the given command is locked for this decision.
        /// </summary>
        public bool CommandLocked (bUI_Command command)
        {
            if (lockedCommands != null) for (int i = 0; i < lockedCommands.Length; i++) if (lockedCommands[i] == command) return true;
            return false;
        }
    }
    /// <summary>
    /// Types of decisions the action wheel can be configured for.
    /// </summary>
    public enum DecisionType
    {
        None,
        BattleUI,
        ActionSelect,
        StanceSelect
    }
    /// <summary>
    /// Action wheel states.
    /// </summary>
    private enum State
    {
        None,
        Offline,
        Ready,
        InTransition
    }
    public AudioClip soundFX_deny;
    public AudioClip soundFX_confirm;
    public AudioClip soundFX_close;
    public AudioClip soundFX_open;
    public BattleAction selectedAction { get { return currentDecision.decideableActions[currentDecision.selectedOptionIndex]; } }
    public BattleStance selectedStance { get { return currentDecision.decideableStances[currentDecision.selectedOptionIndex]; } }
    public bUI_ActionWheelButton selectedButton { get { return activeButtons[currentDecision.selectedOptionIndex]; } }
    public bUI_Command selectedCommand { get { return currentDecision.commands[currentDecision.selectedOptionIndex]; } }
    public GameObject buttonsPrefab;
    public GameObject contents;
    public Image centerIcon;
    public TextMeshProUGUI centerText;
    public Transform buttonsParent;
    public DecisionType decisionType { get { return currentDecision.decisionType; } }
    public bool allowInput { get { return _allowInput && state == State.Ready; } }
    public bool inAttackSelection { get { return currentDecision.decisionType == DecisionType.ActionSelect; } }
    public bool isOpen { get { return state == State.Ready || state == State.InTransition; } }
    public float buttonsDistance;
    private Animator animator;
    /// <summary>
    /// Buttons that are currently tied to options being presented by the action wheel.
    /// </summary>
    private bUI_ActionWheelButton[] activeButtons;
    /// <summary>
    /// All buttons, including inactive ones.
    /// </summary>
    private bUI_ActionWheelButton[] allButtons;
    private bUI_ActionWheelInfobox infobox;
    private bUI_InfoboxShell actingUnitInfobox { get { return decidingBattler.puppet.infoboxShell; } }
    private Battler decidingBattler { get { return bUI_BattleUIController.instance.displayBattler; } }
    private Decision currentDecision { get { if (decisionsStack.Count == 0) return null; else return decisionsStack.Peek(); } }
    private AudioSource audioSource;
    private Quaternion defaultRotation;
    private Stack<Decision> decisionsStack;
    private TextBank actionWheelBank;
    private TextBank stancesCommonBank;
    private State state;
    private bool _allowInput;
    private float interval;
    private static bUI_Command[] topLevel_noSubactions = { bUI_Command.Decide_AttackPrimary, bUI_Command.Move, bUI_Command.Break, bUI_Command.Run };
    private static bUI_Command[] topLevel_yesSubactions = { bUI_Command.Decide_AttackPrimary, bUI_Command.Decide_AttackSecondary, bUI_Command.Move, bUI_Command.Break, bUI_Command.Run };
    private static bUI_Command[] topLevelLock_noMoveNoBreakNoRun = { bUI_Command.Move, bUI_Command.Break, bUI_Command.Run };
    private static bUI_Command[] topLevelLock_noMoveNoBreak = { bUI_Command.Move, bUI_Command.Break };
    private static bUI_Command[] topLevelLock_noMoveNoRun = { bUI_Command.Move, bUI_Command.Run };
    private static bUI_Command[] topLevelLock_noBreakNoRun = { bUI_Command.Break, bUI_Command.Run };
    private readonly static int closeHash = Animator.StringToHash("Base Layer.Close");
    private readonly static int decisionConfirmHash = Animator.StringToHash("Base Layer.DecisionConfirm");
    private readonly static int decisionShowHash = Animator.StringToHash("Base Layer.DecisionShow");
    private readonly static int doneHash = Animator.StringToHash("Base Layer.Done");
    private readonly static int idleHash = Animator.StringToHash("Base Layer.Idle");
    const float placeRotationTime = .4f;
    const int maximumNumberOfOptions = 9;
    const string thisTag = "_bUI_ActionWheel_";

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        infobox = GetComponentInChildren<bUI_ActionWheelInfobox>();
        decisionsStack = new Stack<Decision>();
        defaultRotation = transform.rotation;
        GenerateButtonsFromPrefab();
        LockInput();
        contents.SetActive(false);
        state = State.Offline;
    }

    /// <summary>
    /// MonoBehaviour.Start ()
    /// </summary>
    void Start()
    {
        bUI_BattleUIController.instance.RegisterActionWheel(this);
        infobox.PairWithWheel(this);
    }

    /// <summary>
    /// MonoBehaviour.Update ()
    /// </summary>
    void Update ()
    {       
        if (decidingBattler == null)
        {
            switch (state)
            {
                case State.InTransition:
                    StopTransitionAndNormalizeState();
                    goto case State.Ready;
                case State.Ready:
                    Close();
                    break;
                case State.None:
                case State.Offline:
                    break; // we're not open so it's fine if there's no acting battler
                default:
                    Util.Crash(new Exception("Invalid action wheel state: " + state.ToString()));
                    break;
            }
        }
    }

    /// <summary>
    /// Should this button be active, for a decision with however many options?
    /// </summary>
    public bool ButtonShouldBeActive (bUI_ActionWheelButton button)
    {
        return button.indexOnWheel < currentDecision.optionCount;
    }

    /// <summary>
    /// Clear the decision stack.
    /// Call this between turns to keep from carrying decisions over from the last turn.
    /// </summary>
    public void ClearDecisionStack ()
    {
        if (isOpen) Close();
        decisionsStack.Clear();
    }

    /// <summary>
    /// Close the action wheel.
    /// </summary>
    public void Close ()
    {
        Action onCompletion = () =>
        {
            infobox.Clear();
            contents.SetActive(false);
            state = State.Offline;
        };
        animator.Play(closeHash);
        for (int i = 0; i < activeButtons.Length; i++) activeButtons[i].OnWheelClose();
        if (!audioSource.isPlaying) audioSource.PlayOneShot(soundFX_close);
        UnsetPreviews();
    }

    /// <summary>
    /// Returns true if the command is locked, for the current decison.
    /// </summary>
    public bool CommandLocked (bUI_Command command)
    {
        return currentDecision.CommandLocked(command);
    }

    /// <summary>
    /// Confirms the wheel's selected option.
    /// </summary>
    public void ConfirmSelection ()
    {
        AudioClip clip = soundFX_confirm;
        if (selectedButton.locked) clip = soundFX_deny;
        for (int i = 0; i < activeButtons.Length; i++) activeButtons[i].OnWheelConfirm();
        animator.Play(decisionConfirmHash);
        Action onCompletion = () =>
        {
            if (selectedButton.selected && !selectedButton.locked) currentDecision.Submit();
        };
        if (!audioSource.isPlaying) audioSource.PlayOneShot(clip);
        Timing.RunCoroutine(_CallOnceAnimatorsFinish(onCompletion), thisTag);
    }

    /// <summary>
    /// Use the action wheel to decide which attack to use,
    /// from either currentStance or metaStance.
    /// </summary>
    public void DecideAttacks ()
    {
        decisionsStack.Push(new Decision(bUI_BattleUIController.instance.displayStance, this));
        Action onCompletion = ConformWheelToCurrentDecision;
        if (isOpen) Timing.RunCoroutine(_CallOnceAnimatorsFinish(onCompletion), thisTag);
        else
        {
            Open();
            onCompletion();
        }
    }

    /// <summary>
    /// Use the action wheel to decide which stance to switch into.
    /// </summary>
    public void DecideStances ()
    {
        decisionsStack.Push(new Decision(bUI_BattleUIController.instance.displayStanceSet, bUI_BattleUIController.instance.displayBattler.lockedStance, this));
        Action onCompletion = ConformWheelToCurrentDecision;
        if (isOpen) Timing.RunCoroutine(_CallOnceAnimatorsFinish(onCompletion), thisTag);
        else
        {
            Open();
            onCompletion();
        }
    }

    /// <summary>
    /// Use the action wheel to get a top-level command (attack/move/run/break) from the player.
    /// </summary>
    public void DecideTopLevel ()
    {
        bUI_Command[] commands;
        if (bUI_BattleUIController.instance.displayBattler.metaStance.actionSet.Length < 1) commands = topLevel_noSubactions;
        else commands = topLevel_yesSubactions;
        bUI_Command[] locked;
        bool canMove = bUI_BattleUIController.instance.displayBattler.CanMove();
        bool canBreak = bUI_BattleUIController.instance.displayBattler.CanBreak();
        bool canRun = bUI_BattleUIController.instance.displayBattler.CanRun();
        if (!canMove)
        {
            if (canBreak && canRun) locked = new bUI_Command[] { bUI_Command.Move };
            else if (!canBreak && !canRun) locked = topLevelLock_noMoveNoBreakNoRun;
            else if (!canRun) locked = topLevelLock_noMoveNoRun;
            else locked = topLevelLock_noMoveNoBreak;
        }
        else if (!canBreak)
        {
            if (!canRun) locked = topLevelLock_noBreakNoRun;
            else locked = new bUI_Command[] { bUI_Command.Break };
        }
        else if (!canRun) locked = new bUI_Command[] { bUI_Command.Run };
        else locked = new bUI_Command[0];
        decisionsStack.Push(new Decision(commands, locked, this));
        Action onCompletion = ConformWheelToCurrentDecision;
        if (isOpen) Timing.RunCoroutine(_CallOnceAnimatorsFinish(onCompletion), thisTag);
        else
        {
            Open();
            onCompletion();
        }
    }

    /// <summary>
    /// Disposes of the decision on top of the stack.
    /// This should be used when backing out of a second-level or higher decision (ex: action selection)
    /// back to the preceding one.
    /// </summary>
    public void DisposeOfTopDecision ()
    {
        if (decisionsStack.Count < 2) Util.Crash(new Exception("Can't dispose of decision unless there's at least one decision above base-level decision!"));
        decisionsStack.Pop();
        ConformWheelToCurrentDecision();
    }

    /// <summary>
    /// Gets the action associated with the given button.
    /// </summary>
    public BattleAction GetActionForButton (bUI_ActionWheelButton button)
    {
        return currentDecision.decideableActions[button.indexOnWheel];
    }

    /// <summary>
    /// Gets the command associated with the given button.
    /// </summary>
    public bUI_Command GetCommandForButton (bUI_ActionWheelButton button)
    {
        return currentDecision.commands[button.indexOnWheel];
    }

    /// <summary>
    /// Gets the stance associated with the given button.
    /// </summary>
    public BattleStance GetStanceForButton (bUI_ActionWheelButton button)
    {
        return currentDecision.decideableStances[button.indexOnWheel];
    }

    /// <summary>
    /// Open the action wheel.
    /// </summary>
    public void Open ()
    {
        if (decidingBattler == null) Util.Crash(new Exception("No battler is requesting input so you can't open the action wheel."));
        contents.SetActive(true);
        ConformWheelToCurrentDecision();
        UnlockInput();
        if (!audioSource.isPlaying) audioSource.PlayOneShot(soundFX_open);
        state = State.Ready;
    }

    /// <summary>
    /// Finds the nearest number of places to rotate toward the given button and
    /// rotates toward it.
    /// </summary>
    public void RotateToButton (bUI_ActionWheelButton button)
    {
        if (selectedButton != button)
        {
            int diffA = button.indexOnWheel - currentDecision.selectedOptionIndex;
            int diffB = currentDecision.optionCount - currentDecision.selectedOptionIndex + button.indexOnWheel;
            if (Mathf.Abs(diffB) < diffA || (Mathf.Abs(diffB) == diffA && UnityEngine.Random.Range(0, 2) == 0)) RotatePlaces(diffB);
            else RotatePlaces(diffA);
        }
        else Debug.Log("Tried to rotate toward selected button: " + button.gameObject.name + ". This shouldn't happen. Probably need to debug this.");
    }

    /// <summary>
    /// Returns true if the stance is locked, for the current decision.
    /// </summary>
    public bool StanceLocked(BattleStance stance)
    {
        return currentDecision.lockedStance != null && currentDecision.lockedStance == stance;
    }

    /// <summary>
    /// Conforms state of buttons to decision options.
    /// </summary>
    private void ActivateButtonsForDecision (Decision decision)
    {
        transform.rotation = defaultRotation;
        interval = 360 / decision.optionCount;
        activeButtons = new bUI_ActionWheelButton[decision.optionCount];
        for (int i = 0; i < decision.optionCount; i++)
        {
            float angle = i * interval;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            Vector3 position = rotation * Vector3.up * buttonsDistance;
            allButtons[i].transform.localPosition = position;
            activeButtons[i] = allButtons[i];
            switch (currentDecision.decisionType)
            {
                case DecisionType.ActionSelect:
                    allButtons[i].ConformToBattleAction(currentDecision.decideableActions[i]);
                    break;
                case DecisionType.BattleUI:
                    allButtons[i].ConformToUICommand(currentDecision.commands[i]);
                    break;
                case DecisionType.StanceSelect:
                    allButtons[i].ConformToStance(currentDecision.decideableStances[i]);
                    break;
                default:
                    Util.Crash("ActivateButtonsForDecision bad decision type: " + currentDecision.decisionType);
                    break;
            }
        }
        for (int i = decision.optionCount; i < allButtons.Length; i++) allButtons[i].Disable();
    }

    /// <summary>
    /// Conforms wheel to decision on top of the stack.
    /// </summary>
    private void ConformWheelToCurrentDecision ()
    {
        if (decisionsStack.Count == 0) Util.Crash(new Exception("Can't open wheel before putting a decision on the stack!"));
        ActivateButtonsForDecision(currentDecision);
        for (int i = 0; i < activeButtons.Length; i++)
        {
            activeButtons[i].ConformStateToWheelPosition();
        }
        ConformWheelRotationToSelectedButton();
        SetCenterIcon();
        SetCenterText();
        SetPreviews();
        infobox.OnWheelPositionChange();
        animator.Play(decisionShowHash);
    }

    /// <summary>
    /// Sets rotation to appropriate value for selected button.
    /// </summary>
    private void ConformWheelRotationToSelectedButton ()
    {
        transform.rotation = Quaternion.Euler(0, 0, -(interval * currentDecision.selectedOptionIndex));
    }

    /// <summary>
    /// Generates the button gameobjects for this action wheel.
    /// </summary>
    private void GenerateButtonsFromPrefab ()
    {
        allButtons = new bUI_ActionWheelButton[maximumNumberOfOptions];
        for (int i = 0; i < maximumNumberOfOptions; i++)
        {
            GameObject go = Instantiate(buttonsPrefab, buttonsParent);
            go.transform.localScale = Vector3.one;
            allButtons[i] = go.GetComponent<bUI_ActionWheelButton>();
            allButtons[i].PairWithWheelAndDisable(this, i);
            go.name = "Wheel Button " + i.ToString();
        }
    }

    /// <summary>
    /// Prevents the wheel from receiving input until UnlockInput is called.
    /// </summary>
    private void LockInput ()
    {
        _allowInput = false;
    }

    /// <summary>
    /// Rotates the action wheel by places places.
    /// A "place" here is an interval equivalent to one option on the wheel.
    /// </summary>
    private void RotatePlaces (int places)
    {
        UnsetPreviews();
        int newIndex = currentDecision.selectedOptionIndex + places;
        int diff = newIndex - currentDecision.selectedOptionIndex;
        float degrees = interval * diff;
        float rotationLen = placeRotationTime;
        if (newIndex >= currentDecision.optionCount) newIndex -= currentDecision.optionCount;
        else if (newIndex < 0) newIndex += currentDecision.optionCount;
        currentDecision.selectedOptionIndex = newIndex;
        Action onCompletion = () => 
        {
            for (int i = 0; i < activeButtons.Length; i++)
            {
                activeButtons[i].ConformStateToWheelPosition();
            }
            ConformWheelRotationToSelectedButton();
            SetPreviews();
            infobox.OnWheelPositionChange();
            UnlockInput();
        };
        infobox.Clear();
        Timing.RunCoroutine(_RotateWheel(degrees, rotationLen, onCompletion), thisTag);
    }

    /// <summary>
    /// Sets sprite for center icon, if there should be one.
    /// </summary>
    private void SetCenterIcon ()
    {
        if (currentDecision.baseStance != null)
        {
            centerIcon.gameObject.SetActive(true);
            centerIcon.sprite = StanceDatabase.GetIconForStanceID(currentDecision.baseStance.stanceID);
        }
        else centerIcon.gameObject.SetActive(false);
    }

    /// <summary>
    /// Sets the text in the wheel's center element based on the current decision.
    /// </summary>
    private void SetCenterText ()
    {
        if (actionWheelBank == null) actionWheelBank = TextBankManager.Instance.GetTextBank("Battle/ActionWheel");
        if (stancesCommonBank == null) stancesCommonBank = TextBankManager.Instance.GetCommonTextBank(typeof(StanceType));
        switch (currentDecision.decisionType)
        {
            case DecisionType.ActionSelect:
                centerText.text = stancesCommonBank.GetPage(currentDecision.baseStance.stanceID).TextAsUpper();
                break;
            case DecisionType.BattleUI:
                centerText.text = actionWheelBank.GetPage("centerText_TopLevel").text;
                break;
            case DecisionType.StanceSelect:
                centerText.text = actionWheelBank.GetPage("centerText_Stances").text;
                break;
            default:
                Util.Crash("Bad decision type in SetCenterText: " + currentDecision.decisionType);
                break;
        }
    }

    /// <summary>
    /// Sets up turn order/stamina/etc. previews based on selected action.
    /// </summary>
    private void SetPreviews ()
    {
        if (currentDecision.decisionType == DecisionType.ActionSelect)
        {
            bUI_BattleUIController.instance.turnOrderArea.PreviewTurnOrderForDelayOf(bUI_BattleUIController.instance.displayBattler.GetDelayForAction(selectedAction));
            actingUnitInfobox.DoOnInfoboxen((unitInfobox) => { unitInfobox.HandleResourceBarPreviews(selectedAction); });
        }
        else if (currentDecision.decisionType == DecisionType.BattleUI && selectedCommand == bUI_Command.Break)
        {
            bUI_BattleUIController.instance.turnOrderArea.PreviewTurnOrderForDelayOf(bUI_BattleUIController.instance.displayBattler.GetDelayForAction(ActionDatabase.SpecialActions.selfStanceBreakAction));
            actingUnitInfobox.DoOnInfoboxen((unitInfobox) => { unitInfobox.HandleResourceBarPreviews(ActionDatabase.SpecialActions.selfStanceBreakAction); });
        }
        else UnsetPreviews();
    }

    /// <summary>
    /// Call if you need to stop a wheel transition to ensure wheel state is sane before
    /// doing anything else.
    /// </summary>
    private void StopTransitionAndNormalizeState ()
    {
        Timing.KillCoroutines(thisTag); // Kill all action wheel coroutines
        animator.Play(idleHash); // Set the animator to playing idle anim
        ConformWheelRotationToSelectedButton();
        for (int i = 0; i < activeButtons.Length; i++)
        {
            allButtons[i].ConformStateToWheelPosition();
        }
        UnlockInput();
        state = State.Ready; // we're open but no
    }

    /// <summary>
    /// Allow the wheel to receive input.
    /// </summary>
    private void UnlockInput ()
    {
        _allowInput = true;
    }

    /// <summary>
    /// Make sure we aren't previewing action consequences.
    /// </summary>
    private void UnsetPreviews ()
    {
        bUI_BattleUIController.instance.turnOrderArea.ConformToTurnOrder();
        actingUnitInfobox.DoOnInfoboxen((unitInfobox) => { unitInfobox.NullifyResourceBarPreviews(); });
    }

    /// <summary>
    /// Locks the wheel, waits until every animator is in idle state,
    /// and then unlocks and calls onCompletion.
    /// </summary>
    private IEnumerator<float> _CallOnceAnimatorsFinish(Action onCompletion)
    {
        state = State.InTransition;
        yield return Timing.WaitForOneFrame; // animators need a frame of head time to get their state in order
        bool isDone = false;
        while (isDone == false)
        {
            isDone = true;
            int hash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
            if (hash != idleHash && hash != doneHash) isDone = false;
            for (int i = 0; i < activeButtons.Length; i++) if (activeButtons[i].InTransitionAnimation()) isDone = false;
            yield return 0;
        }
        state = State.Ready;
        onCompletion();
    }

    /// <summary>
    /// Coroutine: rotates wheel a given number of degrees over a given number of seconds.
    /// Input is locked while rotating.
    /// Calls onCompletion after rotation finished.
    /// </summary>
    private IEnumerator<float> _RotateWheel(float degrees, float rotationLen, Action onCompletion)
    {
        LockInput();
        if (degrees == 0 || degrees == 360) Util.Crash(new Exception("Can't rotate 0/360 degrees..."));
        float elapsedTime = 0;
        Quaternion startingRotation = transform.rotation;
        Quaternion finalRotation = startingRotation * Quaternion.Euler(0, 0, -degrees);
        while (elapsedTime < rotationLen)
        {
            elapsedTime += Timing.DeltaTime;
            transform.rotation = Quaternion.Lerp(startingRotation, finalRotation, elapsedTime / rotationLen);
            yield return 0;
        }
        onCompletion();
    }
}
