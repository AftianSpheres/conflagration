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
    private Dictionary<Type, TextBank> commonTextBanks;
    readonly static string[] commonTextBanks_FilePaths = new string[] { "System/Common/BattlerName", "System/Common/ActionName", "System/Common/StanceName" };
    readonly static Type[] commonTextBanks_AssociatedEnums = new Type[] { typeof(CnfBattleSys.BattlerType), typeof(CnfBattleSys.ActionType), typeof(CnfBattleSys.StanceType) };

    /// <summary>
    /// MonoBehaviour.Awake
    /// </summary>
    void Awake ()
    {
        textBanks = new Dictionary<string, TextBank>();
        textLangType = TextLangType.English;
    }

    /// <summary>
    /// Loads in all common textbanks and binds them to their types.
    /// </summary>
    private void LoadAllCommonTextBanks ()
    {
        if (commonTextBanks == null) commonTextBanks = new Dictionary<Type, TextBank>(commonTextBanks_FilePaths.Length);
        else commonTextBanks.Clear();
        for (int i = 0; i < commonTextBanks_FilePaths.Length; i++)
        {
            LoadCommonTextBank(commonTextBanks_FilePaths[i], commonTextBanks_AssociatedEnums[i]);
        }
    }

    /// <summary>
    /// Loads in the specified common textbank and binds it to the given Type.
    /// Note that you can only bind one textbank to each type.
    /// </summary>
    private void LoadCommonTextBank (string xmlFilePath, Type enumType)
    {
        TextBank textBank = new TextBank(xmlFilePath, enumType);
        textBanks.Add(xmlFilePath, textBank);
        commonTextBanks.Add(enumType, textBank);
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
    /// Returns the common textbank associated with the given type.
    /// </summary>
    public TextBank GetCommonTextBank (Type enumType)
    {
        if (commonTextBanks == null) LoadAllCommonTextBanks();
        if (!commonTextBanks.ContainsKey(enumType)) Util.Crash(new Exception("No common textbank associated with type " + enumType.Name));
        return commonTextBanks[enumType];
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
        if (!Enum.IsDefined(typeof(TextLangType), _textLangType)) Util.Crash(new Exception("Tried to change language to undefined value: " + _textLangType.ToString()));
        else if (_textLangType == TextLangType.None) Util.Crash(new Exception("Tried to change language to TextLangType.None."));
        else
        {
            textLangType = _textLangType;
        }
    }
}
