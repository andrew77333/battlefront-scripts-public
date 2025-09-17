using UnityEngine;

#if UNITY_EDITOR
using UnityEditor; // только в редакторе
#endif

/// <summary>
/// Глобальные дефолты фильтров "контакта удара":
/// - фронтальный сектор (Front Arc)
/// - проверка прямой видимости (LOS)
/// Вариант C: агент может либо читать эти дефолты, либо переопределить локально.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Contact Defaults", fileName = "CombatContactDefaults")]
public class CombatContactDefaults : ScriptableObject
{
    [Header("Front Arc")]
    [Tooltip("Требовать, чтобы цель была в фронтальном секторе атакующего.")]
    public bool UseFrontArc = true;

    [Tooltip("Ширина фронтального сектора в градусах (180° = полукруг впереди).")]
    [Range(10f, 180f)] public float FrontArcAngleDeg = 150f;

    [Header("Line of Sight (LOS)")]
    [Tooltip("Проверять ли, что между атакующим и целью нет препятствий.")]
    public bool UseLOS = false;

    [Tooltip("Высота старта луча LOS от позиции атакующего (м).")]
    public float LOSRayHeight = 1.2f;

    [Tooltip("Какие слои считаются препятствиями для ближней атаки (ИСКЛЮЧИ персонажей).")]
    public LayerMask LOSMask = ~0;

    // Утилита-доступ к глобальному экземпляру. Ищем в Resources/CombatContactDefaults.asset.
    private static CombatContactDefaults _cached;
    public static CombatContactDefaults Instance
    {
        get
        {
            if (_cached != null) return _cached;

            // Пытаемся загрузить asset с дефолтным именем из любой папки Resources/
            _cached = Resources.Load<CombatContactDefaults>("CombatContactDefaults");

#if UNITY_EDITOR
            // В редакторе, если в Resources не нашли — попробуем через AssetDatabase (удобно в плеймоде).
            if (_cached == null)
            {
                var guids = AssetDatabase.FindAssets("t:CombatContactDefaults");
                if (guids != null && guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _cached = AssetDatabase.LoadAssetAtPath<CombatContactDefaults>(path);
                }
            }
#endif
            return _cached;
        }
    }
}
