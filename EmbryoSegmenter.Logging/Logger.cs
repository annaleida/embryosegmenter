using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmbryoSegmenter.Logging
{
    public class Logger
    {
        private string _log_file_path;
        private string _owner;

        public Logger(string owner, string LogFilePath)
        {
            _log_file_path = LogFilePath;
            _owner = owner;
        }


        public void Debug(string message)
        {
#if DEBUG
            WriteToFile(AppendInfo(LoggerLevel.DEBUG.ToString(), message));
#endif
        }


        public void Info(string message)
        {
            WriteToFile(AppendInfo(LoggerLevel.INFO.ToString(), message));
        }
        public void Warn(string message)
        {
            WriteToFile(AppendInfo(LoggerLevel.WARNING.ToString(), message));
        }
        public void Warn(string message, Exception exception)
        {
            WriteToFile(AppendInfo(LoggerLevel.WARNING.ToString(), message + exception.Message));
        }
        public void Error(string message)
        {
            WriteToFile(AppendInfo(LoggerLevel.ERROR.ToString(), message));
        }
        public void Error(string message, Exception exception)
        {
            WriteToFile(AppendInfo(LoggerLevel.ERROR.ToString(), message + exception.Message));
        }

        public string AppendInfo(string level, string message)
        {
            string fullMessage = DateTime.Now + " " + _owner + " " + level + " " + message + Environment.NewLine;
            return fullMessage;
        }

        public void WriteToFile(string message)
        {
            File.AppendAllText(_log_file_path, message);
        }
    }

}
