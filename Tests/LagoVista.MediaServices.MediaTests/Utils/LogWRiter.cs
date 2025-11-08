// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 122e1e635a59cc0459e7b42e0fe8cae4eb22e26085df0b98295430f4bd21a047
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.MediaTests.Utils
{
    public class LogWriter : ILogWriter
    {
        public Task WriteError(LogRecord record)
        {
            Console.WriteLine(record.Message);
            return Task.FromResult(default(object));
        }

        public Task WriteEvent(LogRecord record)
        {
            Console.WriteLine(record.Message);
            return Task.FromResult(default(object));
        }
    }
}
