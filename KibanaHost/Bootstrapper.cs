using System;
using System.IO;
using System.Linq;
using Nancy;
using Nancy.Conventions;
using Nancy.Responses;

namespace KibanaDotNet.KibanaHost
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        // The bootstrapper enables you to reconfigure the composition of the framework,
        // by overriding the various methods and properties.
        // For more information https://github.com/NancyFx/Nancy/wiki/Bootstrapper

        protected override void ConfigureConventions(NancyConventions conventions)
        {
            base.ConfigureConventions(conventions);

            conventions.StaticContentsConventions.Clear();
            conventions.StaticContentsConventions.Add((ctx, root) =>
            {
                var reqPath = ctx.Request.Path;

                if (reqPath.Equals("/"))
                {
                    reqPath = "/index.html";
                }

                reqPath = KibanaFileName + reqPath.Replace('\\', '/');

                var fileName = Path.GetFullPath(Path.Combine(root, reqPath));
                if (File.Exists(fileName))
                {
                    return new GenericFileResponse(fileName, ctx);
                }

                return new SpecialEmbeddedFileResponse(
                    GetType().Assembly,
                    ZipFilePath,
                    reqPath,
                    ctx.Request.Headers);
            });
        }

        public static string ZipFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_zipFilePath))
                {
                    var fullZipPath = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "kibana-*.zip").FirstOrDefault();
                    if ((fullZipPath == null || File.Exists(fullZipPath) == false) && Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin")))
                        fullZipPath = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin"), "kibana-*.zip").FirstOrDefault();
                    if (fullZipPath != null)
                    {
                        KibanaFileName = new FileInfo(fullZipPath).Name.Replace(".zip", "");
                    }
                    _zipFilePath = fullZipPath;
                }
                return _zipFilePath;
            }
        }

        private static string _zipFilePath;
        private static string KibanaFileName { get; set; }
    }
}
