using UnityEngine;

/// <summary>
/// Top-level test menu. Lightweight system
/// for handling transitions between test menus.
/// </summary>
public class TestMenu_TopLevel : MonoBehaviour
{
    /// <summary>
    /// GameObject containing widgets for the open menu.
    /// </summary>
    private GameObject openMenu;
    
    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        openMenu = gameObject;
    }

    /// <summary>
    /// Switch the test menu system focus to newFocus.
    /// </summary>
    public void SwitchFocus (GameObject newFocus)
    {
        openMenu.SetActive(false);
        openMenu = newFocus;
        openMenu.SetActive(true);
    }
}
