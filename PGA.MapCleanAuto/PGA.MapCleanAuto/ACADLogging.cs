#define TRACE
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Exception = System.Exception;
using SystemLogger = System.Diagnostics;

namespace PGA.Common.Logging
{
    public static class ACADLogging
    {

        // Create a trace listener for the event log.
        static  SystemLogger.EventLogTraceListener myTraceListener = new SystemLogger.EventLogTraceListener("PGA.Common.Logging");
        static SystemLogger.TextWriterTraceListener myTextListener = null;
        static Stream myFile = null;
        public static bool LogMyExceptions(string message, Exception ex)
        {
            string time = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            if (message != null)
            {
                SystemTracerLogMyExceptions(String.Format("{0} {1}\n", time, message));
            }
            return true;
        }
        public static void LogMyExceptions(string message)
        {
            //Write to File
            string time = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            if (message != null)
            {
                LogTextTraceWriter(String.Format("{0} {1}\n", time, message));
            }
        }

        private static void SystemTracerLogMyExceptions(string message)
        {
            try
            {
                // Register once; adding on every call duplicated each message N times.
                if (!SystemLogger.Trace.Listeners.Contains(myTraceListener))
                    SystemLogger.Trace.Listeners.Add(myTraceListener);

                // Write output to the event log.
                SystemLogger.Trace.WriteLine(message);
            }
            catch (System.Exception)
            {
                // Writing to the event log needs a registered source, which needs
                // administrator rights. Without them, log to the text file instead.
                SystemLogger.Trace.Listeners.Remove(myTraceListener);
                LogTextTraceWriter(message);
            }
        }

        public static bool LogMyExceptions(string message, string exception)
        {
            //Write to File
            string time = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            if (message != null)
            {
                SystemTracerLogMyExceptions(String.Format("{0} {1} {2}\n", time, message, exception));
            }
            return true;
        }

        /// <summary>
        /// Creates the log file at the preferred path, falling back to the user's
        /// temp folder when that location is not writable (the root of C: is not,
        /// for users without administrator rights).
        /// </summary>
        private static Stream CreateLogFile(string preferredPath)
        {
            try
            {
                return File.Create(preferredPath, 4096, FileOptions.RandomAccess);
            }
            catch (System.Exception ex)
            {
                if (!(ex is UnauthorizedAccessException) && !(ex is IOException))
                    throw;

                string fallback = Path.Combine(Path.GetTempPath(), Path.GetFileName(preferredPath));
                return File.Create(fallback, 4096, FileOptions.RandomAccess);
            }
        }

        public static int LogTextTraceWriter(string message)
        {
            string filename = "c:\\PGALogFile.txt";

            if (!String.IsNullOrEmpty(message))
                message = string.Format("{0}\n", message);

            // Create a file for output named TestFile.txt.
            if (myFile == null)
            {
                myFile = CreateLogFile(filename);
            }
            if (myTextListener == null)
            {
            /* Create a new text writer using the output stream, and add it to
             * the trace listeners. */
                myTextListener = new
                    SystemLogger.TextWriterTraceListener(myFile);
                SystemLogger.Trace.Listeners.Add(myTextListener);
            }
            // Write output to the file.
            SystemLogger.Trace.WriteLine(message);

            // Flush the output.
            SystemLogger.Trace.Flush();

            return 0;
        }

    }

}
