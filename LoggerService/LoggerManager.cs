using NLog;
using System;

namespace LoggerService
{
    /// <summary>
    /// Having a seperate LoggerService project allows us to make any needed changes in a single location.
    /// For example if we need to change from NLog, or customize how we handle different levels.
    /// </summary>
    public class LoggerManager : ILoggerManager
    {
        private static ILogger logger = LogManager.GetCurrentClassLogger();

        public LoggerManager()
        {
        }

        public void LogDebug(string message)
        {
            logger.Debug(message);
        }

        public void LogError(Exception ex, string message)
        {
            logger.Error(ex, message);
        }

        public void LogInfo(string message)
        {
            logger.Info(message);
        }

        public void LogWarn(string message)
        {
            logger.Warn(message);
        }
    }
}