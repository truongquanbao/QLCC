using ApartmentManager.GUI.Forms;
using ApartmentManager.Utilities;
using Serilog;
using System;
using System.Windows.Forms;

namespace ApartmentManager
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                // Initialize Serilog
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .WriteTo.File("logs/apartment-manager-.txt", rollingInterval: RollingInterval.Day)
                    .CreateLogger();

                Log.Information("Application started");

                // Enable visual styles
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Show splash screen
                FrmSplashScreen splashScreen = new FrmSplashScreen();
                if (splashScreen.ShowDialog() != DialogResult.OK)
                {
                    Log.Warning("Splash screen initialization failed or cancelled");
                    return;
                }

                // Check if user is logged in, otherwise show login form
                UserSession session = SessionManager.GetSession();
                if (session == null)
                {
                    FrmLogin loginForm = new FrmLogin();
                    Application.Run(loginForm);
                    if (loginForm.DialogResult != DialogResult.OK)
                    {
                        Log.Information("User closed login form");
                        return;
                    }
                    session = SessionManager.GetSession();
                    if (session == null)
                    {
                        Log.Warning("No session after login");
                        return;
                    }
                }

                // Show main dashboard
                Log.Information("Opening main dashboard for user: {Username}", session.Username);
                FrmMainDashboard mainForm = new FrmMainDashboard();
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application crashed");
                MessageBox.Show($"Application error: {ex.Message}\n\nCheck logs for more details.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
