using System.Collections.Generic;
using UnityEngine;
using Grid = Kira.GridGen.Grid;

namespace Kira
{
    public class MarqueSelector : MonoBehaviour
    {
        [SerializeField] private MouseDrag mouseDrag;
        [SerializeField] private Transform marqueSprite;

        [SerializeField] private float wasSelectingCoolDown = 0.2f;

        [Header("Gizmos")]
        [SerializeField] private float gizmoCornerRadius = 4f;
        [SerializeField] private float gizmoNodeRadius = 3f;
        [SerializeField] private Color gizmoCornerColor = Color.green;
        [SerializeField] private Color gizmoNodeColor = Color.magenta;

        private Camera cam;
        private TileGenerator tileGenerator;
        private TileSelector tileSelector;
        private Grid grid;

        private SelectionNode[,] nodes = new SelectionNode[0, 0];
        public static bool IsSelecting;

        // The time the player let go of the selection + the wasSelectingCoolDown
        // Used so that we dont select the tile immediatly after letting go of the mouse button
        public static float SelectionStoppedTime;

        private MarqueeBounds bounds;


        private void Start()
        {
            tileSelector = FindObjectOfType<TileSelector>();
            tileGenerator = FindObjectOfType<TileGenerator>();
            grid = tileGenerator.grid;

            cam = FindObjectOfType<Camera>();
            tileGenerator = FindObjectOfType<TileGenerator>();
            mouseDrag.Init(cam);
            marqueSprite.gameObject.SetActive(false);
        }

        public void Update()
        {
            mouseDrag.Update();


            if (mouseDrag.IsDragging)
            {
                Vector3 start = mouseDrag.DragStartWorld;
                start.y = 1f;
                Vector3 end = cam.ScreenToWorldPoint(Input.mousePosition);

                bounds.Set(start.x, start.z, end.x, end.z);
                UpdateSelectionGrid();

                Vector3 delta = start - end;
                delta.y = 1f;

                Vector3 scale = marqueSprite.localScale;

                scale.x = 1 + Mathf.Abs(delta.x);
                scale.y = 1 + Mathf.Abs(delta.z);

                Vector3 marqueePosOffset = start;
                marqueePosOffset.x -= delta.x / 2f;
                marqueePosOffset.z -= delta.z / 2f;
                marqueePosOffset.y = 35f;

                marqueSprite.position = marqueePosOffset;
                marqueSprite.localScale = scale;
                marqueSprite.gameObject.SetActive(true);
                IsSelecting = true;
                SelectionStoppedTime = Time.time + wasSelectingCoolDown;
            }
            else
            {
                IsSelecting = false;
                marqueSprite.gameObject.SetActive(false);
            }
        }

        private void UpdateSelectionGrid()
        {
            float xDelta = Mathf.Abs(bounds.x - bounds.x1);
            float yDelta = Mathf.Abs(bounds.y - bounds.y1);

            float gridCellSize = grid.cellSize;
            int xNodeCount = Mathf.CeilToInt(xDelta / gridCellSize);

            int yNodeCount = Mathf.CeilToInt(yDelta / gridCellSize);

            float startX = Mathf.Min(bounds.x, bounds.x1);
            float startY = Mathf.Min(bounds.y, bounds.y1);
            float endY = Mathf.Max(bounds.y, bounds.y1);
            float endX = Mathf.Max(bounds.x, bounds.x1);

            nodes = new SelectionNode[yNodeCount + 1, xNodeCount + 1];

            for (int y = 0; y < yNodeCount + 1; y++)
            {
                float yPos = startY + y * gridCellSize;
                if (y == yNodeCount) yPos = endY;

                for (int x = 0; x < xNodeCount + 1; x++)
                {
                    float xPos = startX + x * gridCellSize;
                    if (x == xNodeCount) xPos = endX;
                    nodes[y, x] = new SelectionNode(new Vector3(xPos, 40f, yPos), x, y);
                }
            }

            List<Tile> tilesFound = new List<Tile>();

            foreach (SelectionNode node in nodes)
            {
                var tile = grid.GetTile(node.position, out bool hasTile);
                if (!hasTile) continue;
                tilesFound.Add(tile);
            }

            tileSelector.SelectTiles(tilesFound.ToArray());
        }

        #region Gizmos

        private void DrawSelectionGizmos()
        {
            if (!IsSelecting) return;

            Gizmos.color = gizmoCornerColor;

            var gpos = new Vector3(bounds.x, 30f, bounds.y);

            Gizmos.DrawSphere(gpos, gizmoCornerRadius);
            gpos.z = bounds.y1;
            Gizmos.DrawSphere(gpos, gizmoCornerRadius);
            gpos.x = bounds.x1;
            Gizmos.DrawSphere(gpos, gizmoCornerRadius);
            gpos.z = bounds.y;
            Gizmos.DrawSphere(gpos, gizmoCornerRadius);
        }

        private void DrawGridGizmos()
        {
            if (!Application.isPlaying || !IsSelecting) return;

            foreach (SelectionNode node in nodes)
            {
                Gizmos.color = gizmoNodeColor;
                Gizmos.DrawSphere(node.position, gizmoNodeRadius);
            }
        }

        private void OnDrawGizmos()
        {
            // DrawSelectionGizmos();
            DrawGridGizmos();
        }

        #endregion
    }
}