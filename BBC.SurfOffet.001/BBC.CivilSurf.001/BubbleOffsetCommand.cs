// ***********************************************************************
// Assembly         : BBC.CivilSurf.001
//
// BBC-CreateBubbleOffset: creates a new TIN surface that is a true offset of
// an existing one. See BubbleOffset.cs for the geometry.
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace BBC.CivilSurf
{
    /// <summary>
    /// Command that builds a true ("bubble") offset surface from a TIN surface.
    /// </summary>
    public class BubbleOffsetCommand
    {
        /// <summary>
        /// Prompts for a TIN surface, an offset distance, an optional detail
        /// spacing and an optional boundary polyline, then creates the offset
        /// surface.
        /// </summary>
        [CommandMethod("BBC-CreateBubbleOffset")]
        public static void CreateBubbleOffset()
        {
            var doc = AcadApplication.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            try
            {
                // The surface to offset
                var surfaceOptions = new PromptEntityOptions("\nSelect a TIN Surface: ");
                surfaceOptions.SetRejectMessage("\nOnly Tin Surface allowed");
                surfaceOptions.AddAllowedClass(typeof(TinSurface), true);
                PromptEntityResult surfaceResult = ed.GetEntity(surfaceOptions);
                if (surfaceResult.Status != PromptStatus.OK) return;

                // The offset distance
                var offsetOptions = new PromptDoubleOptions(
                    "\nOffset distance (positive = above the surface, negative = below): ");
                offsetOptions.AllowNegative = true;
                offsetOptions.AllowZero = false;
                PromptDoubleResult offsetResult = ed.GetDouble(offsetOptions);
                if (offsetResult.Status != PromptStatus.OK) return;
                double offset = offsetResult.Value;

                // Extra points for the rounded parts of the offset
                var spacingOptions = new PromptDoubleOptions(
                    "\nSpacing of extra detail points on rounded areas (0 = none): ");
                spacingOptions.AllowNegative = false;
                spacingOptions.AllowZero = true;
                spacingOptions.AllowNone = true;
                spacingOptions.DefaultValue = 0.0;
                spacingOptions.UseDefaultValue = true;
                PromptDoubleResult spacingResult = ed.GetDouble(spacingOptions);
                if (spacingResult.Status != PromptStatus.OK && spacingResult.Status != PromptStatus.None) return;
                double spacing = spacingResult.Status == PromptStatus.OK ? spacingResult.Value : 0.0;

                // Optional limit of the result
                var boundaryOptions = new PromptEntityOptions(
                    "\nSelect a closed Polyline Boundary, or press Enter to offset the whole surface: ");
                boundaryOptions.SetRejectMessage("\nOnly 2D Polylines allowed");
                boundaryOptions.AddAllowedClass(typeof(Polyline), true);
                boundaryOptions.AllowNone = true;
                PromptEntityResult boundaryResult = ed.GetEntity(boundaryOptions);
                if (boundaryResult.Status != PromptStatus.OK && boundaryResult.Status != PromptStatus.None) return;
                ObjectId boundaryId = boundaryResult.Status == PromptStatus.OK
                    ? boundaryResult.ObjectId
                    : ObjectId.Null;

                var timer = Stopwatch.StartNew();

                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    var surface = trans.GetObject(surfaceResult.ObjectId, OpenMode.ForRead) as TinSurface;
                    if (surface == null) return;

                    // The whole surface always shapes the offset, even when the
                    // result is limited to a boundary: ground just outside the
                    // boundary still pushes the bubble.
                    List<BubblePoint> corners = ReadTriangleCorners(surface);
                    if (corners.Count == 0)
                    {
                        ed.WriteMessage("\nThe surface has no triangles.");
                        return;
                    }
                    ed.WriteMessage("\nOffsetting " + (corners.Count / 3) + " triangles by " + offset + " ...");

                    var bubble = new BubbleOffsetSurface(corners, offset);
                    List<BubblePoint> offsetPoints = bubble.CreateOffsetPoints(spacing);

                    List<Point2d> boundary = null;
                    if (!boundaryId.IsNull)
                    {
                        var polyline = trans.GetObject(boundaryId, OpenMode.ForRead) as Polyline;
                        if (polyline != null)
                            boundary = ReadPolylineVertices(polyline);
                    }

                    var newPoints = new Point3dCollection();
                    foreach (BubblePoint point in offsetPoints)
                    {
                        if (boundary == null || IsInsidePolygon(boundary, point.X, point.Y))
                            newPoints.Add(new Point3d(point.X, point.Y, point.Z));
                    }

                    if (newPoints.Count < 3)
                    {
                        ed.WriteMessage("\nNo offset points were produced inside the boundary.");
                        return;
                    }

                    string name = UniqueSurfaceName(trans, surface.Name + " Bubble " + offset.ToString("0.###"));
                    ObjectId newSurfaceId = TinSurface.Create(db, name);
                    var newSurface = trans.GetObject(newSurfaceId, OpenMode.ForWrite) as TinSurface;
                    newSurface.AddVertices(newPoints);

                    if (!boundaryId.IsNull)
                    {
                        var boundaryIds = new ObjectIdCollection();
                        boundaryIds.Add(boundaryId);
                        newSurface.BoundariesDefinition.AddBoundaries(
                            boundaryIds, 0.2, SurfaceBoundaryType.Outer, true);
                    }

                    trans.Commit();

                    ed.WriteMessage("\nCreated surface \"" + name + "\" from " + newPoints.Count +
                                    " points in " + timer.Elapsed.TotalSeconds.ToString("0.0") + " s.");
                }

                ed.Regen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nBBC-CreateBubbleOffset error: " + ex.Message);
            }
        }

        /// <summary>
        /// Reads the visible triangles of a surface as three corners each.
        /// </summary>
        private static List<BubblePoint> ReadTriangleCorners(TinSurface surface)
        {
            var corners = new List<BubblePoint>();

            foreach (TinSurfaceTriangle triangle in surface.GetTriangles(false))
            {
                AddCorner(corners, triangle.Vertex1.Location);
                AddCorner(corners, triangle.Vertex2.Location);
                AddCorner(corners, triangle.Vertex3.Location);
            }

            return corners;
        }

        private static void AddCorner(List<BubblePoint> corners, Point3d location)
        {
            corners.Add(new BubblePoint(location.X, location.Y, location.Z));
        }

        private static List<Point2d> ReadPolylineVertices(Polyline polyline)
        {
            var vertices = new List<Point2d>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
                vertices.Add(polyline.GetPoint2dAt(i));
            return vertices;
        }

        /// <summary>
        /// Even-odd point in polygon test. Arc segments of the polyline are
        /// treated as straight chords.
        /// </summary>
        private static bool IsInsidePolygon(List<Point2d> polygon, double x, double y)
        {
            bool inside = false;

            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if ((polygon[i].Y > y) != (polygon[j].Y > y) &&
                    x < (polygon[j].X - polygon[i].X) * (y - polygon[i].Y) /
                        (polygon[j].Y - polygon[i].Y) + polygon[i].X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <summary>
        /// Returns the name, or the name with a counter added, so that it does
        /// not clash with an existing surface.
        /// </summary>
        private static string UniqueSurfaceName(Transaction trans, string baseName)
        {
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (ObjectId id in CivilApplication.ActiveDocument.GetSurfaceIds())
            {
                var other = trans.GetObject(id, OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Surface;
                if (other != null)
                    existing.Add(other.Name);
            }

            if (!existing.Contains(baseName))
                return baseName;

            for (int counter = 2; ; counter++)
            {
                string candidate = baseName + " (" + counter + ")";
                if (!existing.Contains(candidate))
                    return candidate;
            }
        }
    }
}
