using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ModTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float _showDelay = 0.5f;
    
    private ModData _modData;
    private bool _isHovering = false;
    private Coroutine _showCoroutine;

    public void SetData(ModData modData)
    {
        _modData = modData;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        if (_modData != null && ModTooltip.Instance != null)
        {
            if (_showCoroutine != null)
                StopCoroutine(_showCoroutine);
            _showCoroutine = StartCoroutine(ShowAfterDelay(eventData.position));
        }
    }

    private IEnumerator ShowAfterDelay(Vector2 position)
    {
        yield return new WaitForSeconds(_showDelay);
        
        if (_isHovering && _modData != null && ModTooltip.Instance != null)
        {
            ModTooltip.Instance.Show(_modData, Input.mousePosition);
        }
        _showCoroutine = null;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        
        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine);
            _showCoroutine = null;
        }
        
        if (ModTooltip.Instance != null)
        {
            ModTooltip.Instance.Hide();
        }
    }

    private void OnDisable()
    {
        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine);
            _showCoroutine = null;
        }
        
        if (_isHovering && ModTooltip.Instance != null)
        {
            _isHovering = false;
            ModTooltip.Instance.Hide();
        }
    }

    private void Update()
    {
        if (_isHovering && _modData != null && ModTooltip.Instance != null)
        {
            ModTooltip.Instance.UpdatePosition(Input.mousePosition);
        }
    }
}
