using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmbryoSegmenter.Logging
{
    public class LoggerManager
    {
        private string _log_file;

        public LoggerManager(string logFile)
        {
            _log_file = logFile;
        }

        public void SetLogFilePath(string logFile)
        {
             _log_file = logFile;
        }

        public Logger CreateNewLogger(string Owner)
        {
            Logger log = new Logger(Owner, _log_file);
            return log;
        }

        
    }

    public enum LoggerLevel
    {
        ERROR = 900, WARNING = 700, INFO = 500, DEBUG = 300
    }
}
