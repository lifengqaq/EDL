using System;
using System.IO;
using Avalonia;
using Avalonia.Win32;

namespace lfEDL.Avalonia
{
    public class Program
    {
        [System.STAThread]
        public static void Main(string[] args)
        {
            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                var logPath = Path.Combine(Path.GetTempPath(), "lfEDL.Avalonia.log");
                File.AppendAllText(logPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + ex + Environment.NewLine);
                throw;
            }
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new Win32PlatformOptions
                {
                    RenderingMode = new[] { Win32RenderingMode.Software }
                })
                .LogToTrace();
        }
    }
}

