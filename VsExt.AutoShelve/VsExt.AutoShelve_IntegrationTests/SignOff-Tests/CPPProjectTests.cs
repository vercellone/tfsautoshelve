using System;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VsSDK.IntegrationTestLibrary;
using Microsoft.VSSDK.Tools.VsIdeTesting;

namespace VsExt.AutoShelve_IntegrationTests
{
    [TestClass]
    public class CppProjectTests
    {
        #region fields

        private delegate void ThreadInvoker();

        #endregion

        #region properties

        /// <summary>
        ///     Gets or sets the test context which provides
        ///     information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        #endregion

        #region ctors

        #endregion

        #region Additional test attributes

        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //

        #endregion

        [HostType("VS IDE")]
        [TestMethod]
        public void CppWinformsApplication()
        {
            UIThreadInvoker.Invoke((ThreadInvoker) delegate
            {
                //Solution and project creation parameters
                const string solutionName = "CPPWinApp";
                const string projectName = "CPPWinApp";

                //Template parameters
                const string projectType = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}";
                var projectTemplateName = Path.Combine("vcNet", "mc++appwiz.vsz");

                const string itemTemplateName = "newc++file.cpp";
                const string newFileName = "Test.cpp";

                var dte = (DTE) VsIdeTestHostContext.ServiceProvider.GetService(typeof (DTE));

                var testUtils = new TestUtils();

                testUtils.CreateEmptySolution(TestContext.TestDir, solutionName);
                Assert.AreEqual(0, testUtils.ProjectCount());

                //Add new CPP Windows application project to existing solution
                var solutionDirectory = Directory.GetParent(dte.Solution.FullName).FullName;
                var projectDirectory = TestUtils.GetNewDirectoryName(solutionDirectory, projectName);
                var projectTemplatePath = Path.Combine(dte.Solution.TemplatePath[projectType],
                    projectTemplateName);
                Assert.IsTrue(File.Exists(projectTemplatePath),
                    string.Format("Could not find template file: {0}", projectTemplatePath));
                dte.Solution.AddFromTemplate(projectTemplatePath, projectDirectory, projectName);

                //Verify that the new project has been added to the solution
                Assert.AreEqual(1, testUtils.ProjectCount());

                //Get the project
                Project project = dte.Solution.Item(1);
                Assert.IsNotNull(project);
                Assert.IsTrue(string.Compare(project.Name, projectName, StringComparison.InvariantCultureIgnoreCase) ==
                              0);

                //Verify Adding new code file to project
                string newItemTemplatePath = Path.Combine(dte.Solution.ProjectItemsTemplatePath(projectType),
                    itemTemplateName);
                Assert.IsTrue(File.Exists(newItemTemplatePath));
                ProjectItem item = project.ProjectItems.AddFromTemplate(newItemTemplatePath, newFileName);
                Assert.IsNotNull(item);
            });
        }
    }
}