namespace Desktop;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetDefaultFont(new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point));
        Application.Run(new LoginForm());
    }    
}