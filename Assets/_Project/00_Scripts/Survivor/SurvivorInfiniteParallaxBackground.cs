using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace StrandedRoguelike
{
    [DefaultExecutionOrder(100)]
    public sealed class SurvivorInfiniteParallaxBackground : MonoBehaviour
    {
        private const int RequiredGridCount = 4;
        private const int MaxRepositionsPerFrame = 128;

        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Grid Objects")]
        [SerializeField] private Transform leftTop;
        [SerializeField] private Transform leftDown;
        [SerializeField] private Transform rightTop;
        [SerializeField] private Transform rightDown;
        [SerializeField] private bool autoFindSceneGrids = true;

        [Header("Grid Size")]
        [SerializeField] private bool autoDetectGridSize = true;
        [SerializeField] private Vector2 gridSize = new Vector2(20f, 20f);

        private bool initialized;
        private bool setupWarningShown;

        public void Configure(Transform newTarget, Transform[] grids = null)
        {
            if (newTarget != null)
            {
                target = newTarget;
            }

            if (grids != null && grids.Length > 0)
            {
                AssignGridRoles(grids);
            }

            initialized = false;
            setupWarningShown = false;
            Initialize();
        }

        private void Awake()
        {
            Initialize();
        }

        private void LateUpdate()
        {
            if (!initialized && !Initialize())
            {
                return;
            }

            RecycleHorizontalColumns();
            RecycleVerticalRows();
        }

        private bool Initialize()
        {
            ResolveTarget();
            ResolveGridObjects();

            if (target == null || !HasAllGridObjects())
            {
                ShowSetupWarning();
                initialized = false;
                return false;
            }

            if (autoDetectGridSize)
            {
                gridSize = DetectGridSize(gridSize);
            }

            gridSize.x = Mathf.Max(0.1f, gridSize.x);
            gridSize.y = Mathf.Max(0.1f, gridSize.y);
            initialized = true;
            return true;
        }

        private void ResolveTarget()
        {
            if (target != null)
            {
                return;
            }

            PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
            if (movement != null)
            {
                target = movement.transform;
            }
        }

        private void ResolveGridObjects()
        {
            if (HasAllGridObjects() || !autoFindSceneGrids)
            {
                return;
            }

            Grid[] childGrids = GetComponentsInChildren<Grid>(true);
            if (TryAssignGridComponents(childGrids))
            {
                return;
            }

            Grid[] sceneGrids = FindObjectsByType<Grid>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            TryAssignGridComponents(sceneGrids);
        }

        private bool TryAssignGridComponents(Grid[] grids)
        {
            if (grids == null)
            {
                return false;
            }

            List<Transform> gridTransforms = new List<Transform>(RequiredGridCount);
            for (int i = 0; i < grids.Length; i++)
            {
                Grid grid = grids[i];
                if (grid == null || grid.transform == transform || !grid.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (!gridTransforms.Contains(grid.transform))
                {
                    gridTransforms.Add(grid.transform);
                }
            }

            if (gridTransforms.Count != RequiredGridCount)
            {
                return false;
            }

            AssignGridRoles(gridTransforms.ToArray());
            return HasAllGridObjects();
        }

        private void AssignGridRoles(Transform[] grids)
        {
            List<Transform> uniqueGrids = new List<Transform>(RequiredGridCount);
            for (int i = 0; i < grids.Length; i++)
            {
                if (grids[i] != null && !uniqueGrids.Contains(grids[i]))
                {
                    uniqueGrids.Add(grids[i]);
                }
            }

            if (uniqueGrids.Count != RequiredGridCount)
            {
                return;
            }

            if (TryAssignRolesByName(uniqueGrids))
            {
                return;
            }

            uniqueGrids.Sort(CompareTopToBottomThenLeftToRight);

            Transform firstTop = uniqueGrids[0];
            Transform secondTop = uniqueGrids[1];
            Transform firstDown = uniqueGrids[2];
            Transform secondDown = uniqueGrids[3];

            AssignLeftAndRight(firstTop, secondTop, out leftTop, out rightTop);
            AssignLeftAndRight(firstDown, secondDown, out leftDown, out rightDown);
        }

        private bool TryAssignRolesByName(List<Transform> grids)
        {
            Transform namedLeftTop = null;
            Transform namedLeftDown = null;
            Transform namedRightTop = null;
            Transform namedRightDown = null;

            for (int i = 0; i < grids.Count; i++)
            {
                Transform grid = grids[i];
                string normalizedName = NormalizeName(grid.name);

                if (normalizedName.Contains("lefttop"))
                {
                    namedLeftTop = grid;
                }
                else if (normalizedName.Contains("leftdown") || normalizedName.Contains("leftbottom"))
                {
                    namedLeftDown = grid;
                }
                else if (normalizedName.Contains("righttop"))
                {
                    namedRightTop = grid;
                }
                else if (normalizedName.Contains("rightdown") || normalizedName.Contains("rightbottom"))
                {
                    namedRightDown = grid;
                }
            }

            if (namedLeftTop == null || namedLeftDown == null || namedRightTop == null || namedRightDown == null)
            {
                return false;
            }

            leftTop = namedLeftTop;
            leftDown = namedLeftDown;
            rightTop = namedRightTop;
            rightDown = namedRightDown;
            return true;
        }

        private static string NormalizeName(string objectName)
        {
            return objectName
                .ToLowerInvariant()
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty);
        }

        private int CompareTopToBottomThenLeftToRight(Transform first, Transform second)
        {
            Vector2 firstCenter = GetGridCenter(first);
            Vector2 secondCenter = GetGridCenter(second);

            if (Mathf.Abs(firstCenter.y - secondCenter.y) > 0.05f)
            {
                return secondCenter.y.CompareTo(firstCenter.y);
            }

            return firstCenter.x.CompareTo(secondCenter.x);
        }

        private void AssignLeftAndRight(Transform first, Transform second, out Transform left, out Transform right)
        {
            if (GetGridCenter(first).x <= GetGridCenter(second).x)
            {
                left = first;
                right = second;
            }
            else
            {
                left = second;
                right = first;
            }
        }

        private bool HasAllGridObjects()
        {
            if (leftTop == null || leftDown == null || rightTop == null || rightDown == null)
            {
                return false;
            }

            HashSet<Transform> uniqueGrids = new HashSet<Transform>
            {
                leftTop,
                leftDown,
                rightTop,
                rightDown
            };
            return uniqueGrids.Count == RequiredGridCount;
        }

        private Vector2 DetectGridSize(Vector2 fallback)
        {
            float topWidth = Mathf.Abs(rightTop.position.x - leftTop.position.x);
            float downWidth = Mathf.Abs(rightDown.position.x - leftDown.position.x);
            float leftHeight = Mathf.Abs(leftTop.position.y - leftDown.position.y);
            float rightHeight = Mathf.Abs(rightTop.position.y - rightDown.position.y);

            float detectedWidth = AveragePositive(topWidth, downWidth);
            float detectedHeight = AveragePositive(leftHeight, rightHeight);

            Vector2 rendererSize = GetLargestGridRendererSize();
            if (detectedWidth <= 0.1f)
            {
                detectedWidth = rendererSize.x;
            }

            if (detectedHeight <= 0.1f)
            {
                detectedHeight = rendererSize.y;
            }

            return new Vector2(
                detectedWidth > 0.1f ? detectedWidth : fallback.x,
                detectedHeight > 0.1f ? detectedHeight : fallback.y);
        }

        private static float AveragePositive(float first, float second)
        {
            bool hasFirst = first > 0.1f;
            bool hasSecond = second > 0.1f;

            if (hasFirst && hasSecond)
            {
                return (first + second) * 0.5f;
            }

            if (hasFirst)
            {
                return first;
            }

            return hasSecond ? second : 0f;
        }

        private Vector2 GetLargestGridRendererSize()
        {
            Transform[] grids = { leftTop, leftDown, rightTop, rightDown };
            Vector2 largestSize = Vector2.zero;

            for (int i = 0; i < grids.Length; i++)
            {
                if (TryGetGridBounds(grids[i], out Bounds bounds))
                {
                    largestSize.x = Mathf.Max(largestSize.x, bounds.size.x);
                    largestSize.y = Mathf.Max(largestSize.y, bounds.size.y);
                }
            }

            return largestSize;
        }

        private void RecycleHorizontalColumns()
        {
            int repositionCount = 0;
            while (target.position.x > GetColumnCenterX(rightTop, rightDown)
                && repositionCount++ < MaxRepositionsPerFrame)
            {
                MoveColumnToRight();
            }

            repositionCount = 0;
            while (target.position.x < GetColumnCenterX(leftTop, leftDown)
                && repositionCount++ < MaxRepositionsPerFrame)
            {
                MoveColumnToLeft();
            }
        }

        private void RecycleVerticalRows()
        {
            int repositionCount = 0;
            while (target.position.y > GetRowCenterY(leftTop, rightTop)
                && repositionCount++ < MaxRepositionsPerFrame)
            {
                MoveRowToTop();
            }

            repositionCount = 0;
            while (target.position.y < GetRowCenterY(leftDown, rightDown)
                && repositionCount++ < MaxRepositionsPerFrame)
            {
                MoveRowToDown();
            }
        }

        private void MoveColumnToRight()
        {
            PlaceBeside(leftTop, rightTop, Vector2.right * gridSize.x);
            PlaceBeside(leftDown, rightDown, Vector2.right * gridSize.x);
            Swap(ref leftTop, ref rightTop);
            Swap(ref leftDown, ref rightDown);
        }

        private void MoveColumnToLeft()
        {
            PlaceBeside(rightTop, leftTop, Vector2.left * gridSize.x);
            PlaceBeside(rightDown, leftDown, Vector2.left * gridSize.x);
            Swap(ref leftTop, ref rightTop);
            Swap(ref leftDown, ref rightDown);
        }

        private void MoveRowToTop()
        {
            PlaceBeside(leftDown, leftTop, Vector2.up * gridSize.y);
            PlaceBeside(rightDown, rightTop, Vector2.up * gridSize.y);
            Swap(ref leftTop, ref leftDown);
            Swap(ref rightTop, ref rightDown);
        }

        private void MoveRowToDown()
        {
            PlaceBeside(leftTop, leftDown, Vector2.down * gridSize.y);
            PlaceBeside(rightTop, rightDown, Vector2.down * gridSize.y);
            Swap(ref leftTop, ref leftDown);
            Swap(ref rightTop, ref rightDown);
        }

        private static void PlaceBeside(Transform movingGrid, Transform referenceGrid, Vector2 offset)
        {
            Vector3 position = referenceGrid.position + (Vector3)offset;
            position.z = movingGrid.position.z;
            movingGrid.position = position;
        }

        private float GetColumnCenterX(Transform top, Transform down)
        {
            return (GetGridCenter(top).x + GetGridCenter(down).x) * 0.5f;
        }

        private float GetRowCenterY(Transform left, Transform right)
        {
            return (GetGridCenter(left).y + GetGridCenter(right).y) * 0.5f;
        }

        private Vector2 GetGridCenter(Transform grid)
        {
            return TryGetGridBounds(grid, out Bounds bounds)
                ? bounds.center
                : grid.position;
        }

        private static bool TryGetGridBounds(Transform grid, out Bounds combinedBounds)
        {
            combinedBounds = default;
            if (grid == null)
            {
                return false;
            }

            TilemapRenderer[] renderers = grid.GetComponentsInChildren<TilemapRenderer>(true);
            bool hasBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                TilemapRenderer tilemapRenderer = renderers[i];
                if (tilemapRenderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = tilemapRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(tilemapRenderer.bounds);
                }
            }

            return hasBounds;
        }

        private static void Swap(ref Transform first, ref Transform second)
        {
            Transform temporary = first;
            first = second;
            second = temporary;
        }

        private void ShowSetupWarning()
        {
            if (setupWarningShown)
            {
                return;
            }

            setupWarningShown = true;
            Debug.LogWarning(
                $"{nameof(SurvivorInfiniteParallaxBackground)} needs the player and four Grid objects: left_top, left_down, right_top, right_down.",
                this);
        }

        [ContextMenu("Refresh Grid Chunk Loop")]
        private void RefreshGridChunkLoop()
        {
            initialized = false;
            setupWarningShown = false;
            Initialize();
        }

        private void OnValidate()
        {
            gridSize.x = Mathf.Max(0.1f, gridSize.x);
            gridSize.y = Mathf.Max(0.1f, gridSize.y);
        }

        private void OnDrawGizmosSelected()
        {
            if (!HasAllGridObjects())
            {
                return;
            }

            Gizmos.color = Color.cyan;
            float leftBoundary = GetColumnCenterX(leftTop, leftDown);
            float rightBoundary = GetColumnCenterX(rightTop, rightDown);
            float topBoundary = GetRowCenterY(leftTop, rightTop);
            float downBoundary = GetRowCenterY(leftDown, rightDown);

            Gizmos.DrawLine(new Vector3(leftBoundary, downBoundary - gridSize.y, 0f), new Vector3(leftBoundary, topBoundary + gridSize.y, 0f));
            Gizmos.DrawLine(new Vector3(rightBoundary, downBoundary - gridSize.y, 0f), new Vector3(rightBoundary, topBoundary + gridSize.y, 0f));
            Gizmos.DrawLine(new Vector3(leftBoundary - gridSize.x, topBoundary, 0f), new Vector3(rightBoundary + gridSize.x, topBoundary, 0f));
            Gizmos.DrawLine(new Vector3(leftBoundary - gridSize.x, downBoundary, 0f), new Vector3(rightBoundary + gridSize.x, downBoundary, 0f));
        }
    }
}
