using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelMain : MonoBehaviour, IMenu
{
    [SerializeField] private Button btnTimer;

    [SerializeField] private Button btnMoves;
    [SerializeField] private Button btnAutoplay;
    [SerializeField] private Button btnAutoLose;
    private UIMainManager m_mngr;

    private void Awake()
    {
        btnMoves.onClick.AddListener(OnClickMoves);
        btnTimer.onClick.AddListener(OnClickTimer);
    }

    private void OnDestroy()
    {
        if (btnMoves) btnMoves.onClick.RemoveAllListeners();
        if (btnTimer) btnTimer.onClick.RemoveAllListeners();
    }

    public void Setup(UIMainManager mngr)
    {
        m_mngr = mngr;
        btnAutoplay.onClick.AddListener(OnClickAutoplay);
        btnAutoLose.onClick.AddListener(OnClickAutoLose);
    }

    private void OnClickTimer()
    {
        PlayerPrefs.SetInt("AutoPlay", 0);
        PlayerPrefs.SetInt("AutoLose", 0);
        m_mngr.LoadLevelTimer();
    }

    private void OnClickMoves()
    {
        PlayerPrefs.SetInt("AutoPlay", 0);
        PlayerPrefs.SetInt("AutoLose", 0);
        m_mngr.LoadLevelMoves();
    }

    private void OnClickAutoplay()
    {
        PlayerPrefs.SetInt("AutoPlay", 1);
        PlayerPrefs.SetInt("AutoLose", 0);
        m_mngr.LoadLevelMoves(); 
    }

    private void OnClickAutoLose()
    {
        PlayerPrefs.SetInt("AutoPlay", 1);
        PlayerPrefs.SetInt("AutoLose", 1);
        m_mngr.LoadLevelMoves();
    }
    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
