using Python.Runtime;
using System;

namespace AI2PdfChat
{
    public class AIManager : IDisposable
    {
        private bool isDisposed = false;
        private bool disposedValue;
        private static bool initialized = false;
        private static IntPtr threadState;
        private static string pdfLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "sample_pdfs");

        public AIManager()
        { 
            Runtime.PythonDLL = @"C:\ENG_APPS\Python\python39.dll";
            if (!PythonEngine.IsInitialized)
            {
                PythonEngine.Initialize();
                PythonEngine.BeginAllowThreads();
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

            RefreshDocumentImport();
        }

        public dynamic ChatWithAI(string message = "")
        {
            dynamic result = string.Empty;

            using (Py.GIL())
            {
                try
                {
                    using dynamic py = Py.Import("AIInteractor");
                    if (!initialized)
                    {
                        RefreshDocumentImport();
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(message))
                        {
                            result = py.continue_chat(message);
                        }
                    }

                    return result;
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return result;
                }
            }
        }

        public static bool RefreshDocumentImport()
        {
            bool status = false;
            using (Py.GIL())
            {
                try
                {
                    using dynamic py = Py.Import("AIInteractor");
                    py.import_all_pdfs_in_directory(pdfLocation);
                    py.initialize_vector();
                    initialized = true;
                    status = true;
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            return status;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    PythonEngine.Shutdown();
                    PythonEngine.EndAllowThreads(threadState);
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
