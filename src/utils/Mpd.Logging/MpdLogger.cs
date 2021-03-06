﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using log4net;
using System.Reflection;

namespace Mpd.Logging
{
    public enum MpdLogLevelEnum
    {
        Error,
        Warn,
        Info,
        Debug,
    }

    public class MpdLogger
    {
        public static void Log(params object[] args)
        {
            Log(MpdLogLevelEnum.Info, 1, args);
        }

        public static void Log(int ignoreStackFrameCount, params object[] args)
        {
            Log(MpdLogLevelEnum.Info, ignoreStackFrameCount + 1, args);
        }

        public static void Log(MpdLogLevelEnum level,
            params object[] args)
        {
            Log(level, 1, args);
        }

        public static void Log(MpdLogLevelEnum level,
            int ignoreStackFrameCount,
            params object[] args)
        {
            StackTrace st = new StackTrace(true);
            ignoreStackFrameCount++;

            ILog logger = GetLogger(st, ignoreStackFrameCount);

            if (!CanLog(logger, level))
                return;

            StringBuilder msgBuilder = new StringBuilder();

            CreateCallerMessage(msgBuilder, st, ignoreStackFrameCount);

            ProcessArguments(msgBuilder, args);

            switch (level)
            {
                case MpdLogLevelEnum.Debug:
                    logger.Debug(msgBuilder.ToString());
                    break;
                case MpdLogLevelEnum.Error:
                    logger.Error(msgBuilder.ToString());
                    break;
                case MpdLogLevelEnum.Info:
                    logger.Info(msgBuilder.ToString());
                    break;
                case MpdLogLevelEnum.Warn:
                    logger.Warn(msgBuilder.ToString());
                    break;
            }
        }

        private static void ProcessArguments(StringBuilder builder, object[] args)
        {
            if (args == null || args.Length == 0)
                return;

            List<Exception> exceptions = new List<Exception>();

            StringBuilder b = new StringBuilder();
            b.AppendLine("Messages:");

            foreach (object o in args)
            {
                if (o is Exception)
                    exceptions.Add(o as Exception);
                else if (o != null)
                    b.AppendFormat("\t{0}", o).AppendLine();
            }

            if (exceptions.Count < args.Length)
            {
                builder.AppendLine(b.ToString());
            }

            if (exceptions.Count > 0)
            {
                builder.AppendLine("Exceptions:");

                int i = 1;

                foreach (Exception e in exceptions)
                {
                    if (exceptions.Count > 1)
                    {
                        builder.AppendFormat("\tException {0} Begins------------------", i).AppendLine();
                    }

                    BuildExceptionMessage("\t", builder, e);

                    if (exceptions.Count > 1)
                    {
                        builder.AppendFormat("\tException {0} Ends--------------------", i++).AppendLine();
                    }
                    builder.AppendLine();
                }

                builder.AppendLine();
            }
        }

        private static void BuildExceptionMessage(string prefix, StringBuilder builder, Exception e)
        {
            builder.AppendFormat("{2}\t{0}:{1}", e.GetType().FullName, e.Message, prefix).AppendLine();

            if (!string.IsNullOrEmpty(e.StackTrace))
            {
                builder.AppendFormat("{0}\tStackTrace:", prefix).AppendLine();
                builder.AppendFormat("{1}\t{0}", e.StackTrace, prefix).AppendLine();
            }

            if (e.InnerException != null)
            {
                builder.AppendLine();
                builder.AppendFormat("{0}\tInnerException Begins ------------------------", prefix).AppendLine();
                BuildExceptionMessage(prefix + "\t", builder, e.InnerException);
                builder.AppendFormat("{0}\tInnerException Ends ------------------------", prefix).AppendLine();
            }
        }

        private static void CreateCallerMessage(StringBuilder builder,
            StackTrace st, int ignoreStackFrameCount)
        {
            int lineNum = 0;
            string filename = null;

            MethodBase callerMethod = GetCallerMethod(st,
                ignoreStackFrameCount,
                out lineNum, out filename);

            builder.AppendLine("Log From:");
            builder.AppendFormat("\tType:{0}", callerMethod.ReflectedType.AssemblyQualifiedName).AppendLine();
            builder.AppendFormat("\tMethod:{0}", callerMethod.ToString()).AppendLine();
            builder.AppendFormat("\tFilename:{0}", filename).AppendLine();
            builder.AppendFormat("\tLine:{0}", lineNum).AppendLine();
        }

        private static bool CanLog(ILog logger, MpdLogLevelEnum level)
        {
            switch (level)
            {
                case MpdLogLevelEnum.Debug:
                    return logger.IsDebugEnabled;
                case MpdLogLevelEnum.Error:
                    return logger.IsErrorEnabled || logger.IsFatalEnabled ||
                        logger.IsDebugEnabled || logger.IsInfoEnabled ||
                        logger.IsWarnEnabled;
                case MpdLogLevelEnum.Info:
                    return logger.IsDebugEnabled || logger.IsInfoEnabled;
                case MpdLogLevelEnum.Warn:
                    return logger.IsDebugEnabled || logger.IsInfoEnabled ||
                        logger.IsWarnEnabled;
            }

            return false;
        }

        private static ILog GetLogger(StackTrace st, int ignoreStackFrameCount)
        {
            MethodBase callerMethod = GetCallerMethod(st, ignoreStackFrameCount);

            return LogManager.GetLogger(callerMethod.ReflectedType);
        }

        private static MethodBase GetCallerMethod(StackTrace st, int ignoreStackFrameCount)
        {
            string filename;
            int linenum;

            return GetCallerMethod(st, ignoreStackFrameCount, out linenum, out filename);
        }

        private static MethodBase GetCallerMethod(StackTrace st, int ignoreStackFrameCount,
            out int linnum, out string filename)
        {
            int frameIndex = st.FrameCount > ignoreStackFrameCount ?
                ignoreStackFrameCount : st.FrameCount - 1;

            StackFrame sf = st.GetFrame(frameIndex);

            linnum = sf.GetFileLineNumber();
            filename = sf.GetFileName();

            return sf.GetMethod();
        }
    }
}
