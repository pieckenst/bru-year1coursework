using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.UserSkins;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;
using Serilog;
using Serilog.Events;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Configure Serilog
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory); // Ensure log directory exists

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
                .Enrich.FromLogContext()
                
                
                .WriteTo.Console()
                .WriteTo.Debug()
                .WriteTo.File(Path.Combine(logDirectory, "app-.log"),
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: LogEventLevel.Debug,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Log.Information("Application starting up");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (sender, args) => 
                {
                    Log.Error(args.Exception, "Unhandled thread exception");
                    MessageBox.Show($"An error occurred: {args.Exception.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                };
                
                AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                {
                    Log.Fatal(args.ExceptionObject as Exception, "Unhandled application exception");
                };

                WindowsFormsSettings.EnableMdiFormSkins();
                
                // Show disclaimer about legacy Windows Forms application in Russian
                XtraMessageBox.Show(
                    $"Информация о системе:\n" +
                    $"ОС: {Environment.OSVersion.VersionString}\n" +
                    $".NET: {Environment.Version}\n\n" +
                    "ПРЕДУПРЕЖДЕНИЕ: Это устаревшее приложение Windows Forms с множеством недостатков:\n\n" +
                    "• Часть функциональности может быть неработоспособна или реализована не полностью\n" +
                    "• Механизмы фильтрации данных гарантировано не работают из-за Windows Forms ограничений\n" +
                    "• Приложение может резко замедляться при большом количестве данных\n" +
                    "• Крайне ограниченная отзывчивость интерфейса и отсутствие современных UI-компонентов\n" +
                    "• Ужасное масштабирование на мониторах с высоким разрешением (DPI)\n" +
                    "• Полное отсутствие функций доступности для людей с ограниченными возможностями\n" +
                    "• Серьезные утечки памяти при длительном использовании приложения\n" +
                    "• Устаревшая архитектура, не поддерживающая многопоточность и асинхронные операции\n" +
                    "• Невозможность корректной работы с современными API и протоколами\n" +
                    "• Отсутствие поддержки современных стандартов безопасности\n" +
                    "• Проблемы совместимости с новыми версиями Windows\n" +
                    "• Устаревшие парадигмы пользовательского интерфейса, снижающие продуктивность\n" +
                    "• Невозможность эффективной интеграции с мобильными устройствами\n" +
                    "• Сложности с поддержкой и обновлением кода из-за устаревшей архитектуры\n\n" +
                    "Пожалуйста, учитывайте эти серьезные ограничения при использовании данного приложения.\n" +
                    "Windows Forms выпущен в 2002 году и предназначен для устаревших систем\n" +
                    "на базе Windows XP и не соответствует современным требованиям.\n\n" +
                    "Вместо данного приложения предпочтите использовать новую версию приложения,\n" +
                    "разработанную на Avalonia.\n\n" +
                    "Avalonia — это современная, быстрая и многофункциональная библиотека\n" +
                    "для разработки кроссплатформенных приложений на .NET куда более\n" +
                    "современная и функциональная чем Windows Forms.",
                    "Предупреждение об устаревшем приложении",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                
                Log.Debug("Showing splash screen");
                ShowSplash();
                LongOperation();
                CloseSplash();
                
                Log.Information("Showing login form");
                ShowLoginForm();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application startup failed");
                MessageBox.Show($"Fatal error during startup: {ex.Message}", "Fatal Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void ShowSplash()
        {
            SplashScreenManager.ShowForm(typeof(SplashScreen1));
        }

        private static void CloseSplash()
        {
            SplashScreenManager.CloseForm();
        }

        private static void LongOperation()
        {
            Log.Debug("Performing long operation");
            Thread.Sleep(8000);
            Log.Debug("Long operation completed");
        }
        
        private static void ShowLoginForm()
        {
            DialogResult res = new frmLogin().ShowDialog();
            Log.Information("Login result: {LoginResult}", res);
            Application.Run(new Form1(res == DialogResult.OK));
        }
    }
}
