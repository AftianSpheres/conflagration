using UnityEngine;
using UnityEngine.UI;
using System;
using CnfBattleSys;
using TMPro;

/// <summary>
/// The turn order display for the battle UI.
/// </summary>
public class bUI_TurnOrderArea : MonoBehaviour
{
    /// <summary>
    /// Structure for the little turn indicator widgets this thing manipulates.
    /// </summary>
    private struct TurnIndicator
    {
        public readonly Animator animator;
        public readonly GameObject gameObject;
        public readonly Image background;
        public readonly Image icon;
        public readonly TextMeshProUGUI guiText_Placement;

        /// <summary>
        /// A very boring constructor.
        /// </summary>
        public TurnIndicator(Animator _animator, GameObject _gameObject, Image _background, Image _icon, TextMeshProUGUI _guiText_Placement)
        {
            animator = _animator;
            gameObject = _gameObject;
            background = _background;
            icon = _icon;
            guiText_Placement = _guiText_Placement;
        }
    }
    public Color bgColor_Enemy;
    public Color bgColor_Friend;
    public Color bgColor_Neutral;
    public Color bgColor_Player;
    public TextMeshProUGUI guiText_Header;
    public GameObject turnIndicatorPrefab;
    public Transform turnIndicatorsParent;
    private Battler[] primaryTurnOrderBattlers;
    private Battler[] prospectiveTurnOrderBattlers;
    private TurnIndicator[] turnIndicators;
    private static TextBank localBank;
    private readonly static int activeAnimHash = Animator.StringToHash("active");
    private readonly static int prospectiveAnimHash = Animator.StringToHash("prospective");
    private readonly static int standardAnimHash = Animator.StringToHash("standard");
    private const string iconsResourcePath = "Battle/2D/UI/BattlerIcon/";

    // These values should probably be set somewhere else at some point.

    private static Vector2 turnIndicatorsOffsets = new Vector2(-4, -54);
    private const int turnIndicatorsCount = 11;

    /// <summary>
    /// MonoBehaviour.Awake()
    /// </summary>
    void Awake()
    {
        bUI_BattleUIController.instance.RegisterTurnOrderArea(this);
        turnIndicators = new TurnIndicator[turnIndicatorsCount];
        for (int i = 0; i < turnIndicatorsCount; i++) turnIndicators[i] = GenerateTurnIndicator(i);
    }

    /// <summary>
    /// Resets display based on last real turn order battler set.
    /// </summary>
    public void CancelPreview ()
    {
        DisplayWith(primaryTurnOrderBattlers);
    }

    /// <summary>
    /// Sets real turn order battler set and displays it.
    /// </summary>
    public void ConformToTurnOrder ()
    {
        primaryTurnOrderBattlers = BattleOverseer.GetBattlersByTurnOrder();
        DisplayWith(primaryTurnOrderBattlers);
    }

    /// <summary>
    /// Hides the turn info area.
    /// </summary>
    public void Hide ()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Sets prospective turn order battler set and displays it.
    /// </summary>
    public void PreviewTurnOrderForDelayOf (float delay)
    {
        prospectiveTurnOrderBattlers = BattleOverseer.GetBattlersBySimulatedTurnOrder(delay);
        DisplayWith(prospectiveTurnOrderBattlers);
    }

    /// <summary>
    /// Unhides the turn info area.
    /// </summary>
    public void Unhide ()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Updates the turn order display based on the given list of battlers.
    /// This runs the exact same code path whether we're working with real or prospective turn order,
    /// because we can just detect a tentative turn order placement by checking units after position 0
    /// to see if they're actually the same unit that's at position 0.
    /// </summary>
    private void DisplayWith(Battler[] battlersArray)
    {
        if (localBank == null) localBank = TextBankManager.Instance.GetTextBank("Battle/TurnOrderArea");
        guiText_Header.text = localBank.GetPage("next").text;
        for (int i = 0; i < turnIndicators.Length; i++)
        {
            if (i >= battlersArray.Length) turnIndicators[i].gameObject.SetActive(false);
            else
            {
                turnIndicators[i].gameObject.SetActive(true);
                if (i == 0)
                {
                    turnIndicators[i].guiText_Placement.text = localBank.GetPage("now").text;
                    turnIndicators[i].animator.Play(activeAnimHash);
                }
                else
                {
                    if (battlersArray[i] == battlersArray[0]) turnIndicators[i].animator.Play(prospectiveAnimHash);
                    else turnIndicators[i].animator.Play(standardAnimHash);
                    turnIndicators[i].guiText_Placement.text = i.ToString();
                }
                switch (BattleUtility.GetRelativeSidesFor(battlersArray[i].side, BattlerSideFlags.PlayerSide))
                {
                    case TargetSideFlags.MySide:
                        turnIndicators[i].background.color = bgColor_Player;
                        break;
                    case TargetSideFlags.MyFriends:
                        turnIndicators[i].background.color = bgColor_Friend;
                        break;
                    case TargetSideFlags.MyEnemies:
                        turnIndicators[i].background.color = bgColor_Enemy;
                        break;
                    default:
                        turnIndicators[i].background.color = bgColor_Neutral;
                        break;
                }
                Sprite iconSprite = Resources.Load<Sprite>(iconsResourcePath + battlersArray[i].battlerType.ToString());
                if (iconSprite == null) iconSprite = Resources.Load<Sprite>(iconsResourcePath + "noIcon"); // we do this silently because there are valid cases for a "unit" with no icon to need to display itself in the turn order
                if (iconSprite == null) throw new Exception("Couldn't find the no-icon icon placeholder whatsit!");
                turnIndicators[i].icon.sprite = iconSprite;
            }
        }
    }

    /// <summary>
    /// Creates turn indicator corresponding to index.
    /// </summary>
    private TurnIndicator GenerateTurnIndicator (int index)
    {
        GameObject turnIndicator = (GameObject)Instantiate(turnIndicatorPrefab, turnIndicatorsParent);
        turnIndicator.name = "Turn Order Indicator " + index.ToString();
        float x = 0;
        if (index > 0) x = turnIndicatorsOffsets.x;
        float y = turnIndicatorsOffsets.y * index;
        turnIndicator.transform.Translate(new Vector2(x, y));
        Animator animator = turnIndicator.GetComponent<Animator>();
        Image background = turnIndicator.transform.Find("PanelBG").GetComponent<Image>();
        Image icon = turnIndicator.transform.Find("Mugshot").GetComponent<Image>();
        TextMeshProUGUI guiText_Placement = turnIndicator.transform.Find("PlacementLabel").GetComponent<TextMeshProUGUI>();
        return new TurnIndicator(animator, turnIndicator, background, icon, guiText_Placement);
    }
}