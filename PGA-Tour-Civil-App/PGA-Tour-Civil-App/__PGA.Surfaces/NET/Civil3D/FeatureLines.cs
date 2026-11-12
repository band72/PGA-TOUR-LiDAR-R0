using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Civil3D = Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices;
namespace PGA.Surfaces.Civil3D
{
    class FeatureLines
    {

        //  Try

        //    If oAcadApp Is Nothing Then

        //      oAcadApp = GetObject(, "AutoCAD.Application")

        //    End If

        //  Catch ex As Exception

        //    ed.WriteMessage(ex.Message)

        //  End Try

        //  Try

        //    oAeccApp = oAcadApp.GetInterfaceObject("AeccXUiLand.AeccApplication.10.0")

        //    oAeccDoc = oAeccApp.ActiveDocument

        //    oAeccDB = oAeccApp.ActiveDocument.Database

        ////    ' Select the 3D Polyline which you want to convert to Feature Line

        ////    Dim promptEntOp As New PromptEntityOptions(vbCrLf + "Select a 3D Polyline : ")

        ////    Dim promptEntRs As PromptEntityResult

        ////    promptEntRs = ed.GetEntity(promptEntOp)

        ////    If promptEntRs.Status <> PromptStatus.OK Then

        ////      ed.WriteMessage("Exiting! Try Again !")

        ////      Exit Sub

        ////    End If

        ////    Dim idEnt As ObjectId

        ////    idEnt = promptEntRs.ObjectId

        ////    Dim oFtrLn As AeccLandFeatureLine = Nothing

        ////    Dim oFtrLns As AeccLandFeatureLines = oAeccDoc.Sites.Item(0).FeatureLines

        ////    Dim plineObjId As Long = idEnt.OldIdPtr

        ////    oFtrLn = oFtrLns.AddFromPolyline(plineObjId, oAeccDB.FeatureLineStyles.Item(0))

        ////    trans.Commit()

        ////  Catch ex As Exception

        ////    ed.WriteMessage("Error : ", ex.Message & vbCrLf)

        ////  End Try

        ////End Using

    }
}