using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.UserSkins;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;
using NLog;
using NLog.Config;
using NLog.Targets;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Media; // Added for SoundPlayer

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    internal static class Program
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private const string CrashReportArg = "--crashreport";

        private const string TestCrashArg = "--testcrash";

        // add argument for full screen post of sales mode
        private const string FullScreenPosArg = "--fullscreenpos";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) // Modified to accept args
        {
            ConfigureLogging(); // Extracted logging configuration

            // Check if running in crash report mode
            if (args.Length > 1 && args[0] == CrashReportArg)
            {
                HandleCrashReportMode(args[1]);
                return; // Exit after handling crash report
            }

            // Normal application startup flow
            try
            {
                Log.Info("Application starting up in normal mode");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Setup global exception handlers
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += Application_ThreadException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;




                // Initialize DevExpress skins and styles
                BonusSkins.Register();
                SkinManager.EnableFormSkins();
                WindowsFormsSettings.EnableMdiFormSkins();

                // Set default DevExpress style
                UserLookAndFeel.Default.SetSkinStyle("DevExpress Style");

                ShowLegacyWarning(); // Extracted warning message


                // Only throw test exception if --testcrash argument is provided
                if (args.Contains(TestCrashArg))
                {
                    Log.Info("Deliberately throwing exception for testing crash handler...");
                    throw new InvalidOperationException("Test Crash - Ignore This");
                }

                Log.Debug("Showing splash screen");
                ShowSplash();
                LongOperation();
                CloseSplash();

                Log.Info("Showing login form");
                ShowLoginForm();
            }
            catch (Exception ex)
            {
                // Catch startup exceptions specifically if handlers not yet fully attached
                string fatalMsg = string.Format("Application startup failed critically before exception handlers fully attached. Exception: {0}", ex.ToString());
                Log.Fatal(fatalMsg);
                HandleFatalException(ex); // Attempt to use the handler even for startup errors
            }
            finally
            {
                // Ensure NLog is shutdown cleanly
                LogManager.Shutdown();
            }
        }

        private static void ConfigureLogging()
        {
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory); // Ensure log directory exists

            var config = new LoggingConfiguration();

            // Create targets
            var consoleTarget = new ColoredConsoleTarget()
            {
                Name = "console",
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss.fff zzz} [${level}] (${threadid}) ${message}${onexception:${newline}${exception:format=tostring}}",
              
            };

            var fileTarget = new FileTarget()
            {
                FileName = Path.Combine(logDirectory, "app-${shortdate}.log"),
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss.fff zzz} [${level}] (${machinename}/${threadid}) ${message}${onexception:${newline}${exception:format=tostring}}",
                ArchiveNumbering = ArchiveNumberingMode.Date,
                MaxArchiveFiles = 31,
                ArchiveEvery = FileArchivePeriod.Day,
                Encoding = System.Text.Encoding.UTF8 // Use System.Text.Encoding for UTF-8
            };

            // Add targets to configuration
            config.AddTarget("console", consoleTarget);
            config.AddTarget("file", fileTarget);

            // Set up rules
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, consoleTarget));
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, fileTarget));

            // Apply config
            LogManager.Configuration = config;
        }

        private static void ShowLegacyWarning()
        {
                // Show disclaimer about legacy Windows Forms application in Russian
                XtraMessageBox.Show(
                    "Информация о системе:\n" +
                    string.Format("ОС: {0}\n", Environment.OSVersion.VersionString) +
                    string.Format(".NET: {0}\n\n", Environment.Version) +
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
            if (Environment.GetCommandLineArgs().Contains(FullScreenPosArg))
            {
                POSLogin loginForm = new POSLogin();
                DialogResult res = loginForm.ShowDialog();
                Log.Info(string.Format("Login result: {0}", res));
                if (res == DialogResult.OK) 
                {
                    Application.Run(new Form1(true));
                }
                else
                {
                    Log.Warn("Login cancelled or failed. Exiting application.");
                    Application.Exit(); 
                }
            }
            else
        {
            frmLogin loginForm = new frmLogin();
            DialogResult res = loginForm.ShowDialog();
                Log.Info(string.Format("Login result: {0}", res));
            if (res == DialogResult.OK) 
            {
                Application.Run(new Form1(true));
            }
            else
            {
                 Log.Warn("Login cancelled or failed. Exiting application.");
                 Application.Exit(); 
            }
        }
        }

        // --- Crash Handling Logic ---

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            string fatalMsg = string.Format("Unhandled Domain Exception. IsTerminating: {0}. Exception: {1}", e.IsTerminating, ex.ToString());
            Log.Fatal(fatalMsg);

            // Log additional details about the exception
            Log.Fatal("Exception Type: " + ex.GetType().FullName);
            Log.Fatal("Exception Message: " + ex.Message);
            Log.Fatal("Exception Source: " + ex.Source);
            Log.Fatal("Exception Stack Trace: " + ex.StackTrace);

            if (ex.TargetSite != null)
            {
                Log.Fatal("Exception Target Site: " + ex.TargetSite.ToString());
                Log.Fatal("Exception Target Site Module: " + (ex.TargetSite.Module != null ? ex.TargetSite.Module.Name : "Unknown"));
                Log.Fatal("Exception Target Site Class: " + (ex.TargetSite.DeclaringType != null ? ex.TargetSite.DeclaringType.FullName : "Unknown"));
            }

            HandleFatalException(ex);
            // Ensure termination if the exception is fatal
            if (e.IsTerminating)
            {
                LogManager.Shutdown(); // Attempt to flush logs
                Environment.Exit(1); // Force exit
            }
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            string errorMsg = string.Format("Unhandled Thread Exception: {0}", ex.ToString());
            Log.Error(errorMsg);

            // Log additional details about the thread exception
            Log.Error("Thread Exception Type: " + ex.GetType().FullName);
            Log.Error("Thread Exception Message: " + ex.Message);
            Log.Error("Thread Exception Source: " + ex.Source);
            Log.Error("Thread Exception Stack Trace: " + ex.StackTrace);

            if (ex.TargetSite != null)
            {
                Log.Error("Thread Exception Target Site: " + ex.TargetSite.ToString());
                Log.Error("Thread Exception Target Site Module: " + (ex.TargetSite.Module != null ? ex.TargetSite.Module.Name : "Unknown"));
            }

            // Log current thread information
            Log.Error("Current Thread ID: " + Thread.CurrentThread.ManagedThreadId);
            Log.Error("Current Thread Name: " + (string.IsNullOrEmpty(Thread.CurrentThread.Name) ? "Unnamed" : Thread.CurrentThread.Name));
            Log.Error("Current Thread State: " + Thread.CurrentThread.ThreadState.ToString());
            Log.Error("Current Thread Priority: " + Thread.CurrentThread.Priority.ToString());

            HandleFatalException(ex);
        }

        private static void HandleFatalException(Exception ex)
        {
            // 1. Log the exception (already done by callers, but log context here too)
            string logMsg = string.Format("HandleFatalException started. Attempting to restart in crash report mode. Exception: {0}", ex.ToString());
            Log.Fatal(logMsg);

            // Log system environment information for diagnostics
            Log.Fatal("OS Version: " + Environment.OSVersion.ToString());
            Log.Fatal(".NET Runtime Version: " + Environment.Version.ToString());
            Log.Fatal("64-bit OS: " + Environment.Is64BitOperatingSystem);
            Log.Fatal("64-bit Process: " + Environment.Is64BitProcess);
            Log.Fatal("Machine Name: " + Environment.MachineName);
            Log.Fatal("Processor Count: " + Environment.ProcessorCount);
            Log.Fatal("System Directory: " + Environment.SystemDirectory);
            Log.Fatal("User Domain Name: " + Environment.UserDomainName);
            Log.Fatal("User Name: " + Environment.UserName);
            Log.Fatal("Working Set Memory: " + Environment.WorkingSet + " bytes");
            Log.Fatal("Current Directory: " + Environment.CurrentDirectory);

            // Log application domain information
            Log.Fatal("AppDomain ID: " + AppDomain.CurrentDomain.Id);
            Log.Fatal("AppDomain Base Directory: " + AppDomain.CurrentDomain.BaseDirectory);
            

            // 2. Create a text-based crash report file
            string crashReportPath = Path.Combine(Path.GetTempPath(), string.Format("crash_{0}.txt", Guid.NewGuid()));
            try
            {
                // Create a detailed crash report as plain text
                System.Text.StringBuilder reportBuilder = new System.Text.StringBuilder();
                reportBuilder.AppendLine("=== CRASH REPORT ===");
                reportBuilder.AppendLine("Timestamp: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                // System Information
                reportBuilder.AppendLine("\r\n=== SYSTEM INFORMATION ===");
                reportBuilder.AppendLine("OS Version: " + Environment.OSVersion.ToString());
                reportBuilder.AppendLine("Runtime Version: " + Environment.Version.ToString());
                reportBuilder.AppendLine("64-bit OS: " + Environment.Is64BitOperatingSystem);
                reportBuilder.AppendLine("64-bit Process: " + Environment.Is64BitProcess);
                reportBuilder.AppendLine("Machine Name: " + Environment.MachineName);
                reportBuilder.AppendLine("Processor Count: " + Environment.ProcessorCount);
                reportBuilder.AppendLine("System Directory: " + Environment.SystemDirectory);
                reportBuilder.AppendLine("User Domain Name: " + Environment.UserDomainName);
                reportBuilder.AppendLine("User Name: " + Environment.UserName);
                reportBuilder.AppendLine("Working Set Memory: " + Environment.WorkingSet + " bytes");
                reportBuilder.AppendLine("Current Directory: " + Environment.CurrentDirectory);

                // AppDomain Information
                reportBuilder.AppendLine("\r\n=== APPDOMAIN INFORMATION ===");
                reportBuilder.AppendLine("AppDomain ID: " + AppDomain.CurrentDomain.Id);
                reportBuilder.AppendLine("AppDomain Base Directory: " + AppDomain.CurrentDomain.BaseDirectory);
              

                // Thread Information
                reportBuilder.AppendLine("\r\n=== THREAD INFORMATION ===");
                reportBuilder.AppendLine("Thread ID: " + Thread.CurrentThread.ManagedThreadId);
                reportBuilder.AppendLine("Thread Name: " + (string.IsNullOrEmpty(Thread.CurrentThread.Name) ? "Unnamed" : Thread.CurrentThread.Name));
                reportBuilder.AppendLine("Thread State: " + Thread.CurrentThread.ThreadState.ToString());
                reportBuilder.AppendLine("Thread Priority: " + Thread.CurrentThread.Priority.ToString());

                // Exception Information
                reportBuilder.AppendLine("\r\n=== EXCEPTION DETAILS ===");
                Exception currentEx = ex;
                int exceptionLevel = 0;

                while (currentEx != null)
                {
                    reportBuilder.AppendLine("\r\nException Level " + exceptionLevel + ":");
                    reportBuilder.AppendLine("Type: " + currentEx.GetType().FullName);
                    reportBuilder.AppendLine("Message: " + currentEx.Message);
                    reportBuilder.AppendLine("Source: " + (currentEx.Source ?? "Unknown"));

                    if (currentEx.TargetSite != null)
                    {
                        reportBuilder.AppendLine("Target Site: " + currentEx.TargetSite.ToString());
                        reportBuilder.AppendLine("Target Site Module: " + (currentEx.TargetSite.Module != null ? currentEx.TargetSite.Module.Name : "Unknown"));
                        reportBuilder.AppendLine("Target Site Class: " + (currentEx.TargetSite.DeclaringType != null ? currentEx.TargetSite.DeclaringType.FullName : "Unknown"));
                    }
                    else
                    {
                        reportBuilder.AppendLine("Target Site: Unknown");
                    }

                    // Add exception data dictionary if present
                    if (currentEx.Data != null && currentEx.Data.Count > 0)
                    {
                        reportBuilder.AppendLine("Data Items:");
                        foreach (System.Collections.DictionaryEntry entry in currentEx.Data)
                        {
                            reportBuilder.AppendLine("    " + (entry.Key != null ? entry.Key.ToString() : "null") +
                                " = " + (entry.Value != null ? entry.Value.ToString() : "null"));
                        }
                    }

                    reportBuilder.AppendLine("Stack Trace:");
                    reportBuilder.AppendLine(currentEx.StackTrace ?? "No stack trace available");

                    currentEx = currentEx.InnerException;
                    exceptionLevel++;
                }

                // Loaded Assemblies
                reportBuilder.AppendLine("\r\n=== LOADED ASSEMBLIES ===");
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        reportBuilder.AppendLine(assembly.FullName);
                        reportBuilder.AppendLine("    Location: " + assembly.Location);
                        reportBuilder.AppendLine("    Version: " + assembly.GetName().Version.ToString());
                    }
                    catch (Exception asmEx)
                    {
                        reportBuilder.AppendLine("    [Error retrieving assembly info: " + asmEx.Message + "]");
                    }
                }

                // Write the report to a text file
                File.WriteAllText(crashReportPath, reportBuilder.ToString());
                Log.Info(string.Format("Detailed crash report saved to: {0}", crashReportPath));
            }
            catch (Exception serializationEx)
            {
                string errorMsg = string.Format("Failed to create crash report file. Exception: {0}", serializationEx.ToString());
                Log.Error(errorMsg);

                // Log inner exceptions if present
                Exception innerEx = serializationEx.InnerException;
                int innerLevel = 1;
                while (innerEx != null)
                {
                    Log.Error(string.Format("Inner Exception Level {0}: {1}", innerLevel, innerEx.ToString()));
                    Log.Error(string.Format("Inner Exception {0} Stack Trace: {1}", innerLevel, innerEx.StackTrace));
                    innerEx = innerEx.InnerException;
                    innerLevel++;
                }

                // Display a very basic error if serialization fails
                ShowBasicCrashError(ex);
                LogManager.Shutdown();
                Environment.Exit(1);
                return; // Exit
            }

            // 3. Restart the application in crash report mode
            try
            {
                // C# 4.0 compatible way to get entry assembly location
                Assembly entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly == null)
                {
                    throw new InvalidOperationException("Could not get entry assembly.");
                }
                string appPath = entryAssembly.Location;
                if (string.IsNullOrEmpty(appPath))
                {
                    throw new InvalidOperationException("Could not determine application executable path from entry assembly.");
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = appPath,
                    Arguments = string.Format("{0} \"{1}\"", CrashReportArg, crashReportPath), // Pass arg and path
                    UseShellExecute = true // Use ShellExecute to handle potential UAC or path issues
                };

                Log.Info(string.Format("Attempting to restart in crash mode: {0} {1}", startInfo.FileName, startInfo.Arguments));
                Process.Start(startInfo);
            }
            catch (Exception restartEx)
            {
                string errorMsg = string.Format("Failed to restart application in crash report mode. Restart Exception: {0}", restartEx.ToString());
                Log.Error(errorMsg);

                // Log detailed stack trace
                Log.Error(string.Format("Restart Exception Stack Trace: {0}", restartEx.StackTrace));

                // Log detailed information about inner exceptions
                Exception innerEx = restartEx.InnerException;
                int innerLevel = 1;
                while (innerEx != null)
                {
                    Log.Error(string.Format("Inner Exception Level {0}: {1}", innerLevel, innerEx.ToString()));
                    Log.Error(string.Format("Inner Exception {0} Stack Trace: {1}", innerLevel, innerEx.StackTrace));
                    Log.Error(string.Format("Inner Exception {0} Source: {1}", innerLevel, innerEx.Source));
                    Log.Error(string.Format("Inner Exception {0} Target Site: {1}", innerLevel,
                        innerEx.TargetSite != null ? innerEx.TargetSite.ToString() : "null"));

                    // Log any additional exception data
                    if (innerEx.Data != null && innerEx.Data.Count > 0)
                    {
                        Log.Error(string.Format("Inner Exception {0} Additional Data:", innerLevel));
                        foreach (System.Collections.DictionaryEntry entry in innerEx.Data)
                        {
                            Log.Error(string.Format("    Key: {0}, Value: {1}",
                                entry.Key != null ? entry.Key.ToString() : "null",
                                entry.Value != null ? entry.Value.ToString() : "null"));
                        }
                    }

                    innerEx = innerEx.InnerException;
                    innerLevel++;
                }

                ShowBasicCrashError(ex, "Additionally, failed to launch the crash reporter.");
            }
            finally
            {
                // 4. Shutdown logger and exit current (crashing) instance
                LogManager.Shutdown();
                Environment.Exit(1); // Use Environment.Exit for a more forceful exit if needed
            }
        }

        private static void HandleCrashReportMode(string crashReportPath)
        {
            Log.Info(string.Format("Application started in crash report mode. Report path: {0}", crashReportPath));
            string errorMessage = "An unexpected error occurred.";
            string detailedReport = "";

            // --- Play Crash Sound ---
            try
            {
                Assembly executingAssembly = Assembly.GetExecutingAssembly();
                // IMPORTANT: The resource name is Namespace.FileName
                string resourceName = "TicketSalesApp.UI.LegacyForms.DX.Windows.crash.wav"; 
                using (Stream resourceStream = executingAssembly.GetManifestResourceStream(resourceName))
                {
                    if (resourceStream != null)
                    {
                        using (SoundPlayer player = new SoundPlayer(resourceStream))
                        {
                            player.Play(); // Play asynchronously
                            Log.Info("Playing crash sound.");
                        }
                    }
                    else
                    {
                        Log.Warn(string.Format("Crash sound resource not found: {0}", resourceName));
                    }
                }
            }
            catch (Exception soundEx)
            {
                Log.Error(string.Format("Failed to play crash sound. Exception: {0}", soundEx.ToString()));
            }
            // --- End Play Crash Sound ---

            // Attempt to read the crash report file
            try
            {
                if (File.Exists(crashReportPath))
                {
                    // Read the text file
                    detailedReport = File.ReadAllText(crashReportPath);
                    Log.Info("Crash report loaded successfully");

                    // Clean up the temporary file
                    try { File.Delete(crashReportPath); }
                    catch (Exception deleteEx)
                    {
                        string warnMsg = string.Format("Failed to delete crash report file: {0}. Deletion Exception: {1}", crashReportPath, deleteEx.ToString());
                        Log.Warn(warnMsg);
                    }
                }
                else
                {
                    errorMessage = "Crash report file not found.";
                    Log.Error(errorMessage + string.Format(" Path: {0}", crashReportPath));
                    detailedReport = "Crash report file not found at: " + crashReportPath;
                }
            }
            catch (Exception readEx)
            {
                errorMessage = "Failed to read crash report details.";
                string readErrorMsg = string.Format("Failed to read crash report details from path: {0}. Exception: {1}", crashReportPath, readEx.ToString());
                Log.Error(readErrorMsg);
                detailedReport = "Error reading crash report: " + readEx.ToString();
            }

            // Display the crash report information using a dedicated form
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Initialize DevExpress skins and styles
                BonusSkins.Register();
                SkinManager.EnableFormSkins();
                WindowsFormsSettings.EnableMdiFormSkins();

                // Set default DevExpress style
                UserLookAndFeel.Default.SetSkinStyle("DevExpress Style");

                // Create crash report form dynamically with Windows XP inspired styling
                XtraForm crashReportForm = new XtraForm();
                crashReportForm.Text = "Application Crash Report";
                crashReportForm.Size = new System.Drawing.Size(650, 520);
                crashReportForm.StartPosition = FormStartPosition.CenterScreen;
                crashReportForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                crashReportForm.MaximizeBox = false;
                crashReportForm.MinimizeBox = false;

                // Create a gradient panel for the header (XP style)
                Panel headerPanel = new Panel();
                headerPanel.Dock = DockStyle.Top;
                headerPanel.Height = 60;
                headerPanel.Paint += (sender, e) =>
                {
                    // Create a gradient brush for XP-style header
                    using (System.Drawing.Drawing2D.LinearGradientBrush brush =
                        new System.Drawing.Drawing2D.LinearGradientBrush(
                            headerPanel.ClientRectangle,
                            System.Drawing.Color.FromArgb(0, 0, 170),  // Dark blue
                            System.Drawing.Color.FromArgb(100, 150, 255), // Light blue
                            System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                    {
                        e.Graphics.FillRectangle(brush, headerPanel.ClientRectangle);
                    }
                };

                // Add error icon to header
                PictureBox errorIcon = new PictureBox();
                errorIcon.Size = new System.Drawing.Size(32, 32);
                errorIcon.Location = new System.Drawing.Point(15, 14);
                errorIcon.Image = SystemIcons.Error.ToBitmap();
                errorIcon.SizeMode = PictureBoxSizeMode.StretchImage;
                errorIcon.BackColor = System.Drawing.Color.Transparent; // Make background transparent
                headerPanel.Controls.Add(errorIcon);

                // Add title to header
                Label headerLabel = new Label();
                headerLabel.Text = "Application Error";
                headerLabel.ForeColor = System.Drawing.Color.White; // White text for visibility on dark gradient
                headerLabel.BackColor = System.Drawing.Color.Transparent; // Make background transparent
                headerLabel.Font = new System.Drawing.Font("Tahoma", 14, System.Drawing.FontStyle.Bold);
                headerLabel.Location = new System.Drawing.Point(60, 18);
                headerLabel.AutoSize = true;
                headerPanel.Controls.Add(headerLabel);

                // Create content panel
                Panel contentPanel = new Panel();
                contentPanel.Dock = DockStyle.Fill;
                contentPanel.Padding = new Padding(20);

                // Create controls
                Label titleLabel = new Label();
                titleLabel.Text = "The application encountered a fatal error and had to close.";
                titleLabel.Font = new System.Drawing.Font("Tahoma", 9, System.Drawing.FontStyle.Bold);
                titleLabel.Location = new System.Drawing.Point(20, 15);
                titleLabel.Size = new System.Drawing.Size(580, 20);

                // Extract error type from the report if possible
                string extractedErrorType = "Unknown Error";
                if (detailedReport.Contains("Type: "))
                {
                    int typeIndex = detailedReport.IndexOf("Type: ");
                    if (typeIndex >= 0)
                    {
                        int endIndex = detailedReport.IndexOf("\r\n", typeIndex);
                        if (endIndex > typeIndex)
                        {
                            extractedErrorType = detailedReport.Substring(typeIndex + 6, endIndex - typeIndex - 6);
                        }
                    }
                }

                Label typeLabel = new Label();
                typeLabel.Text = "Error Type: " + extractedErrorType;
                typeLabel.Font = new System.Drawing.Font("Tahoma", 8.25F);
                typeLabel.Location = new System.Drawing.Point(20, 45);
                typeLabel.Size = new System.Drawing.Size(580, 20);

                // Extract error message from the report if possible
                string extractedErrorMsg = errorMessage;
                if (detailedReport.Contains("Message: "))
                {
                    int msgIndex = detailedReport.IndexOf("Message: ");
                    if (msgIndex >= 0)
                    {
                        int endIndex = detailedReport.IndexOf("\r\n", msgIndex);
                        if (endIndex > msgIndex)
                        {
                            extractedErrorMsg = detailedReport.Substring(msgIndex + 9, endIndex - msgIndex - 9);
                        }
                    }
                }

                Label messageLabel = new Label();
                messageLabel.Text = "Message: " + extractedErrorMsg;
                messageLabel.Font = new System.Drawing.Font("Tahoma", 8.25F);
                messageLabel.Location = new System.Drawing.Point(20, 75);
                messageLabel.Size = new System.Drawing.Size(580, 40);
                messageLabel.AutoSize = false;

                Label stackTraceLabel = new Label();
                stackTraceLabel.Text = "Crash Report Details:";
                stackTraceLabel.Font = new System.Drawing.Font("Tahoma", 8.25F);
                stackTraceLabel.Location = new System.Drawing.Point(20, 125);
                stackTraceLabel.Size = new System.Drawing.Size(580, 20);

                TextBox stackTraceTextBox = new TextBox();
                stackTraceTextBox.Multiline = true;
                stackTraceTextBox.ScrollBars = ScrollBars.Vertical;
                stackTraceTextBox.ReadOnly = true;

                stackTraceTextBox.Text = detailedReport;
                stackTraceTextBox.Location = new System.Drawing.Point(20, 150);
                stackTraceTextBox.Size = new System.Drawing.Size(590, 230);
                stackTraceTextBox.Font = new System.Drawing.Font("Consolas", 8.25F);
                stackTraceTextBox.BorderStyle = BorderStyle.Fixed3D;

                // Create button panel with light gray background (XP style)
                Panel buttonPanel = new Panel();
                buttonPanel.Dock = DockStyle.Bottom;
                buttonPanel.Height = 50;
                buttonPanel.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);

                Button yesButton = new Button();
                yesButton.Text = "Restart Application";
                yesButton.DialogResult = DialogResult.Yes;
                yesButton.Location = new System.Drawing.Point(350, 10);
                yesButton.Size = new System.Drawing.Size(130, 30);
                yesButton.FlatStyle = FlatStyle.System;
                yesButton.Font = new System.Drawing.Font("Tahoma", 8.25F);

                Button noButton = new Button();
                noButton.Text = "Exit";
                noButton.DialogResult = DialogResult.No;
                noButton.Location = new System.Drawing.Point(490, 10);
                noButton.Size = new System.Drawing.Size(130, 30);
                noButton.FlatStyle = FlatStyle.System;
                noButton.Font = new System.Drawing.Font("Tahoma", 8.25F);

                // Add controls to panels
                contentPanel.Controls.Add(titleLabel);
                contentPanel.Controls.Add(typeLabel);
                contentPanel.Controls.Add(messageLabel);
                contentPanel.Controls.Add(stackTraceLabel);
                contentPanel.Controls.Add(stackTraceTextBox);

                buttonPanel.Controls.Add(yesButton);
                buttonPanel.Controls.Add(noButton);

                // Add panels to form
                crashReportForm.Controls.Add(contentPanel);
                crashReportForm.Controls.Add(buttonPanel);
                crashReportForm.Controls.Add(headerPanel);

                // Show the form
                Log.Info("Displaying crash report form to user");
                DialogResult result = crashReportForm.ShowDialog();

                if (result == DialogResult.Yes)
                {
                    Log.Info("User chose to restart the application from crash report mode.");
                    try
                    {
                        // C# 4.0 compatible way to get entry assembly location
                        Assembly entryAssembly = Assembly.GetEntryAssembly();
                        if (entryAssembly == null)
                        {
                            Log.Error("Could not get entry assembly to restart application.");
                            XtraMessageBox.Show("Failed to determine the application path for restarting.",
                                "Restart Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            string appPath = entryAssembly.Location;
                            if (!string.IsNullOrEmpty(appPath))
                            {
                                ProcessStartInfo startInfo = new ProcessStartInfo
                                {
                                    FileName = appPath,
                                    UseShellExecute = true
                                };
                                Process.Start(startInfo);
                            }
                            else
                            {
                                Log.Error("Could not determine application path from entry assembly to restart.");
                                XtraMessageBox.Show("Failed to determine the application path for restarting.",
                                    "Restart Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    catch (Exception restartEx)
                    {
                        string restartErrorMsg = string.Format("Failed to restart application from crash report mode. Restart Exception: {0}", restartEx.ToString());
                        Log.Error(restartErrorMsg);
                        XtraMessageBox.Show(string.Format("Failed to restart the application.\nError: {0}", restartEx.Message),
                            "Restart Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    Log.Info("User chose not to restart the application from crash report mode.");
                }
            }
            catch (Exception formEx)
            {
                // Fallback to basic message box if the form fails
                Log.Error("Failed to display crash report form: " + formEx.ToString());

                // Using string concatenation for C# 4 compatibility
                string fullErrorMessage = "The application encountered a fatal error and had to close.\n\n" +
                                          "Error Type: Unknown\n" +
                                          "Message: " + errorMessage + "\n\n" +
                                          "Stack Trace:\n" + detailedReport + "\n\n" +
                                          "Would you like to try restarting the application?";

                DialogResult result = XtraMessageBox.Show(fullErrorMessage,
                                                         "Application Crash Report",
                                                         MessageBoxButtons.YesNo,
                                                         MessageBoxIcon.Error);

                // Handle restart logic same as above
                if (result == DialogResult.Yes)
                {
                    // Restart logic (same as above)
                    try
                    {
                        Assembly entryAssembly = Assembly.GetEntryAssembly();
                        if (entryAssembly != null && !string.IsNullOrEmpty(entryAssembly.Location))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = entryAssembly.Location,
                                UseShellExecute = true
                            });
                        }
                    }
                    catch (Exception restartEx)
                    {
                        Log.Error("Fallback restart failed: " + restartEx.ToString());
                    }
                }
            }

            // Crash report mode finishes here
            LogManager.Shutdown();
            Environment.Exit(0); // Clean exit from crash reporter
        }

        private static void ShowBasicCrashError(Exception ex, string additionalInfo = null) // Note: default parameter value is C# 4.0 feature
        {
            // Using string.Format and conditional operator for C# 4 compatibility
            string message = string.Format("A critical error occurred: {0}\n\n{1}\n\nThe application will now exit.",
                                           ex != null ? ex.Message : "N/A",
                                           additionalInfo ?? ""); // Note: ?? operator IS available in C# 4.0
            // Use basic MessageBox as DevExpress might not be initialized or could be part of the problem
            MessageBox.Show(message, "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }
    }
}
