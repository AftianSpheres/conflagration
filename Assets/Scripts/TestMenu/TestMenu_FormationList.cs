using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using CnfBattleSys;
using TMPro;

/// <summary>
/// TestMenu MonoBehaviour that populates the formation select list
/// with the start-battle buttons. Kind of hacky, but I'm not
/// worried about this menu being pretty.
/// </summary>
public class TestMenu_FormationList : MonoBehaviour
{
    public RectTransform buttonsParent;
    private Button buttonsPrefab;
    private static TextBank localBank;
    private const string bgmPlaceholder = "!bgm!";
    private const string flagsPlaceholder = "!flags!";
    private const string venuePlaceholder = "!venue!";

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        buttonsPrefab = GetComponentInChildren<Button>();
        TextBankManager.DoOnceOnline(GenerateButtons);
    }

    /// <summary>
    /// Generates buttons for all formations and populates the scroll rect.
    /// </summary>
    private void GenerateButtons ()
    {
        if (localBank == null) localBank = TextBankManager.Instance.GetTextBank("TestMenu/battle");
        float baseSize = buttonsPrefab.GetComponent<RectTransform>().sizeDelta.y;
        BattleFormation[] formations = FormationDatabase.GetAll();
        buttonsParent.sizeDelta = new Vector2(buttonsParent.sizeDelta.x, baseSize * (formations.Length + 1));
        for (int i = 0; i < formations.Length; i++)
        {
            BattleFormation formation = formations[i];
            Button newButton = Instantiate(buttonsPrefab, buttonsParent);
            newButton.GetComponent<RectTransform>().anchoredPosition = baseSize * Vector2.down * i;
            TextMeshProUGUI label = newButton.GetComponentInChildren<TextMeshProUGUI>();
            string line0 = i + ": " + formation.formationID.ToString();
            string flagsList = string.Empty;
            for (int f = 1, b = 0; b < 32; b++, f = f << 1)
            {
                if ((formation.flags & (BattleFormationFlags)f) == (BattleFormationFlags)f)
                {
                    if (flagsList.Length > 0) flagsList += ", ";
                    flagsList += ((BattleFormationFlags)f).ToString();
                }
            }
            if (flagsList.Length == 0) flagsList = BattleFormationFlags.None.ToString();
            string line1 = localBank.GetPage("bgmLabel").text.Replace(bgmPlaceholder, formation.bgmTrack.ToString()) + ", " +
                           localBank.GetPage("venueLabel").text.Replace(venuePlaceholder, formation.venue.ToString()) + ", " +
                           localBank.GetPage("flagsLabel").text.Replace(flagsPlaceholder, flagsList);
            string line2 = GetBattlersListStringFor(formation);
            label.text = line0 + Environment.NewLine + line1 + Environment.NewLine + line2;
            
            newButton.onClick.AddListener(new UnityAction(() => { BattleTransitionManager.Instance.EnterBattleScene(formation); }));
            newButton.gameObject.name = "Button: " + formation.formationID.ToString();
        }
        buttonsPrefab.gameObject.SetActive(false);
        Destroy(buttonsPrefab.gameObject);
    }

    /// <summary>
    /// Get a string listing off every battler in the formation by side.
    /// </summary>
    private string GetBattlersListStringFor (BattleFormation formation)
    {
        Dictionary<BattlerSideFlags, Stack<BattlerData>> dict = new Dictionary<BattlerSideFlags, Stack<BattlerData>>(8);
        for (int b = 0; b < formation.battlers.Length; b++)
        {
            BattlerSideFlags side = formation.battlers[b].side;
            if (!dict.ContainsKey(side)) dict.Add(side, new Stack<BattlerData>(16));
            dict[side].Push(formation.battlers[b].battlerData);
        }
        BattlerSideFlags[] sides = new BattlerSideFlags[dict.Keys.Count];
        dict.Keys.CopyTo(sides, 0);
        string[] sideLists = new string[sides.Length];
        Dictionary<BattlerType, int> battlerCounts = new Dictionary<BattlerType, int>(16);
        for (int s = 0; s < sides.Length; s++)
        {
            sideLists[s] = string.Empty;
            int count = dict[sides[s]].Count;
            battlerCounts.Clear();
            for (int b = 0; b < count; b++)
            {
                BattlerData battlerData = dict[sides[s]].Pop();
                if (!battlerCounts.ContainsKey(battlerData.battlerType)) battlerCounts.Add(battlerData.battlerType, 1);
                else battlerCounts[battlerData.battlerType]++;
            }
            BattlerType[] battlerTypes = new BattlerType[battlerCounts.Keys.Count];
            battlerCounts.Keys.CopyTo(battlerTypes, 0);
            if (battlerTypes.Length > 0)
            {
                sideLists[s] += sides[s].ToString() + ": ";
                int originalLength = sideLists[s].Length;
                for (int b = 0; b < battlerTypes.Length; b++)
                {
                    if (sideLists[s].Length > originalLength) sideLists[s] += ", ";
                    sideLists[s] = sideLists[s] + battlerTypes[b].ToString() + " x" + battlerCounts[battlerTypes[b]].ToString();
                }
            }
        }
        string outputString = string.Empty;
        for (int s = 0; s < sideLists.Length; s++)
        {
            if (sideLists[s].Length > 0)
            {
                if (outputString.Length > 0) outputString += "; ";
                outputString += sideLists[s];
            }
        }
        return outputString;
    }
}
