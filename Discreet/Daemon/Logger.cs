using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Discreet.Daemon
{
    /**
     * Logs messages to the console and a log file.
     * Default channel is Console.Out.
     */
    public class Logger
    {
        private static Logger logger;

        private static object logger_lock = new object();

        private static object writer_lock = new object();

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
                    logger = new Logger(DaemonConfig.GetDefault().LogPath);
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

            openLogPath = Path.Combine(path, "log_" + $"{openLogTime.Date.Day.ToString().PadLeft(2, '0')}{openLogTime.Date.Month.ToString().PadLeft(2, '0')}{openLogTime.Date.Year.ToString().PadLeft(4, '0')}_{openLogTime.Hour.ToString().PadLeft(2, '0')}{openLogTime.Minute.ToString().PadLeft(2, '0')}{openLogTime.Second.ToString().PadLeft(2, '0')}" + ".txt");

            int i = 0;

            while (File.Exists(openLogPath))
            {
                openLogPath = Path.Combine(path, "log_" + $"{openLogTime.Date.Day.ToString().PadLeft(2, '0')}{openLogTime.Date.Month.ToString().PadLeft(2, '0')}{openLogTime.Date.Year.ToString().PadLeft(4, '0')}_{openLogTime.Hour.ToString().PadLeft(2, '0')}{openLogTime.Minute.ToString().PadLeft(2, '0')}{openLogTime.Second.ToString().PadLeft(2, '0')}" + $"_{i}.txt");
                i++;
            }

            openLog = File.CreateText(openLogPath);
        }

        public static void Log(string msg)
        {
            lock (writer_lock)
            {
                msg = $"[{DateTime.Now.Hour.ToString().PadLeft(2, '0')}:{DateTime.Now.Minute.ToString().PadLeft(2, '0')}:{DateTime.Now.Second.ToString().PadLeft(2, '0')}] " + msg;

                Logger logger = GetLogger();

                logger.openLog.WriteLine(msg);
                logger.openLog.Flush();

                Console.WriteLine(msg);
            }
        }

        public static void Log(string msg, string lvl)
        {
            lock (writer_lock)
            {
                msg = $"[{DateTime.Now.Hour.ToString().PadLeft(2, '0')}:{DateTime.Now.Minute.ToString().PadLeft(2, '0')}:{DateTime.Now.Second.ToString().PadLeft(2, '0')}] [{lvl}] - " + msg;

                Logger logger = GetLogger();

                logger.openLog.WriteLine(msg);
                logger.openLog.Flush();

                Console.WriteLine(msg);
            }
        }

        public static void Info(string msg)
        {
            Log(msg, "INFO");
        }

        public static void Warn(string msg)
        {
            Log(msg, "WARN");
        }

        public static void Error(string msg)
        {
            Log(msg, "ERROR");
        }

        public static void Fatal(string msg)
        {
            Log(msg, "FATAL");
        }

        public static void Debug(string msg)
        {
            Log(msg, "DEBUG");
        }
    }
}
