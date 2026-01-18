using UnityEngine;
using UnityEngine.UI;

public class BackButton : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => WindowManager.Instance.Back());
    }
}