using RapidKnowledgeware.Models;
using System;
using System.Windows;

namespace RapidKnowledgeware.Functions.DebugFunc
{
    /// <summary>
    /// 调试服务：提供全局静态方法记录信息与错误
    /// </summary>
    public static class DebugService
    {
        private static readonly DebugModel _model = DebugModel.Instance;

        /// <summary>
        /// 记录一条普通信息日志（线程安全）
        /// </summary>
        /// <param name="message">信息内容</param>
        public static void Info(string message)
        {
            AddEntry("Info", message, null);
        }

        /// <summary>
        /// 记录一条错误日志，可附带异常对象（线程安全）
        /// </summary>
        /// <param name="message">错误描述</param>
        /// <param name="ex">相关异常（可选）</param>
        public static void Error(string message, Exception ex = null)
        {
            AddEntry("Error", message, ex);
        }

        private static void AddEntry(string level, string message, Exception ex)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                _model.LogEntries.Add(new DebugLogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = level,
                    Message = message,
                    ExceptionDetail = ex?.Message   // 仅保留异常消息，不记录堆栈
                });
            }));
        }
    }
}