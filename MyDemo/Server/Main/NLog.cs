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

        // 兼容仅传 ProcessMode 的 Fantasy 版本。
        public void Initialize(ProcessMode processMode)
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

        // 兼容截图中当前 ILog 所要求的初始化签名。
        public void Initialize(string name, ProcessMode processMode)
        {
            Initialize(processMode);
        }

        public void Trace(string message) => _logger.Trace(message);
        public void Warning(string message) => _logger.Warn(message);
        public void Info(string message) => _logger.Info(message);
        public void Debug(string message) => _logger.Debug(message);
        public void Error(string message) => _logger.Error(message);
        public void Fatal(string message) => _logger.Fatal(message);

        public void Trace(string message, params object[] args) => _logger.Trace(message, args);
        public void Warning(string message, params object[] args) => _logger.Warn(message, args);
        public void Info(string message, params object[] args) => _logger.Info(message, args);
        public void Debug(string message, params object[] args) => _logger.Debug(message, args);
        public void Error(string message, params object[] args) => _logger.Error(message, args);
        public void Fatal(string message, params object[] args) => _logger.Fatal(message, args);

        // 兼容截图中当前 ILog 所要求的“分类/场景名 + 消息”签名。
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