using System;

namespace Vigo.Bas.ManagementAgent.Log
{
    public class Logger
    {
        private static IManagementAgentLogger _log;
    
        /// <summary>
        /// Sets up the <see cref="Log" /> with the config in the specified file
        /// </summary>
        /// <param name="filename"></param>
        public static void LoadLogFromConfig(string filename)
        {
            throw new NotImplementedException();
        }

        public static IManagementAgentLogger Log
        {
            get
            {
                if (_log == null)
                {
                    _log = new Log4NetLogger();
                }

                return _log;
            }
        }
    }
}
