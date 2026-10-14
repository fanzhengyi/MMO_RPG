using Fantasy.Platform.Net;
using NLog;

namespace Fantasy
{
    /// <summary>
    /// 与当前项目 ILog 接口兼容的 NLog 实现。
    /// </summary>
    public class NLog : ILog
    {
        private readonly Logger _logger;

        public NLog(string name)
        {
            _logger = LogManager.GetLogger(name);
        }

        /// <summary>根据启服模式选择控制台和文件日志规则。</summary>
        public void Initialize(string name, ProcessMode processMode)
        {
            switch (processMode)
            {
                case ProcessMode.Develop:
                    LogManager.Configuration.RemoveRuleByName("ServerDebug");
                    LogManager.Configuration.RemoveRuleByName("ServerTrace");
                    LogManager.Configuration.RemoveRuleByName("ServerInfo");
                    LogManager.Configuration.RemoveRuleByName("ServerWarn");
                    LogManager.Configuration.RemoveRuleByName("ServerError");
                    break;

                case ProcessMode.Release:
                    LogManager.Configuration.RemoveRuleByName("ConsoleTrace");
                    LogManager.Configuration.RemoveRuleByName("ConsoleDebug");
                    LogManager.Configuration.RemoveRuleByName("ConsoleInfo");
                    LogManager.Configuration.RemoveRuleByName("ConsoleWarn");
                    LogManager.Configuration.RemoveRuleByName("ConsoleError");
                    break;
            }
        }

        public void Trace(string message) => _logger.Trace(message);
        public void Warning(string message) => _logger.Warn(message);
        public void Info(string message) => _logger.Info(message);
        public void Debug(string message) => _logger.Debug(message);
        public void Error(string message) => _logger.Error(message);

        public void Trace(string message, params object[] args) => _logger.Trace(message, args);
        public void Warning(string message, params object[] args) => _logger.Warn(message, args);
        public void Info(string message, params object[] args) => _logger.Info(message, args);
        public void Debug(string message, params object[] args) => _logger.Debug(message, args);
        public void Error(string message, params object[] args) => _logger.Error(message, args);

        // ILog 的分类日志入口，保留框架要求的全部签名。
        public void Trace(string source, string message) => _logger.Trace("[{0}] {1}", source, message);
        public void Warning(string source, string message) => _logger.Warn("[{0}] {1}", source, message);
        public void Info(string source, string message) => _logger.Info("[{0}] {1}", source, message);
        public void Debug(string source, string message) => _logger.Debug("[{0}] {1}", source, message);
        public void Error(string source, string message) => _logger.Error("[{0}] {1}", source, message);

        public void Trace(string source, string message, params object[] args) =>
            _logger.Trace($"[{source}] {message}", args);

        public void Warning(string source, string message, params object[] args) =>
            _logger.Warn($"[{source}] {message}", args);

        public void Info(string source, string message, params object[] args) =>
            _logger.Info($"[{source}] {message}", args);

        public void Debug(string source, string message, params object[] args) =>
            _logger.Debug($"[{source}] {message}", args);

        public void Error(string source, string message, params object[] args) =>
            _logger.Error($"[{source}] {message}", args);
    }
}
