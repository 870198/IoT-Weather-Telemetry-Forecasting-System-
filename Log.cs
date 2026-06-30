using System;
using System.IO;
using Core;
public static class Log
{
    private static readonly object _lock = new();
    private static readonly string _path = "app.log";

    public static void Info(string msg) => Write("INFO", msg);
    public static void Error(string msg, Exception ex) => Write("ERROR", $"{msg}\n{ex}");

    private static void Write(string level, string msg)
    {
        lock (_lock)
        {
            File.AppendAllText(_path, $"{DateTime.Now:O} [{level}] {msg}\n");
        }
    }
}