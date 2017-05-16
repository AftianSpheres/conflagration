using System;
using System.Collections.Generic;
using UnityEngine;
using CnfBattleSys;
using MovementEffects;

public class bUI_ActionWheel : MonoBehaviour
{
    /// <summary>
    /// Models a decision the action wheel can make.
    /// </summary>
    private class Decision
    {
        public readonly bUI_BattleUIController.Command[] commands;
        public readonly BattleAction[] decideableActions;
        public readonly DecisionType decisionType;
        public int optionCount { get { if (commands != null) return commands.Length; else return decideableActions.Length; } }
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
        /// Constructor for a decision between BattleActions.
        /// </summary>
        public Decision (BattleAction[] _decideableActions)
        {
            decideableActions = _decideableActions;
            decisionType = DecisionType.ActionSelect;
            commands = null; // If it's an action sel
        }

        /// <summary>
        /// Submits the appropriate commands to the UI controller.
        /// </summary>
        public void Submit (int optionIndex)
        {
            switch (decisionType)
            {
                case DecisionType.BattleUI:
                    Debug.Log(commands[optionIndex]);
                    break;
                case DecisionType.ActionSelect:
                    Debug.Log(decideableActions[optionIndex]);
                    break;
                default:
                    throw new Exception("Invalid action wheel decision type: " + decisionType.ToString());
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
        Online
    }
    public bUI_ActionWheelButton selectedButton { get { return activeButtons[selectedOptionIndex]; } }
    public GameObject buttonsPrefab;
    public GameObject contents;
    public Transform buttonsParent;
    public bool allowInput { get { return _allowInput && state != State.Offline; } }
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
    private Quaternion defaultRotation;
    private Stack<Animator> animatorsForWaitingOn;
    private Stack<Decision> decisionsStack;
    private State state;
    private bool _allowInput;
    private float interval;
    private int selectedOptionIndex;
    private readonly static bUI_BattleUIController.Command[] emptyCommandsArray = { };
    const float placeRotationTime = .4f;
    const int maximumNumberOfOptions = 9;
    const string thisTag = "_bUI_ActionWheel_";

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake()
    {
        defaultRotation = transform.rotation;
        animatorsForWaitingOn = new Stack<Animator>();
        decisionsStack = new Stack<Decision>();
        GenerateButtonsFromPrefab();
        LockInput();
        Close();
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
        Debug.Log("Confirmed af");
    }

    /// <summary>
    /// Disposes of the decision on top of the stack.
    /// This should be used when backing out of a second-level or higher decision (ex: action selection)
    /// back to the preceding one.
    /// </summary>
    public void DisposeOfTopDecision ()
    {
        if (decisionsStack.Count < 2) throw new Exception("Can't dispose of decision unless there's at least one decision above base-level decision!");
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
        if (currentDecision == null)
        {
            selectedOptionIndex = 0;
            if (decisionsStack.Count == 0) throw new Exception("Can't open wheel before putting a decision on the stack!");
            currentDecision = decisionsStack.Peek();
            ActivateButtonsForDecision(currentDecision);
        }
        for (int i = 0; i < activeButtons.Length; i++)
        {
            activeButtons[i].OnWheelMove();
        }
        contents.SetActive(true);
        UnlockInput();
        state = State.Online;
    }

    /// <summary>
    /// Push a decision with the specified battle actions as options.
    /// </summary>
    public void PushDecision (BattleAction[] battleActions)
    {
        decisionsStack.Push(new Decision(battleActions));
    }

    /// <summary>
    /// Push a decision with the specified UI commands as options, and no locked commands.
    /// </summary>
    public void PushDecision (bUI_BattleUIController.Command[] commands)
    {
        PushDecision(commands, new bUI_BattleUIController.Command[0]);
    }

    /// <summary>
    /// Push a decision with the specified UI commands as options, and the given set of locked commands.
    /// </summary>
    public void PushDecision (bUI_BattleUIController.Command[] commands, bUI_BattleUIController.Command[] lockedCommands)
    {
        decisionsStack.Push(new Decision(commands, lockedCommands));
    }

    /// <summary>
    /// Finds the nearest number of places to rotate toward the given button and
    /// rotates toward it.
    /// </summary>
    public void RotateToButton (bUI_ActionWheelButton button)
    {
        if (selectedButton != button)
        {
            int diffA = button.indexOnWheel - selectedOptionIndex;
            int diffB = currentDecision.optionCount - selectedOptionIndex + button.indexOnWheel;
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
    /// Sets rotation to appropriate value for selected button.
    /// </summary>
    private void ConformWheelRotationToSelectedButton ()
    {
        transform.rotation = Quaternion.Euler(0, 0, -(interval * selectedOptionIndex));
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
        int newIndex = selectedOptionIndex + places;
        int diff = newIndex - selectedOptionIndex;
        float degrees = interval * diff;
        float rotationLen = placeRotationTime;
        if (Mathf.Abs(diff) > 1) rotationLen *= 2;
        if (newIndex >= currentDecision.optionCount) newIndex -= currentDecision.optionCount;
        else if (newIndex < 0) newIndex += currentDecision.optionCount;
        selectedOptionIndex = newIndex;
        Action onCompletion = () => 
        {
            for (int i = 0; i < activeButtons.Length; i++)
            {
                activeButtons[i].OnWheelMove();
            }
            ConformWheelRotationToSelectedButton();
            UnlockInput();
        };
        Timing.RunCoroutine(_RotateWheel(degrees, rotationLen, onCompletion), thisTag);
    }

    /// <summary>
    /// Coroutine: rotates wheel a given number of degrees over a given number of seconds.
    /// Input is locked while rotating.
    /// Calls onCompletion after rotation finished.
    /// </summary>
    private IEnumerator<float> _RotateWheel (float degrees, float rotationLen, Action onCompletion)
    {
        LockInput();
        if (degrees == 0 || degrees == 360) throw new Exception("Can't rotate 0/360 degrees...");
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

    /// <summary>
    /// Allow the wheel to receive input.
    /// </summary>
    private void UnlockInput ()
    {
        _allowInput = true;
    }
}
