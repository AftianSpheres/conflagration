using UnityEngine;
using System;
using System.Collections.Generic;
using System.Xml;

/// <summary>
/// Class that contains the data loaded from a single textbank.
/// </summary>
public class TextBank
{
    const string textBanksResourcePath = "Text/";

    /// <summary>
    /// Contains a block of text and associated metadata for control codes, etc. None of that actually exists right now,
    /// but encapsulating the string in a stub Page implementation means less work down the line when I actually get to this.
    /// </summary>
    public struct Page
    {
        /// <summary>
        /// The text associated with this TextBank.Page.
        /// </summary>
        public readonly string text;
        /// <summary>
        /// Is this a valid page?
        /// </summary>
        public readonly bool isValid;

        /// <summary>
        /// Constructor for a Page. Takes an unformatted string.
        /// Once we have control codes and shit, this needs to parse those out and set up the appropriate metadata.
        /// </summary>
        public Page(string _text, bool _isValid = true)
        {
            text = _text;
            isValid = _isValid;
        }

        /// <summary>
        /// Gets the text in this page in lowercase, but only if the current textLangType permits that.
        /// </summary>
        public string TextAsLower()
        {
            if (TextBankManager.Instance.CurrentTextLangTypeSupportsCases()) return text.ToLower();
            else return text;
        }

        /// <summary>
        /// Gets the text in this page in uppercase, but only if the current textLangType permits that.
        /// </summary>
        public string TextAsUpper ()
        {
            if (TextBankManager.Instance.CurrentTextLangTypeSupportsCases()) return text.ToUpper();
            else return text;
        }
    }

    /// <summary>
    /// Dictionary that stores string:page associations.
    /// We use this if (and only if) we're an enum-backed textbank.
    /// This dict isn't exposed - basically, we want to take an enum value
    /// and invisibly return the Page associated with the name
    /// </summary>
    private Dictionary<string, Page> backingDict_ForEnumAssociatedBank;

    /// <summary>
    /// Dictionary containing pages that are given names within the xml file.
    /// This actually stores indices in the main pages array, not pages, so
    /// keep that in mind. (Page is a value type, so we don't want to store duplicates here
    /// when we can just use integer indices to get a page out of the array.)
    /// </summary>
    private Dictionary<string, int> namedPageIndices;

    /// <summary>
    /// Pages in the textbank.
    /// </summary>
    private Page[] pages;

    /// <summary>
    /// The language that was set when this TextBank was loaded.
    /// If this is ever in disagreement with the manager's textLangType, we'll
    /// reload the text bank before we pass out a Page.
    /// </summary>
    private TextLangType textLangType;

    /// <summary>
    /// Type of the associated enum.
    /// If enumBackingType != null, this is an enum-associated textbank,
    /// which results in some different behavior at the loading stage
    /// in order to facilitate direct enum-value-to-Page transactions.
    /// </summary>
    private Type enumBackingType;

    /// <summary>
    /// The original filename the textbank was loaded from.
    /// </summary>
    public readonly string fileName;

    /// <summary>
    /// Constructor. Sets fileName based on the given path, then loads in the Pages.
    /// </summary>
    public TextBank(string xmlFilePath)
    {
        fileName = textBanksResourcePath + xmlFilePath;
        namedPageIndices = new Dictionary<string, int>();
        LoadIn();
    }

    /// <summary>
    /// Special constructor for textbanks that are associated with specific enumerated types.
    /// </summary>
    public TextBank(string xmlFilePath, Type _enumBackingType)
    {
        fileName = textBanksResourcePath + xmlFilePath;
        backingDict_ForEnumAssociatedBank = new Dictionary<string, Page>();
        enumBackingType = _enumBackingType;
    }

    /// <summary>
    /// Loads strings from the xml file at fileName.
    /// If associatedEnumType != null, we'll make sure to match the indices to the enum entries.
    /// </summary>
    public void LoadIn ()
    {
        textLangType = TextBankManager.Instance.textLangType;
        if (namedPageIndices != null) namedPageIndices.Clear();
        if (backingDict_ForEnumAssociatedBank != null) backingDict_ForEnumAssociatedBank.Clear();
        TextAsset xmlFile = Resources.Load<TextAsset>(fileName);
        if (xmlFile == null) Util.Crash(new Exception("Tried to import text bank at bad filepath: " + fileName));
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xmlFile.text);
        XmlNode rootNode = doc.DocumentElement;
        const string langNode_English = "eng";
        string langNode;
        switch (textLangType)
        {
            case TextLangType.English:
                langNode = langNode_English;
                break;
            default:
                Util.Crash(new Exception("Invalid language type: " + TextBankManager.Instance.textLangType.ToString()));
                langNode = string.Empty;
                break;
        }
        XmlNodeList pageNodes = rootNode.SelectNodes("//page");
        Page[] _pages;
        if (enumBackingType == null) _pages = new Page[pageNodes.Count];
        else _pages = default(Page[]);
        for (int i = 0; i < pageNodes.Count; i++)
        {
            XmlNode pageNode = pageNodes[i].SelectSingleNode(langNode);
            XmlAttribute nameAttribute = pageNodes[i].Attributes.GetNamedItem("name") as XmlAttribute;
            if (nameAttribute != null && enumBackingType == null)
            {
                string name = nameAttribute.Value;
                if (namedPageIndices.ContainsKey(name)) Util.Crash(new Exception("Page name collision in " + fileName + " for name " + name));
                namedPageIndices[name] = i;
            }
            string text;
            if (pageNode != null) text = pageNode.InnerText;
            else text = "No " + textLangType.ToString() + " entry for textbank " + fileName + ", line " + i;
            if (enumBackingType != null)
            {
                if (nameAttribute == null) Util.Crash(new Exception("Page " + i.ToString() + " in " + fileName + " has no page name. Every page in a textBank tied to an enum needs to be named with the associated enum value."));
                if (!Enum.IsDefined(enumBackingType, nameAttribute.InnerText)) Util.Crash(new Exception("No item of name " + nameAttribute.InnerText + " in enumeration " + enumBackingType.Name));
                backingDict_ForEnumAssociatedBank[nameAttribute.InnerText] = new Page(text);
            }
            else _pages[i] = new Page(text);

        }
        if (enumBackingType == null) pages = _pages;
        Resources.UnloadAsset(xmlFile);
    }

    /// <summary>
    /// Returns a Page by index within the pages array.
    /// </summary>
    public Page GetPage (int index)
    {
        if (textLangType != TextBankManager.Instance.textLangType) LoadIn();
        if (backingDict_ForEnumAssociatedBank != null)
        {
            Util.Crash(new Exception("Can't get enum-associated textbanks with page indices!"));
        }
        if (index >= pages.Length) return new Page("Bad textbank / page index combination: " + fileName + ", page " + index, false);
        return pages[index];
    }

    /// <summary>
    /// Returns a page by name, if a page of that name exists.
    /// </summary>
    public Page GetPage (string name)
    {
        if (textLangType != TextBankManager.Instance.textLangType) LoadIn();
        if (backingDict_ForEnumAssociatedBank != null)
        {
            if (!backingDict_ForEnumAssociatedBank.ContainsKey(name)) return new Page("Bad common textbank / page name combination: " + fileName + ", named page " + name, false);
            return backingDict_ForEnumAssociatedBank[name];
        }
        if (!namedPageIndices.ContainsKey(name)) return new Page("Bad textbank / page name combination: " + fileName + ", named page " + name, false);
        return pages[namedPageIndices[name]];
    }

    /// <summary>
    /// Generic functionality for getting pages based on enum values,
    /// where appropriate.
    /// </summary>
    public Page GetPage<T> (T enumName)
    {
        string s = Enum.GetName(enumBackingType, enumName);
        return GetPage(s);
    }
}
