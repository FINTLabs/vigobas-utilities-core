using System;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using Microsoft.MetadirectoryServices;

namespace Vigo.Bas.ManagementAgent.Log
{
    internal class Log4NetLogger: IManagementAgentLogger
    {
        private const string DefaultMaFolderLogFileName = "log.log";
        /// <summary>
        /// .config or .xml file with log4net configuration
        /// </summary>
        private const string DefaultConfigFile = "log.config";
        const string DefaultLogFormat = "%date %-5level - %message%newline%exception";
        private const string FallbackLogFormat = "%date %-5level - %message%newline%exception";
        private readonly ILog _log;

        /// <summary>
        /// MaMaFolder can only be used in the context of MIM-processes
        /// </summary>
        //private readonly string[] _appDomainsThatCanUseMaUtils = new[] { " MA " };

        /// 
        public Log4NetLogger()
        {
            try
            {
                SetupMaUtilsLogging();
            }
            catch (Exception)
            {
                SetupFallbackLogging();
            }
            
            _log = LogManager.GetLogger(Assembly.GetCallingAssembly().FullName.Split(',')[0]);
        }

        /// <summary>
        /// Sets up logging based on ManagementAgent folder in MaData. Can only be used when called by a MIM-process.
        /// </summary>
        private void SetupMaUtilsLogging()
        {
            string logFolder = MAUtils.MAFolder;
            string configFileName = Path.Combine(logFolder, DefaultConfigFile);

            if (File.Exists(configFileName))
            {
                SetupLoggerFromConfigFile(configFileName);
            }
            else 
            {
                string maFolderLogFile = Path.Combine(logFolder, DefaultMaFolderLogFileName);

                Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();

                PatternLayout patternLayout = new PatternLayout();
                patternLayout.ConversionPattern = DefaultLogFormat;
                patternLayout.ActivateOptions();

                RollingFileAppender roller = new RollingFileAppender();
                roller.AppendToFile = true;
                roller.File = maFolderLogFile;
                roller.Layout = patternLayout;
                roller.MaxSizeRollBackups = 5;
                roller.MaximumFileSize = "500KB";
                roller.RollingStyle = RollingFileAppender.RollingMode.Size;
                roller.StaticLogFileName = true;
                roller.ActivateOptions();
                hierarchy.Root.AddAppender(roller);

                hierarchy.Root.Level = Level.Debug;
                hierarchy.Configured = true;
            }
        }

        private void SetupLoggerFromConfigFile(string configFileName)
        {
            var configFile = new FileInfo(configFileName);
            log4net.Config.XmlConfigurator.ConfigureAndWatch(configFile);
        }

        private void SetupFallbackLogging()
        {
            //TODO: NB! Will only work when run with administrative rights
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();

            var patternLayout = new PatternLayout();
            patternLayout.ConversionPattern = FallbackLogFormat;
            patternLayout.IgnoresException = false;
            patternLayout.ActivateOptions();

            var eventLogger = new EventLogAppender();
            eventLogger.ApplicationName = "Buddy logger";
            eventLogger.Layout = patternLayout;
            eventLogger.ActivateOptions();
            hierarchy.Root.AddAppender(eventLogger);

            hierarchy.Root.Level = Level.Debug;
            hierarchy.Configured = true;
        }

        public void Debug(object message)
        {
            _log.Debug(message);
        }

        public void DebugFormat(string message, params object[] args)
        {
            _log.DebugFormat(message, args);
        }

        public void Debug(object message, Exception exception)
        {
            _log.Debug(message, exception);
        }

        public void Info(object message)
        {
            _log.Info(message);
        }

        public void InfoFormat(string message, params object[] args)
        {
            _log.InfoFormat(message, args);
        }

        public void Info(object message, Exception exception)
        {
            _log.Info(message, exception);
        }

        public void Error(object message)
        {
            _log.Error(message);
        }

        public void ErrorFormat(string message, params object[] args)
        {
            _log.ErrorFormat(message, args);
        }

        public void Error(object message, Exception exception)
        {
            _log.Error(message, exception);
        }
    }
}
