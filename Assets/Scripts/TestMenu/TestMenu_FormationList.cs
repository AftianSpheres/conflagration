using UnityEngine;
using CnfBattleSys;
using System;

/// <summary>
/// TestMenu MonoBehaviour that populates the formation select list
/// with the start-battle buttons. Kind of hacky, but I'm not
/// worried about this menu being pretty.
/// </summary>
public class TestMenu_FormationList : MonoBehaviour
{
    public GUIStyle buttonStyle;
    private BattleFormation[] allFormationsInRange;
    private GUILayoutOption[] scrollOptions;
    private RectTransform rectTransform;
    private TextBank testMenuBank;
    private Vector2 scrollPosition;
    private string[] buttonStrings;

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        scrollOptions = new GUILayoutOption[] { GUILayout.Width(350) };
    }

    /// <summary>
    /// MonoBehaviour.OnGUI ()
    /// </summary>
    void OnGUI ()
    {
        if (allFormationsInRange == null) Setup();
        if (allFormationsInRange == null) return;
        GUILayout.BeginArea(new Rect(820, 60, 500, 650));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, scrollOptions);
        for (int i = 0; i < allFormationsInRange.Length; i++)
        {
            if (GUILayout.Button(buttonStrings[i], buttonStyle)) TransitionToBattle(allFormationsInRange[i]);
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    /// <summary>
    /// Sets up the strings and formation data for this menu.
    /// </summary>
    private void Setup ()
    {
        if (TextBankManager.Instance == null) return;
        if (testMenuBank == null) testMenuBank = TextBankManager.Instance.GetTextBank("TestMenu/battle");
        allFormationsInRange = FormationDatabase.GetAll();
        buttonStrings = new string[allFormationsInRange.Length];
        string line0 = testMenuBank.GetPage("btn_line0").text;
        string line1 = testMenuBank.GetPage("btn_battler").text;
        string[] line0Placeholders = { "[#formationNo]", "[#formationID]", "[#venueID]", "[#bgmID]" };
        string[] line1Placeholders = { "[#enemyID]", "[#sideID]" };
        for (int i = 0; i < allFormationsInRange.Length; i++)
        {
            string[] replacements = { (i + 1).ToString(), allFormationsInRange[i].formation.ToString(), allFormationsInRange[i].venue.ToString(), allFormationsInRange[i].bgmTrack.ToString() };
            string thisLine0 = Util.BatchReplace(line0, line0Placeholders, replacements);
            string thisLine1 = string.Empty;
            for (int b = 0; b < allFormationsInRange[i].battlers.Length; b++)
            {
                replacements = new string[] { allFormationsInRange[i].battlers[b].battlerData.battlerType.ToString(), allFormationsInRange[i].battlers[b].side.ToString() };
                thisLine1 += Util.BatchReplace(line1, line1Placeholders, replacements);
                if (b + 1 < allFormationsInRange[i].battlers.Length) thisLine1 += ", ";
            }
            buttonStrings[i] = thisLine0 + Environment.NewLine + thisLine1;
        }
    }

    /// <summary>
    /// Starts a battle from the test menu.
    /// </summary>
    private void TransitionToBattle (BattleFormation formation)
    {
        BattleTransitionManager.Instance.EnterBattleScene(formation);
    }
}
