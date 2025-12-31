using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

[Serializable]
public class WindowInfo
{
    public Button Btn;
    public UIWindow Window;
}

public class WindowManager : MonoBehaviour
{
    public static WindowManager Instance { get; private set; }

    [SerializeField] private UIWindow _mainMenu;
    [SerializeField] private Button _exitBtn;
    [SerializeField] private List<WindowInfo> _windows;

    private Stack<UIWindow> _history = new();

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
        
        foreach (var w in _windows)
        {
            if (w.Window != null)
                w.Window.OnHide();

            if (w.Btn != null)
            {
                UIWindow capturedWin = w.Window;
                w.Btn.onClick.AddListener(() => OpenWindow(capturedWin));
            }
        }
        
        _exitBtn.onClick.AddListener(Application.Quit);

        OpenMainMenu();
    }

    public void OpenWindow(UIWindow window)
    {
        if (_history.Count > 0)
            _history.Peek().OnHide();

        _history.Push(window);
        window.OnShow();
    }

    public void Back()
    {
        if (_history.Count <= 1) return;

        _history.Pop().OnHide();
        _history.Peek().OnShow();
    }

    public void OpenMainMenu()
    {
        while (_history.Count > 0)
            _history.Pop().OnHide();

        _history.Push(_mainMenu);
        _mainMenu.OnShow();
    }
}