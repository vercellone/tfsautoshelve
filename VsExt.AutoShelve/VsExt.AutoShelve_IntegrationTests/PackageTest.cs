using System; 
using System.Windows.Threading; 
using Microsoft.VisualStudio.Shell; 
using Microsoft.VisualStudio.Shell.Interop; 
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VsExt.AutoShelve_IntegrationTests
{
    /// <summary>
    /// Integration test for package validation
    /// </summary>
    [TestClass]
    public class PackageTest {
        private delegate void ThreadInvoker();

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        protected static IServiceProvider ServiceProvider { get; private set; }
        protected static Dispatcher UIThreadDispatcher { get; private set; }

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context) {
            ThreadHelper.Generic.Invoke(delegate
            {
                UIThreadDispatcher = Dispatcher.CurrentDispatcher;
            });
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void PackageLoadTest() {
            //Get the Shell Service
            var serviceProvider = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider;

            var shellService = (IVsShell)serviceProvider.GetService(typeof(SVsShell));
            Assert.IsNotNull(shellService);

            //Validate package load
            IVsPackage package;
            var packageGuid = new Guid(AutoShelve.Packaging.GuidList.GuidAutoShelvePkgString);
            Assert.IsTrue(0 == shellService.LoadPackage(ref packageGuid, out package));
            Assert.IsNotNull(package, "Package failed to load");
        }
    }
}
