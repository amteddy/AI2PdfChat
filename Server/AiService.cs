using Python.Runtime;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json;

namespace AIChat.Server.AIService
{ 
    public class AiService : IDisposable
    {
        private bool isDisposed = false;
        private bool disposedValue;
        private static bool initialized = false;
        dynamic threadState;
        private static string pdfLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "sample_pdfs");

        public AiService()
        {
            if (!PythonEngine.IsInitialized)
            {
                Runtime.PythonDLL = GetPythonPath();
                PythonEngine.Initialize();
                threadState = PythonEngine.BeginAllowThreads();
            }

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

            // RefreshDocumentImport();
        }

        private string GetPythonPath()
        {
            var pythonDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Python");
            if (Directory.Exists(pythonDir))
            {
                string actualDir = Directory.GetDirectories(pythonDir).First();
                pythonDir = Path.Combine(actualDir, new DirectoryInfo(actualDir).Name + ".dll");
            }

            return pythonDir;
        }

        public dynamic ChatWithAI(string message = "")
        {
            dynamic result = string.Empty;
            try
            {
                using (Py.GIL())
                {
                    using dynamic py = Py.Import("AI");
                    if (!initialized)
                    {
                        py.load_vector_store_from_existing();
                        initialized = true;
                    }

                    if (!string.IsNullOrEmpty(message))
                    {
                        result = py.chat(message);
                    }

                    return result;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return result;
            }
        }

        public static bool RefreshDocumentImport()
        {
            bool status = false;
            try
            {
                using (Py.GIL())
                {

                    using dynamic py = Py.Import("AI");
                    py.import_all_pdfs_in_directory(pdfLocation);
                    py.initialize();
                    initialized = true;
                    status = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
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
