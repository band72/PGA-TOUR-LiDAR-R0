using System;
using System.IO;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using PGA.Civil.Logging;
using AcadRuntime = Autodesk.AutoCAD.Runtime;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace C3DSurfacesDemo
{
   public class PolylineUtils

    {

        //            SelectionFilter sf = new SelectionFilter

        //            sfd.Dispose();// non resident object, so kill 'em

        public static void GetIntersections()
        {
            Database db = HostApplicationServices.WorkingDatabase;

            Document doc = Application.DocumentManager.MdiActiveDocument;

            Editor ed = doc.Editor;

            using (DocumentLock docLock = doc.LockDocument())
            {
                using ( Transaction tr = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                        PromptEntityOptions peo = new PromptEntityOptions("\nSelect a single polyline  >>");

                        peo.SetRejectMessage("\nSelected object might be of type polyline only >>");

                        peo.AddAllowedClass(typeof(Polyline), false);

                        PromptEntityResult res;

                        res = ed.GetEntity(peo);

                        if (res.Status != PromptStatus.OK)

                            return;

                        DBObject ent = (DBObject)tr.GetObject(res.ObjectId, OpenMode.ForRead);

                        if (ent == null) return;

                        Curve curv = ent as Curve;

                        DBObjectCollection pcurves = new DBObjectCollection();

                        curv.Explode(pcurves);
                        TypedValue[] values = new TypedValue[]
                     {
                        new TypedValue(0, "lwpolyline")
                        //might be added layer name to select curve:
                        //, new TypedValue(8, "mylayer")
                     };
                        SelectionFilter filter = new SelectionFilter(values);

                        Point3dCollection fence = new Point3dCollection();

                        double leng = curv.GetDistanceAtParameter(curv.EndParam) - curv.GetDistanceAtParameter(curv.StartParam);
                        // number of divisions along polyline to create fence selection
                        double step = leng / 256;// set number of steps to your suit

                        int num = Convert.ToInt32(leng / step);

                        int i = 0;

                        for (i = 0; i < num; i++)
                        {
                            Point3d pp = curv.GetPointAtDist(step * i);

                            fence.Add(curv.GetClosestPointTo(pp, false));
                        }

                        PromptSelectionResult selres = ed.SelectFence(fence, filter);

                        if (selres.Status != PromptStatus.OK) return;
                        Point3dCollection intpts = new Point3dCollection();

                        DBObjectCollection qcurves = new DBObjectCollection();

                        foreach (SelectedObject selobj in selres.Value)
                        {
                            DBObject obj = tr.GetObject(selobj.ObjectId, OpenMode.ForRead, false) as DBObject;
                            if (selobj.ObjectId != curv.ObjectId)
                            {
                                DBObjectCollection icurves = new DBObjectCollection();
                                Curve icurv = obj as Curve;
                                icurv.Explode(icurves);
                                foreach (DBObject dbo in icurves)
                                {
                                    if (!qcurves.Contains(dbo))
                                        qcurves.Add(dbo);
                                }
                            }

                        }
                        ed.WriteMessage("\n{0}", qcurves.Count);

                        int j = 0;
                        Point3dCollection polypts = new Point3dCollection();

                        for (i = 0; i < pcurves.Count; ++i)
                        {
                            for (j = 0; j < qcurves.Count; ++j)
                            {
                                Curve curve1 = pcurves[i] as Curve;

                                Curve curve2 = qcurves[j] as Curve;

                                Point3dCollection pts = new Point3dCollection();

                                curve1.IntersectWith(curve2, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
                                foreach (Point3d pt in pts)
                                {
                                    if (!polypts.Contains(pt))
                                        polypts.Add(pt);
                                }
                            }
                        }

                        Application.SetSystemVariable("osmode", 0);// optional
                        // for debug only
                        Application.ShowAlertDialog(string.Format("\nNumber of Intersections: {0}", polypts.Count));
                        // test for visulization only
                        foreach (Point3d inspt in polypts)
                        {
                            Circle circ = new Circle(inspt, Vector3d.ZAxis, 10 * db.Dimtxt);
                            circ.ColorIndex = 1;
                            btr.AppendEntity(circ);
                            tr.AddNewlyCreatedDBObject(circ, true);

                        }
                        tr.Commit();
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage("\n{0}\n{1}", ex.Message, ex.StackTrace);
                    }
                }
            }

        }

        // based on article by Wayne Brill
        // http://adndevblog.typepad.com/autocad/2012/06/use-getclosestpointto-and-onsegat-to-locate-a-polyline-vertex-near-a-point.html
        public static void TEST_PointOnPoly()
        {

            Database db = Application.DocumentManager.MdiActiveDocument.Database;

            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            CoordinateSystem3d cs = ed.CurrentUserCoordinateSystem.CoordinateSystem3d;

            Plane plan = new Plane(Point3d.Origin, cs.Zaxis);

            Application.SetSystemVariable("osmode", 512);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    Entity ename;

                    Point3d pt;

                    Point3d ptWcs;

                    PromptEntityOptions peo = new PromptEntityOptions("\nSelect Pline: ");

                    peo.SetRejectMessage("\nYou have to select LWPOLYLINE!");

                    peo.AddAllowedClass(typeof(Polyline), false);

                    PromptEntityResult res = ed.GetEntity(peo);

                    if (res.Status != PromptStatus.OK) return;

                    ObjectId id = res.ObjectId;

                    // Convert to WCS incase selection was made

                    // while in a UCS.

                    pt = res.PickedPoint;

                    // Transform from UCS to WCS

                    Matrix3d mat =

                      Matrix3d.AlignCoordinateSystem(

                        Point3d.Origin,

                        Vector3d.XAxis,

                        Vector3d.YAxis,

                        Vector3d.ZAxis,

                        cs.Origin,

                        cs.Xaxis,

                        cs.Yaxis,

                        cs.Zaxis

                      );

                    ptWcs = pt.TransformBy(mat);

                    ename = tr.GetObject(id, OpenMode.ForRead) as Entity;

                    Polyline pline = ename as Polyline;

                    if (pline == null)
                    {

                        ed.WriteMessage("\nSelected Entity is not a Polyline");

                        return;

                    }

                    Point3d clickpt = pline.GetClosestPointTo(ptWcs, false);

                    for (int c = 0; c < pline.NumberOfVertices; c++)
                    {

                        double segParam = new double();

                        // This is the test filter here...it uses the

                        // nifty API OnSegmentAt

                        if (pline.OnSegmentAt(c, clickpt.Convert2d(plan), segParam))
                        {

                            ed.WriteMessage("\nSelected Segment: {0} \n", c + 1);

                            break;

                        }

                    }
                }

                catch (System.Exception ex)
                {

                    ed.WriteMessage("\n" + ex.Message + "\n" + ex.StackTrace);

                }

            }

        }

        //                bt[BlockTableRecord.ModelSpace],

        public static void GetComplexity()
        {
            Database db = HostApplicationServices.WorkingDatabase;

            Document doc = Application.DocumentManager.MdiActiveDocument;

            Editor ed = doc.Editor;

            using (DocumentLock docLock = doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                        PromptEntityOptions peo = new PromptEntityOptions("\nSelect a single polyline  >>");

                        peo.SetRejectMessage("\nSelected object might be of type polyline only >>");

                        peo.AddAllowedClass(typeof(Polyline), false);

                        PromptEntityResult res;

                        res = ed.GetEntity(peo);

                        if (res.Status != PromptStatus.OK)

                            return;

                        DBObject ent = (DBObject)tr.GetObject(res.ObjectId, OpenMode.ForRead);

                        if (ent == null) return;

                        Curve curv = ent as Curve;

                        DBObjectCollection pcurves = new DBObjectCollection();

                        curv.Explode(pcurves);
                        TypedValue[] values = new TypedValue[]
                     {
                        new TypedValue(0, "lwpolyline")
                        //might be added layer name to select curve:
                        //, new TypedValue(8, "mylayer")
                     };
                        SelectionFilter filter = new SelectionFilter(values);

                        Point3dCollection fence = new Point3dCollection();

                        double leng = curv.GetDistanceAtParameter(curv.EndParam) - curv.GetDistanceAtParameter(curv.StartParam);
                        // number of divisions along polyline to create fence selection
                        //double step = leng / 256;// set number of steps to your suit
                        double step = leng / pcurves.Count;
                        Application.SetSystemVariable("osmode", 0);// optional
                        // for debug only
                        Application.ShowAlertDialog(string.Format("\nNumber of Steps: {0}", step));

                        tr.Commit();
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage("\n{0}\n{1}", ex.Message, ex.StackTrace);
                    }
                }
            }
        }
        public static bool ExcludePolyBasedOnComplexity(ObjectId oid)
        {
            if (oid == null)
                return true;

            bool result = false; //include by default

            Database db = HostApplicationServices.WorkingDatabase;

            Document doc = Application.DocumentManager.MdiActiveDocument;

            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {

                    DBObject ent = (DBObject)tr.GetObject(oid, OpenMode.ForRead);

                    if (ent == null) return false;

                    Curve curv = ent as Curve;

                    DBObjectCollection pcurves = new DBObjectCollection();

                    curv.Explode(pcurves);
                    TypedValue[] values = new TypedValue[]
                    {
                    new TypedValue(0, "lwpolyline")
                    };
                    SelectionFilter filter = new SelectionFilter(values);

                    Point3dCollection fence = new Point3dCollection();

                    double leng = curv.GetDistanceAtParameter(curv.EndParam) - curv.GetDistanceAtParameter(curv.StartParam);
                    // number of divisions along polyline to create fence selection
                    //double step = leng / 256;// set number of steps to your suit
                    double step = leng / pcurves.Count;

                    if ((leng > 1000 || pcurves.Count > 3000) && (step < 1.0))
                        result = true; // too complex

                    if (pcurves.Count > 10000)
                        result = true;

                    if (result)
                    {
                        ACADLogging.LogMyExceptions(
                            String.Format("\nExcluded Polyline: {0}\nComplexity: {1}\nLayer: {2}\n",
                            pcurves.Count, step.ToString("n3"), curv.Layer));
                    }
                    tr.Commit();
                }
                catch (System.Exception ex)
                {
                    ACADLogging.LogMyExceptions(String.Format("\n{0}\n{1}", ex.Message, ex.StackTrace));
                }

                return result;
            }

        }

    }
}
