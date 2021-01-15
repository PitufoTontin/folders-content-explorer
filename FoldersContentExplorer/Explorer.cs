using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FoldersContentExplorer
{
    public class Explorer
    {
        private string[] paths;
        private List<Action> actions;
        private StreamWriter explorerLog;
        private DirectoryInfo outputDirectory;
        private List<string> outputFileNames;

        public Explorer(string[] paths)
        {
            actions = new List<Action>();
            outputFileNames = new List<string>();
            this.paths = paths;

            outputDirectory = Directory.CreateDirectory(Guid.NewGuid().ToString());

            explorerLog = new StreamWriter($@"{outputDirectory.FullName}\explorer.log");
        }

        public void Explore()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

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

            explorerLog.WriteLine($"{DateTime.Now} Actions created. Time elapsed: {stopwatch.Elapsed.TotalMinutes} minutes");
            Console.WriteLine($"{DateTime.Now} Actions created.");

            Console.WriteLine($"{DateTime.Now} Invoking actions.");
            explorerLog.WriteLine($"{DateTime.Now} Invoking actions.");

            try
            {
                Parallel.Invoke(actions.ToArray());
            }
           catch(Exception exception)
            {
                explorerLog.WriteLine($"{DateTime.Now} There was an error invoking actions: {exception.Message}.");
            }

            UnifyOutputFiles();

            Console.WriteLine($"{DateTime.Now} Exploration finished.");
            explorerLog.WriteLine($"{DateTime.Now} Exploration finished. Time elapsed: {stopwatch.Elapsed.TotalMinutes} minutes");

            explorerLog.Close();
            stopwatch.Stop();
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

            var outputFileName = GetOutputFileName();
            actions.Add(() => ExploreContent(directoryInfo, outputFileName));

            //TODO PLinq
            foreach (DirectoryInfo subDirectory in subDirectories)
            {
                CreateActions(subDirectory.FullName);
            }
        }

        private void ExploreContent(DirectoryInfo directoryInfo, string outputFileName)
        {
            var extensions = new List<string>();

            using var outputStream = new StreamWriter($@"{outputDirectory.FullName}\{outputFileName}.tmp");

            outputStream.WriteLine("{");
            outputStream.WriteLine($"\t\"Folder\": \"{directoryInfo.FullName}\"");
            outputStream.WriteLine($"\t\"Parent\": \"{directoryInfo.Parent?.FullName}\"");
            outputStream.WriteLine($"\t\"NumberOfFiles\": {directoryInfo.GetFiles().Length}");
            outputStream.WriteLine($"\t\"NumberOfSubfolders\": {directoryInfo.GetDirectories().Length}");

            //TODO PLinq
            var filesInfo = new List<string>();
            double folderSize = 0;

            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                folderSize += file.Length;
                if(extensions.IndexOf($"\"{file.Extension.ToLower().Trim()}\"") < 0)
                {
                    extensions.Add($"\"{file.Extension.ToLower().Trim()}\"");
                }

                filesInfo.Add($"\t\t{{\"Filename\": \"{file.Name}\", \"Extension\": \"{file.Extension}\", \"Size\": {file.Length}}}");
            }

            outputStream.WriteLine($"\t\"Size\": {folderSize}");
            outputStream.WriteLine($"\t\"Extensions\": [{string.Join(", ", extensions)}]");
            outputStream.WriteLine("\t\"Files\": \n\t[");

            if(filesInfo.Count > 0)
            {
                outputStream.WriteLine(string.Join(", \n", filesInfo));
            }
           
            outputStream.WriteLine("\t]");
            outputStream.WriteLine("},");

            outputStream.Close();
        }

        public string GetOutputFileName()
        {
            var outputFileName = string.Empty;

            while(string.IsNullOrWhiteSpace(outputFileName) || outputFileNames.IndexOf(outputFileName) >= 0)
            {
                outputFileName = Guid.NewGuid().ToString();
            }

            outputFileNames.Add(outputFileName);

            return outputFileName;
        }

        private void UnifyOutputFiles()
        {
            using var finalOutputStream = new StreamWriter($@"{outputDirectory.FullName}\explorer.json");
            finalOutputStream.WriteLine("[");

            foreach (string outputFileName in outputFileNames)
            {
                using var fileReadStream = new StreamReader($@"{outputDirectory.FullName}\{outputFileName}.tmp");

                finalOutputStream.WriteLine(fileReadStream.ReadToEnd());

                fileReadStream.Close();

                File.Delete($@"{outputDirectory.FullName}\{outputFileName}.tmp");
            }

            finalOutputStream.WriteLine("]");
            finalOutputStream.Close();
        }
    }
}
