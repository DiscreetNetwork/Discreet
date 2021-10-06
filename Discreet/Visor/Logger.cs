using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Discreet.Visor
{
    /**
     * Logs messages to the console and a log file.
     * Default channel is Console.Out.
     */
    public class Logger
    {
        private static Logger logger;

        private static object logger_lock = new object();

        public static Logger GetLogger()
        {
            lock (logger_lock)
            {
                if (logger == null) Initialize();

                return logger;
            }
        }

        public static void Initialize()
        {
            lock (logger_lock)
            {
                if (logger == null)
                {
                    logger = new Logger(VisorConfig.GetDefault().LogPath);
                }
            }
        }

        private string path;
        private DateTime openLogTime;
        private StreamWriter openLog;
        private string openLogPath;

        public Logger(string logpath)
        {
            path = logpath;

            if (File.Exists(path)) throw new Exception("Discreet.Visor.Logger: expects a valid directory path, not a file");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            openLogTime = DateTime.Now;

            openLogPath = Path.Combine(path, "log_" + openLogTime.Date.ToLongDateString() + ".txt");

            int i = 0;

            while (File.Exists(openLogPath))
            {
                openLogPath = Path.Combine(path, "log_" + openLogTime.Date.ToLongDateString() + $"_{i}.txt");
            }

            openLog = File.CreateText(openLogPath);
        }

        public static void Log(string msg)
        {
            Logger logger = GetLogger();

            logger.openLog.WriteLine(msg);

            Console.WriteLine(msg);
        }

        // WIP
    }
}
