using UnityEngine;
using UnityEngine.UI;
using CnfBattleSys;

/// <summary>
/// One of the panels the bUI_TurnOrderArea controls.
/// </summary>
public class bUI_TurnOrderPanel : MonoBehaviour
{
    /// <summary>
    /// States for the TurnOrderPanel's animator.
    /// </summary>
    private enum AnimatorState
    {
        None,
        Active,
        Idle,
        WillPass
    }
    public Battler battler { get; private set; }
    public Image mugshotImage;
    private Animator animator;
    private bUI_TurnOrderArea turnOrderArea;
    private Image bgImage;
    private int index;
    private readonly static int stateHash = Animator.StringToHash("state");
    private const string iconsResourcePath = "Battle/2D/UI/BattlerIcon/Cutin/";
    private bUI_TurnOrderPanel zeroIndexPanel;

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        animator = GetComponent<Animator>();
        bgImage = GetComponent<Image>();
    }

    /// <summary>
    /// Sets index of panel.
    /// </summary>
    public void SetIndex (int _index)
    {
        index = _index;
        if (index == 0) zeroIndexPanel = this;
    }

    /// <summary>
    /// Hides an unused panel.
    /// </summary>
    public void Hide ()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Pairs with given battler and updates panel according to battler data.
    /// </summary>
    public void PairWithBattler (Battler _battler)
    {
        AnimatorState state;
        gameObject.SetActive(true);
        battler = _battler;
        Sprite iconSprite = Resources.Load<Sprite>(iconsResourcePath + battler.battlerType);
        if (iconSprite == null) iconSprite = Resources.Load<Sprite>(iconsResourcePath + "invalidIcon");
        if (iconSprite == null) Util.Crash("Couldn't get invalid icon sprite placeholder");
        mugshotImage.sprite = iconSprite;
        bgImage.color = bUI_BattleUIController.instance.GetPanelColorFor(battler);
        if (!battler.CanExecuteAction(ActionDatabase.SpecialActions.noneBattleAction)) state = AnimatorState.WillPass;
        else if (index == 0 || battler == zeroIndexPanel.battler) state = AnimatorState.Active;
        else state = AnimatorState.Idle;
        animator.SetInteger(stateHash, (int)state);
    }
}
