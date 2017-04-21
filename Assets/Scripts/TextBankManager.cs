using System;
using System.Collections.Generic;
using Universe;

/// <summary>
/// Lightweight class that manages loading and handling of TextBanks.
/// </summary>
public class TextBankManager : Manager<TextBankManager>
{
    public TextLangType textLangType { get; private set; }
    private Dictionary<string, TextBank> textBanks;

    /// <summary>
    /// MonoBehaviour.Awake
    /// </summary>
    void Awake ()
    {
        textBanks = new Dictionary<string, TextBank>();
        textLangType = TextLangType.English;
    }

    /// <summary>
    /// Loads in the specified TextBank.
    /// </summary>
    private void LoadTextBank (string xmlFilePath)
    {
        TextBank textBank = new TextBank(xmlFilePath);
        textBanks.Add(xmlFilePath, textBank);
    }

    /// <summary>
    /// Fetches the specified TextBank.
    /// If it's not already loaded, loads it in first.
    /// </summary>
    public TextBank GetTextBank (string xmlFilePath)
    {
        if (!textBanks.ContainsKey(xmlFilePath)) LoadTextBank(xmlFilePath);
        return textBanks[xmlFilePath];
    }

    /// <summary>
    /// Changes languages to the specified TextLangType, then reloads all loaded text banks.
    /// </summary>
    public void ChangeLanguage (TextLangType _textLangType)
    {
        if (!Enum.IsDefined(typeof(TextLangType), _textLangType)) throw new Exception("Tried to change language to undefined value: " + _textLangType.ToString());
        else if (_textLangType == TextLangType.None) throw new Exception("Tried to change language to TextLangType.None.");
        else
        {
            textLangType = _textLangType;
        }
    }
}
