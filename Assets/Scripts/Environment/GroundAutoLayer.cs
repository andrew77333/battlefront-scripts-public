using UnityEngine;

//Что это даёт: теперь любой твой «пол» сам переключится на слой Ground и будет готов для GroundMask у агентов. 
//Про это больше не надо помнить.

[ExecuteAlways] // работает и в редакторе, и в рантайме
public class GroundAutoLayer : MonoBehaviour
{
    [SerializeField] private string groundLayerName = "Ground";
    [SerializeField] private bool applyToChildren = true;

    private void Awake() { Apply(); }
    private void OnValidate() { Apply(); } // если переименуешь слой — обновится

    private void Apply()
    {
        int layer = LayerMask.NameToLayer(groundLayerName);
        if (layer < 0)
        {
            Debug.LogWarning($"[GroundAutoLayer] Layer '{groundLayerName}' не найден. Создай его в Tags & Layers.");
            return;
        }

        if (applyToChildren)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }
        else
        {
            gameObject.layer = layer;
        }

        // Не обязательно, но подстрахуем: у пола должен быть коллайдер и он не триггер
        if (!TryGetComponent<Collider>(out var col))
            col = gameObject.AddComponent<MeshCollider>();
        if (col != null) col.isTrigger = false;
    }
}
