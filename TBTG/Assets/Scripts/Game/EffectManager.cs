using UnityEngine;
using System.Collections;

/// <summary>
/// Менеджер спецефектів (заглушки)
/// </summary>
public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Ефект на самому персонажі, який атакує (дим, реакція)
    /// </summary>
    public void PlayAttackerEffect(Vector3 position)
    {
        CreatePlaceholder(position + Vector3.up * 0.8f, Color.gray, "Attacker_Smoke_Stub", 0.7f);
    }

    /// <summary>
    /// Ефект попадання по персонажу (реакція на урон)
    /// </summary>
    public void PlayHitEffect(Vector3 position)
    {
        CreatePlaceholder(position + Vector3.up * 1.0f, Color.red, "Hit_Reaction_Stub", 1.0f);
    }

    /// <summary>
    /// Ефект атаки по порожній клітинці (де нікого немає)
    /// </summary>
    public void PlayMissEffect(Vector3 position)
    {
        CreatePlaceholder(position + Vector3.up * 0.1f, Color.cyan, "Miss_EmptyTile_Stub", 0.4f);
    }

    private void CreatePlaceholder(Vector3 pos, Color color, string name, float scale)
    {
        GameObject stub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        stub.name = name;
        stub.transform.position = pos;
        stub.transform.localScale = Vector3.one * scale;
        
        Destroy(stub.GetComponent<Collider>()); // Щоб не заважало клікам

        var renderer = stub.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.color = color;
        }

        StartCoroutine(ExecutePulse(stub));
    }

    private IEnumerator ExecutePulse(GameObject obj)
    {
        float duration = 0.6f;
        float elapsed = 0;
        Vector3 startScale = obj.transform.localScale;
        var renderer = obj.GetComponent<Renderer>();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            obj.transform.localScale = startScale * (1f + t * 0.5f);
            if (renderer != null)
            {
                Color c = renderer.material.color;
                c.a = 1f - t;
                renderer.material.color = c;
            }
            yield return null;
        }
        Destroy(obj);
    }

    // Тимчасовий метод для тесту
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) PlayAttackerEffect(Vector3.zero);
        if (Input.GetKeyDown(KeyCode.Alpha2)) PlayHitEffect(Vector3.zero);
        if (Input.GetKeyDown(KeyCode.Alpha3)) PlayMissEffect(Vector3.zero);
    }
}
