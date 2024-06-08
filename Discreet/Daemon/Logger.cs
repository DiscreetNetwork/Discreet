using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;

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
                    logger = new Logger(DaemonConfig.GetConfig().LogPath);
                }
            }
        }

        private string path;
        private DateTime openLogTime;
        private StreamWriter openLog;
        private string openLogPath;
        private long bytesWritten = 0;

        private ConcurrentQueue<(ConsoleColor, bool, string)> logQueue = new();
        private ConsoleColor _restoreBg;
        private ConsoleColor _restoreFg;

        public Logger(string logpath)
        {
            path = logpath;

            if (File.Exists(path)) throw new Exception("Discreet.Daemon.Daemon: expects a valid directory path, not a file");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            CreateLogfile();

            _restoreBg = Console.BackgroundColor;
            _restoreFg = Console.ForegroundColor;

            Console.CancelKeyPress += delegate
            {
                Console.BackgroundColor = _restoreBg;
                Console.ForegroundColor = _restoreFg;
            };
        }

        private void CreateLogfile()
        {
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

        private void WriteToFile(string msg)
        {
            logger.bytesWritten += msg.Length;
            logger.openLog.WriteLine(msg);
            logger.openLog.Flush();

            if (DaemonConfig.GetConfig().MaxLogfileSize == 0) return;

            if (logger.bytesWritten > DaemonConfig.GetConfig().MaxLogfileSize)
            {
                logger.openLog.Close();

                CreateLogfile();
                logger.bytesWritten = 0;

                if (DaemonConfig.GetConfig().MaxNumLogfiles > 0)
                {
                    //check whether or not to delete old logfiles
                    var sortedFiles = new DirectoryInfo(path).GetFiles()
                                                      .OrderByDescending(f => f.LastWriteTime)
                                                      .ToList();

                    sortedFiles.RemoveRange(0, DaemonConfig.GetConfig().MaxNumLogfiles.Value);

                    foreach (var file in sortedFiles)
                    {
                        file.Delete();
                    }
                }
            }
        }

        public void Start(CancellationToken token = default)
        {
            _ = Task.Run(() => _Start(token), token).ConfigureAwait(false);
        }

        private async Task _Start(CancellationToken token = default)
        {
            while (!token.IsCancellationRequested)
            {
                while (!logQueue.IsEmpty)
                {
                    logQueue.TryDequeue(out var log);

                    if (log == default) break;

                    if (log.Item2) logger.WriteToFile(log.Item3);
                    if (Console.ForegroundColor != log.Item1) Console.ForegroundColor = log.Item1;
                    Console.WriteLine(log.Item3);
                }

                await Task.Delay(25, token);
            }
        }


        public static void CrashLog(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;

            Fatal($"CRASH: Unhandled exception in program: {e}. Consider creating an issue on Github.");
            Environment.Exit(-1); // -1 = fatal crash
        }

        public static void Log(string msg)
        {
            msg = $"[{DateTime.Now.Hour.ToString().PadLeft(2, '0')}:{DateTime.Now.Minute.ToString().PadLeft(2, '0')}:{DateTime.Now.Second.ToString().PadLeft(2, '0')}] " + msg;
            Logger logger = GetLogger();
            logger.logQueue.Enqueue((ConsoleColor.White, true, msg));
        }

        public static void Log(string msg, string lvl, bool save = true, int verbose = 0, ConsoleColor color = ConsoleColor.White)
        {
            if (DaemonConfig.GetConfig().VerboseLevel.Value < verbose) return;

            msg = $"[{DateTime.Now.Hour.ToString().PadLeft(2, '0')}:{DateTime.Now.Minute.ToString().PadLeft(2, '0')}:{DateTime.Now.Second.ToString().PadLeft(2, '0')}] [{lvl}] - " + msg;
            Logger logger = GetLogger();
            logger.logQueue.Enqueue((color, save, msg));
        }

        public static void Info(string msg, bool save = true, int verbose = 0)
        {
            Log(msg, "INFO", save, verbose, ConsoleColor.White);
        }

        public static void Warn(string msg, bool save = true, int verbose = 0)
        {
            Log(msg, "WARN", save, verbose, ConsoleColor.Yellow);
        }

        public static void Critical(string msg, bool save = true, int verbose = 0)
        {
            Log(msg, "CRITICAL", save, verbose, ConsoleColor.DarkYellow);
        }

        public static void Error(string msg, Exception exc = null, bool save = true, int verbose = 0)
        {
            if (exc != null && DaemonConfig.GetConfig().PrintStackTraces.Value)
                Log($"{msg}\nStack Trace:\n{exc.StackTrace}", "ERROR", save, verbose, ConsoleColor.Red);
            else
                Log(msg, "ERROR", save, verbose, ConsoleColor.Red);
        }

        public static void Fatal(string msg, bool save = true, int verbose = 0)
        {
            Log(msg, "FATAL", save, verbose, ConsoleColor.DarkRed);
        }

        public static void Debug(string msg, bool save = true, int verbose = 0)
        {
            if (Daemon.DebugMode || DaemonConfig.GetConfig().DbgConfig.DebugPrints.Value)
                Log(msg, "DEBUG", save, verbose, ConsoleColor.DarkGreen);
        }
    }
}
