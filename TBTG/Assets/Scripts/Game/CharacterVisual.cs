using UnityEngine;

public class CharacterVisual : MonoBehaviour
{
    [SerializeField] private Material _default;
    [SerializeField] private Material _active;
    
    [SerializeField] private MeshRenderer _rune;

    public void SetIsActive(bool isActive)
    {
        if (_rune != null)
        {
            _rune.material = isActive ? _active : _default;
        }
    }
}