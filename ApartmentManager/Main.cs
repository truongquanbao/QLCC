using ApartmentManager.GUI.Forms;
using ApartmentManager.Utilities;
using Serilog;
using System;
using System.Windows.Forms;

namespace ApartmentManager
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
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

                if (!EnsureDatabaseConnection())
                {
                    Log.Warning("Application stopped because database connection is not available");
                    return;
                }

                // Check if user is logged in, otherwise show login form
                UserSession session = SessionManager.GetSession();
                if (session == null)
                {
                    using (var loginForm = new FrmLogin())
                    {
                        if (loginForm.ShowDialog() != DialogResult.OK)
                        {
                            Log.Information("User closed login form");
                            return;
                        }
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

        private static bool EnsureDatabaseConnection()
        {
            var ensureResult = DatabaseHelper.EnsureActiveConnection();
            if (ensureResult.success)
            {
                return true;
            }

            while (true)
            {
                var dialogResult = MessageBox.Show(
                    $"Không thể kết nối cơ sở dữ liệu.\n\n{ensureResult.message}\n\nChọn Yes để cấu hình lại kết nối, hoặc No để thoát ứng dụng.",
                    "Kết nối cơ sở dữ liệu",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (dialogResult != DialogResult.Yes)
                {
                    return false;
                }

                using var databaseSetup = new FrmDatabaseSetup();
                if (databaseSetup.ShowDialog() != DialogResult.OK)
                {
                    return false;
                }

                ensureResult = DatabaseHelper.EnsureActiveConnection();
                if (ensureResult.success)
                {
                    return true;
                }
            }
        }
    }
}
