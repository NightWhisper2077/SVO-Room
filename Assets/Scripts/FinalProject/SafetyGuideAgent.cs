using System.Collections.Generic;
using UnityEngine;

namespace FinalProject
{
    public sealed class SafetyGuideAgent : MonoBehaviour
    {
        [SerializeField] private AStarPathfinder pathfinder;
        [SerializeField] private Transform[] routePoints;
        [SerializeField] private LineRenderer routeLine;
        [SerializeField] private RouteMarkerPool markerPool;
        [SerializeField] private Transform modelRoot;
        [SerializeField] private float moveSpeed = 1.2f;
        [SerializeField] private float rotateSpeed = 540f;
        [SerializeField] private float waypointPause = 0.8f;
        [SerializeField] private float reachDistance = 0.12f;
        [SerializeField] private float hoverAmplitude = 0.045f;
        [SerializeField] private float hoverFrequency = 1.4f;
        [SerializeField] private float obstacleProbeRadius = 0.18f;
        [SerializeField] private float replanCooldown = 0.4f;
        [SerializeField] private bool showRoute = true;
        [SerializeField] private bool runOnStart = true;

        private readonly List<Vector3> currentPath = new List<Vector3>(64);
        private int pathIndex;
        private int nextRoutePoint;
        private int activeRoutePoint = -1;
        private float pauseTimer;
        private float replanTimer;
        private float baseModelY;
        private bool isPaused;

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0.1f, value);
        }

        public bool ShowRoute
        {
            get => showRoute;
            set
            {
                showRoute = value;
                RefreshRouteVisuals();
            }
        }

        public void Configure(AStarPathfinder newPathfinder, Transform[] newRoutePoints, LineRenderer newRouteLine, RouteMarkerPool newMarkerPool, Transform newModelRoot, float newMoveSpeed, bool routeVisible)
        {
            pathfinder = newPathfinder;
            routePoints = newRoutePoints;
            routeLine = newRouteLine;
            markerPool = newMarkerPool;
            modelRoot = newModelRoot != null ? newModelRoot : transform;
            moveSpeed = Mathf.Max(0.1f, newMoveSpeed);
            showRoute = routeVisible;
            baseModelY = modelRoot.localPosition.y;
            RefreshRouteVisuals();
        }

        private void Awake()
        {
            if (pathfinder == null)
                pathfinder = FindFirstObjectByType<AStarPathfinder>();

            if (modelRoot == null)
                modelRoot = transform;

            baseModelY = modelRoot.localPosition.y;
        }

        private void Start()
        {
            if (runOnStart)
                RequestNextPath();
        }

        private void Update()
        {
            AnimateHover();

            if (isPaused || routePoints == null || routePoints.Length == 0)
                return;

            if (pauseTimer > 0f)
            {
                pauseTimer -= Time.deltaTime;
                return;
            }

            if (replanTimer > 0f)
                replanTimer -= Time.deltaTime;

            if (currentPath.Count == 0 || pathIndex >= currentPath.Count)
            {
                RequestNextPath();
                return;
            }

            MoveAlongPath();
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        public void RestartRoute()
        {
            nextRoutePoint = 0;
            activeRoutePoint = -1;
            RequestNextPath();
        }

        private void RequestNextPath()
        {
            if (routePoints == null || routePoints.Length == 0)
            {
                currentPath.Clear();
                pathIndex = 0;
                RefreshRouteVisuals();
                return;
            }

            activeRoutePoint = nextRoutePoint;
            nextRoutePoint = (nextRoutePoint + 1) % routePoints.Length;
            RequestPathToActiveRoutePoint();
        }

        private void RequestPathToActiveRoutePoint()
        {
            currentPath.Clear();
            pathIndex = 0;

            if (routePoints == null || routePoints.Length == 0 || activeRoutePoint < 0 || activeRoutePoint >= routePoints.Length)
            {
                RefreshRouteVisuals();
                return;
            }

            var target = routePoints[activeRoutePoint];

            if (pathfinder != null && pathfinder.TryFindPath(transform.position, target.position, currentPath))
            {
                if (currentPath.Count > 1)
                    pathIndex = 1;
            }
            else
            {
                pauseTimer = replanCooldown;
            }

            RefreshRouteVisuals();
        }

        private void MoveAlongPath()
        {
            var target = currentPath[pathIndex];
            var currentPosition = transform.position;
            target.y = currentPosition.y;

            var toTarget = target - currentPosition;
            if (toTarget.magnitude <= reachDistance)
            {
                pathIndex++;
                if (pathIndex >= currentPath.Count)
                    pauseTimer = waypointPause;

                return;
            }

            if (pathfinder != null && !pathfinder.IsSegmentClear(currentPosition, target, obstacleProbeRadius))
            {
                if (replanTimer <= 0f)
                {
                    replanTimer = replanCooldown;
                    RequestPathToActiveRoutePoint();
                }

                return;
            }

            transform.position = Vector3.MoveTowards(currentPosition, target, moveSpeed * Time.deltaTime);

            var flatDirection = new Vector3(toTarget.x, 0f, toTarget.z);
            if (flatDirection.sqrMagnitude > 0.001f)
            {
                var targetRotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
            }
        }

        private void AnimateHover()
        {
            if (modelRoot == null)
                return;

            var localPosition = modelRoot.localPosition;
            localPosition.y = baseModelY + Mathf.Sin(Time.time * hoverFrequency * Mathf.PI * 2f) * hoverAmplitude;
            modelRoot.localPosition = localPosition;
        }

        private void RefreshRouteVisuals()
        {
            if (routeLine != null)
            {
                routeLine.enabled = showRoute && currentPath.Count > 1;
                routeLine.positionCount = routeLine.enabled ? currentPath.Count : 0;

                for (var i = 0; i < currentPath.Count; i++)
                    routeLine.SetPosition(i, currentPath[i] + Vector3.up * 0.025f);
            }

            if (markerPool != null)
                markerPool.Show(currentPath, showRoute);
        }
    }
}
