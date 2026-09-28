using System.Collections.Generic;
using UnityEngine;

public class MarkerPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] PooledMarker _markerPrefab;
    [SerializeField] int _poolSize = 9;

    readonly Queue<PooledMarker> _available = new();
    readonly List<PooledMarker> _active = new();

    void Awake()
    {
        for (int i = 0; i < _poolSize; i++)
        {
            var marker = Instantiate(_markerPrefab, transform);
            marker.gameObject.SetActive(false);
            _available.Enqueue(marker);
        }
        Debug.Log($"[ObjectPool] MarkerPool initialized: {_poolSize} markers pre-instantiated");
    }

    public PooledMarker Get(Transform parent)
    {
        if (_available.Count == 0)
        {
            Debug.LogWarning("[ObjectPool] Pool exhausted — no markers available");
            return null;
        }

        var marker = _available.Dequeue();
        marker.transform.SetParent(parent, false);
        marker.gameObject.SetActive(true);
        _active.Add(marker);
        Debug.Log($"[ObjectPool] Marker retrieved — active: {_active.Count}, available: {_available.Count}");
        return marker;
    }

    public void Return(PooledMarker marker)
    {
        if (marker == null) return;

        marker.DrawAnimation.ResetFill();
        marker.Image.enabled = false;
        marker.gameObject.SetActive(false);
        marker.transform.SetParent(transform, false);
        _active.Remove(marker);
        _available.Enqueue(marker);
        Debug.Log($"[ObjectPool] Marker returned — active: {_active.Count}, available: {_available.Count}");
    }

    public void ReturnAll()
    {
        int count = _active.Count;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var marker = _active[i];
            marker.DrawAnimation.ResetFill();
            marker.Image.enabled = false;
            marker.gameObject.SetActive(false);
            marker.transform.SetParent(transform, false);
            _available.Enqueue(marker);
        }
        _active.Clear();
        if (count > 0)
            Debug.Log($"[ObjectPool] All markers returned to pool (count: {count})");
    }
}
