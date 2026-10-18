using Serilog;

namespace Desktop;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // === Serilog Logging Configuration ===
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "desktop-.log");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("=== Smart Supermarket Desktop Application Starting ===");

        // === Global Exception Handling ===
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += Application_ThreadException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        try
        {
            ApplicationConfiguration.Initialize();
            Application.SetDefaultFont(new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point));
            Application.Run(new LoginForm());
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            ShowFatalError(ex);
        }
        finally
        {
            Log.Information("=== Smart Supermarket Desktop Application Closing ===");
            Log.CloseAndFlush();
        }
    }

    private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
    {
        Log.Error(e.Exception, "[UI Thread Exception] {Message}", e.Exception.Message);
        ShowErrorDialog(e.Exception);
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        Log.Fatal(ex, "[Unhandled Domain Exception] IsTerminating={IsTerminating}", e.IsTerminating);
        if (ex != null) ShowFatalError(ex);
    }

    private static void ShowErrorDialog(Exception ex)
    {
        try
        {
            MessageBox.Show(
                $"Đã xảy ra lỗi không mong muốn:\n\n{ex.Message}\n\n" +
                $"Chi tiết lỗi đã được ghi vào thư mục Logs.\n" +
                $"Ứng dụng sẽ tiếp tục hoạt động.",
                "⚠️ Lỗi Hệ Thống",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch { }
    }

    private static void ShowFatalError(Exception ex)
    {
        try
        {
            MessageBox.Show(
                $"Lỗi nghiêm trọng, ứng dụng cần đóng:\n\n{ex.Message}\n\n" +
                $"Vui lòng kiểm tra thư mục Logs để biết chi tiết.\n" +
                $"Liên hệ quản trị viên nếu lỗi lặp lại.",
                "❌ Lỗi Nghiêm Trọng",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch { }
    }
}