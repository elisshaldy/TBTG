// DraftPhaseController.cs - додатковий контролер для керування станом UI
using UnityEngine;
using UnityEngine.UI;

public class DraftPhaseController : MonoBehaviour
{
    [Header("UI Elements to Control")]
    public RawImage GameFieldRawImage;
    public CanvasGroup MainGameUI;
    public GameObject DraftPanel;
    public Button StartGameButton;

    [Header("Animation Settings")]
    public float FadeDuration = 0.5f;

    private GameDeckManager _deckManager;
    private bool _isDraftActive = false;

    void Start()
    {
        _deckManager = FindObjectOfType<GameDeckManager>();

        // Підписка на події
        if (_deckManager != null)
        {
            // Можна додати події для більш точного контролю
        }

        // Початковий стан - драфт активний
        SetDraftUIActive(true);
    }

    void Update()
    {
        // Моніторинг стану драфту (альтернативний підхід)
        if (_deckManager != null && _deckManager.IsDraftPhaseComplete() && _isDraftActive)
        {
            SetDraftUIActive(false);
        }
    }

    /// <summary>
    /// Контролює весь UI під час драфту
    /// </summary>
    public void SetDraftUIActive(bool isDraftActive)
    {
        _isDraftActive = isDraftActive;

        // Контроль RawImage
        if (GameFieldRawImage != null)
        {
            GameFieldRawImage.gameObject.SetActive(!isDraftActive);
            GameFieldRawImage.raycastTarget = !isDraftActive;
        }

        // Контроль основного UI гри
        if (MainGameUI != null)
        {
            MainGameUI.interactable = !isDraftActive;
            MainGameUI.blocksRaycasts = !isDraftActive;

            // Плавне затемнення/з'явлення
            StartCoroutine(FadeCanvasGroup(MainGameUI, isDraftActive ? 0.3f : 1f, FadeDuration));
        }

        // Контроль панелі драфту
        if (DraftPanel != null)
        {
            DraftPanel.SetActive(isDraftActive);
        }

        // Кнопка старту гри
        if (StartGameButton != null)
        {
            StartGameButton.gameObject.SetActive(!isDraftActive);
        }

        Debug.Log($"Draft UI Active: {isDraftActive}, Game Field Visible: {!isDraftActive}");
    }

    private System.Collections.IEnumerator FadeCanvasGroup(CanvasGroup group, float targetAlpha, float duration)
    {
        float startAlpha = group.alpha;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }

        group.alpha = targetAlpha;
    }

    /// <summary>
    /// Викликається кнопкою для початку гри після драфту
    /// </summary>
    public void OnStartGamePressed()
    {
        SetDraftUIActive(false);
        // Тут можна додати логіку запуску наступної фази гри
    }
}