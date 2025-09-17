using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text label;     // подтянем автоматом, если пусто
    [SerializeField] private Canvas canvas;      // чтобы проставить камеру

    [Header("FX")]
    [SerializeField] private float lifetime = 0.7f;
    [SerializeField] private float rise = 0.8f;        // насколько вверх уехать
    [SerializeField] private float startScale = 1.0f;
    [SerializeField] private float endScale = 0.8f;

    private float t;
    private Vector3 startPos;

    // ---------- NEW: статический фабричный метод ----------
    // Ждёт префаб по пути: Assets/Resources/UI/DamagePopup.prefab
    public static DamagePopup Spawn(Vector3 worldPos, string text, Color color)
    {
        var prefab = Resources.Load<DamagePopup>("UI/DamagePopup");
        if (prefab == null)
        {
            Debug.LogError("DamagePopup prefab not found at Resources/UI/DamagePopup");
            return null;
        }

        var inst = Instantiate(prefab, worldPos, Quaternion.identity);
        // подстрахуем камеру у канваса
        if (inst.canvas && inst.canvas.renderMode == RenderMode.WorldSpace && inst.canvas.worldCamera == null && Camera.main)
            inst.canvas.worldCamera = Camera.main;

        inst.Setup(text, color);
        return inst;
    }
    // ------------------------------------------------------

    private void Awake()
    {
        if (!label) label = GetComponentInChildren<TMP_Text>(true);
        if (!canvas) canvas = GetComponent<Canvas>();
        if (canvas && canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
            canvas.worldCamera = Camera.main; // чтобы в ворлд-канвасе работали дальности и ясность

        // если префаб пустой в сцене для пробы — покажем примерный текст
        if (label && string.IsNullOrEmpty(label.text)) label.text = "42";
    }

    // Упрощённый сетап: текст+цвет и сброс таймера
    public void Setup(string text, Color color)
    {
        if (label)
        {
            label.text = text;
            label.color = color;
        }
        startPos = transform.position;
        t = 0f;
    }

    // Оставляю твой Init на случай внешнего вызова; он идентичен Setup
    public void Init(string text, Color color) => Setup(text, color);

    private void LateUpdate()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / lifetime);

        // подниматься вверх
        transform.position = startPos + Vector3.up * Mathf.Lerp(0f, rise, k);

        // всегда смотреть на камеру
        var cam = Camera.main;
        if (cam) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);

        // скейл и затухание
        float s = Mathf.Lerp(startScale, endScale, k);
        transform.localScale = Vector3.one * s * 0.01f; // твой префаб уже в масштабе 0.01

        if (label)
        {
            var c = label.color;
            c.a = 1f - k;
            label.color = c;
        }

        if (t >= lifetime) Destroy(gameObject);
    }
}
