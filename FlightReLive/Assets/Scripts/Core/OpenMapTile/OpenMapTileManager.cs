using FlightReLive.Core.Pipeline;
using FlightReLive.Core.ProceduralTerrain;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VexTile.Mapbox.VectorTile.Geometry;

namespace FlightReLive.Core.OpenMapTile
{
    /// <summary>
    /// Manages OpenMapTile loading, water contour extraction, and ARM texture generation for terrain shading.
    /// </summary>
    internal class OpenMapTileManager : MonoBehaviour
    {
        #region CONSTANTS
        private const float OPENMAPTILE_EXTENT = 4096f;
        #endregion

        #region ATTRIBUTES
        private readonly Dictionary<(int x, int y), List<List<Vector2>>> _waterContoursByTile = new Dictionary<(int x, int y), List<List<Vector2>>>();
        private List<Edge>[] _edgeTablePool = new List<Edge>[512];
        private readonly List<Edge> _activeEdgeList = new List<Edge>();
        #endregion

        #region PROPERTIES
        internal static OpenMapTileManager Instance;
        #endregion

        #region STRUCTS
        /// <summary>
        /// Represents an edge used in scanline rasterization.
        /// </summary>
        private struct Edge
        {
            public int yMax;
            public float x;
            public float invSlope;
        }
        #endregion

        #region UNITY METHODS
        /// <summary>
        /// Initializes the singleton instance of the OpenMapTileManager.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Loads a tile, extracts water contours, and generates its ARM texture.
        /// </summary>
        internal void LoadTile(TileDefinition tile)
        {
            if (tile == null || tile.Features == null || tile.Features.Count == 0)
            {
                return;
            }

            int zoom;

            switch (tile.Priority)
            {
                case 0:
                    zoom = MapTools.ZOOM_LEVEL_SATELLITE_PRIORITY_0;
                    break;
                case 1:
                    zoom = MapTools.ZOOM_LEVEL_SATELLITE_PRIORITY_1;
                    break;
                default:
                case 2:
                    zoom = MapTools.ZOOM_LEVEL_SATELLITE_PRIORITY_OTHER;
                    break;
            }

            AccumulateWaterContoursForTile(tile);
            GenerateARMTextureForTile(tile, zoom);
        }

        /// <summary>
        /// Clears all cached water contours.
        /// </summary>
        internal async Task Unload()
        {
            await UnityMainThreadDispatcher.AwaitOnMainThread(() =>
            {
                _waterContoursByTile.Clear();
            });
        }

        /// <summary>
        /// Extracts water feature contours from a tile and stores them in UV space.
        /// </summary>
        private void AccumulateWaterContoursForTile(TileDefinition tile)
        {
            (int x, int y) key = (tile.X, tile.Y);

            if (!_waterContoursByTile.TryGetValue(key, out List<List<Vector2>> list))
            {
                list = new List<List<Vector2>>();
                _waterContoursByTile[key] = list;
            }

            foreach (OpenMapTileFeature feature in tile.Features)
            {
                if (feature is WaterFeature waterFeature && waterFeature.Geometry != null)
                {
                    foreach (List<SerializablePoint2D> ringRaw in waterFeature.Geometry)
                    {
                        if (ringRaw == null || ringRaw.Count < 3)
                        {
                            continue;
                        }

                        List<Point2d<int>> ring = new List<Point2d<int>>(ringRaw.Count);

                        for (int i = 0; i < ringRaw.Count; i++)
                        {
                            ring.Add(ringRaw[i].ToPoint2D());
                        }

                        ring = ClipRingToExtent(ring);

                        if (ring.Count < 3)
                        {
                            continue;
                        }

                        List<Vector2> contourUV = ConvertGeometryToUV_Flipped(ring);

                        if (contourUV.Count >= 3)
                        {
                            list.Add(contourUV);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Converts geometry from tile coordinates to flipped UV coordinates.
        /// </summary>
        private static List<Vector2> ConvertGeometryToUV_Flipped(List<Point2d<int>> ring)
        {
            List<Vector2> contour = new List<Vector2>(ring.Count);

            for (int i = 0; i < ring.Count; i++)
            {
                float u = (float)ring[i].X / OPENMAPTILE_EXTENT;
                float v = 1f - ((float)ring[i].Y / OPENMAPTILE_EXTENT);
                contour.Add(new Vector2(u, v));
            }

            return contour;
        }

        /// <summary>
        /// Clips geometry points to the tile extent.
        /// </summary>
        private static List<Point2d<int>> ClipRingToExtent(List<Point2d<int>> ring)
        {
            List<Point2d<int>> clipped = new List<Point2d<int>>(ring.Count);

            for (int i = 0; i < ring.Count; i++)
            {
                clipped.Add(new Point2d<int>(
                    Mathf.Clamp(ring[i].X, 0, (int)OPENMAPTILE_EXTENT),
                    Mathf.Clamp(ring[i].Y, 0, (int)OPENMAPTILE_EXTENT)));
            }

            return clipped;
        }

        /// <summary>
        /// Generates the ARM texture for a tile using water contours and scanline rasterization.
        /// </summary>
        private void GenerateARMTextureForTile(TileDefinition tile, int zoom)
        {
            if (tile.SatelliteTexture == null)
            {
                Debug.LogWarning($"[OpenMapTileManager] No satellite texture for tile {tile.X},{tile.Y}");
                return;
            }

            (int x, int y) key = (tile.X, tile.Y);
            _waterContoursByTile.TryGetValue(key, out List<List<Vector2>> waterContoursUV);

            int outW = tile.SatelliteTexture.width;
            int outH = tile.SatelliteTexture.height;
            int inW = Mathf.Max(4, outW);
            int inH = Mathf.Max(4, outH);

            Color32[] armPixels = ArrayPool<Color32>.Shared.Rent(outW * outH);
            Color32 landARM = new Color32(0, 0, 0, 0);
            Color32 waterARM = new Color32(0, 255, 0, 255);

            Parallel.For(0, outW * outH, i =>
            {
                armPixels[i] = landARM;
            });

            if (waterContoursUV != null && waterContoursUV.Count > 0)
            {
                BitArray lowMask = new BitArray(inW * inH);
                RasterizeContoursEvenOddUV(lowMask, inW, inH, waterContoursUV);

                Parallel.For(0, outH, y =>
                {
                    int row = y * outW;

                    for (int x = 0; x < outW; x++)
                    {
                        int i = row + x;

                        if (lowMask.Get(i))
                        {
                            armPixels[i] = waterARM;
                        }
                    }
                });
            }

            Texture2D armTex = new Texture2D(outW, outH, TextureFormat.RGBA32, false, true);
            armTex.wrapMode = TextureWrapMode.Clamp;
            armTex.filterMode = FilterMode.Bilinear;
            armTex.SetPixels32(armPixels, 0);
            armTex.Apply(false, false);
            tile.ARMTexture = armTex;

            ArrayPool<Color32>.Shared.Return(armPixels, clearArray: true);
        }

        /// <summary>
        /// Rasterizes water contours using Even-Odd scanline fill in UV space.
        /// </summary>
        private void RasterizeContoursEvenOddUV(BitArray mask, int w, int h, List<List<Vector2>> contoursUV)
        {
            if (_edgeTablePool.Length < h)
            {
                System.Array.Resize(ref _edgeTablePool, h);
            }

            for (int i = 0; i < h; i++)
            {
                if (_edgeTablePool[i] == null)
                {
                    _edgeTablePool[i] = new List<Edge>();
                }
                else
                {
                    _edgeTablePool[i].Clear();
                }
            }

            float wMinus1 = (float)(w - 1);
            float hMinus1 = (float)(h - 1);

            Parallel.For(0, contoursUV.Count, c =>
            {
                List<Vector2> contour = contoursUV[c];
                int n = contour.Count;

                if (n < 3)
                {
                    return;
                }

                int[] px = new int[n];
                int[] py = new int[n];

                for (int i = 0; i < n; i++)
                {
                    px[i] = Mathf.Clamp(Mathf.RoundToInt(contour[i].x * wMinus1), 0, w - 1);
                    py[i] = Mathf.Clamp(Mathf.RoundToInt(contour[i].y * hMinus1), 0, h - 1);
                }

                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    int x0 = px[i];
                    int y0 = py[i];
                    int x1 = px[j];
                    int y1 = py[j];

                    if (y0 == y1)
                    {
                        continue;
                    }

                    if (y0 > y1)
                    {
                        int tempX = x0;
                        x0 = x1;
                        x1 = tempX;

                        int tempY = y0;
                        y0 = y1;
                        y1 = tempY;
                    }

                    float invSlope = (float)(x1 - x0) / (float)(y1 - y0);

                    Edge edge = new Edge
                    {
                        yMax = y1,
                        x = x0,
                        invSlope = invSlope
                    };

                    lock (_edgeTablePool[y0])
                    {
                        _edgeTablePool[y0].Add(edge);
                    }
                }
            });

            _activeEdgeList.Clear();

            for (int y = 0; y < h; y++)
            {
                if (_edgeTablePool[y].Count > 0)
                {
                    _activeEdgeList.AddRange(_edgeTablePool[y]);
                }

                int activeCount = 0;

                for (int i = 0; i < _activeEdgeList.Count; i++)
                {
                    if (_activeEdgeList[i].yMax > y)
                    {
                        _activeEdgeList[activeCount++] = _activeEdgeList[i];
                    }
                }

                if (activeCount == 0)
                {
                    _activeEdgeList.Clear();
                    continue;
                }

                _activeEdgeList.RemoveRange(activeCount, _activeEdgeList.Count - activeCount);

                if (_activeEdgeList.Count < 32)
                {
                    InsertionSortByX(_activeEdgeList);
                }
                else
                {
                    _activeEdgeList.Sort(CompareEdgesByX);
                }

                for (int i = 0; i + 1 < _activeEdgeList.Count; i += 2)
                {
                    int xStart = Mathf.CeilToInt(_activeEdgeList[i].x);
                    int xEnd = Mathf.FloorToInt(_activeEdgeList[i + 1].x);

                    if (xEnd < xStart)
                    {
                        continue;
                    }

                    int row = y * w;
                    int xs = Mathf.Clamp(xStart, 0, w - 1);
                    int xe = Mathf.Clamp(xEnd, 0, w - 1);

                    for (int x = xs; x <= xe; x++)
                    {
                        mask.Set(row + x, true);
                    }
                }

                for (int i = 0; i < _activeEdgeList.Count; i++)
                {
                    Edge edge = _activeEdgeList[i];
                    edge.x += edge.invSlope;
                    _activeEdgeList[i] = edge;
                }
            }
        }

        /// <summary>
        /// Sorts a list of edges by their x-coordinate using insertion sort.
        /// </summary>
        private static void InsertionSortByX(List<Edge> edges)
        {
            for (int i = 1; i < edges.Count; i++)
            {
                Edge key = edges[i];
                int j = i - 1;

                while (j >= 0 && edges[j].x > key.x)
                {
                    edges[j + 1] = edges[j];
                    j--;
                }

                edges[j + 1] = key;
            }
        }

        /// <summary>
        /// Compares two edges by their x-coordinate.
        /// </summary>
        private static int CompareEdgesByX(Edge a, Edge b)
        {
            return a.x.CompareTo(b.x);
        }
        #endregion
    }
}
