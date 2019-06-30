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
