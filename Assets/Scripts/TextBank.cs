using UnityEngine;
using System;
using System.Collections.Generic;
using System.Xml;

/// <summary>
/// Class that contains the data loaded from a single textbank.
/// </summary>
public class TextBank
{
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
        /// Constructor for a Page. Takes an unformatted string.
        /// Once we have control codes and shit, this needs to parse those out and set up the appropriate metadata.
        /// </summary>
        public Page(string _text)
        {
            text = _text;
        }
    }

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
    /// The original filename the textbank was loaded from.
    /// </summary>
    public readonly string fileName;

    /// <summary>
    /// Constructor. Sets fileName based on the given path, then loads in the Pages.
    /// </summary>
    public TextBank(string xmlFilePath)
    {
        const string textBanksResourcePath = "Text/";
        fileName = textBanksResourcePath + xmlFilePath;
        namedPageIndices = new Dictionary<string, int>();
        LoadIn();
    }

    /// <summary>
    /// Loads strings from the xml file at fileName.
    /// </summary>
    public void LoadIn ()
    {
        textLangType = TextBankManager.Instance.textLangType;
        namedPageIndices.Clear();
        TextAsset xmlFile = Resources.Load<TextAsset>(fileName);
        if (xmlFile == null) throw new Exception("Tried to import text bank at bad filepath: " + fileName);
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
                throw new Exception("Invalid language type: " + TextBankManager.Instance.textLangType.ToString());
        }
        XmlNodeList pageNodes = rootNode.SelectNodes("//page");
        Page[] _pages = new Page[pageNodes.Count];
        for (int i = 0; i < _pages.Length; i++)
        {
            XmlNode pageNode = pageNodes[i].SelectSingleNode(langNode);
            XmlAttribute nameAttribute = pageNodes[i].Attributes.GetNamedItem("name") as XmlAttribute;
            if (nameAttribute != null)
            {
                string name = nameAttribute.Value;
                if (namedPageIndices.ContainsKey(name)) throw new Exception("Page name collision in " + fileName + " for name " + name);
                namedPageIndices[name] = i;
            }
            string text;
            if (pageNode != null) text = pageNode.InnerText;
            else text = "No " + textLangType.ToString() + " entry for textbank " + fileName + ", line " + i;
            _pages[i] = new Page(text);
        }
        pages = _pages;
    }

    /// <summary>
    /// Returns a Page by index within the pages array.
    /// </summary>
    public Page GetPage (int index)
    {
        if (textLangType != TextBankManager.Instance.textLangType) LoadIn();
        if (index >= pages.Length) return new Page("Bad textbank / page index combination: " + fileName + ", page " + index);
        return pages[index];
    }

    /// <summary>
    /// Returns a page by name, if a page of that name exists.
    /// </summary>
    public Page GetPage (string name)
    {
        if (textLangType != TextBankManager.Instance.textLangType) LoadIn();
        if (!namedPageIndices.ContainsKey(name)) return new Page("Bad textbank / page name combination: " + fileName + ", named page " + name);
        return pages[namedPageIndices[name]];
    }
}
