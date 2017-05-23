using System;
using System.Collections.Generic;
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
    private class Decision
    {
        public readonly BattleStance baseStance;
        public readonly bUI_BattleUIController.Command[] commands;
        public readonly BattleAction[] decideableActions;
        public readonly DecisionType decisionType;
        public int optionCount { get { if (commands != null) return commands.Length; else return decideableActions.Length; } }
        public int selectedOptionIndex = 0;
        private readonly bUI_BattleUIController.Command[] lockedCommands;

        /// <summary>
        /// Constructor for a decision between bui controller commands.
        /// </summary>
        public Decision (bUI_BattleUIController.Command[] _commands, bUI_BattleUIController.Command[] _lockedCommands)
        {
            decisionType = DecisionType.BattleUI;
            commands = _commands;
            lockedCommands = _lockedCommands;
            decideableActions = null;
        }

        /// <summary>
        /// Constructor for a decision between actions in a stance.
        /// </summary>
        public Decision (BattleStance _baseStance)
        {
            baseStance = _baseStance;
            decideableActions = baseStance.actionSet;
            decisionType = DecisionType.ActionSelect;
            commands = null;
        }

        /// <summary>
        /// Submits the appropriate commands to the UI controller.
        /// </summary>
        public void Submit ()
        {
            switch (decisionType)
            {
                case DecisionType.BattleUI:
                    Debug.Log(commands[selectedOptionIndex]);
                    break;
                case DecisionType.ActionSelect:
                    Debug.Log(decideableActions[selectedOptionIndex]);
                    break;
                default:
                    Util.Crash(new Exception("Invalid action wheel decision type: " + decisionType.ToString()));
                    break;
            }
        }

        /// <summary>
        /// Returns true if the given command is locked for this decision.
        /// </summary>
        public bool CommandLocked (bUI_BattleUIController.Command command)
        {
            if (lockedCommands != null) for (int i = 0; i < lockedCommands.Length; i++) if (lockedCommands[i] == command) return true;
            return false;
        }
    }
    /// <summary>
    /// Types of decisions the action wheel can be configured for.
    /// </summary>
    private enum DecisionType
    {
        None,
        BattleUI,
        ActionSelect
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
    public Animator animator;
    public bUI_ActionWheelButton selectedButton { get { return activeButtons[currentDecision.selectedOptionIndex]; } }
    public GameObject buttonsPrefab;
    public GameObject contents;
    public Image centerIcon;
    public TextMeshProUGUI centerText;
    public Transform buttonsParent;
    public bool allowInput { get { return _allowInput && state == State.Ready; } }
    public bool inAttackSelection { get { return currentDecision.decisionType == DecisionType.ActionSelect; } }
    public bool isOpen { get { return state == State.Ready || state == State.InTransition; } }
    public float buttonsDistance;
    /// <summary>
    /// Buttons that are currently tied to options being presented by the action wheel.
    /// </summary>
    private bUI_ActionWheelButton[] activeButtons;
    /// <summary>
    /// All buttons, including inactive ones.
    /// </summary>
    private bUI_ActionWheelButton[] allButtons;
    private Decision currentDecision;
    private Dictionary<Animator, int> waitingAnimatorsPushTimeStateHashes;
    private Quaternion defaultRotation;
    private Stack<Animator> animatorsForWaitingOn;
    private Stack<Decision> decisionsStack;
    private TextBank actionWheelBank;
    private TextBank stancesCommonBank;
    private State state;
    private bool _allowInput;
    private float interval;
    private readonly static int decisionConfirmHash = Animator.StringToHash("Base Layer.DecisionConfirm");
    private readonly static int decisionShowHash = Animator.StringToHash("Base Layer.DecisionShow");
    private readonly static int idleHash = Animator.StringToHash("Base Layer.Idle");
    const float placeRotationTime = .4f;
    const int maximumNumberOfOptions = 9;
    const string thisTag = "_bUI_ActionWheel_";

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        defaultRotation = transform.rotation;
        waitingAnimatorsPushTimeStateHashes = new Dictionary<Animator, int>();
        animatorsForWaitingOn = new Stack<Animator>();
        decisionsStack = new Stack<Decision>();
        bUI_BattleUIController.instance.RegisterActionWheel(this);
        GenerateButtonsFromPrefab();
        LockInput();
        Close();
    }

    /// <summary>
    /// MonoBehaviour.Update ()
    /// </summary>
    void Update ()
    {
        if (BattleOverseer.currentTurnBattler == null)
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
    /// Close the action wheel.
    /// </summary>
    public void Close ()
    {
        contents.SetActive(false);
        state = State.Offline;
    }

    /// <summary>
    /// Returns true if the command is locked, for the current decison.
    /// </summary>
    public bool CommandLocked (bUI_BattleUIController.Command command)
    {
        return currentDecision.CommandLocked(command);
    }

    /// <summary>
    /// Confirms the wheel's selected option.
    /// </summary>
    public void ConfirmSelection ()
    {
        for (int i = 0; i < activeButtons.Length; i++) activeButtons[i].OnWheelConfirm();
        animator.Play(decisionConfirmHash);
        WaitOnAnimator(animator);
        Action onCompletion = () =>
        {
            currentDecision.Submit();
        };
        Timing.RunCoroutine(_CallOnceAnimatorsFinish(onCompletion), thisTag);
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
        currentDecision = null;
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
    public bUI_BattleUIController.Command GetCommandForButton (bUI_ActionWheelButton button)
    {
        return currentDecision.commands[button.indexOnWheel];
    }

    /// <summary>
    /// Open the action wheel.
    /// </summary>
    public void Open ()
    {
        if (BattleOverseer.currentTurnBattler == null) Util.Crash(new Exception("No battler is taking a turn, so you can't open the action wheel."));
        ConformWheelToCurrentDecision();
        contents.SetActive(true);
        UnlockInput();
        state = State.Ready;
    }

    /// <summary>
    /// Push a decision with the specified battle actions as options.
    /// The action wheel will update itself to present the new decision.
    /// </summary>
    public void PushDecision (BattleStance stance)
    {
        decisionsStack.Push(new Decision(stance));
        ConformWheelToCurrentDecision();
    }

    /// <summary>
    /// Push a decision with the specified UI commands as options, and no locked commands.
    /// The action wheel will update itself to present the new decision.
    /// </summary>
    public void PushDecision (bUI_BattleUIController.Command[] commands)
    {
        PushDecision(commands, new bUI_BattleUIController.Command[0]);
    }

    /// <summary>
    /// Push a decision with the specified UI commands as options, and the given set of locked commands.
    /// The action wheel will update itself to present the new decision.
    /// </summary>
    public void PushDecision (bUI_BattleUIController.Command[] commands, bUI_BattleUIController.Command[] lockedCommands)
    {
        decisionsStack.Push(new Decision(commands, lockedCommands));
        ConformWheelToCurrentDecision();
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
    /// Adds the animator to the stack of animators that need to finish what they're doing before the wheel changes states.
    /// </summary>
    public void WaitOnAnimator (Animator waitingAnimator)
    {
        animatorsForWaitingOn.Push(waitingAnimator);
        waitingAnimatorsPushTimeStateHashes.Add(waitingAnimator, waitingAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash);
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
            Vector3 position = rotation * Vector3.right * buttonsDistance;
            allButtons[i].transform.localPosition = position;
            activeButtons[i] = allButtons[i];
            if (currentDecision.decisionType == DecisionType.ActionSelect) allButtons[i].ConformToBattleAction(currentDecision.decideableActions[i]);
            else allButtons[i].ConformToUICommand(currentDecision.commands[i]);
        }
    }

    /// <summary>
    /// Conforms wheel to decision on top of the stack.
    /// </summary>
    private void ConformWheelToCurrentDecision ()
    {
        if (decisionsStack.Count == 0) Util.Crash(new Exception("Can't open wheel before putting a decision on the stack!"));
        currentDecision = decisionsStack.Peek();
        ActivateButtonsForDecision(currentDecision);
        for (int i = 0; i < activeButtons.Length; i++)
        {
            activeButtons[i].ConformStateToWheelPosition();
        }
        ConformWheelRotationToSelectedButton();
        SetCenterIcon();
        centerText.text = string.Empty;
        animator.Play(decisionShowHash);
        WaitOnAnimator(animator);
        Action onCompletion = () =>
        {
            SetCenterText();
        };
        Timing.RunCoroutine(_CallOnceAnimatorsFinish(onCompletion), thisTag);
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
        int newIndex = currentDecision.selectedOptionIndex + places;
        int diff = newIndex - currentDecision.selectedOptionIndex;
        float degrees = interval * diff;
        float rotationLen = placeRotationTime;
        if (Mathf.Abs(diff) > 1) rotationLen *= 2;
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
            UnlockInput();
        };
        Timing.RunCoroutine(_RotateWheel(degrees, rotationLen, onCompletion), thisTag);
    }

    /// <summary>
    /// Sets sprite for center icon, if there should be one.
    /// </summary>
    private void SetCenterIcon ()
    {
        if (currentDecision.baseStance != null) centerIcon.sprite = StanceDatabase.GetIconForStanceID(currentDecision.baseStance.stanceID);
        else centerIcon.sprite = null;
    }

    /// <summary>
    /// Sets the text in the wheel's center element based on the current decision.
    /// </summary>
    private void SetCenterText ()
    {
        if (actionWheelBank == null) actionWheelBank = TextBankManager.Instance.GetTextBank("Battle/ActionWheel");
        if (stancesCommonBank == null) stancesCommonBank = TextBankManager.Instance.GetCommonTextBank(typeof(StanceType));
        if (currentDecision.baseStance != null) centerText.text = stancesCommonBank.GetPage(currentDecision.baseStance.stanceID).text;
        else centerText.text = actionWheelBank.GetPage("centerText").text;
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
    /// Locks the wheel, waits until every animator on the stack has completed the state it was in when it was pushed,
    /// and then unlocks and calls onCompletion.
    /// </summary>
    private IEnumerator<float> _CallOnceAnimatorsFinish(Action onCompletion)
    {
        state = State.InTransition;
        while (animatorsForWaitingOn.Count > 0)
        {
            while (true)
            {
                if (animatorsForWaitingOn.Count == 0) break;
                Animator _animator = animatorsForWaitingOn.Peek();
                if (waitingAnimatorsPushTimeStateHashes[_animator] != _animator.GetCurrentAnimatorStateInfo(0).fullPathHash)
                {
                    animatorsForWaitingOn.Pop();
                    waitingAnimatorsPushTimeStateHashes.Remove(_animator);
                }
                else break;
            }
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
