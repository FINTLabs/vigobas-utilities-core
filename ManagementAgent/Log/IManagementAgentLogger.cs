using System;

namespace Vigo.Bas.ManagementAgent.Log
{
    public interface IManagementAgentLogger
    {
        void Debug(object message);
        void Debug(object message, Exception exception);
        void DebugFormat(string message, params object[] args);
        void Info(object message);
        void Info(object message, Exception exception);
        void InfoFormat(string message, params object[] args);
        void Error(object message);
        void Error(object message, Exception exception);
        void ErrorFormat(string message, params object[] args);
    }
}