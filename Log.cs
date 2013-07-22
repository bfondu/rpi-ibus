using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

namespace IBUS_sniffer
{
    public class Log
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static bool _isConfigured;

        public static void Exception(Exception ex)
        {
            if (!_isConfigured) Init();
            Logger.Error(ex.Message, ex);
        }

        public static void Debug(string message)
        {
            if (!_isConfigured) Init();
            Logger.Debug(message);
        }

        public static void Warning(string message)
        {
            if (!_isConfigured) Init();
            Logger.Warn(message);
        }

        public static void Fatal(string message)
        {
            if (!_isConfigured) Init();
            Logger.Fatal(message);
        }

        public static void Info(string message)
        {
            if (!_isConfigured) Init();
            Logger.Info(message);
        }

        public static void Init()
        {
            log4net.Config.XmlConfigurator.Configure();
            _isConfigured = true;
        }
    }
}
