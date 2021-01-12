using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoldersContentExplorer
{
    public class Explorer
    {
        private string[] paths;
        private List<Action> actions;
        private StreamWriter explorerLog;
        private DirectoryInfo outputDirectory;

        public Explorer(string[] paths)
        {
            actions = new List<Action>();
            this.paths = paths;

            outputDirectory = Directory.CreateDirectory(Guid.NewGuid().ToString());

            explorerLog = new StreamWriter($@"{outputDirectory.FullName}\explorer.log");
        }

        public void Explore()
        {
            explorerLog.WriteLine(DateTime.Now.ToString() + " Begin exploration.");
            Console.WriteLine(DateTime.Now.ToString() + " Begin exploration.");

            try
            {
                CheckAndCreateActions();
            }
            catch (Exception exception)
            {
                explorerLog.WriteLine($"{DateTime.Now} There was an error creating actions: {exception.Message}.");
            }

            explorerLog.WriteLine($"{DateTime.Now} Actions created.");
            Console.WriteLine($"{DateTime.Now} Actions created.");

            Console.WriteLine($"{DateTime.Now} Invoking actions.");
            explorerLog.WriteLine($"{DateTime.Now} Invoking actions.");

            Parallel.Invoke(actions.ToArray());

            Console.WriteLine($"{DateTime.Now} Exploration finished.");
            explorerLog.WriteLine($"{DateTime.Now} Exploration finished.");

            explorerLog.Close();
        }

        private void CheckAndCreateActions()
        {
            foreach (string path in paths)
            {
                if (!Directory.Exists(path))
                {
                    explorerLog.WriteLine($"{DateTime.Now} Path does not exists: {path}");
                    Console.WriteLine($"{DateTime.Now} Path does not exists: {path}");

                    continue;
                }

                CreateActions(path);
            }
        }

        private void CreateActions(string path)
        {
            explorerLog.WriteLine($"{DateTime.Now} Path to explore: {path}");

            var directoryInfo = new DirectoryInfo(path);
            DirectoryInfo[] subDirectories = Array.Empty<DirectoryInfo>();

            try
            {
                subDirectories = directoryInfo.GetDirectories();
            }
            catch (UnauthorizedAccessException)
            {
                explorerLog.WriteLine($"{DateTime.Now} Unauthorized access to {path}");
                return;
            }

            actions.Add(() => ExploreContent(directoryInfo, Guid.NewGuid().ToString()));

            foreach (DirectoryInfo subDirectory in subDirectories)
            {
                CreateActions(subDirectory.FullName);
            }
        }

        private void ExploreContent(DirectoryInfo directoryInfo, string outputFileName)
        {
            using var outputStream = new StreamWriter($@"{outputDirectory.FullName}\{outputFileName}.tmp");
            outputStream.WriteLine($"{DateTime.Now} {directoryInfo.FullName} contains {directoryInfo.GetFiles().Length} file/s");

            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                outputStream.WriteLine($"{DateTime.Now} {file.FullName}");
            }

            outputStream.Close();
        }
    }
}
