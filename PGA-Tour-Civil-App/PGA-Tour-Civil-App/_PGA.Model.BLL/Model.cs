#region

using System;
using PGA.Autodesk.Settings;
using PGA.Common.Registry;

#endregion

namespace PGA.Model.BLL

{
    public class Model : IModel
    {
        public void WriteDataToRegistry(string Key, string Val)
        {
            if (Key == null) throw new ArgumentNullException("Key");
            try
            {
                RegistryFunctions.GlobalWriteToRegistry(Key, Val,
                    AcadSettings.RegistryPath);
            }
            catch (Exception ex)
            {
                MessengerManager.MessengerManager.AddLog(ex.Message);
            }
        }

        public LayerStatesBLL GetDataFromRegistry()
        {
            try
            {
                var states = new LayerStatesBLL();

                CleanDataFromRegistry("Bridge", AcadSettings.RegistryPath, states.Bridge);
                CleanDataFromRegistry("Building", AcadSettings.RegistryPath, states.Building);
                CleanDataFromRegistry("Bunker", AcadSettings.RegistryPath, states.Bunker);
                CleanDataFromRegistry("BushOutline", AcadSettings.RegistryPath, states.BushOutline);
                CleanDataFromRegistry("CartPath", AcadSettings.RegistryPath, states.CartPath);
                CleanDataFromRegistry("Collar", AcadSettings.RegistryPath, states.Collar);
                CleanDataFromRegistry("DirtOutline", AcadSettings.RegistryPath, states.DirtOutline);
                CleanDataFromRegistry("Fairway", AcadSettings.RegistryPath, states.Fairway);
                CleanDataFromRegistry("Green", AcadSettings.RegistryPath, states.Green);
                CleanDataFromRegistry("GreenSideBunker", AcadSettings.RegistryPath, states.GreenSideBunker);
                CleanDataFromRegistry("IntMedRough", AcadSettings.RegistryPath, states.IntMedRough);
                CleanDataFromRegistry("LandScaping", AcadSettings.RegistryPath, states.LandScaping);
                CleanDataFromRegistry("NativeArea", AcadSettings.RegistryPath, states.NativeArea);
                CleanDataFromRegistry("Other", AcadSettings.RegistryPath, states.Other);
                CleanDataFromRegistry("Path", AcadSettings.RegistryPath, states.Path);
                CleanDataFromRegistry("RockOutline", AcadSettings.RegistryPath, states.RockOutline);
                CleanDataFromRegistry("RoughOutline", AcadSettings.RegistryPath, states.RoughOutline);
                CleanDataFromRegistry("Steps", AcadSettings.RegistryPath, states.Steps);
                CleanDataFromRegistry("TeeBox", AcadSettings.RegistryPath, states.TeeBox);
                CleanDataFromRegistry("TreeOutline", AcadSettings.RegistryPath, states.TreeOutline);
                CleanDataFromRegistry("WalkStrip", AcadSettings.RegistryPath, states.WalkStrip);
                CleanDataFromRegistry("Wall", AcadSettings.RegistryPath, states.Wall);
                CleanDataFromRegistry("Water", AcadSettings.RegistryPath, states.Water);
                CleanDataFromRegistry("WaterDrop", AcadSettings.RegistryPath, states.WaterDrop);

                //if(!String.IsNullOrEmpty(value)) {states.Bridge = value;}

                //if (!String.IsNullOrEmpty(value)) { states.Building = value; }

                //if (!String.IsNullOrEmpty(value)) { states.WaterDrop = value; }

                return states; //only loop once
            }
            catch (Exception ex)
            {
                MessengerManager.MessengerManager.AddLog(ex.Message);
            }
            return null;
        }

        public void GetDataFromRegistry(string Key)
        {
            if (Key == null) throw new ArgumentNullException("Key");
            try
            {
                RegistryFunctions.GlobalReadFromRegistry(Key,
                    AcadSettings.RegistryPath);
            }
            catch (Exception ex)
            {
                MessengerManager.MessengerManager.AddLog(ex.Message);
            }
        }

        public LayerStatesBLL GetPathDataFromRegistry()
        {
            // TODO: Implement this method
            return null;
        }

        public string CleanDataFromRegistry(string name, string Path, string states)
        {
            var value = "";
            value = RegistryFunctions.GlobalReadFromRegistry(name, Path);
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
            return states;
        }

        public void AssignStates()
        {
        }
    }
}