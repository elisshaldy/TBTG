using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using UnityEngine.Events;

[Serializable]
public class WindowInfo
{
    public Button Btn;
    public UIWindow Window;
    public UnityEvent OnOpen;
}

public class WindowManager : MonoBehaviour
{
    public static WindowManager Instance { get; private set; }

    public event Action<SceneState> OnSceneStateSelected;
    public SceneState CurrentSceneState { get; private set; }
    
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
                UIWindow targetWindow = w.Window;
                UnityEvent onOpen = w.OnOpen;
                w.Btn.onClick.AddListener(() => 
                {
                    OpenWindow(targetWindow);
                    onOpen?.Invoke();
                });
            }
        }
        
        _exitBtn.onClick.AddListener(Application.Quit);

        OpenMainMenu();
    }
    
    public void SelectSceneState(int stateIndex)
    {
        SelectSceneState((SceneState)stateIndex);
    }

    public void SelectSceneState(SceneState state)
    {
        //Debug.Log($"[WindowManager] SelectSceneState called with: {state}");
        CurrentSceneState = state;
        OnSceneStateSelected?.Invoke(state);
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