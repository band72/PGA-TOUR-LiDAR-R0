// ***********************************************************************
// Assembly         : BBC.CivilSurf.001
//
// True ("bubble") offset of a triangulated surface.
//
// The offset surface is the envelope of a ball of radius |offset| rolled over
// the surface: every point of it lies exactly |offset| from the nearest point
// of the original surface. It rounds over ridges and peaks and trims itself in
// valleys, so it never self-intersects.
//
// A TIN is a height field, so the envelope can be evaluated exactly. Above any
// (x, y) the offset elevation is the highest of three kinds of pieces:
//   - each triangle moved along its unit normal by the offset   (flat faces)
//   - a cylinder of radius offset around each edge              (ridges)
//   - a sphere of radius offset at each vertex                  (peaks)
// A negative offset gives the envelope below the surface.
//
// This file has no AutoCAD or Civil 3D dependency.
// ***********************************************************************
using System;
using System.Collections.Generic;

namespace BBC.CivilSurf
{
    /// <summary>
    /// A point in space. Plain doubles, so the geometry can be used and tested
    /// outside AutoCAD.
    /// </summary>
    public struct BubblePoint
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;

        public BubblePoint(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    /// <summary>
    /// Evaluates the true offset of a triangle mesh that is a height field.
    /// </summary>
    public sealed class BubbleOffsetSurface
    {
        private const double Tiny = 1e-12;

        // Triangle corners, three per triangle. Z is stored multiplied by _sign,
        // so a downward offset is computed as an upward offset of the mirrored
        // surface and mirrored back.
        private readonly double[] _x;
        private readonly double[] _y;
        private readonly double[] _z;

        // Upward unit normal per triangle (mirrored space); _hasNormal is false
        // for degenerate and vertical triangles, which have no face piece.
        private readonly double[] _nx;
        private readonly double[] _ny;
        private readonly double[] _nz;
        private readonly bool[] _hasNormal;

        private readonly int _triangleCount;
        private readonly double _radius;
        private readonly double _sign;

        // Uniform grid over XY. Each cell lists the triangles whose bounding box,
        // grown by the radius, overlaps it.
        private readonly Dictionary<long, List<int>> _cells = new Dictionary<long, List<int>>();
        private readonly double _cellSize;
        private readonly double _minX;
        private readonly double _minY;
        private readonly double _maxX;
        private readonly double _maxY;

        /// <summary>
        /// Builds the offset evaluator.
        /// </summary>
        /// <param name="triangleCorners">Triangle corners, three consecutive points per triangle.</param>
        /// <param name="offset">Offset distance. Positive is above the surface, negative below.</param>
        public BubbleOffsetSurface(IList<BubblePoint> triangleCorners, double offset)
        {
            if (triangleCorners == null)
                throw new ArgumentNullException("triangleCorners");
            if (triangleCorners.Count == 0 || triangleCorners.Count % 3 != 0)
                throw new ArgumentException("Expected three corners per triangle.", "triangleCorners");
            if (offset == 0.0 || double.IsNaN(offset) || double.IsInfinity(offset))
                throw new ArgumentOutOfRangeException("offset", "Offset must be a non-zero number.");

            _radius = Math.Abs(offset);
            _sign = offset > 0.0 ? 1.0 : -1.0;
            _triangleCount = triangleCorners.Count / 3;

            int n = triangleCorners.Count;
            _x = new double[n];
            _y = new double[n];
            _z = new double[n];
            _nx = new double[_triangleCount];
            _ny = new double[_triangleCount];
            _nz = new double[_triangleCount];
            _hasNormal = new bool[_triangleCount];

            _minX = double.MaxValue;
            _minY = double.MaxValue;
            _maxX = double.MinValue;
            _maxY = double.MinValue;

            for (int i = 0; i < n; i++)
            {
                _x[i] = triangleCorners[i].X;
                _y[i] = triangleCorners[i].Y;
                _z[i] = triangleCorners[i].Z * _sign;

                if (_x[i] < _minX) _minX = _x[i];
                if (_y[i] < _minY) _minY = _y[i];
                if (_x[i] > _maxX) _maxX = _x[i];
                if (_y[i] > _maxY) _maxY = _y[i];
            }

            double extentSum = 0.0;
            for (int t = 0; t < _triangleCount; t++)
            {
                int a = 3 * t, b = a + 1, c = a + 2;

                double ux = _x[b] - _x[a], uy = _y[b] - _y[a], uz = _z[b] - _z[a];
                double vx = _x[c] - _x[a], vy = _y[c] - _y[a], vz = _z[c] - _z[a];

                double cx = uy * vz - uz * vy;
                double cy = uz * vx - ux * vz;
                double cz = ux * vy - uy * vx;
                double length = Math.Sqrt(cx * cx + cy * cy + cz * cz);

                if (length > Tiny)
                {
                    if (cz < 0.0)
                    {
                        cx = -cx;
                        cy = -cy;
                        cz = -cz;
                    }
                    _nx[t] = cx / length;
                    _ny[t] = cy / length;
                    _nz[t] = cz / length;
                    _hasNormal[t] = _nz[t] > 1e-9;
                }

                extentSum += Math.Max(
                    Math.Max(_x[a], Math.Max(_x[b], _x[c])) - Math.Min(_x[a], Math.Min(_x[b], _x[c])),
                    Math.Max(_y[a], Math.Max(_y[b], _y[c])) - Math.Min(_y[a], Math.Min(_y[b], _y[c])));
            }

            _cellSize = Math.Max(_radius, extentSum / _triangleCount);
            if (_cellSize <= 0.0)
                _cellSize = 1.0;

            for (int t = 0; t < _triangleCount; t++)
            {
                int a = 3 * t, b = a + 1, c = a + 2;

                int ix0 = CellIndex(Math.Min(_x[a], Math.Min(_x[b], _x[c])) - _radius, _minX);
                int ix1 = CellIndex(Math.Max(_x[a], Math.Max(_x[b], _x[c])) + _radius, _minX);
                int iy0 = CellIndex(Math.Min(_y[a], Math.Min(_y[b], _y[c])) - _radius, _minY);
                int iy1 = CellIndex(Math.Max(_y[a], Math.Max(_y[b], _y[c])) + _radius, _minY);

                for (int ix = ix0; ix <= ix1; ix++)
                {
                    for (int iy = iy0; iy <= iy1; iy++)
                    {
                        long key = CellKey(ix, iy);
                        List<int> list;
                        if (!_cells.TryGetValue(key, out list))
                        {
                            list = new List<int>();
                            _cells.Add(key, list);
                        }
                        list.Add(t);
                    }
                }
            }
        }

        /// <summary>The offset distance without its sign.</summary>
        public double Radius
        {
            get { return _radius; }
        }

        /// <summary>
        /// True if (x, y) lies on or inside some triangle of the original surface.
        /// </summary>
        public bool IsInsideFootprint(double x, double y)
        {
            List<int> candidates = CandidatesAt(x, y);
            if (candidates == null)
                return false;

            for (int i = 0; i < candidates.Count; i++)
            {
                if (!_hasNormal[candidates[i]])
                    continue; // degenerate or vertical: no area in plan

                int a = 3 * candidates[i];
                if (PointInTriangle(x, y, _x[a], _y[a], _x[a + 1], _y[a + 1], _x[a + 2], _y[a + 2]))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Elevation of the offset surface at (x, y).
        /// </summary>
        /// <returns>False if no part of the offset surface lies above or below (x, y).</returns>
        public bool TryGetElevation(double x, double y, out double z)
        {
            z = 0.0;

            List<int> candidates = CandidatesAt(x, y);
            if (candidates == null)
                return false;

            double best = double.NegativeInfinity;
            double r2 = _radius * _radius;

            for (int i = 0; i < candidates.Count; i++)
            {
                int t = candidates[i];
                int a = 3 * t, b = a + 1, c = a + 2;
                double top;

                // Face: the triangle moved along its normal by the radius.
                if (_hasNormal[t])
                {
                    double footX = x - _radius * _nx[t];
                    double footY = y - _radius * _ny[t];

                    if (PointInTriangle(footX, footY, _x[a], _y[a], _x[b], _y[b], _x[c], _y[c]))
                    {
                        double planeX = _x[a] + _radius * _nx[t];
                        double planeY = _y[a] + _radius * _ny[t];
                        double planeZ = _z[a] + _radius * _nz[t];

                        top = planeZ - (_nx[t] * (x - planeX) + _ny[t] * (y - planeY)) / _nz[t];
                        if (top > best) best = top;
                    }
                }

                // Edges: a cylinder around each side.
                if (TryEdgeTop(a, b, x, y, out top) && top > best) best = top;
                if (TryEdgeTop(b, c, x, y, out top) && top > best) best = top;
                if (TryEdgeTop(c, a, x, y, out top) && top > best) best = top;

                // Vertices: a sphere at each corner.
                for (int v = a; v <= c; v++)
                {
                    double dx = x - _x[v];
                    double dy = y - _y[v];
                    double d2 = dx * dx + dy * dy;

                    if (d2 <= r2)
                    {
                        top = _z[v] + Math.Sqrt(r2 - d2);
                        if (top > best) best = top;
                    }
                }
            }

            if (double.IsNegativeInfinity(best))
                return false;

            z = best * _sign;
            return true;
        }

        /// <summary>
        /// Creates the points that define the offset surface, ready to be
        /// triangulated. Points are taken at every original vertex, at every
        /// corner of every moved face (where flat and rounded pieces meet), and
        /// optionally on a regular grid that adds detail to the rounded parts.
        /// Only points over the footprint of the original surface are returned.
        /// </summary>
        /// <param name="gridSpacing">Spacing of the extra grid; zero or less for none.</param>
        public List<BubblePoint> CreateOffsetPoints(double gridSpacing)
        {
            var points = new List<BubblePoint>();
            var seen = new HashSet<long>();

            for (int i = 0; i < _x.Length; i++)
                AddSample(_x[i], _y[i], points, seen);

            for (int t = 0; t < _triangleCount; t++)
            {
                if (!_hasNormal[t])
                    continue;

                double shiftX = _radius * _nx[t];
                double shiftY = _radius * _ny[t];

                for (int v = 3 * t; v < 3 * t + 3; v++)
                    AddSample(_x[v] + shiftX, _y[v] + shiftY, points, seen);
            }

            if (gridSpacing > 0.0)
            {
                for (double gx = _minX; gx <= _maxX; gx += gridSpacing)
                {
                    for (double gy = _minY; gy <= _maxY; gy += gridSpacing)
                        AddSample(gx, gy, points, seen);
                }
            }

            return points;
        }

        private void AddSample(double x, double y, List<BubblePoint> points, HashSet<long> seen)
        {
            // One point per 0.0001 x 0.0001 square: a TIN cannot hold two points
            // at the same location. The key is unique while the surface is less
            // than about 200,000 units across.
            long keyX = (long)Math.Round((x - _minX) * 1e4);
            long keyY = (long)Math.Round((y - _minY) * 1e4);
            long key = unchecked((keyX << 32) ^ (keyY & 0xFFFFFFFFL));
            if (!seen.Add(key))
                return;

            double z;
            if (IsInsideFootprint(x, y) && TryGetElevation(x, y, out z))
                points.Add(new BubblePoint(x, y, z));
        }

        /// <summary>
        /// Highest point above (x, y) of the cylinder of the offset radius around
        /// the segment between corners p and q.
        /// </summary>
        private bool TryEdgeTop(int p, int q, double x, double y, out double top)
        {
            top = 0.0;

            double ex = _x[q] - _x[p], ey = _y[q] - _y[p], ez = _z[q] - _z[p];
            double length = Math.Sqrt(ex * ex + ey * ey + ez * ez);
            if (length < Tiny)
                return false;

            double ux = ex / length, uy = ey / length, uz = ez / length;

            // The vertical line through (x, y) is w + t * (0, 0, 1), measured from p.
            double wx = x - _x[p], wy = y - _y[p], wz = -_z[p];
            double along = wx * ux + wy * uy + wz * uz;

            // Parts of w and of the vertical direction perpendicular to the edge.
            double ax = wx - along * ux, ay = wy - along * uy, az = wz - along * uz;
            double bx = -uz * ux, by = -uz * uy, bz = 1.0 - uz * uz;

            double bb = bx * bx + by * by + bz * bz;
            if (bb < Tiny)
                return false; // vertical edge

            double ab = ax * bx + ay * by + az * bz;
            double aa = ax * ax + ay * ay + az * az;
            double discriminant = ab * ab - bb * (aa - _radius * _radius);
            if (discriminant < 0.0)
                return false;

            double t = (-ab + Math.Sqrt(discriminant)) / bb;

            // The nearest point on the edge line must lie within the segment;
            // beyond its ends the vertex spheres take over.
            double station = along + t * uz;
            if (station < 0.0 || station > length)
                return false;

            top = t;
            return true;
        }

        private static bool PointInTriangle(double px, double py,
            double ax, double ay, double bx, double by, double cx, double cy)
        {
            double d1 = (px - bx) * (ay - by) - (ax - bx) * (py - by);
            double d2 = (px - cx) * (by - cy) - (bx - cx) * (py - cy);
            double d3 = (px - ax) * (cy - ay) - (cx - ax) * (py - ay);

            // Scale the tolerance to the triangle so points on an edge count as inside.
            double scale = Math.Abs(ax - bx) + Math.Abs(ay - by) + Math.Abs(bx - cx) + Math.Abs(by - cy);
            double eps = 1e-9 * scale * scale;

            bool hasNegative = d1 < -eps || d2 < -eps || d3 < -eps;
            bool hasPositive = d1 > eps || d2 > eps || d3 > eps;
            return !(hasNegative && hasPositive);
        }

        private List<int> CandidatesAt(double x, double y)
        {
            if (x < _minX - _radius || x > _maxX + _radius || y < _minY - _radius || y > _maxY + _radius)
                return null;

            List<int> list;
            return _cells.TryGetValue(CellKey(CellIndex(x, _minX), CellIndex(y, _minY)), out list) ? list : null;
        }

        private int CellIndex(double value, double origin)
        {
            return (int)Math.Floor((value - origin) / _cellSize);
        }

        private static long CellKey(int ix, int iy)
        {
            return unchecked(((long)ix << 32) ^ (iy & 0xFFFFFFFFL));
        }
    }
}
