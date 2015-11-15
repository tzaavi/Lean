using System.IO;
using System.Reflection;
using Nancy;

namespace QuantConnect.Optimization.Engine.Web
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override IRootPathProvider RootPathProvider
        {
            get { return new CustomRootPathProvider(); }
        }
    }

    public class CustomRootPathProvider : IRootPathProvider
    {
        public string GetRootPath()
        {
            var assembly = Assembly.GetEntryAssembly();

            var assemblyDir = Path.GetDirectoryName(assembly.Location);

            var appDir = Directory.GetParent(assemblyDir).Parent.FullName;

            return Path.Combine(appDir, "Web");
        }
    }
}