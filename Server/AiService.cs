using Python.Runtime;
using System.IO;
using System.Xml.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Serilog;
using Serilog.Events;
using System.Diagnostics;

namespace ChatTheDoc.Server.AIService
{
    public class StatusUpdateEventArgs : EventArgs
    {
        public object Data { get; }
        public StatusUpdateEventArgs(object data)
        {
            Data = data;
        }
    }
    
    public class AiService : IDisposable
    {
        private bool isDisposed = false;
        private static bool initialized = false;
        private dynamic threadState;
        private static string pdfLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "sample_pdfs");
        public string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "logs", "log.txt");


        //Test Use
        public event EventHandler<StatusUpdateEventArgs> StatusUpdate;

        public AiService()
        {
            Log.Logger = new LoggerConfiguration().WriteTo.File(logFile, rollingInterval: RollingInterval.Day, shared: true).CreateLogger();
            Initialize();
            RefreshDocumentImport();           
        }

        private void OnStatusUpdate(dynamic statusUpdateEventArgs)
        {
            Log.Logger.Information("Status Update from Python " + statusUpdateEventArgs);
            StatusUpdate?.Invoke(this, new StatusUpdateEventArgs(statusUpdateEventArgs));
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
        public dynamic ChatWithAI(string message = "")
        {
            dynamic result = string.Empty;

            try
            {
                using (Py.GIL())
                {
                    using dynamic py = Py.Import("AI");
                    dynamic aiClassObject = py.AiClass;

                    if (!initialized)
                    {
                        Log.Logger.Information("Vector store not initialized. Initializing from vector file...!");
                        aiClassObject.load_vector_store_from_existing(aiClassObject);
                        initialized = true;
                    }

                    if (!string.IsNullOrEmpty(message))
                    {
                        aiClassObject.set_status_update_event(aiClassObject, new Action<dynamic>(OnStatusUpdate));                       
                        result = aiClassObject.chat(aiClassObject, message);
                    }

                    return result;
                }
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
                    dynamic aiClassObject = py.AiClass;
                    var importProperties = new PdfProperties
                    {
                        FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "import.xml"),
                    };

                    XmlSerializer serializer = new(typeof(PdfProperties));

                    if (File.Exists(importProperties.FileName))
                    {
                        StreamReader reader = new(new FileStream(importProperties.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                        importProperties = serializer.Deserialize(reader) as PdfProperties;
                        reader.Close();
                    }

                    if ((importProperties != null && importProperties.UpdateRequired) || !File.Exists(importProperties.FileName))
                    {
                        Log.Logger.Information("Starting document import...!");
                        aiClassObject.import_all_pdfs_in_directory(aiClassObject, pdfLocation);
                        aiClassObject.initialize(aiClassObject);
                        initialized = true;
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
