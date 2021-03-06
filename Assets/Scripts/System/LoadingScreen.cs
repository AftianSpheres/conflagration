﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

using MovementEffects;
using ExtendedSceneManagement;

/// <summary>
/// Controls the loading screen.
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    /// <summary>
    /// Loading screen states.
    /// </summary>
    public enum State
    {
        None,
        Closed,
        DisplayOverScenes,
        DisplayWithShade
    }
    public static LoadingScreen instance { get; private set; }
    public Image loadingShade;
    private Text loadingText;
    public Animator[] unconditionalAnimators;
    public State state { get; private set; }
    public float progress { get; private set; }
    private TextBank globalUIBank;
    private readonly static int clipNameHash = Animator.StringToHash("Base Layer.loading");
    private const string thisTag = "_LoadingScreen_Display";

    /// <summary>
    /// MonoBehaviour.Awake?()
    /// </summary>
    void Awake()
    {
        instance = this;
        loadingText = GetComponentInChildren<Text>();
        Close();
    }

    /// <summary>
    /// Closes the loading screen.
    /// </summary>
    private void Close ()
    {
        loadingShade.gameObject.SetActive(false);
        loadingText.gameObject.SetActive(false);
        state = State.Closed;
        progress = 1.0f;
    }

    /// <summary>
    /// Brings up the loading screen with the black shade we use to hide
    /// partially loaded/unloaded scenes.
    /// </summary>
    public void DisplayWithShade ()
    {
        Timing.KillCoroutines(thisTag);
        Timing.RunCoroutine(_Display(true), thisTag);
    }

    /// <summary>
    /// Brings up the loading screen, but with no shade, for when we can get
    /// away with leaving what's onscreen visible.
    /// (Usually, the game won't stop responding to player input either.)
    /// </summary>
    public void DisplayWithoutShade ()
    {
        Timing.KillCoroutines(thisTag);
        Timing.RunCoroutine(_Display(false), thisTag);
    }

    /// <summary>
    /// Coroutine: displays loading screen until ops have finished executing,
    /// with or without shade.
    /// </summary>
    private IEnumerator<float> _Display (bool withShade)
    {
        if (globalUIBank == null) globalUIBank = TextBankManager.Instance.GetTextBank("System/Global");
        loadingText.gameObject.SetActive(true);
        loadingText.text = globalUIBank.GetPage("loading").text;
        for (int i = 0; i < unconditionalAnimators.Length; i++)
        {
            unconditionalAnimators[i].Play(clipNameHash, 0, 0);
        }
        if (withShade)
        {
            loadingShade.gameObject.SetActive(true);
            state = State.DisplayWithShade;
        }
        else
        {
            loadingShade.gameObject.SetActive(false);
            state = State.DisplayOverScenes;
        }
        bool isDone = false;
        while (!isDone)
        {
            progress = ExtendedSceneManager.Instance.GetProgressOfLoad();
            if (progress >= 1.0f) isDone = true;
            else
            {
                yield return progress; // we don't do anything too complex with this right now, but this is the most correct way to do this anyway + it gives us the ability to easily add animations or w/e tied to progress down the line
            }
        }
        Close();
    }
}
