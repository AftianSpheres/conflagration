using UnityEngine;
using UnityEngine.UI;
using CnfBattleSys;


/// <summary>
/// One of the status icons managed by the StatusBar.
/// </summary>
public class bUI_StatusIcon : MonoBehaviour
{
    private Image bgImage;
    private Text count;
    private string statusIconsResourcePath = "Battle/2D/UI/StatusIcon/";

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake()
    {
        bgImage = GetComponent<Image>();
        count = GetComponentInChildren<Text>();
    }

    /// <summary>
    /// Conforms state of icon to given status packet.
    /// </summary>
    public void ConformToStatus (StatusPacket sPacket)
    {
        gameObject.SetActive(true);
        if (sPacket.duration == float.PositiveInfinity) count.text = string.Empty;
        else count.text = sPacket.duration.ToString();
        Sprite iconSprite = Resources.Load<Sprite>(statusIconsResourcePath + sPacket.statusType.ToString());
        if (iconSprite == null) iconSprite = Resources.Load<Sprite>(statusIconsResourcePath + "InvalidStatus");
        if (iconSprite == null) Util.Crash("Couldn't load default status icon!");
        bgImage.sprite = iconSprite;
    }

    /// <summary>
    /// Conceals unused status icons.
    /// </summary>
    public void Hide ()
    {
        gameObject.SetActive(false);
    }
}
