using UnityEngine;
using TMPro;

public class RainbowText : MonoBehaviour
{
    [SerializeField] private float speed = 1f;

    private float _hue;
    private TextMeshProUGUI _text;

    private void Start()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }
    
    private void Update()
    {
        _hue += Time.deltaTime * speed;
        if (_hue > 1f) _hue -= 1f;

        _text.color = Color.HSVToRGB(_hue, 1f, 1f);
    }
}