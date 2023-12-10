using Python.Runtime;
using System.IO;
using System.Xml.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Serilog;
using Serilog.Events;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace ChatTheDoc.Server.AIService
{
    public class StatusUpdateEventArgs : EventArgs
    {
        public object Data { get; }
        public object User { get; }
        public StatusUpdateEventArgs(object user, object data)
        {
            Data = data;
            User = user;
        }
    }

    public class AiService : IDisposable
    {
        private bool isDisposed = false;
        private static bool initialized = false;
        private dynamic threadState;
        private static string sample_pdfs_directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "sample_pdfs");
        private static string pdfLocation = sample_pdfs_directory; 
        public string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "logs", "log.txt");
        private readonly Dictionary<string, EventHandler<StatusUpdateEventArgs>> eventHandlers = new();

        //Define event based on the delegate

        public AiService()
        {
            Log.Logger = new LoggerConfiguration().WriteTo.File(logFile, rollingInterval: RollingInterval.Day, shared: true).CreateLogger();
            Initialize();
            RefreshDocumentImport();
        }

        public void SubscribeUser(string userId, EventHandler<StatusUpdateEventArgs> handler)
        {
            eventHandlers[userId] = handler;
            Log.Logger.Information("Subscribe: user " + userId);           
        }

        public void UnSubscribeUser(string userId)
        {
            if (eventHandlers.TryGetValue(userId, out var handler))
            {
                eventHandlers.Remove(userId);
                Log.Logger.Information("UnSubscribe: user " + userId);
            }
        }

        //Method to rasise status update event
        private void OnStatusUpdate(dynamic statusData)
        {

            using (Py.GIL())
            {
                string data = statusData["data"];
                string user = statusData["user"];
                Log.Logger.Information("Status Update from Python " + data);
                var handler = eventHandlers[user];
                handler?.Invoke(this, new StatusUpdateEventArgs(user, data));
            }
        }

        /* 
        * Initialize by preparing python environmental variables and creating needed directories
        */
        private void Initialize()
        {
            var neededDirectories = new List<string>
            {
                "data",
                "data\\vector_store",
                "data\\sample_pdfs"
            };

            foreach (var item in neededDirectories)
            {
                var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, item);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            if (!PythonEngine.IsInitialized)
            {
                string pythonDllPath = Environment.GetEnvironmentVariable("PYTHONNET_PYDLL");
                if (pythonDllPath != null)
                {
                    string pythonHome = Directory.GetParent(pythonDllPath).FullName;
                    Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDllPath);
                    Environment.SetEnvironmentVariable("PYTHONHOME", pythonHome, EnvironmentVariableTarget.Process);

                    var path = Environment.GetEnvironmentVariable("PATH").TrimEnd(';');
                    path = !string.IsNullOrEmpty(path) ? $"{path};{pythonHome};{pythonHome}\\Lib\\site-packages;{pythonHome}\\Lib; {pythonHome}\\Lib\\site-packages\\onnxruntime" : pythonHome;
                    Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PYTHONPATH", $"{pythonHome}\\Lib", EnvironmentVariableTarget.Process);

                    Runtime.PythonDLL = pythonDllPath;
                    PythonEngine.Initialize();

                    PythonEngine.PythonHome = pythonHome;
                    PythonEngine.PythonPath = Environment.GetEnvironmentVariable("PYTHONPATH", EnvironmentVariableTarget.Process);

                    threadState = PythonEngine.BeginAllowThreads();

                }
            }
        }

        /* 
         * Chat with AI via pythonnet
         */
        public dynamic ChatWithAI(string user, string message = "", string topic="")
        {
            dynamic result = string.Empty;

            try
            {
                using (Py.GIL())
                {
                    dynamic py = Py.Import("__main__");
                    using dynamic myModule = Py.Import("AI");
                    dynamic aiClassObject = myModule.AiClass();

                    if (!initialized)
                    {
                        Log.Logger.Information("Vector store not initialized. Initializing from vector file...!");
                        aiClassObject.load_vector_store_from_existing(topic);
                        initialized = true;
                    }

                    if (!string.IsNullOrEmpty(message))
                    {
                        aiClassObject.set_status_update_event(user, new Action<dynamic>(OnStatusUpdate));
                        result = aiClassObject.chat(user, message, topic);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex.ToString());
                return result;
            }
        }

        /* 
         * Import and refresh document sources to be used as inputs
         */
        public static bool RefreshDocumentImport()
        {
            bool status = false;

            try
            {
                using (Py.GIL())
                {
                    using dynamic py = Py.Import("AI");
                    dynamic aiClassObject = py.AiClass();
                    var importProperties = new PdfProperties
                    {
                        FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "import.xml"),
                        Topics = new List<string>()
                    };

                    XmlSerializer serializer = new(typeof(PdfProperties));

                    if (File.Exists(importProperties.FileName))
                    {
                        StreamReader reader = new(new FileStream(importProperties.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                        importProperties = serializer.Deserialize(reader) as PdfProperties;
                        reader.Close();
                    }

                    if ((importProperties != null && importProperties.UpdateRequired) || !File.Exists(importProperties?.FileName))
                    {
                        Log.Logger.Information("Starting document import...!");
                        var directroies = Directory.GetDirectories(sample_pdfs_directory);                       
                        foreach (string dir in directroies)
                        {
                            DirectoryInfo dirInfo = new(dir);
                            aiClassObject.import_all_pdfs_in_directory(dirInfo.FullName);
                            importProperties?.Topics.Add(dirInfo.Name);
                            initialized = true;                            
                        }

                        status = true;
                        importProperties.LastImportDate = DateTime.Now;
                        importProperties.UpdateRequired = false;

                        StreamWriter writer = new(new FileStream(importProperties.FileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
                        serializer.Serialize(writer, importProperties);
                        writer.Close();
                        Log.Logger.Information("Document import DONE!");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex.ToString());
            }
            return status;
        }

        public List<string> SetPreferedTopicCategories() 
        {
            var directroies = Directory.GetDirectories(sample_pdfs_directory);
            var directoryList = new List<string>();

            foreach(string dir in directroies)
            {
                DirectoryInfo dirInfo = new(dir);
                directoryList.Add(dirInfo.Name);
            }

            directoryList.Add("All");
            return directoryList;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    PythonEngine.EndAllowThreads(threadState);
                    PythonEngine.Shutdown();
                }
                isDisposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
