using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using global::Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using COMS  = PGA.MessengerManager.MessengerManager;
namespace PGA.DeleteDupSurfaces
{
    public static class SelectPolylines
    {

        public static ObjectIdCollection GetIdsByTypeTypeValue(params string[] types)
        {

            // Get the document
            var doc = Application.DocumentManager.MdiActiveDocument;

            // Get the editor to make the selection
            Editor oEd = doc.Editor;

            // Add our or operators so we can grab multiple types.
            IList<TypedValue> typedValueSelection = new List<TypedValue> {
                    new TypedValue(Convert.ToInt32(DxfCode.Operator), "<or"),
                    new TypedValue(Convert.ToInt32(DxfCode.Operator), "or>")
                };

            // We will need to insert our requested types into the collection.
            // Since we knew we would have to insert they types inbetween the operators..
            // I used a Enumerable type which gave me that functionallity. (IListf<T>)
            foreach (var type in types)
                typedValueSelection.Insert(1, new TypedValue(Convert.ToInt32(DxfCode.Start), type));

            SelectionFilter selectionFilter = new SelectionFilter(typedValueSelection.ToArray());

            // because we have to.. Not really sure why, I assume this is our only access point
            // to grab the entities that we want. (I am open to being corrected)
            PromptSelectionResult promptSelectionResult = oEd.SelectAll(selectionFilter);

            // return our new ObjectIdCollection that is "Hopefully" full of the types that we want.
            ObjectIdCollection selection = (promptSelectionResult.Value == null
                ? new ObjectIdCollection() // nothing matched the filter
                : new ObjectIdCollection(promptSelectionResult.Value.GetObjectIds()));
            return selection;
        }
        public static IList<ObjectId> GetAllPolylines()
        {
            IList<ObjectId> ids = GetPolylineEntityIDs();

            return ids;
        }

        public static double? GetPolyLineArea(ObjectId selectedObjectId, global::Autodesk.AutoCAD.DatabaseServices.Database db)
        {
            Transaction tr = db.TransactionManager.StartTransaction();

            // Get the current UCS

            //CoordinateSystem3d ucs =

            using (tr)
            {
                DBObject obj =
                    tr.GetObject(selectedObjectId, OpenMode.ForRead);

                // If a "lightweight" (or optimized) polyline

                Polyline lwp = obj as Polyline;

                if (lwp != null)
                {
                    if (lwp.Closed)
                    {
                        return lwp.Area;
                    }

                }

                else
                {
                    // If an old-style, 2D polyline

                    Polyline2d p2d = obj as Polyline2d;

                    if (p2d != null)
                    {
                       return p2d.Area;
                    }

                    else
                    {
                        // If an old-style, 3D polyline

                        Polyline3d p3d = obj as Polyline3d;

                        if (p3d != null)
                        {
                            return p3d.Area;
                        }
                    }
                }

                tr.Commit();
            }

            return null;
        }
        public static Polyline GetPolyLineObject(ObjectId selectedObjectId, global::Autodesk.AutoCAD.DatabaseServices.Database db)
        {
            // Callers hold an outer transaction and use the returned object after this
            // method returns. Opening it in a nested transaction that is disposed here
            // would close it, so reuse the outer transaction when one exists.
            var top = db.TransactionManager.TopTransaction;
            if (top != null)
            {
                var poly = top.GetObject(selectedObjectId, OpenMode.ForRead) as Polyline;
                return poly != null && poly.Closed ? poly : null;
            }

            Transaction tr = db.TransactionManager.StartTransaction();

            using (tr)
            {
                DBObject obj =
                    tr.GetObject(selectedObjectId, OpenMode.ForRead);

                // If a "lightweight" (or optimized) polyline

                Polyline lwp = obj as Polyline;

                if (lwp != null)
                {
                    if (lwp.Closed)
                    {
                        return lwp;
                    }

                }

                tr.Commit();
            }

            return null;
        }

        /// <summary>
        /// Returns the layer name of any entity, or null if the id is not an entity.
        /// Safe replacement for GetPolyLineObject(id, db).Layer, which threw for
        /// anything other than a closed lightweight polyline.
        /// </summary>
        public static string GetEntityLayer(ObjectId selectedObjectId, global::Autodesk.AutoCAD.DatabaseServices.Database db)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(selectedObjectId, OpenMode.ForRead) as Entity;
                var layer = ent == null ? null : ent.Layer;
                tr.Commit();
                return layer;
            }
        }

        public static  List<ObjectId> GetPolylineEntityIDs()

        {
            List<ObjectId> ids = null;

            try
            {
                // Get the document
                var doc = Application.DocumentManager.MdiActiveDocument;
                var db = doc.Database;

                using (var tran = db.TransactionManager.StartTransaction())

                {
                    var tbl =
                        (BlockTable)tran.GetObject(db.BlockTableId, OpenMode.ForRead);

                    var br =
                        (BlockTableRecord)tran.GetObject(tbl[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                    var b = br.Cast<ObjectId>();

                    ids = (from id in b
                           where id.ObjectClass.Name == "AcDb2dPolyline" // DXF "POLYLINE" covers 2D and 3D; class name distinguishes them

                           select id).ToList();

                    tran.Commit();
                }

            }
            catch (System.Exception ex)
            {
                COMS.LogException(ex);
            }

            return ids;
        }

    }
}
