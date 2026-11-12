using Microsoft.VisualStudio.TestTools.UnitTesting;
using PGA.Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PGA.DataContext;

namespace PGA.Database.Tests
{
    [TestClass()]
    public class DatabaseCommandsTests
    {
        [TestMethod()]
        public void LoadandProcessPolysTest()
        {
            DatabaseCommands commands = new DatabaseCommands();
            var test = commands.GetTasksProjectName();
            //var tasks = (from p in context.TaskManager



            Assert.AreEqual(1,1);
        }

        [TestMethod()]
        public void TestDatabaseMethod()
        {
                DatabaseCommands commands = new DatabaseCommands();
                IList<ExcludedFeatures> excludedfeatures;
                excludedfeatures = commands.GetAllExcludedFeatures_V2();
                {
                    foreach (var str in excludedfeatures)
                    {
                        Debug.WriteLine(str.Handle.Trim());
                    }
                }
        }

        [TestMethod()]
        public void TestDatabaseMethodSkipDXF()
        {
             
            DatabaseCommands commands = new DatabaseCommands();
            IList<ExcludedFeatures> excludedfeatures;
            var datetime = (from p in commands.GetSettingsCustom()
                select p.DateStamp).Distinct().FirstOrDefault();
            var resultdxf = commands.IsSkipDXFByDate(date:Convert.ToDateTime(datetime));
            
        }
    }
}