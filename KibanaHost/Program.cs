using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Principal;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Owin.Hosting;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KibanaHost
{
    class Program
    {
        private const string kibanaFilename = "kibana-4.0.0-beta3";
        private const string kibanaUri = "https://download.elasticsearch.org/kibana/kibana/{0}.zip";

        static void Main(string[] args)
        {
            if (string.IsNullOrWhiteSpace(Bootstrapper.ZipFilePath) || !File.Exists(Bootstrapper.ZipFilePath))
            {
                var url = String.Format(kibanaUri, kibanaFilename);
                var wc = new WebClient();
                var task = wc.DownloadFileTaskAsync(url, kibanaFilename + ".zip.tmp");
                Console.WriteLine("Downloading Kibana from {0}...", url);
                Console.WriteLine();
                wc.DownloadProgressChanged += (sender, eventArgs) =>
                {
                    Console.SetCursorPosition(0, Math.Max(Console.CursorTop - 1, 0));
                    Console.WriteLine("Downloaded {0} / {1} ({2}% Completed)", eventArgs.BytesReceived,
                        eventArgs.TotalBytesToReceive, eventArgs.ProgressPercentage);
                };
                task.Wait();

                // Extract the kibana jar containing all the statics
                using (var fs = new FileStream(kibanaFilename + ".zip.tmp", FileMode.Open, FileAccess.Read))
                using (var zf = new ZipFile(fs))
                {
                    ExtractFileFromOpenZipFile(zf, kibanaFilename + "/lib/kibana.jar", "kibana-statics.zip");
                    ExtractFileFromOpenZipFile(zf, kibanaFilename + "/config/kibana.yml", "kibana.yml");
                }

                File.Delete(kibanaFilename + ".zip.tmp");

                Console.WriteLine("Kibana downloaded successfully");
                Console.WriteLine();
            }
            StartKibanaHost();
        }

        private static void ExtractFileFromOpenZipFile(ZipFile zf, string zipFilePath, string extractTo)
        {
            var ze = zf.GetEntry(zipFilePath);
            if (ze == null)
            {
                throw new ArgumentException(zipFilePath + " wasn't found");
            }

            using (var s = zf.GetInputStream(ze))
            {
                using (var streamWriter = File.Create(extractTo))
                {
                    var buffer = new byte[4096];
                    StreamUtils.Copy(s, streamWriter, buffer);
                }
            }
        }

        private static void StartKibanaHost()
        {
            if (string.IsNullOrWhiteSpace(Bootstrapper.ZipFilePath) || !File.Exists(Bootstrapper.ZipFilePath))
            {
                Console.WriteLine("Unable to find Kibana, quitting");
                return;
            }

            if (File.Exists("kibana.yml"))
            {
                var configText = File.ReadAllText("kibana.yml");  // yes, I'm lazy
                configText = configText.Replace("bundledPluginIds:", "bundled_plugin_ids:"); // yes, this is hacky
                var deserializer = new Deserializer(namingConvention: new UnderscoredNamingConvention());
                Config.Instance = deserializer.Deserialize<Config>(new StringReader(configText));
            }

            string uri;
            if (Config.Instance.Host.Equals("0.0.0.0"))
                uri = string.Format("http://localhost:{0}/", Config.Instance.Port);
            else
                uri = string.Format("http://{0}:{1}/", Config.Instance.Host, Config.Instance.Port);

            var defaultColor = Console.ForegroundColor;
            bool aclTried = false;
            run_server:
            try
            {
                using (WebApp.Start<Startup>(uri))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Kibana is now available on " + uri);
                    Console.ForegroundColor = defaultColor;
                    Console.WriteLine("Press any key to close the host.");

                    Console.ReadLine();
                }
            }
            catch (HttpListenerException e)
            {
                if (!aclTried && e.Message.Equals("Access is denied", StringComparison.InvariantCultureIgnoreCase))
                {
                    aclTried = true;
                    Console.WriteLine("Trying to add {0} to the ACL...", uri);
                    if (TryAddUrlReservations(uri))
                        goto run_server;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed adding URL to the ACL");
                    Console.ForegroundColor = defaultColor;
                }
                throw;
            }
        }

        private static bool TryAddUrlReservations(string uri)
        {
            var user = WindowsIdentity.GetCurrent().Name;

            if (!AddUrlAcl(uri.Replace("localhost", "+"), user))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Add a url reservation
        /// </summary>
        /// <param name="url">Url to add</param>
        /// <param name="user">User to add the reservation for</param>
        /// <returns>True if successful, false otherwise.</returns>
        private static bool AddUrlAcl(string url, string user)
        {
            try
            {
                var args = string.Format("http add urlacl url={0} user={1}", url, user);

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        Verb = "runas",
                        Arguments = args,
                        FileName = "netsh",
                    }
                };

                process.Start();
                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
