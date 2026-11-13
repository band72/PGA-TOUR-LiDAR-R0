// ***********************************************************************
// Assembly         : PGA.SportVision
// Author           : Daryl Banks, PSM
// Created          : 07-03-2016
//
// Last Modified By : Daryl Banks, PSM
// Last Modified On : 08-19-2016
// ***********************************************************************
// <copyright file="Program.cs" company="Banks & Banks Consulting">
//     Copyright ©  2016
// </copyright>
// <summary></summary>
// ***********************************************************************

#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using BBC.Common.AutoCAD;
using MathNet.Numerics;
using PGA.Database;
using PGA.DataContext;
using PGA.WindingNumAlgorithm;
using Acaddb = Autodesk.AutoCAD.DatabaseServices;
using Active = PGA.PolylineManager.Active;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using COMS = PGA.MessengerManager;
using Exception = System.Exception;
using OpenDwg = PGA.OpenDWG.OpenDWG;
using Process = ProcessPolylines.ProcessPolylines;

#endregion

namespace PGA.SportVision
{
    /// <summary>
    ///     Class Program.
    /// </summary>
    /// <seealso cref="IExtensionApplication" />
    /// <seealso cref="IExtensionApplication" />
    public class Program : IExtensionApplication
    {
        /// <summary>
        ///     The offset object
        /// </summary>
        public static Acaddb.ObjectId OffsetObject = Acaddb.ObjectId.Null;

        /// <summary>
        ///     The mas polyline ids
        /// </summary>
        public static IEnumerable<Acaddb.ObjectId> MasPolylineIds = new List<Acaddb.ObjectId>();

        /// <summary>
        ///     The mas regions
        /// </summary>
        public static IEnumerable<Acaddb.Region> MasRegions = new List<Acaddb.Region>();

        /// <summary>
        ///     The Drawings
        /// </summary>
        public static List<string> Drawings = new List<string>();

        /// <summary>
        ///     The counter
        /// </summary>
        public static int Counter;

        /// <summary>
        ///     The Stopwatch
        /// </summary>
        public static Stopwatch Stopwatch;

        /// <summary>
        ///     The maximum wait
        /// </summary>
        public static TimeSpan MaxWait = new TimeSpan(0, 0, 300);

        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            UnlockCommands(Active.Document);
            SetVars();
        }

        //    CryptoLicense license =

        /// <summary>
        ///     Terminates this instance.
        /// </summary>
        public void Terminate()
        {
            ResetVars();
        }

        /// <summary>
        ///     Unlocks the commands.
        /// </summary>
        /// <param name="doc">The document.</param>
        public void UnlockCommands(Document doc)
        {
            // Add our command handlers

            doc.CommandEnded += OnCommandEnded;
            doc.CommandCancelled += OnCommandEnded;
            doc.CommandFailed += OnCommandEnded;
        }

        /// <summary>
        ///     Handles the <see cref="E:CommandEnded" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="CommandEventArgs" /> instance containing the event data.</param>
        private void OnCommandEnded(object sender, CommandEventArgs e)
        {
            try
            {
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }

        /// <summary>
        ///     Sets the vars.
        /// </summary>
        private static void SetVars()
        {
            Application.SetSystemVariable("FILEDIA", 0);
            Application.SetSystemVariable("NOMUTT", 0);
            Application.SetSystemVariable("CMDECHO", 0);
            SetSaveTime();
        }

        private static void SetSaveTime()
        {
            try
            {
                Application.SetSystemVariable("SAVETIME", 0);

            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }

        /// <summary>
        ///     Resets the vars.
        /// </summary>
        private static void ResetVars()
        {
            Application.SetSystemVariable("FILEDIA", 1);
            Application.SetSystemVariable("NOMUTT", 0);
            Application.SetSystemVariable("CMDECHO", 1);
        }

        [CommandMethod("PGA-WN_SportVision")]
        public static void WN_StartProgram()
        {
            try
            {
                var ed = Application.DocumentManager.MdiActiveDocument.Editor;

                ed.Document.SendStringToExecute(".PGA-SV-Timer-Disable\n", true, false, true);
                AcadUtilities.SendStringToExecute(".ZoomExtents\n");

                ed.Document.SendStringToExecute(".SetXData\n", true, false, true);
                ed.Document.SendStringToExecute(".WN_RegisterPolyInners\n", true, false, true);
                ed.Document.SendStringToExecute("-Mapcreatecentroids\n ALL\n\n", true, false, true);
                ed.Document.SendStringToExecute(".PGA-OffsetGreen\n", true, false, true);
                ed.Document.SendStringToExecute(".CreateRegions\n", true, false, true);
                ed.Document.SendStringToExecute(".IntersectOffsetRegion\n", true, false, true);
                ed.Document.SendStringToExecute(".DeleteAllPolylines\n", true, false, true);
                ed.Document.SendStringToExecute(".SubtractRegions\n", true, false, true);
                ed.Document.SendStringToExecute(".ConvertRegToPolyline\n", true, false, true);
                ed.Document.SendStringToExecute(".DeletePoints\n", true, false, true);
                ed.Document.SendStringToExecute(".DeleteDuplicates\n", true, false, true);
                ed.Document.SendStringToExecute(".PGA-DeleteOffsetPolylines\n", true, false, true);
                AcadUtilities.SendStringToExecute(".ZoomExtents\n");
                ed.Document.SendStringToExecute(".PGA-SaveAS\n", true, false, true);
                ed.Document.SendStringToExecute(".PGA-SV-Timer-Enable\n", true, false, true);

                ResetVars();
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }

        /// <summary>
        ///     Starts the program.
        /// </summary>
        [CommandMethod("PGA-StartSportVision",CommandFlags.Session)]
        public static void StartProgram()
        {
            try
            {
                var ed = Application.DocumentManager.MdiActiveDocument.Editor;

                ed.Document.SendStringToExecute(".PGA-SV-Timer-Disable\n", true, false, true);
                AcadUtilities.SendStringToExecute(".ZoomExtents\n");

                ed.Document.SendStringToExecute(".SetXData\n", true, false, true);
                ed.Document.SendStringToExecute(".RegisterPolyInners\n", true, false, true);
                ed.Document.SendStringToExecute("-Mapcreatecentroids\n ALL\n\n", true, false, true);
                ed.Document.SendStringToExecute(".PGA-OffsetGreen\n", true, false, true);
                ed.Document.SendStringToExecute(".CreateRegions\n", true, false, true);
                ed.Document.SendStringToExecute(".IntersectOffsetRegion\n", true, false, true);
                ed.Document.SendStringToExecute(".DeleteAllPolylines\n", true, false, true);
                ed.Document.SendStringToExecute(".SubtractRegions\n", true, false, true);
                ed.Document.SendStringToExecute(".ConvertRegToPolyline\n", true, false, true);
                ed.Document.SendStringToExecute(".DeletePoints\n", true, false, true);
                ed.Document.SendStringToExecute(".DeleteDuplicates\n", true, false, true);
                ed.Document.SendStringToExecute(".PGA-DeleteOffsetPolylines\n", true, false, true);
                ed.Document.SendStringToExecute(".PGA-RefinePolylines\n", true, false, true);
                AcadUtilities.SendStringToExecute(".ZoomExtents\n");
                ed.Document.SendStringToExecute(".PGA-SaveAS\n", true, false, true);
                ed.Document.SendStringToExecute(".PGA-SV-Timer-Enable\n", true, false, true);
                ResetVars();
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }

        /// <summary>
        ///     Starts the program synch.
        /// </summary>
        [CommandMethod("PGA-StartSportVisionSynch")]
        public static void StartProgramSynch()
        {
            try
            {
                var ed = Application.DocumentManager.MdiActiveDocument.Editor;

                AcadUtilities.SendStringToExecute(".ZoomExtents\n");

                ed.Command(".PGA-ClosePolylines", 1, 1, 0);

                ed.Command(".SetXData", 1, 1, 0);
                ed.Command(".RegisterPolyInners", 1, 1, 0);
                ed.Command("-Mapcreatecentroids", "All", 1, 1, 0);
                ed.Command(".PGA-OffsetGreen", 1, 1, 0);
                ed.Command(".CreateRegions", 1, 1, 0);
                ed.Command(".IntersectOffsetRegion", 1, 1, 0);
                ed.Command(".DeleteAllPolylines", 1, 1, 0);
                ed.Command(".PGA-OffsetGreen", 1, 1, 0);

                AcadUtilities.SendStringToExecute(".ZoomExtents\n");

                ed.Command(".SubtractRegions", 1, 1, 0);
                ed.Command(".ConvertRegToPolyline", 1, 1, 0);
                ed.Command(".DeletePoints", 1, 1, 0);
                ed.Command(".DeleteDuplicates", 1, 1, 0);

                AcadUtilities.SendStringToExecute(".ZoomExtents\n");
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }

        /// <summary>
        ///     Zooms the extents.
        /// </summary>
        [CommandMethod("ZoomExtents")]
        public static void ZoomExtents()
        {
            var acad = global::Autodesk.AutoCAD.ApplicationServices.Application.AcadApplication;
            acad.GetType().InvokeMember("ZoomExtents", BindingFlags.Public |
                                                       BindingFlags.InvokeMethod, null, acad, null);
        }

        /// <summary>
        ///     Closes the active DWG.
        /// </summary>
        [CommandMethod("PGA-CloseActiveDWG")]
        public static void CloseActiveDwg()
        {
            Active.Document.CloseAndDiscard();
        }

        /// <summary>
        ///     Saves the DWG.
        /// </summary>
        [CommandMethod("PGA-SaveAS")]
        public static void SaveDwg()
        {
            try
            {
                using (var lLock = Active.Document.LockDocument())
                {
                    using (var db = Active.Database)
                    {
                        var dir = Path.GetDirectoryName(db.OriginalFileName);
                        if (dir != null)
                        {
                            var path = Path.Combine(dir, "SV");
                            if (!Directory.Exists(path))
                                Directory.CreateDirectory(path);

                            var file = Path.GetFileName(db.OriginalFileName);

                            path = Path.Combine(path, "SV-" + file);

                            db.SaveAs(path, Acaddb.DwgVersion.Current);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }

        /// <summary>
        ///     Hatches this instance.
        /// </summary>
        [CommandMethod("HatchPolys")]
        public static void Hatch()
        {
            try
            {
                var polyoId = GetAllPolylines();
                var ordered = OrderPolylines(polyoId);
                foreach (var p in ordered)
                {

                    if (p == null) continue;

                    try
                    {
                        HatchPolyline.TestHatch(p);
                    }
                    catch (Exception ex)
                    {
                        COMS.MessengerManager.LogException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }

        /// <summary>
        ///     Orders the polylines.
        /// </summary>
        /// <param name="polyoId">The polyo identifier.</param>
        /// <returns>IOrderedEnumerable&lt;Acaddb.Polyline&gt;.</returns>
        private static IOrderedEnumerable<Acaddb.Polyline> OrderPolylines(Acaddb.ObjectIdCollection polyoId)
        {
            var ordered = new List<Acaddb.Polyline>();
            foreach (Acaddb.ObjectId i in polyoId)
            {
                using (var tr = Active.StartTransaction())
                {
                    ordered.Add(tr.GetObject(i, Acaddb.OpenMode.ForWrite) as Acaddb.Polyline);
                }
            }

            return ordered.OrderBy(p => p.Area);
        }

        /// <summary>
        ///     Tags the x data.
        /// </summary>
        [CommandMethod("SetXData")]
        public static void TagXData()
        {
            try
            {
                var polyoId = GetAllPolylines();

                foreach (Acaddb.ObjectId oid in polyoId)
                {
                    var polyline = GetObject(oid);

                    if (polyline == null)
                        continue;

                    ForceClosePolyLine(ref polyline);

                    AddRegAppTableRecord("PGA");
                    var rb =
                        new Acaddb.ResultBuffer(
                            new Acaddb.TypedValue(1001, "PGA"),
                            new Acaddb.TypedValue(1000, oid.Handle)
                            );
                    try
                    {
                        SetXData(oid, rb);
                    }
                    catch (Exception ex)
                    {
                        // ignored
                    }
                }
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }

        /// <summary>
        ///     Forces the close poly line.
        /// </summary>
        /// <param name="polyline">The polyline.</param>
        private static void ForceClosePolyLine(ref Acaddb.Polyline polyline)
        {
            try
            {
                using (var tr = Active.StartTransaction())
                {
                    var p = tr.GetObject(polyline.ObjectId, Acaddb.OpenMode.ForWrite)
                        as Acaddb.Polyline;
                    if (p != null)
                    {
                        p.Closed = true;
                        polyline = p;
                    }
                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }

        /// <summary>
        ///     Creates the regions.
        /// </summary>
        [CommandMethod("CreateRegions")]
        public static void CreateRegions()
        {
            try
            {
                var layer = "0";

                var acDoc = Application.DocumentManager.MdiActiveDocument;
                var acCurDb = acDoc.Database;
                var polyoId = GetAllPolylines();

                Acaddb.Region region;

                foreach (Acaddb.ObjectId oid in polyoId)
                {
                    var polyline = GetObject(oid);

                    if (polyline == null) continue;
                    AddRegAppTableRecord("PGA");

                    try
                    {
                        layer = polyline.Layer;

                        using (var tr = Application.DocumentManager.MdiActiveDocument
                            .TransactionManager.StartTransaction())
                        {
                            // Open the Block table for read
                            Acaddb.BlockTable acBlkTbl;
                            acBlkTbl = tr.GetObject(acCurDb.BlockTableId,
                                Acaddb.OpenMode.ForRead) as Acaddb.BlockTable;

                            // Open the Block table record Model space for write
                            Acaddb.BlockTableRecord acBlkTblRec;
                            acBlkTblRec = tr.GetObject(acBlkTbl[Acaddb.BlockTableRecord.ModelSpace],
                                Acaddb.OpenMode.ForWrite) as Acaddb.BlockTableRecord;

                            var curves = GetCurves(polyline);

                            var reg = Acaddb.Region.CreateFromCurves(curves).Cast<Acaddb.Region>();
                            var enumerable = reg as Acaddb.Region[] ?? reg.ToArray();
                            enumerable.SingleOrDefault().Layer = layer;
                            region = enumerable.SingleOrDefault();
                            // Add the final region to the database
                            acBlkTblRec.AppendEntity(enumerable.SingleOrDefault());
                            tr.AddNewlyCreatedDBObject(enumerable.SingleOrDefault(), true);
                            tr.Commit();
                        }

                        AddRegAppTableRecord("PGA");
                        var rb =
                            new Acaddb.ResultBuffer(
                                new Acaddb.TypedValue(1001, "PGA"),
                                new Acaddb.TypedValue(1000, oid.Handle)
                                );

                        SetXData(region.ObjectId, rb);
                    }
                    catch (Exception ex)
                    {
                        COMS.MessengerManager.LogException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }

        /// <summary>
        ///     Gets the curves.
        /// </summary>
        /// <param name="polyline">The polyline.</param>
        /// <returns>Acaddb.DBObjectCollection.</returns>
        /// <exception cref="System.ArgumentNullException">Offsets Curves were not created!</exception>
        public static Acaddb.DBObjectCollection GetCurves(Acaddb.Polyline polyline)
        {
            try
            {
                var curves = polyline.GetOffsetCurves(0.0001);
                if (curves.Count == 0)
                    curves = polyline.GetOffsetCurves(0.001);
                if (curves.Count == 0)
                    curves = polyline.GetOffsetCurves(0.01);
                if (curves.Count == 0)
                    curves = polyline.GetOffsetCurves(0.1);

                return curves;
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }

            throw new ArgumentNullException
                ("Offsets Curves were not created!");
        }

        /// <summary>
        ///     Creates the regions.
        /// </summary>
        /// <param name="oid">The oid.</param>
        /// <returns>Acaddb.ObjectId.</returns>
        public static Acaddb.ObjectId CreateRegions(Acaddb.ObjectId oid)
        {
            try
            {
                var polyline = GetObject(oid);

                if (polyline == null)
                    return Acaddb.ObjectId.Null;

                var acDoc = Application.DocumentManager.MdiActiveDocument;
                var acCurDb = acDoc.Database;
                var res = Acaddb.ObjectId.Null;
                try
                {
                    using (var tr = Application.DocumentManager.MdiActiveDocument
                        .TransactionManager.StartTransaction())
                    {
                        // Open the Block table for read
                        Acaddb.BlockTable acBlkTbl;
                        acBlkTbl = tr.GetObject(acCurDb.BlockTableId,
                            Acaddb.OpenMode.ForRead) as Acaddb.BlockTable;

                        // Open the Block table record Model space for write
                        Acaddb.BlockTableRecord acBlkTblRec;
                        acBlkTblRec = tr.GetObject(acBlkTbl[Acaddb.BlockTableRecord.ModelSpace],
                            Acaddb.OpenMode.ForWrite) as Acaddb.BlockTableRecord;
                        var curves = polyline.GetOffsetCurves(0.0001);
                        var reg = Acaddb.Region.CreateFromCurves(curves).Cast<Acaddb.Region>();

                        // Add the final region to the database
                        if (acBlkTblRec != null) res = acBlkTblRec.AppendEntity(reg.SingleOrDefault());
                        tr.AddNewlyCreatedDBObject(reg.SingleOrDefault(), true);
                        tr.Commit();
                    }
                    return res;
                }
                catch (Exception ex)
                {
                    COMS.MessengerManager.LogException(ex);
                }
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }

            return Acaddb.ObjectId.Null;
        }

        /// <summary>
        ///     Intersects the offset region.
        /// </summary>
        [CommandMethod("IntersectOffsetRegion")]
        public static void IntersectOffsetRegion()
        {
            try
            {
                Acaddb.Region reg = null;
                var regId = GetAllRegions();

                foreach (Acaddb.ObjectId oid in regId)
                {
                    // Get the current document and database
                    var acDoc = Application.DocumentManager.MdiActiveDocument;
                    var acCurDb = acDoc.Database;

                    try
                    {
                        using (var tr = acCurDb.TransactionManager.StartTransaction())
                        {
                            var r1 = GetObjectAsRegion(oid);
                            var r2 = GetObjectAsRegion(CreateRegions(OffsetObject));
                            if (r1 == null || r2 == null) continue;

                            var layer = r1.Layer;

                            // Open the Block table for read
                            Acaddb.BlockTable acBlkTbl;
                            acBlkTbl = tr.GetObject(acCurDb.BlockTableId,
                                Acaddb.OpenMode.ForRead) as Acaddb.BlockTable;

                            // Open the Block table record Model space for write
                            Acaddb.BlockTableRecord acBlkTblRec;
                            acBlkTblRec = tr.GetObject(acBlkTbl[Acaddb.BlockTableRecord.ModelSpace],
                                Acaddb.OpenMode.ForWrite) as Acaddb.BlockTableRecord;
                            var reg1 = tr.GetObject(r1.ObjectId, Acaddb.OpenMode.ForWrite) as Acaddb.Region;
                            var reg2 = tr.GetObject(r2.ObjectId, Acaddb.OpenMode.ForWrite) as Acaddb.Region;
                            var r1X = GetXData(oid);
                            var r2X = GetXData(oid); //Force one unique value. Offset oid not needed
                            var r1V = ParseXData(r1X.SingleOrDefault(p => p.TypeCode.CompareTo(1000).Equals(0)));
                            var r2V = ParseXData(r2X.SingleOrDefault(p => p.TypeCode.CompareTo(1000).Equals(0)));
                            AddRegAppTableRecord("PGA");
                            var rb1 =
                                new Acaddb.ResultBuffer(
                                    new Acaddb.TypedValue(1001, "PGA"),
                                    new Acaddb.TypedValue(1000, r1V)
                                    );
                            var rb2 =
                                new Acaddb.ResultBuffer(
                                    new Acaddb.TypedValue(1001, "PGA"),
                                    new Acaddb.TypedValue(1000, r2V)
                                    );

                            SetXData(reg1.ObjectId, rb1);
                            SetXData(reg2.ObjectId, rb2);

                            if (r1.Area > r2.Area)
                            {
                                reg1.BooleanOperation(Acaddb.BooleanOperationType.BoolIntersect, r2);
                                reg1.Layer = layer;
                                reg = reg1;
                            }
                            else if (r1.Area < r2.Area)
                            {
                                if (reg2.IsWriteEnabled)
                                    reg2.BooleanOperation(Acaddb.BooleanOperationType.BoolIntersect, reg1);
                                reg2.Layer = layer;
                                reg = reg2;
                            }

                            if (reg == null)
                                tr.Commit();

                            tr.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        COMS.MessengerManager.LogException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }

        /// <summary>
        ///     Gets the object as region.
        /// </summary>
        /// <param name="oid">The oid.</param>
        /// <returns>Acaddb.Region.</returns>
        private static Acaddb.Region GetObjectAsRegion(Acaddb.ObjectId oid)
        {
            using (var tr = Application.DocumentManager.MdiActiveDocument
                .TransactionManager.StartTransaction())
            {
                var obj =
                    tr.GetObject(oid, Acaddb.OpenMode.ForWrite);
                var reg = obj as Acaddb.Region;
                return reg;
            }
        }

        /// <summary>
        ///     Intersects the regions.
        /// </summary>
        /// <param name="r1">The r1.</param>
        /// <param name="r2">The r2.</param>
        /// <returns>Acaddb.Region.</returns>
        public static Acaddb.Region IntersectRegions(Acaddb.Region r1, Acaddb.Region r2)
        {
            if (r1.Area > r2.Area)
            {
                r1.UpgradeOpen();
                r1.BooleanOperation(Acaddb.BooleanOperationType.BoolIntersect, r2);
                r1.DowngradeOpen();

                return r1;
            }
            {
                r2.UpgradeOpen();
                r2.BooleanOperation(Acaddb.BooleanOperationType.BoolIntersect, r1);
                r2.DowngradeOpen();
                return r2;
            }
        }

        /// <summary>
        ///     Gets all polylines.
        /// </summary>
        /// <returns>ACADDB.ObjectIdCollection.</returns>
        private static Acaddb.ObjectIdCollection GetAllPolylines()
        {
            return Process.GetIdsByTypeTypeValue(
                "POLYLINE",
                "LWPOLYLINE",
                "POLYLINE2D");
        }

        /// <summary>
        ///     Gets all regions.
        /// </summary>
        /// <returns>Acaddb.ObjectIdCollection.</returns>
        private static Acaddb.ObjectIdCollection GetAllRegions()
        {
            return Process.GetIdsByTypeTypeValue("REGION");
        }

        /// <summary>
        ///     Gets all points.
        /// </summary>
        /// <returns>Acaddb.ObjectIdCollection.</returns>
        private static Acaddb.ObjectIdCollection GetAllPoints()
        {
            return Process.GetIdsByTypeTypeValue("Point");
        }

        /// <summary>
        ///     Gets the object.
        /// </summary>
        /// <param name="polyoid">The polyoid.</param>
        /// <returns>ACADDB.Polyline.</returns>
        private static Acaddb.Polyline GetObject(Acaddb.ObjectId polyoid)
        {
            using (var tr = Application.DocumentManager.MdiActiveDocument
                .TransactionManager.StartTransaction())
            {
                var obj =
                    tr.GetObject(polyoid, Acaddb.OpenMode.ForRead);

                // If a "lightweight" (or optimized) polyline

                var lwp = obj as Acaddb.Polyline;

                if (lwp != null)
                {
                    // Is Polyline Closed
                    if (lwp.Closed)
                    {
                        return lwp;
                    }
                    lwp.UpgradeOpen();
                    lwp.Closed = true;
                    lwp.DowngradeOpen();
                    return lwp;
                }
                return null;
            }
        }

        /// <summary>
        ///     Offsets the green.
        /// </summary>
        [CommandMethod("PGA-OffsetGreen")]
        public static void OffsetGreen()
        {
            try
            {
                var polylineOids = GetAllPolylines();
                var OGR = GetGreen(polylineOids);

                if (OGR == null)
                    return;
                var offsetDist = 3.2808*30.0; //30 meters
                var polyline = OGR.Offset(offsetDist);

                if (polyline == null)
                    return;
                polyline.Layer = "0";

                // Get the current document and database
                var acDoc = Application.DocumentManager.MdiActiveDocument;
                var acCurDb = acDoc.Database;

                using (var acTrans = Application.DocumentManager.MdiActiveDocument
                    .TransactionManager.StartTransaction())
                {
                    // Open the Block table for read
                    Acaddb.BlockTable acBlkTbl;
                    acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
                        Acaddb.OpenMode.ForRead) as Acaddb.BlockTable;

                    // Open the Block table record Model space for write
                    Acaddb.BlockTableRecord acBlkTblRec;
                    acBlkTblRec = acTrans.GetObject(acBlkTbl[Acaddb.BlockTableRecord.ModelSpace],
                        Acaddb.OpenMode.ForWrite) as Acaddb.BlockTableRecord;
                    // Add each offset object
                    OffsetObject = acBlkTblRec.AppendEntity(polyline);
                    acTrans.AddNewlyCreatedDBObject(polyline, true);
                    acTrans.Commit();
                }

                AddRegAppTableRecord("PGA");
                var rb =
                    new Acaddb.ResultBuffer(
                        new Acaddb.TypedValue(1001, "PGA"),
                        new Acaddb.TypedValue(1000, OffsetObject.Handle)
                        );

                SetXData(OffsetObject, rb);
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }

        //todo complete //

        /// <summary>
        ///     Refines the polylines.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">Failed to Retrieve 2 Polylines to Compare!</exception>
        [CommandMethod("PGA-RefinePolylines")]
        public static void RefinePolylines()
        {
            try
            {
                var polys = GetAllPolylines();
                var filtered = FilteredPolys("ORO");

                if (filtered.FirstOrDefault() == null || filtered.Count < 2)
                {
                    return;
                }

                var ordered = filtered.OrderByDescending(p => p.Area);

                if (ordered.ElementAtOrDefault(0) != ordered.ElementAtOrDefault(1))
                {
                    //Is Larger Within Inner OR On Boundary
                    var o = ordered.ElementAtOrDefault(0);
                    var i = ordered.ElementAtOrDefault(1);
                    var outer = AcadUtilities.GetPointsFromPolyline(o);
                    var inner = AcadUtilities.GetPointsFromPolyline(i);
                    var intpoints = new Point3dCollection();

                    foreach (var p in inner)
                    {
                        if (WNumAlgorithm
                            .wn_PnPoly(p, outer.ToArray(), outer.Count) != 0)
                        {
                            if (PolylinesIntersect(o, i))
                            {
                                using (var tr = Active.StartTransaction())
                                {
                                    o.ObjectId.GetObject(Acaddb.OpenMode.ForWrite).Erase(true);
                                    tr.Commit();
                                }
                            }
                            break;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }

        /// <summary>
        ///     Polylineses the intersect.
        /// </summary>
        /// <param name="p1">The p1.</param>
        /// <param name="p2">The p2.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool PolylinesIntersect(Acaddb.Polyline p1, Acaddb.Polyline p2)
        {
            try
            {
                var intpoints = new Point3dCollection();

                p1.IntersectWith(p2, Acaddb.Intersect.OnBothOperands,
                    intpoints, IntPtr.Zero, IntPtr.Zero);

                if (intpoints.Count > 0)
                {
                    var a = intpoints.Count/1.0;
                    var b = p1.NumberOfVertices/1.0;

                    var result = Math.Abs(a/b)*100.0;

                    if (result > 50)
                        return true;
                }
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
            return false;
        }

        /// <summary>
        ///     Filtereds the polys.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>List&lt;Acaddb.Polyline&gt;.</returns>
        private static List<Acaddb.Polyline> FilteredPolys(string name)
        {
            var oids = new List<Acaddb.Polyline>();
            var polys = GetAllPolylines();

            using (var tr = Active.StartTransaction())
            {
                foreach (Acaddb.ObjectId oid in polys)
                {
                    var RXclass = RXObject.GetClass(typeof (Acaddb.Polyline));

                    if (RXclass.IsDerivedFrom(RXclass))
                    {
                        var dbobject = oid.GetObject(Acaddb.OpenMode.ForRead);
                        var polyline = dbobject as Acaddb.Polyline;
                        if (polyline != null)
                        {
                            var layer = polyline.Layer.ToUpper();

                            if (name == layer)
                            {
                                oids.Add(polyline);
                            }
                        }
                    }
                }
                return oids;
            }
        }

        /// <summary>
        ///     Gets the green.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns>ACADDB.Polyline.</returns>
        private static Acaddb.Polyline GetGreen(Acaddb.ObjectIdCollection collection)
        {
            try
            {
                var theClass = RXObject.GetClass(typeof (Acaddb.Polyline));

                foreach (Acaddb.ObjectId oid in collection)
                {
                    if (!oid.ObjectClass.IsDerivedFrom(theClass))
                        continue;

                    var polyline = GetObject(oid);
                    if (polyline == null)
                        continue;
                    if (polyline.Layer.Length < 8)
                        if (polyline.Layer.Equals("OGR") ||
                            polyline.Layer.Contains("S-GREEN"))
                            return polyline;
                }
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
            return null;
        }

        /// <summary>
        ///     Converts the reg to polyline.
        /// </summary>
        [CommandMethod("ConvertRegToPolyline")]
        public static void ConvertRegToPolyline()
        {
            try
            {
                var regId = GetAllRegions();

                foreach (Acaddb.ObjectId oid in regId)
                {
                    try
                    {
                        RegionConversion.Commands.RegionToPolyline(oid);
                    }
                    catch (Exception ex)
                    {
                        COMS.MessengerManager.LogException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }

        /// <summary>
        ///     Gets the feature lines.
        /// </summary>
        /// <returns>List&lt;Acaddb.ObjectId&gt;.</returns>
        public static List<Acaddb.ObjectId> GetFeatureLines()

        {
            List<Acaddb.ObjectId> ids = null;

            try
            {
                var db = Active.Database;

                using (var tran = db.TransactionManager.StartTransaction())

                {
                    var tbl =
                        (Acaddb.BlockTable) tran.GetObject(db.BlockTableId, Acaddb.OpenMode.ForRead);

                    var br =
                        (Acaddb.BlockTableRecord)
                            tran.GetObject(tbl[Acaddb.BlockTableRecord.ModelSpace], Acaddb.OpenMode.ForRead);

                    var b = br.Cast<Acaddb.ObjectId>();

                    #region Other Types

                    //==============search certain entity========================//

                    //"LINE" for line

                    //"LWPOLYLINE" for polyline

                    //"CIRCLE" for circle

                    //"INSERT" for block reference

                    //...

                    //We can use "||" (or) to search for more then one entity types

                    //============================================================//

                    //Use lambda extension method

                    #endregion

                    ids = (from id in b
                        where id.ObjectClass.DxfName.ToUpper() == "AECC_FEATURE_LINE"
                        select id).ToList();

                    tran.Commit();
                }
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }

            return ids;
        }

        /// <summary>
        ///     Gets the internal polys.
        /// </summary>
        /// <returns>Dictionary&lt;Acaddb.ObjectId, Acaddb.ObjectIdCollection&gt;.</returns>
        public static Dictionary<Acaddb.ObjectId, Acaddb.ObjectIdCollection> GetInternalPolys()
        {
            var dictionary = new Dictionary<Acaddb.ObjectId, Acaddb.ObjectIdCollection>();

            var polyLines = new Acaddb.ObjectIdCollection();

            using (var db = Active.WorkingDatabase)
            {
                var oids = GetAllPolylines();

                foreach (Acaddb.ObjectId objectId in oids)
                {
                    try
                    {
                        polyLines = PolylineManager.PolylineManager.GetAllInternalPolyLines(objectId, oids);
                        dictionary.Add(objectId, polyLines);
                    }
                    catch (NullReferenceException)
                    {
                    }
                }
                return dictionary;
            }
        }

        /// <summary>
        ///     Writes the polys to database.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="p">The p.</param>
        public static void WritePolysToDatabase(object data, Acaddb.Polyline p)
        {
            using (var commands = new DatabaseCommands())
            {
                var dictionary = (Dictionary<Acaddb.ObjectId, Acaddb.ObjectIdCollection>) data;

                if (dictionary == null)
                    return;

                foreach (var obj in dictionary)
                {
                    try
                    {
                        var date = DateTime.Now;

                        var o = obj.Key is Acaddb.ObjectId
                            ? obj.Key
                            : new Acaddb.ObjectId();
                        var c = obj.Value is Acaddb.ObjectIdCollection
                            ? obj.Value
                            : new Acaddb.ObjectIdCollection();
                        foreach (Acaddb.ObjectId ob in c)
                        {
                            var po = new Polyline();
                            po.Handle = o.Handle.ToString();
                            po.Layer = p.Layer;
                            po.Area = p.Area.ToString();
                            po.DateStamp = date;
                            po.Length = p.Length.ToString();
                            po.Depends = ob.Handle.ToString();
                            po.Level = c.Count.ToString();
                            po.Nodes = p.NumberOfVertices.ToString();
                            commands.InsertPolyline(po);
                        }
                    }
                    catch (Exception ex)
                    {
                        COMS.MessengerManager.LogException(ex);
                    }
                }
            }
        }

        /// <summary>
        ///     Registers the polys.
        /// </summary>
        /// <exception cref="System.ArgumentNullException"></exception>
        [CommandMethod("RegisterPolyInners")]
        public static void RegisterPolys()
        {
            var dictionary = new Dictionary<Acaddb.ObjectId, Acaddb.ObjectIdCollection>();

            var polyLines = new Acaddb.ObjectIdCollection();

            ClearPolylineInners();

            using (var db = Active.WorkingDatabase)
            {
                var oids = GetAllPolylines();

                foreach (Acaddb.ObjectId objectId in oids)
                {
                    try
                    {
                        using (var tr = Active.StartTransaction())
                        {
                            var p = tr.GetObject(objectId, Acaddb.OpenMode.ForRead) as Acaddb.Polyline;
                            if (p == null) throw new ArgumentNullException(nameof(p));
                            polyLines = PolylineManager.PolylineManager.GetAllInternalPolyLines(objectId, oids);

                            if (polyLines == null)
                                continue; // throw new ArgumentNullException();

                            dictionary.Add(objectId, polyLines);
                            WritePolysToDatabase(dictionary, p);
                            tr.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        COMS.MessengerManager.LogException(ex);
                    }
                }
            }
        }

        /// <summary>
        ///     ws the n_ register polys.
        /// </summary>
        /// <exception cref="System.ArgumentNullException"></exception>
        [CommandMethod("WN_RegisterPolyInners")]
        public static void WN_RegisterPolys()
        {
            var dictionary = new Dictionary<Acaddb.ObjectId, Acaddb.ObjectIdCollection>();

            var polyLines = new Acaddb.ObjectIdCollection();

            ClearPolylineInners();

            using (var db = Active.WorkingDatabase)
            {
                var oids = GetAllPolylines();

                foreach (Acaddb.ObjectId objectId in oids)
                {
                    try
                    {
                        using (var tr = Active.StartTransaction())
                        {
                            var p = tr.GetObject(objectId, Acaddb.OpenMode.ForRead) as Acaddb.Polyline;
                            if (p == null) throw new ArgumentNullException(nameof(p));
                            polyLines = PolylineManager.PolylineManager.GetAllInternalPolyLines(objectId, oids);

                            if (polyLines == null)
                                continue; // throw new ArgumentNullException();

                            dictionary.Add(objectId, polyLines);
                            WritePolysToDatabase(dictionary, p);
                            tr.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        COMS.MessengerManager.LogException(ex);
                    }
                }
            }
        }

        /// <summary>
        ///     Clears the polyline inners.
        /// </summary>
        private static void ClearPolylineInners()
        {
            try
            {
                using (var commands = new DatabaseCommands())
                {
                    commands.ClearPolylineInners();
                }
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }

        /// <summary>
        ///     Deletes the zero regions.
        /// </summary>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static void DeleteZeroRegions()
        {
            using (var db = Active.WorkingDatabase)
            {
                var oids = GetAllRegions();

                foreach (Acaddb.ObjectId objectId in oids)
                {
                    try
                    {
                        using (var tr = Active.StartTransaction())
                        {
                            var p = tr.GetObject(objectId, Acaddb.OpenMode.ForRead) as Acaddb.Region;
                            if (p == null) throw new ArgumentNullException(nameof(p));
                            if (p.Area == 0.0)
                            {
                                p.UpgradeOpen();
                                p.Erase();
                            }
                            tr.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        COMS.MessengerManager.LogException(ex);
                    }
                }
            }
        }

        /// <summary>
        ///     Deletes the polys.
        /// </summary>
        /// <exception cref="System.ArgumentNullException"></exception>
        [CommandMethod("DeleteAllPolylines")]
        public static void DeletePolys()
        {
            using (var db = Active.WorkingDatabase)
            {
                var oids = GetAllPolylines();

                foreach (Acaddb.ObjectId objectId in oids)
                {
                    try
                    {
                        using (var tr = Active.StartTransaction())
                        {
                            var p = tr.GetObject(objectId, Acaddb.OpenMode.ForRead) as Acaddb.Polyline;
                            if (p == null) throw new ArgumentNullException(nameof(p));
                            p.UpgradeOpen();
                            p.Erase();
                            tr.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        COMS.MessengerManager.LogException(ex);
                    }
                }
            }
        }

        /// <summary>
        ///     Deletes the offset polys. On Layer 0 and by ObjectId.
        /// </summary>
        /// <exception cref="System.ArgumentNullException"></exception>
        [CommandMethod("PGA-DeleteOffsetPolylines")]
        public static void DeleteOffsetPolys()
        {
            using (var db = Active.WorkingDatabase)
            {
                var oids = GetAllPolylines();

                foreach (Acaddb.ObjectId objectId in oids)
                {
                    try
                    {
                        using (var tr = Active.StartTransaction())
                        {
                            var p = tr.GetObject(objectId, Acaddb.OpenMode.ForRead) as Acaddb.Polyline;
                            if (p == null) throw new ArgumentNullException(nameof(p));
                            if (p.Layer == "0" || objectId == OffsetObject)
                            {
                                p.UpgradeOpen();
                                p.Erase();
                            }
                            tr.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        COMS.MessengerManager.LogException(ex);
                    }
                }
            }
        }

        /// <summary>
        ///     Deletes the dups.
        /// </summary>
        /// <exception cref="System.ArgumentNullException"></exception>
        [CommandMethod("DeleteDuplicates")]
        public static void DeleteDups()
        {
            var polylines = new List<Acaddb.Polyline>();
            var usedPolys = new List<Acaddb.Polyline>();
            var TOLERANCE = 0.1;

            using (var db = Active.WorkingDatabase)
            {
                var oids = GetAllPolylines();

                foreach (Acaddb.ObjectId objectId in oids)
                {
                    try
                    {
                        using (var tr = Active.StartTransaction())
                        {
                            var p = tr.GetObject(objectId, Acaddb.OpenMode.ForRead) as Acaddb.Polyline;
                            if (p == null) throw new ArgumentNullException(nameof(p));
                            polylines.Add(p);
                            tr.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        COMS.MessengerManager.LogException(ex);
                    }
                }
                foreach (var p in polylines)
                {
                    try
                    {
                        using (var tr = Active.StartTransaction())
                        {
                            {
                                var result =
                                    polylines.Where(r => Math.Abs(r.Area - p.Area) < TOLERANCE && r.Layer == p.Layer)
                                        .Select(d => d);
                                for (var i = 0; i < result.Count(); i++)
                                {
                                    if (i > 0)
                                    {
                                        var g = result.ElementAt(i);

                                        if (g.ObjectId == p.ObjectId || g.IsErased)
                                            continue;
                                        var obj = g.ObjectId.GetObject(Acaddb.OpenMode.ForWrite) as Acaddb.Polyline;
                                        if (obj.StartPoint.Equals(p.StartPoint))
                                            obj.Erase();
                                    }
                                }
                            }
                            tr.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        COMS.MessengerManager.LogException(ex);
                    }
                }
            }
        }

        /// <summary>
        ///     Substracts the regions.
        /// </summary>
        [CommandMethod("SubtractRegions")]
        public static void SubstractRegions()
        {
            try
            {
                Acaddb.Region reg = null;
                var regId = GetAllRegions();
                var polyinfo = GetPolylineInfo();
                var used = new List<string>();
                var delete = new List<Acaddb.Region>();

                DeleteZeroRegions();

                foreach (Acaddb.ObjectId oid in regId)
                {
                    if (oid.IsErased)
                        continue;

                    // Get the current document and database
                    var acDoc = Application.DocumentManager.MdiActiveDocument;
                    var acCurDb = acDoc.Database;

                    var master = GetXData(oid);
                    var outer = ParseXData(master.SingleOrDefault(p => p.TypeCode.CompareTo(1000).Equals(0)));

                    IEnumerable<string> dependants = new List<string>();

                    try
                    {
                        if (GetObjectAsRegion(oid).Layer == "0")
                            continue;

                        var h = GetAllInnerToSelected(oid);

                        if (h.Count == 0)
                            continue;

                        dependants = from v in h
                            where v.Handle.Trim() == outer.Trim()
                            select v.Depends.Trim();

                        if (dependants == null)
                            continue;

                        foreach (Acaddb.ObjectId o in regId)
                        {
                            if (o.IsErased)
                                continue;

                            var xd = GetXData(o);
                            var inner = ParseXData(xd.SingleOrDefault(p => p.TypeCode.CompareTo(1000).Equals(0)));

                            Debug.WriteLine("Regions: " + inner);

                            if (dependants.Contains(inner))
                            {
                                using (var tr = acCurDb.TransactionManager.StartTransaction())
                                {
                                    var r1 = GetObjectAsRegion(oid); //Outer Region
                                    var r2 = GetObjectAsRegion(o); //Inner Region

                                    if (r1 == null || r2 == null)
                                        continue;
                                    if (r1.Area == 0 || r2.Area == 0)
                                        continue;
                                    if (r1.Area == r2.Area)
                                        continue;

                                    var layer = r1.Layer;

                                    if (layer == "0")
                                        continue;

                                    // Open the Block table for read
                                    Acaddb.BlockTable acBlkTbl;
                                    acBlkTbl = tr.GetObject(acCurDb.BlockTableId,
                                        Acaddb.OpenMode.ForRead) as Acaddb.BlockTable;

                                    // Open the Block table record Model space for write
                                    Acaddb.BlockTableRecord acBlkTblRec;
                                    acBlkTblRec = tr.GetObject(acBlkTbl[Acaddb.BlockTableRecord.ModelSpace],
                                        Acaddb.OpenMode.ForWrite) as Acaddb.BlockTableRecord;
                                    var reg1 = tr.GetObject(r1.ObjectId, Acaddb.OpenMode.ForWrite) as Acaddb.Region;
                                    var reg2 = tr.GetObject(r2.ObjectId, Acaddb.OpenMode.ForWrite) as Acaddb.Region;

                                    var treg1 = new Acaddb.Region();
                                    var treg2 = new Acaddb.Region();

                                    //Create a Copy

                                    treg1.CopyFrom(reg1);
                                    treg2.CopyFrom(reg2);

                                    if (reg1.Area > 0 && reg2.Area > 0)
                                    {
                                        if (treg1.Area == treg2.Area)
                                            continue;

                                        try
                                        {
                                            if (treg1.Layer == treg2.Layer)
                                            {
                                                treg1.BooleanOperation(Acaddb.BooleanOperationType.BoolSubtract, treg2);
                                                treg1.Layer = layer;
                                                acBlkTblRec.AppendEntity(treg1);
                                                tr.AddNewlyCreatedDBObject(treg1, true);
                                                delete.Add(reg1);
                                                delete.Add(reg2);
                                            }
                                        }
                                        catch (Exception ex)
                                        {

                                            {
                                                COMS.MessengerManager.LogException(ex);
                                            }
                                        }
                                    }
                                    tr.Commit();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        COMS.MessengerManager.LogException(ex);
                    }
                }
                DeleteOperations(delete);
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }

        /// <summary>
        ///     Deletes the operations.
        /// </summary>
        /// <param name="delete">The delete.</param>
        public static void DeleteOperations(List<Acaddb.Region> delete)
        {
            using (var tr = Active.StartTransaction())
            {
                var data = GetAllRegions();
                foreach (Acaddb.ObjectId o in data)
                {
                    var r = tr.GetObject(o, Acaddb.OpenMode.ForWrite) as Acaddb.Region;
                    if (delete.Where(p => p.ObjectId == o).Any())
                        r.Erase();
                }
                tr.Commit();
            }
        }

        /// <summary>
        ///     Deletes the operations.
        /// </summary>
        [CommandMethod("DeletePoints")]
        public static void DeleteOperations()
        {
            try
            {
                DeleteOperations(GetAllPoints());
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }

        /// <summary>
        ///     Deletes the operations.
        /// </summary>
        /// <param name="delete">The delete.</param>
        public static void DeleteOperations(Acaddb.ObjectIdCollection delete)
        {
            try
            {
                #region MyRegion

                using (var tr = Active.StartTransaction())
                {
                    foreach (Acaddb.ObjectId o in delete)
                    {
                        var r = tr.GetObject(o, Acaddb.OpenMode.ForWrite) as Acaddb.DBPoint;
                        r.Erase();
                    }
                    tr.Commit();
                }

                #endregion
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }

        /// <summary>
        ///     Gets all inner to selected.
        /// </summary>
        /// <param name="oid">The oid.</param>
        /// <returns>IList&lt;Polyline&gt;.</returns>
        private static IList<PGA.DataContext.Polyline> GetAllInnerToSelected(Acaddb.ObjectId oid)
        {
            IList<Polyline> pl;
            var doc = Active.MdiDocument;
            using (var tr = Active.StartTransaction())
            {
                var data = GetXData(oid);
                var s = ParseXData(data.SingleOrDefault(p => p.TypeCode.CompareTo(1000).Equals(0)));

                using (var commands = new DatabaseCommands())
                {
                    pl = commands.GetPolyInnersByHandle(s);
                }

                tr.Commit();
            }
            return pl;
        }

        /// <summary>
        ///     Gets the handlesby area layer.
        /// </summary>
        /// <param name="polyinfo">The polyinfo.</param>
        /// <param name="r">The r.</param>
        /// <returns>System.String.</returns>
        private static string GetHandlesbyAreaLayer(IList<PGA.DataContext.Polyline> polyinfo, Acaddb.Region r)
        {
            var result = (from p in polyinfo
                where Convert.ToDouble(p.Area) == Convert.ToDouble(r.Area)
                      && p.Layer == r.Layer
                select p.Handle).FirstOrDefault();

            return result;
        }

        /// <summary>
        ///     Gets the innersby handle.
        /// </summary>
        /// <param name="polyinfo">The polyinfo.</param>
        /// <param name="handle">The handle.</param>
        /// <returns>IList&lt;Polyline&gt;.</returns>
        private static IList<PGA.DataContext.Polyline> GetInnersbyHandle(IList<PGA.DataContext.Polyline> polyinfo, string handle)
        {
            var result = from p in polyinfo
                where p.Handle == handle
                select p;

            return result.ToList();
        }

        /// <summary>
        ///     Gets the polyline information.
        /// </summary>
        /// <returns>IList&lt;DataContext.Polyline&gt;.</returns>
        private static IList<PGA.DataContext.Polyline> GetPolylineInfo()
        {
            using (var commands = new DatabaseCommands())
            {
                return commands.GetPolyInners();
            }
        }

        /// <summary>
        ///     Sets the x data.
        /// </summary>
        /// <param name="o">The o.</param>
        /// <param name="rb">The rb.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private static bool SetXData(Acaddb.ObjectId o, Acaddb.ResultBuffer rb)
        {
            var doc = Active.MdiDocument;
            using (var tr = Active.StartTransaction())
            {
                AcadDatabaseManager.SetXData(tr, o, "PGA", rb);
                tr.Commit();
            }
            return true;
        }

        /// <summary>
        ///     Gets the x data.
        /// </summary>
        /// <param name="o">The o.</param>
        /// <returns>IEnumerable&lt;Acaddb.TypedValue&gt;.</returns>
        public static IEnumerable<Acaddb.TypedValue> GetXData(Acaddb.ObjectId o)
        {
            IEnumerable<Acaddb.TypedValue> tv;

            var doc = Active.MdiDocument;
            using (var tr = Active.StartTransaction())
            {
                tv = AcadDatabaseManager.GetXData(tr, o, "PGA");
                tr.Commit();
            }
            return tv;
        }

        /// <summary>
        ///     Parses the x data.
        /// </summary>
        /// <param name="tv">The tv.</param>
        /// <returns>System.String.</returns>
        private static string ParseXData(Acaddb.TypedValue tv)
        {
            return tv.Value.ToString();
        }

        /// <summary>
        ///     Adds the reg application table record.
        /// </summary>
        /// <param name="regAppName">Name of the reg application.</param>
        private static void AddRegAppTableRecord(string regAppName)
        {
            var doc =
                Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var tr =
                doc.TransactionManager.StartTransaction();
            using (tr)
            {
                var rat =
                    (Acaddb.RegAppTable) tr.GetObject(
                        db.RegAppTableId,
                        Acaddb.OpenMode.ForRead,
                        false
                        );
                if (!rat.Has(regAppName))
                {
                    rat.UpgradeOpen();
                    var ratr =
                        new Acaddb.RegAppTableRecord();
                    ratr.Name = regAppName;
                    rat.Add(ratr);
                    tr.AddNewlyCreatedDBObject(ratr, true);
                }
                tr.Commit();
            }
        }

        /// <summary>
        ///     Get3s the d polylines.
        /// </summary>
        /// <returns>Acaddb.ObjectIdCollection.</returns>
        public static Acaddb.ObjectIdCollection Get3DPolylines()
        {
            // 3D polylines have DXF type "POLYLINE" (shared with 2D); callers filter by class.
            return Process.GetIdsByTypeTypeValue("POLYLINE");
        }

        /// <summary>
        ///     Convert3s the d polylines.
        /// </summary>
        /// <param name="oids">The oids.</param>
        /// <returns>List&lt;Acaddb.Polyline3d&gt;.</returns>
        public static List<Acaddb.Polyline3d> Convert3DPolylines(Acaddb.ObjectIdCollection oids)
        {
            try
            {
                var outids = new List<Acaddb.Polyline3d>();

                using (var tr = Active.StartTransaction())
                {
                    foreach (Acaddb.ObjectId o in oids)
                    {
                        var v = tr.GetObject(o, Acaddb.OpenMode.ForRead) as Acaddb.Polyline3d;
                        if (v != null)
                            outids.Add(v);
                    }
                    tr.Commit();
                }

                return outids;
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }

            return null;
        }

        /// <summary>
        ///     Corrects 3D polylines that contain vertices at elevation zero by
        ///     interpolating those elevations from the neighbouring vertices.
        /// </summary>
        [CommandMethod("CorrectZeroPolylines")]
        public static void CorrectPolylines()
        {
            try
            {
                var objects = Get3DPolylines();

                using (var tr = Active.StartTransaction())
                {
                    foreach (Acaddb.ObjectId id in objects)
                    {
                        // DXF "POLYLINE" also matches 2D polylines; the cast keeps the 3D ones.
                        var p = tr.GetObject(id, Acaddb.OpenMode.ForRead) as Acaddb.Polyline3d;
                        if (p != null)
                            CorrectPolylines(p);
                    }
                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }

        /// <summary>
        ///     Replaces zero elevations on the vertices of a 3D polyline with values
        ///     interpolated from the nearest non-zero vertices.
        /// </summary>
        /// <param name="poly">The polyline, open in an active transaction.</param>
        /// <returns>The polyline, or null if it could not be corrected.</returns>
        public static Acaddb.Polyline3d CorrectPolylines(Acaddb.Polyline3d poly)
        {
            if (poly == null)
                return null;

            try
            {
                using (var tr = Active.StartTransaction())
                {
                    var vertices = new List<Acaddb.PolylineVertex3d>();
                    foreach (Acaddb.ObjectId vId in poly)
                    {
                        var v = tr.GetObject(vId, Acaddb.OpenMode.ForRead) as Acaddb.PolylineVertex3d;
                        if (v != null)
                            vertices.Add(v);
                    }

                    var points = vertices.Select(v => v.Position).ToList();
                    var elevations = InterpolateZeroElevations(points);

                    for (var i = 0; i < vertices.Count; i++)
                    {
                        if (elevations[i] == points[i].Z)
                            continue;

                        vertices[i].UpgradeOpen();
                        vertices[i].Position = new Point3d(points[i].X, points[i].Y, elevations[i]);
                    }
                    tr.Commit();
                }
                return poly;
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
            return null;
        }

        /// <summary>
        ///     Computes corrected elevations for a run of points. Every point at
        ///     elevation zero is linearly interpolated, by horizontal distance along
        ///     the path, between the nearest non-zero points before and after it.
        ///     Zero points at either end take the elevation of the nearest non-zero
        ///     point. If no point has an elevation, nothing changes.
        /// </summary>
        /// <param name="points">The points in path order.</param>
        /// <returns>One elevation per input point.</returns>
        public static double[] InterpolateZeroElevations(IList<Point3d> points)
        {
            const double zeroTolerance = 1e-9;

            var count = points.Count;
            var elevations = new double[count];
            var station = new double[count];

            for (var i = 0; i < count; i++)
            {
                elevations[i] = points[i].Z;
                if (i > 0)
                {
                    var dx = points[i].X - points[i - 1].X;
                    var dy = points[i].Y - points[i - 1].Y;
                    station[i] = station[i - 1] + Math.Sqrt(dx * dx + dy * dy);
                }
            }

            var previous = -1; // index of the last non-zero point seen
            for (var i = 0; i < count; i++)
            {
                if (Math.Abs(points[i].Z) > zeroTolerance)
                {
                    previous = i;
                    continue;
                }

                var next = -1;
                for (var j = i + 1; j < count; j++)
                {
                    if (Math.Abs(points[j].Z) > zeroTolerance)
                    {
                        next = j;
                        break;
                    }
                }

                if (previous >= 0 && next >= 0)
                {
                    var span = station[next] - station[previous];
                    var ratio = span > 0.0 ? (station[i] - station[previous]) / span : 0.0;
                    elevations[i] = points[previous].Z + (points[next].Z - points[previous].Z) * ratio;
                }
                else if (previous >= 0)
                {
                    elevations[i] = points[previous].Z;
                }
                else if (next >= 0)
                {
                    elevations[i] = points[next].Z;
                }
            }

            return elevations;
        }

        /// <summary>
        ///     Executes the automatic cad file.
        /// </summary>
        /// <param name="drawings">The drawings.</param>
        public static void ExecuteAutoCADFile(List<string> drawings)
        {
            Application.SetSystemVariable("FILEDIA", 0);
            Application.SetSystemVariable("NOMUTT", 1);
            Application.SetSystemVariable("CMDECHO", 1);

            foreach (var filename in drawings)
            {
                var doc = Application.DocumentManager.Open(filename, true);
                Application.DocumentManager.MdiActiveDocument = doc;
                var db = doc.Database;
                var ed = doc.Editor;
                using (doc.LockDocument())
                {
                    ed.Command(".PGA-StartSportVisionSynch", 1, 1, 0);
                }
            }
        }

        /// <summary>
        ///     Executes the automatic cad file.
        /// </summary>
        [CommandMethod("ExecuteAutoCADFile")]
        public static void ExecuteAutoCADFile()
        {
            Application.SetSystemVariable("FILEDIA", 0);
            Application.SetSystemVariable("NOMUTT", 0);
            Application.SetSystemVariable("CMDECHO", 0);

            {
                var selectedfile = Drawings.Skip(Counter).Take(1).FirstOrDefault();

                var doc = Application.DocumentManager.Open(selectedfile, true);
                Application.DocumentManager.MdiActiveDocument = doc;
                var db = doc.Database;
                var ed = doc.Editor;
                using (doc.LockDocument())
                {
                    ed.Command(".PGA-StartSportVisionSynch", 1, 1, 0);
                }
            }
        }

        /// <summary>
        ///     Saves the drawings.
        /// </summary>
        [CommandMethod("PGA-SaveDrawing")]
        public static void SaveDrawings()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            using (doc.LockDocument())
            {
                var name = Path.GetFileName(doc.Name);
                name = "ACAD-sv-" + name;
                db.SaveAs(name, Acaddb.DwgVersion.Current);
            }
        }

        /// <summary>
        ///     Closes the drawings.
        /// </summary>
        [CommandMethod("PGA-CloseDrawing")]
        public static void CloseDrawings()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            using (doc.LockDocument())
            {
                doc.CloseAndDiscard();
            }
        }

        /// <summary>
        ///     Closes the polylines.
        /// </summary>
        [CommandMethod("PGA-ClosePolylines")]
        public static void ClosePolylines()
        {
            try
            {
                var pId = GetAllPolylines();

                foreach (Acaddb.ObjectId oid in pId)
                {
                    try
                    {
                        using (var tr = Active.StartTransaction())
                        {
                            var p = tr.GetObject(oid, Acaddb.OpenMode.ForWrite) as Acaddb.Polyline;
                            if (p != null)
                                p.Closed = true;
                            tr.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        COMS.MessengerManager.LogException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                COMS.MessengerManager.LogException(ex);
            }
        }
    }
}