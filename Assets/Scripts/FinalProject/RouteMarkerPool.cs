using System.Collections.Generic;
using UnityEngine;

namespace FinalProject
{
    public sealed class RouteMarkerPool : MonoBehaviour
    {
        [SerializeField] private GameObject markerPrefab = null;
        [SerializeField] private Material markerMaterial;
        [SerializeField] private float markerScale = 0.06f;
        [SerializeField] private int stride = 2;

        private readonly List<GameObject> pool = new List<GameObject>(64);

        public void Configure(Material material, float scale, int markerStride)
        {
            markerMaterial = material;
            markerScale = Mathf.Max(0.01f, scale);
            stride = Mathf.Max(1, markerStride);
        }

        public void SetVisible(bool visible)
        {
            for (var i = 0; i < pool.Count; i++)
                pool[i].SetActive(visible && pool[i].activeSelf);
        }

        public void Show(IReadOnlyList<Vector3> path, bool visible)
        {
            HideAll();

            if (!visible || path == null || path.Count == 0)
                return;

            var markerIndex = 0;
            for (var i = 0; i < path.Count; i += Mathf.Max(1, stride))
            {
                var marker = GetMarker(markerIndex++);
                marker.transform.position = path[i] + Vector3.up * 0.015f;
                marker.transform.localScale = Vector3.one * markerScale;
                marker.SetActive(true);
            }
        }

        private void HideAll()
        {
            for (var i = 0; i < pool.Count; i++)
                pool[i].SetActive(false);
        }

        private GameObject GetMarker(int index)
        {
            while (pool.Count <= index)
                pool.Add(CreateMarker());

            return pool[index];
        }

        private GameObject CreateMarker()
        {
            GameObject marker;
            if (markerPrefab != null)
            {
                marker = Instantiate(markerPrefab, transform);
            }
            else
            {
                marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.transform.SetParent(transform, false);
                var collider = marker.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);
            }

            marker.name = "Pooled A* Route Marker";
            var renderer = marker.GetComponentInChildren<Renderer>();
            if (renderer != null && markerMaterial != null)
                renderer.sharedMaterial = markerMaterial;

            marker.SetActive(false);
            return marker;
        }
    }
}
