using Python.Runtime;
using System;

namespace AI2PdfChat
{
    public class AIManager : IDisposable
    {
        private bool isDisposed = false;
        private bool disposedValue;
        private bool initialized = false;
        private static IntPtr threadState;

        public AIManager()
        {
            Runtime.PythonDLL = @"C:\ENG_APPS\Python\python39.dll";
            if (!PythonEngine.IsInitialized)
            {
                PythonEngine.Initialize();
                PythonEngine.BeginAllowThreads();
            }
            Thread.Sleep(2000);

            var pdfPath = Path.Combine(Directory.GetCurrentDirectory(), "sample_pdfs");
            ChatWithAI();
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
                        py.import_all_pdfs_in_directory("sample_pdfs");
                        py.initialize_vector();
                        initialized = true;
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
