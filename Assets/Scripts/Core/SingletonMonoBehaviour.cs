using UnityEngine;

public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log($"[Singleton] Duplicate {typeof(T).Name} detected — destroying");
            Destroy(gameObject);
            return;
        }

        Instance = this as T;
        DontDestroyOnLoad(gameObject);
        Debug.Log($"[Singleton] {typeof(T).Name} instance created");
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
        {
            Debug.Log($"[Singleton] {typeof(T).Name} instance cleared");
            Instance = null;
        }
    }
}
