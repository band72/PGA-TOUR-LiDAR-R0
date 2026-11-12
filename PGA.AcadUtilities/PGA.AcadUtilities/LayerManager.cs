using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using global::Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using global::Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Colors;

namespace PGA.AcadUtilities
{
    /// <summary>
    /// Contains helper methods for interacting with AutoCAD layers
    /// </summary>
    public class LayerManager
    {

        #region Private Members

        //private static readonly ILog //_logger = LogManager.GetLogger(typeof(LayerManager));

        #endregion

        #region CreateLayer

        /// <summary>
        /// Determines whether the specified db is defined.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="layerName">Name of the layer.</param>
        /// <returns>
        ///   <c>true</c> if the specified db is defined; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsDefined(Autodesk.AutoCAD.DatabaseServices.Database db, string layerName)
        {
            bool defined = false;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                defined = IsDefined(db, trans, layerName);
            }
            return defined;
        }

        /// <summary>
        /// Determines whether the specified db is defined.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="trans">The trans.</param>
        /// <param name="layername">The layername.</param>
        /// <returns>
        ///   <c>true</c> if the specified db is defined; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsDefined(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, string layername)
        {
            bool defined = false;
            using (LayerTable layerTable = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite, false))
            {
                if (layerTable.Has(layername))
                {
                    defined = true;
                }
            }
            return defined;
        }

        /// <summary>
        /// Creates the layer.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="layername">The layername.</param>
        /// <returns></returns>
        public static ObjectId CreateLayer(Autodesk.AutoCAD.DatabaseServices.Database db, string layername)// , string linetype, Color color, PlotStyleDescriptor poltStyle, bool plots, bool frewezeInViewPorts)
        {
            if (db == null)
                throw new ArgumentNullException("db", "The argument 'db' was null.");

            if (string.IsNullOrEmpty(layername))
                throw new ArgumentNullException("layername", "The argument 'layername' was null or empty.");

            ObjectId id = ObjectId.Null;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                id = CreateLayer(db, trans, layername);
                trans.Commit();
            }
            return id;
        }

        /// <summary>
        /// Creates the layer.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="layername">The layername.</param>
        /// <param name="color">The color.</param>
        /// <returns></returns>
        public static ObjectId CreateLayer(Autodesk.AutoCAD.DatabaseServices.Database db, string layername, Color color)
        {
            if (db == null)
                throw new ArgumentNullException("db", "The argument 'db' was null.");

            if (string.IsNullOrEmpty(layername))
                throw new ArgumentNullException("layername", "The argument 'layername' was null or empty.");

            if (color == null)
                throw new ArgumentNullException("color", "The argument 'color' was null.");

            ObjectId id = ObjectId.Null;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                id = CreateLayer(db, trans, layername, color);
                trans.Commit();
            }
            return id;
        }

        /// <summary>
        /// Creates the layer.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="trans">The trans.</param>
        /// <param name="layername">The layername.</param>
        /// <returns></returns>
        public static ObjectId CreateLayer(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, string layername) //, ObjectId linetypeId, Color color, ObjectId plotStyleNameId, bool plots, bool frewezeInViewPorts)
        {
            return CreateLayer(db, trans, layername, null, ObjectId.Null);
        }

        /// <summary>
        /// Creates the layer.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="trans">The trans.</param>
        /// <param name="layername">The layername.</param>
        /// <param name="color">The color.</param>
        /// <returns></returns>
        public static ObjectId CreateLayer(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, string layername, Color color) //, ObjectId linetypeId, Color color, ObjectId plotStyleNameId, bool plots, bool frewezeInViewPorts)
        {   
            return CreateLayer(db, trans, layername, color, ObjectId.Null);            
        }

        /// <summary>
        /// Creates the layer.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="trans">The trans.</param>
        /// <param name="layername">The layername.</param>
        /// <param name="color">The color.</param>
        /// <param name="linetypeId">The linetype id.</param>
        /// <returns></returns>
        public static ObjectId CreateLayer(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, string layername, Color color, ObjectId linetypeId)
        {
            if (trans == null)
                throw new ArgumentNullException("trans", "The argument 'trans' was null.");

            if (string.IsNullOrEmpty(layername))
                throw new ArgumentNullException("layername", "The argument 'layername' was null or empty.");

            ObjectId id = ObjectId.Null;

            using (LayerTable layerTable = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForRead, false))
            {
                if (!layerTable.Has(layername))
                {
                    using (LayerTableRecord layerTableRecord = new LayerTableRecord())
                    {
                        layerTable.UpgradeOpen();
                        layerTableRecord.Name = layername;
                        if (color != null)
                            layerTableRecord.Color = color;
                        if (linetypeId != ObjectId.Null)
                            layerTableRecord.LinetypeObjectId = linetypeId;
                        id = layerTable.Add(layerTableRecord);
                        trans.AddNewlyCreatedDBObject(layerTableRecord, true);
                    }
                }
                else
                {
                    id = layerTable[layername];
                }
            }

            return id;
        }

        #endregion

        /// <summary>
        /// Deletes the layer.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="layername">The layername.</param>
        public static void DeleteLayer(Autodesk.AutoCAD.DatabaseServices.Database db, string layername)
        {
            if (db == null)
                throw new ArgumentNullException("db", "The argument 'db' was null.");

            if (string.IsNullOrEmpty(layername))
                throw new ArgumentNullException("layername", "The argument 'layername' was null or empty.");

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                DeleteLayer(db, trans, layername);
                trans.Commit();
            }
        }
        /// <summary>
        /// Deletes the layer.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="trans">The trans.</param>
        /// <param name="layername">The layername.</param>
        /// <returns></returns>
        public static bool DeleteLayer(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, string layername) //, ObjectId linetypeId, Color color, ObjectId plotStyleNameId, bool plots, bool frewezeInViewPorts)
        {
            if (trans == null)
                throw new ArgumentNullException("trans", "The argument 'trans' was null.");

            if (string.IsNullOrEmpty(layername))
                throw new ArgumentNullException("layername", "The argument 'layername' was null or empty.");

            bool erased = false;
            try
            {
                using (LayerTable layerTable = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite, false))
                {
                    if (layerTable.Has(layername))
                    {
                        ObjectId layerId = layerTable[layername];
                        using (LayerTableRecord layerTableRecord = trans.GetObject(layerId, OpenMode.ForWrite, false) as LayerTableRecord)
                        {
                            if (!layerTableRecord.IsErased)
                            {
                                layerTableRecord.Erase();
                                erased = true;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
               PGA.MessengerManager.MessengerManager.AddLog("Error in DeleteLayer", ex);
                throw;
            }

            return erased;
        }
        /// <summary>
        /// Returns the layer's color.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="trans">The trans.</param>
        /// <param name="layername">The layername.</param>
        /// <returns></returns>
        /// <summary>
        /// Deletes the layer.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="layername">The layername.</param>
        /// <returns></returns>
        public static void GetLayerColor(Autodesk.AutoCAD.DatabaseServices.Database db, string layername)
        {
            if (db == null)
                throw new ArgumentNullException("db", "The argument 'db' was null.");

            if (string.IsNullOrEmpty(layername))
                throw new ArgumentNullException("layername", "The argument 'layername' was null or empty.");

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                GetLayerColor(db, trans, layername);
                trans.Commit();
            }
        }
        public static Color GetLayerColor(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, string layername) //, ObjectId linetypeId, Color color, ObjectId plotStyleNameId, bool plots, bool frewezeInViewPorts)
        {
            if (trans == null)
                throw new ArgumentNullException("trans", "The argument 'trans' was null.");

            if (string.IsNullOrEmpty(layername))
                throw new ArgumentNullException("layername", "The argument 'layername' was null or empty.");

            try
            {
                using (LayerTable layerTable = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite, false))
                {
                    if (layerTable.Has(layername))
                    {
                        ObjectId layerId = layerTable[layername];
                        using (LayerTableRecord layerTableRecord = trans.GetObject(layerId, OpenMode.ForWrite, false) as LayerTableRecord)
                        {
                            if (!layerTableRecord.IsErased)
                            {
                                return layerTableRecord.Color;
                            }
                        }
                    }
                }
            }
            catch(System.Exception ex)
            {
               PGA.MessengerManager.MessengerManager.AddLog("Error in DeleteLayer", ex);
                throw;
            }

            return null;
        }

        /// <summary>
        /// Gets the layer id.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="layername">The layername.</param>
        /// <returns>The object id the layer if it exists, otherwise ObjectId.Null</returns>        
        public static ObjectId GetLayerId(Autodesk.AutoCAD.DatabaseServices.Database db, string layername)
        {
            ObjectId layerId = ObjectId.Null;
            using(Transaction trans =  db.TransactionManager.StartTransaction())
            {
                layerId = GetLayerId(db, trans, layername);
                trans.Commit();
            }
            return layerId;
        }

        /// <summary>
        /// Gets the layer id.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="trans">The trans.</param>
        /// <param name="layername">The layername.</param>
        /// <returns>The object id the layer if it exists, otherwise ObjectId.Null</returns>
        public static ObjectId GetLayerId(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, string layername)
        {
            ObjectId layerId = ObjectId.Null;
            using (LayerTable layerTable = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite, false))
            {
                if (layerTable.Has(layername))
                {
                    layerId = layerTable[layername];
                }
            }
            return layerId;
        }

        /// <summary>
        /// Gets the Object Ids of all layers matching the filter
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="trans">The trans.</param>
        /// <param name="layerFilter">The layer filter.</param>
        /// <param name="useWildCard">if set to <c>true</c> [use wild card].</param>
        /// <returns></returns>
        public static ObjectIdCollection GetLayerIds(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, string layerFilter, bool useWildCard)
        {
            if (trans == null)
                throw new ArgumentNullException("trans", "The argument 'trans' was null.");

            ObjectIdCollection layerIds = new ObjectIdCollection();
            ObjectId layerId = ObjectId.Null;

            using (LayerTable layerTable = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForRead, false))
            {
                foreach (ObjectId id in layerTable)
                {
                    using (LayerTableRecord record = trans.GetObject(id, OpenMode.ForRead) as LayerTableRecord)
                    {
                        if (useWildCard == true)
                        {
                            if (record.Name.ToUpper().StartsWith(layerFilter.ToUpper().Replace("*", string.Empty)))
                            {
                                layerIds.Add(record.ObjectId);
                            }
                        }
                        else
                        {
                            if (record.Name.ToUpper() == layerFilter.ToUpper())
                            {
                                layerIds.Add(record.ObjectId);
                            }
                        }
                    }
                }
            }
            return layerIds;
        }

        /// <summary>
        /// Gets a list of layer names.
        /// </summary>
        /// <param name="db">The database to read.</param>
        /// <returns></returns>
        public static IList<string> GetLayerNames(Autodesk.AutoCAD.DatabaseServices.Database db)
        {
            IList<string> layerNames = new List<string>();
            using(Transaction trans =  db.TransactionManager.StartTransaction())
            {
                layerNames = GetLayerNames(db, trans);
                trans.Commit();
            }
            return layerNames;
        }

        /// <summary>
        /// Gets a list of layer names.
        /// </summary>
        /// <param name="db">The database to read.</param>
        /// <param name="trans">The transaction to use.</param>
        /// <returns>A list of layer names</returns>
        public static IList<string> GetLayerNames(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans)
        {
            IList<string> layerNames = new List<string>();
            using (LayerTable layerTable = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite, false))
            {
                foreach (ObjectId id in layerTable)
                {
                    using (LayerTableRecord record = trans.GetObject(id, OpenMode.ForRead) as LayerTableRecord)
                    {
                        layerNames.Add(record.Name);
                    }
                }
            }
            return layerNames;
        }
        /// <summary>
        /// Gets the layer color by layer identifier.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="trans">The trans.</param>
        /// <param name="layerid">The layerid.</param>
        /// <returns>Color.</returns>
        /// <exception cref="System.ArgumentNullException">trans;The argument 'trans' was null.</exception>
        public static Color GetLayerColorByLayerId(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, string layername)
        {
            if (trans == null)
                throw new ArgumentNullException("trans", "The argument 'trans' was null.");

            IList<string> layerNames = new List<string>();

            using (LayerTable layerTable = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForRead, false))
            {
                foreach (ObjectId id in layerTable)
                {

                    using (LayerTableRecord record = trans.GetObject(id, OpenMode.ForRead) as LayerTableRecord)
                    {
                        if (record.Name.ToUpper() == layername.ToUpper())
                            return record.Color;
                    }

                }
            }
            return null;
        }
        /// <summary>
        /// Gets a list of layer names.
        /// </summary>
        /// <param name="db">The database to read.</param>
        /// <param name="trans">The transaction to use.</param>
        /// <returns>A list of layer names</returns>
        public static IList<string> GetLayerNames(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, string filter)
        {
            if (trans == null)
                throw new ArgumentNullException("trans", "The argument 'trans' was null.");

            IList<string> layerNames = new List<string>();

            using (LayerTable layerTable = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForRead, false))
            {
                foreach (ObjectId id in layerTable)
                {
                    using (LayerTableRecord record = trans.GetObject(id, OpenMode.ForRead) as LayerTableRecord)
                    {
                        if (record.Name.ToUpper().StartsWith(filter.ToUpper().Replace("*", string.Empty)))
                        {
                            layerNames.Add(record.Name);
                        }
                    }
                }
            }
            return layerNames;
        }

        #region Lock

        /// <summary>
        /// Gets a list of layer names.
        /// </summary>
        /// <param name="db">The database to read.</param>
        /// <returns></returns>
        public static bool IsLocked(Autodesk.AutoCAD.DatabaseServices.Database db, string layerName)
        {
            bool locked = false;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                locked = IsLocked(db, trans, layerName);
                trans.Commit();
            }
            return locked;
        }

        /// <summary>
        /// Gets a list of layer names.
        /// </summary>
        /// <param name="db">The database to read.</param>
        /// <returns></returns>
        public static bool IsLocked(Autodesk.AutoCAD.DatabaseServices.Database db, ObjectId layerId)
        {
            bool locked = false;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                locked = IsLocked(db, trans, layerId);
                trans.Commit();
            }
            return locked;
        }

        /// <summary>
        /// Gets a list of layer names.
        /// </summary>
        /// <param name="db">The database to read.</param>
        /// <param name="trans">The transaction to use.</param>
        /// <returns>A list of layer names</returns>
        public static bool IsLocked(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, string layername)
        {
            bool locked = false;
            using (LayerTable layerTable = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite, false))
            {
                ObjectId id = layerTable[layername];
                locked = IsLocked(db, trans, id);
            }
            return locked;
        }

        /// <summary>
        /// Gets a list of layer names.
        /// </summary>
        /// <param name="db">The database to read.</param>
        /// <param name="trans">The transaction to use.</param>
        /// <returns>A list of layer names</returns>
        public static bool IsLocked(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, ObjectId layerId)
        {
            bool locked = false;             
            using (LayerTableRecord layerTableRecord = trans.GetObject(layerId, OpenMode.ForRead, false) as LayerTableRecord)
            {
                locked = layerTableRecord.IsLocked;
            }
            return locked;
        }

        /// <summary>
        /// Locks the layer.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="layerName">Name of the layer.</param>
        /// <param name="isLocked">if set to <c>true</c> [is locked].</param>
        /// <returns></returns>
        public static void LockLayer(Autodesk.AutoCAD.DatabaseServices.Database db, string layerName, bool isLocked)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LockLayer(db, trans, layerName, isLocked);
                trans.Commit();
            }
        }

        /// <summary>
        /// Locks the layer.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="trans">The trans.</param>
        /// <param name="layerName">Name of the layer.</param>
        /// <param name="isLocked">if set to <c>true</c> [is locked].</param>
        /// <returns></returns>
        public static void LockLayer(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, string layername, bool isLocked)
        {
            using (LayerTable layerTable = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite, false))
            {
                if (layerTable.Has(layername) == true)
                {
                    ObjectId id = layerTable[layername];

                    using (LayerTableRecord record = trans.GetObject(id, OpenMode.ForWrite) as LayerTableRecord)
                    {
                        record.IsLocked = isLocked;
                    }
                }
            }
        }

        /// <summary>
        /// Locks all layers.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="isLocked">if set to <c>true</c> [is locked].</param>
        public static void LockAllLayers(Autodesk.AutoCAD.DatabaseServices.Database db, bool isLocked)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LockAllLayers(db, trans, isLocked);
                trans.Commit();
            }
        }

        /// <summary>
        /// Locks all layers.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="trans">The trans.</param>
        /// <param name="isLocked">if set to <c>true</c> [is locked].</param>
        public static void LockAllLayers(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, bool isLocked)
        {
            using (LayerTable layerTable = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite, false))
            {
                foreach (ObjectId id in layerTable)
                {
                    using (LayerTableRecord record = trans.GetObject(id, OpenMode.ForWrite) as LayerTableRecord)
                    {
                        record.IsLocked = isLocked;
                    }
                }
            }
        }

        #endregion

        #region Freeze

        /// <summary>
        /// Freezes the layer.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="layerName">Name of the layer.</param>
        /// <param name="isFrozen">if set to <c>true</c> [is frozen].</param>
        public static void FreezeLayer(Autodesk.AutoCAD.DatabaseServices.Database db, string layerName, bool isFrozen)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                FreezeLayer(db, trans, layerName, isFrozen);
                trans.Commit();
            }
        }

        /// <summary>
        /// Freezes the layer.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="trans">The trans.</param>
        /// <param name="layerName">Name of the layer.</param>
        /// <param name="isFrozen">if set to <c>true</c> [is frozen].</param>
        public static void FreezeLayer(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, string layerName, bool isFrozen)
        {
            using (LayerTable layerTable = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite, false))
            {

                string cLayerName = String.Empty;
                ObjectId cLayerId = db.Clayer;
                using (LayerTableRecord record = trans.GetObject(cLayerId, OpenMode.ForRead) as LayerTableRecord)
                {
                    cLayerName = record.Name.ToUpper();
                }
                if (layerName.ToUpper() != cLayerName)
                {
                    if (layerTable.Has(layerName) == true)
                    {
                        ObjectId id = layerTable[layerName];

                        using (LayerTableRecord record = trans.GetObject(id, OpenMode.ForWrite) as LayerTableRecord)
                        {
                            record.IsFrozen = isFrozen;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Freezes all layers.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="isFrozen">if set to <c>true</c> [is frozen].</param>
        public static void FreezeAllLayers(Autodesk.AutoCAD.DatabaseServices.Database db, bool isFrozen)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                FreezeAllLayers(db, trans, isFrozen);
                trans.Commit();
            }
        }

        /// <summary>
        /// Freezes all layers.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="trans">The trans.</param>
        /// <param name="isFrozen">if set to <c>true</c> [is frozen].</param>
        public static void FreezeAllLayers(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, bool isFrozen)
        {
            using (LayerTable layerTable = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite, false))
            {
                string cLayerName = String.Empty;
                ObjectId cLayerId = db.Clayer;
                using (LayerTableRecord record = trans.GetObject(cLayerId, OpenMode.ForRead) as LayerTableRecord)
                {
                    cLayerName = record.Name.ToUpper();
                }
                foreach (ObjectId id in layerTable)
                {
                    using (LayerTableRecord record = trans.GetObject(id, OpenMode.ForWrite) as LayerTableRecord)
                    {
                        if (record.Name.ToUpper() != cLayerName)
                        {
                            record.IsFrozen = isFrozen;
                        }
                    }
                }
            }
        }

        #endregion

        #region Off

        /// <summary>
        /// Determines whether the specified db is off.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="layerName">Name of the layer.</param>
        /// <returns>
        ///   <c>true</c> if the specified db is off; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsOff(Autodesk.AutoCAD.DatabaseServices.Database db, string layerName)
        {
            bool off = false;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                off = IsOff(db, trans, layerName);
                trans.Commit();
            }
            return off;
        }

        /// <summary>
        /// Determines whether the specified db is off.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="layerId">The layer id.</param>
        /// <returns>
        ///   <c>true</c> if the specified db is off; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsOff(Autodesk.AutoCAD.DatabaseServices.Database db, ObjectId layerId)
        {
            bool off = false;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                off = IsOff(db, trans, layerId);
                trans.Commit();
            }
            return off;
        }

        /// <summary>
        /// Determines whether the specified db is off.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="trans">The trans.</param>
        /// <param name="layername">The layername.</param>
        /// <returns>
        ///   <c>true</c> if the specified db is off; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsOff(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, string layername)
        {
            bool off = false;
            using (LayerTable layerTable = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite, false))
            {
                ObjectId id = layerTable[layername];
                off = IsOff(db, trans, id);
            }
            return off;
        }

        /// <summary>
        /// Determines whether the specified db is off.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="trans">The trans.</param>
        /// <param name="layerId">The layer id.</param>
        /// <returns>
        ///   <c>true</c> if the specified db is off; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsOff(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, ObjectId layerId)
        {
            bool off = false;
            using (LayerTableRecord layerTableRecord = trans.GetObject(layerId, OpenMode.ForRead, false) as LayerTableRecord)
            {
                off = layerTableRecord.IsOff;
            }
            return off;
        }

        /// <summary>
        /// Offs the layer.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="layerName">Name of the layer.</param>
        /// <param name="isOff">if set to <c>true</c> [is off].</param>
        public static void OffLayer(Autodesk.AutoCAD.DatabaseServices.Database db, string layerName, bool isOff)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                OffLayer(db, trans, layerName, isOff);
                trans.Commit();
            }
        }

        /// <summary>
        /// Offs the layer.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="trans">The trans.</param>
        /// <param name="layerName">Name of the layer.</param>
        /// <param name="isOff">if set to <c>true</c> [is off].</param>
        public static void OffLayer(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, string layerName, bool isOff)
        {
            using (LayerTable layerTable = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite, false))
            {
                if (layerTable.Has(layerName) == true)
                {
                    ObjectId id = layerTable[layerName];

                    using (LayerTableRecord record = trans.GetObject(id, OpenMode.ForWrite) as LayerTableRecord)
                    {
                        record.IsOff = isOff;
                    }
                }
            }
        }

        /// <summary>
        /// Offs all layers.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="isOff">if set to <c>true</c> [is off].</param>
        public static void OffAllLayers(Autodesk.AutoCAD.DatabaseServices.Database db, bool isOff)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                OffAllLayers(db, trans, isOff);
                trans.Commit();
            }
        }

        /// <summary>
        /// Offs all layers.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="trans">The trans.</param>
        /// <param name="isOff">if set to <c>true</c> [is off].</param>
        public static void OffAllLayers(Autodesk.AutoCAD.DatabaseServices.Database db, Transaction trans, bool isOff)
        {
            using (LayerTable layerTable = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite, false))
            {
                foreach (ObjectId id in layerTable)
                {
                    using (LayerTableRecord record = trans.GetObject(id, OpenMode.ForWrite) as LayerTableRecord)
                    {
                        record.IsOff = isOff;
                    }
                }
            }
        }

        #endregion

    }
}

