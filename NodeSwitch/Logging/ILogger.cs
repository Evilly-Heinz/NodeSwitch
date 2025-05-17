namespace NodeSwitch.Logging
{
    public interface ILogger
    {
        void LogInformation(string classAndMethod, string message);
        void LogWarning(string classAndMethod, string message);
        void LogError(string classAndMethod, string message);
        void LogError(string classAndMethod, string message, Exception ex);
    }
}
