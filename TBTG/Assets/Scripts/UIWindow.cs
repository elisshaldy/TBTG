using UnityEngine;

public abstract class UIWindow : MonoBehaviour
{
    public virtual void OnShow() => gameObject.SetActive(true);
    public virtual void OnHide() => gameObject.SetActive(false);
}