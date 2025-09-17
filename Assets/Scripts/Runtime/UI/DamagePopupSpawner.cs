using UnityEngine;

public static class DamagePopupSpawner
{
    private static DamagePopup _prefab;

    private static void EnsureLoaded()
    {
        if (_prefab == null)
            _prefab = Resources.Load<DamagePopup>("UI/DamagePopup"); // путь внутри Resources
    }

    public static void Show(Vector3 worldPos, string text, Color color)
    {
        EnsureLoaded();
        if (_prefab == null)
        {
            Debug.LogWarning("DamagePopup prefab not found at Resources/UI/DamagePopup");
            return;
        }

        var popup = Object.Instantiate(_prefab, worldPos, Quaternion.identity);
        popup.Init(text, color);
    }
}
