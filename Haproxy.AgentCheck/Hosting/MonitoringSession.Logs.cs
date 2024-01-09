﻿namespace Lucca.Infra.Haproxy.AgentCheck.Hosting;

#pragma warning disable S3881
internal partial class MonitoringSession
{
    [LoggerMessage(LogLevel.Error, "No process {watchedProcess} currently running.")]
    public static partial void LogNoProcessRunning(ILogger logger, string watchedProcess);

    [LoggerMessage(LogLevel.Warning, "Found {count} {watchedProcess} currently running, watching pid {pid}.")]
    public static partial void LogMoreThanOneProcessRunning(ILogger logger, string watchedProcess, int pid, int count);

    [LoggerMessage(LogLevel.Information, "Watching {watchedProcess} pid {pid}.")]
    public static partial void LogOneProcessRunning(ILogger logger, string watchedProcess, int pid);

    [LoggerMessage(LogLevel.Warning, "An error occured while watching process. Process may have ended, retrying.")]
    public static partial void LogProcessingException(ILogger logger, Exception e);
}
#pragma warning restore S3881
