using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using Microsoft.Win32.SafeHandles;
using System.Windows.Media;
using System.Collections.ObjectModel;
using IOPath = System.IO.Path;
using System.Text;
using Npgsql;
using System.Linq;
using Serilog;

namespace FireLog
{
    public partial class MainWindow : Window
    {
        private FirewallLogParser _logParser;
        private string _selectedDataSource = "Local"; // Default to Local
        private string _selectedChartType = "Pie Chart"; // Default chart type
        private List<Process> _runningProcesses = new List<Process>(); // Track running processes
        private ObservableCollection<FirewallLogEntry> _originalLogEntries; // Store original log entries for filtering
        private bool _dbAvailable = false; //Availability of ENV or db_credentials.json

        private void InitAppMode()
        {
            _dbAvailable = DetectDbAvailability();
            Serilog.Context.LogContext.PushProperty("SessionId", App.SessionId);
            Serilog.Context.LogContext.PushProperty("Mode", _dbAvailable ? "Online" : "Offline");
        }

        private bool DetectDbAvailability()
        {
            try
            {
                var host = Environment.GetEnvironmentVariable("APP_DB_HOST");
                if (!string.IsNullOrWhiteSpace(host)) return true;

                var jsonPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "db_credentials.json");
                return File.Exists(jsonPath);
            }
            catch
            {
                return false;
            }
        }
        

        private bool EnsureDb(string featureName)
        {
            if (!_dbAvailable)
            {
                MessageBox.Show(
                    $"{featureName} is unavailable in Offline mode.\n" +
                    "To enable DB features: create your own Postgres/Supabase and fill db_credentials.json.",
                    "Database disabled", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            return true;
        }
        private bool _initializing;
        private bool _changingSelection;         // reentry protection
        private bool _shownOfflineDbTip = false; // Anti MessageBox-Spam

        public MainWindow(string username)
        {
            _initializing = true;
            _dbAvailable = DetectDbAvailability();
            InitializeComponent();
            InitAppMode(); // <-- offline/online mode

            ShowUsername(username);
            // Add closing event handler
            this.Closing += Window_Closing;

            // Initialize parser
            _logParser = new FirewallLogParser();
            _originalLogEntries = new ObservableCollection<FirewallLogEntry>();

            // Set DataGrid for displaying logs
            DataContext = _logParser;

            // Assign collection to DataGrid
            var dataGrid = FindName("LogsDataGrid") as DataGrid;
            if (dataGrid != null)
            {
                dataGrid.ItemsSource = _logParser.LogEntries;
            }

            // Set statistical textblocks
            var totalCountText = FindName("TotalCountText") as TextBlock;
            var allowedCountText = FindName("AllowedCountText") as TextBlock;
            var blockedCountText = FindName("BlockedCountText") as TextBlock;

            if (totalCountText != null && allowedCountText != null && blockedCountText != null)
            {
                totalCountText.SetBinding(TextBlock.TextProperty, "TotalCount");
                allowedCountText.SetBinding(TextBlock.TextProperty, "AllowedCount");
                blockedCountText.SetBinding(TextBlock.TextProperty, "BlockedCount");
            }
            ApplyModeToUi();
            // Initalize default chart
            UpdateChart();
            _initializing = false;
        }
        private void ApplyModeToUi()
        {
            if (FindName("UploadToDbButton") is Button uploadBtn) uploadBtn.IsEnabled = _dbAvailable;
            if (FindName("LoginPanel") is FrameworkElement loginPanel) loginPanel.Visibility = _dbAvailable ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("StatusBarText") is TextBlock status) status.Text = _dbAvailable ? "Mode: Online (DB enabled)" : "Mode: Offline (no DB)";

            if (FindName("DataSourceCombo") is ComboBox ds)
            {
                foreach (var it in ds.Items.OfType<ComboBoxItem>())
                {
                    if (string.Equals(it.Content?.ToString(), "Database", StringComparison.OrdinalIgnoreCase))
                        it.IsEnabled = _dbAvailable;
                }
                if (!_dbAvailable)
                {
                    var localItem = ds.Items.OfType<ComboBoxItem>()
                        .FirstOrDefault(i => string.Equals(i.Content?.ToString(), "Local", StringComparison.OrdinalIgnoreCase));
                    if (localItem != null) ds.SelectedItem = localItem;
                }
            }
        }

        // Event handler for window closing
        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Debug.WriteLine("Window closing - cleaning up resources");
                
                // Terminate all tracked processes
                foreach (var process in _runningProcesses)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            Debug.WriteLine($"Terminating process: {process.Id}");
                            process.Kill();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error terminating process: {ex.Message}");
                    }
                }
                // Clear the list
                _runningProcesses.Clear();
                // Kill any remaining FireLog processes (in case we missed some)
                try
                {
                    var processName = Process.GetCurrentProcess().ProcessName;
                    Debug.WriteLine($"Looking for other instances of {processName}");
                    foreach (var proc in Process.GetProcessesByName(processName))
                    {
                        if (proc.Id != Process.GetCurrentProcess().Id)
                        {
                            Debug.WriteLine($"Terminating other instance: {proc.Id}");
                            proc.Kill();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error cleaning up other processes: {ex.Message}");
                }
                Debug.WriteLine("Cleanup complete");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in Window_Closing: {ex}");
            }
        }
        
        private void ApplyFilters()
        {
            try
            {
                Debug.WriteLine("ApplyFilters called");

                // Get filter values
                string? ipFilter = (FindName("IpAddressFilter") as TextBox)?.Text;
                DateTime? fromDate = (FindName("TimeFromFilter") as DatePicker)?.SelectedDate;
                DateTime? toDate = (FindName("TimeToFilter") as DatePicker)?.SelectedDate;
                string? portFilter = (FindName("PortFilter") as TextBox)?.Text;

                string actionFilter = "All";
                var typeFilter = FindName("TypeFilter") as ComboBox;
                if (typeFilter?.SelectedItem is ComboBoxItem cbi && cbi.Content is string s && !string.IsNullOrWhiteSpace(s))
                {
                    actionFilter = s;
                }

                Debug.WriteLine($"Filters - IP: '{ipFilter}', Port: '{portFilter}', From: {fromDate}, To: {toDate}, Action: {actionFilter}");

                // Make sure we have log entries to filter
                if (_originalLogEntries.Count == 0 && _logParser.LogEntries.Count > 0)
                {
                    // Store original log entries if not already stored
                    foreach (var entry in _logParser.LogEntries)
                    {
                        _originalLogEntries.Add(entry);
                    }
                    Debug.WriteLine($"Stored {_originalLogEntries.Count} original log entries");
                }
                else if (_originalLogEntries.Count == 0)
                {
                    Debug.WriteLine("No log entries to filter");
                    MessageBox.Show("No log entries to filter. Please load logs first.",
                                   "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Apply filters to original log entries
                var filteredEntries = new ObservableCollection<FirewallLogEntry>();
                int allowedCount = 0;
                int blockedCount = 0;

                foreach (var entry in _originalLogEntries)
                {
                    bool includeEntry = true;

                    // Apply date filters
                    if (fromDate.HasValue && entry.Timestamp < fromDate.Value)
                        includeEntry = false;

                    if (toDate.HasValue && entry.Timestamp > toDate.Value)
                        includeEntry = false;

                    // Apply IP filter
                    if (!string.IsNullOrWhiteSpace(ipFilter) &&
    !((entry.SrcIP?.Contains(ipFilter, StringComparison.OrdinalIgnoreCase) ?? false) ||
      (entry.DestIP?.Contains(ipFilter, StringComparison.OrdinalIgnoreCase) ?? false)))
                    {
                        includeEntry = false;
                    }

                    // Apply port filter (new)
                    if (!string.IsNullOrEmpty(portFilter) && int.TryParse(portFilter, out int port) &&
                        entry.SrcPort != port && entry.DestPort != port)
                        includeEntry = false;

                    // Apply action filter
                    if (!string.IsNullOrWhiteSpace(actionFilter) &&
    !actionFilter.Equals("all", StringComparison.OrdinalIgnoreCase) &&
    !TextUtil.ContainsIgnoreCase(entry.Action, actionFilter))
                    {
                        includeEntry = false;
                    }


                    // If the entry meets all filter criteria, include it
                    if (includeEntry)
                    {
                        filteredEntries.Add(entry);

                        // Update statistics
                        if (TextUtil.ContainsIgnoreCase(entry.Action, "allow"))
                            allowedCount++;
                        else if (TextUtil.ContainsIgnoreCase(entry.Action, "drop") ||
                                 TextUtil.ContainsIgnoreCase(entry.Action, "block"))
                            blockedCount++;

                    }
                }

                // Update the log parser with filtered entries
                _logParser.UpdateFilteredEntries(filteredEntries, allowedCount, blockedCount);

                // Update DataGrid
                var dataGrid = FindName("LogsDataGrid") as DataGrid;
                if (dataGrid != null)
                {
                    dataGrid.ItemsSource = _logParser.LogEntries;
                    dataGrid.Items.Refresh();
                }

                // Update list of suspicious activities
                UpdateSuspiciousActivities();

                // Update chart
                UpdateChart();

                Debug.WriteLine($"Filters applied. Total: {_logParser.TotalCount}, Allowed: {_logParser.AllowedCount}, Blocked: {_logParser.BlockedCount}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying filters: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Error in ApplyFilters: {ex}");
            }
        }
        private void btn_export_reports_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_logParser.LogEntries.Count == 0)
                {
                    MessageBox.Show("No data to export. Please load logs first.", "No Data",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var formatDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export Report",
                    Filter = "CSV File|*.csv|Text File|*.txt|All Files|*.*",
                    DefaultExt = "csv",
                    FileName = $"FireLog_Report_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (formatDialog.ShowDialog() == true)
                {
                    string extension = System.IO.Path.GetExtension(formatDialog.FileName).ToLower();

                    if (extension == ".csv")
                    {
                        ExportToCSV(formatDialog.FileName);
                    }
                    else
                    {
                        ExportToText(formatDialog.FileName);
                    }

                    MessageBox.Show($"Report exported successfully to:\n{formatDialog.FileName}",
                                   "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting report: {ex.Message}", "Export Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCSV(string filePath)
        {
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine("Timestamp,Action,Protocol,Source IP,Destination IP,Source Port,Destination Port,User ID");

                foreach (var entry in _logParser.LogEntries)
                {
                    writer.WriteLine($"{entry.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                                   $"{EscapeCSV(entry.Action)}," +
                                   $"{EscapeCSV(entry.Protocol)}," +
                                   $"{EscapeCSV(entry.SrcIP)}," +
                                   $"{EscapeCSV(entry.DestIP)}," +
                                   $"{entry.SrcPort}," +
                                   $"{entry.DestPort}," +
                                   $"{EscapeCSV(entry.UserID)}");
                }

                writer.WriteLine();
                writer.WriteLine("STATISTICS");
                writer.WriteLine($"Total Events,{_logParser.TotalCount}");
                writer.WriteLine($"Allowed Events,{_logParser.AllowedCount}");
                writer.WriteLine($"Blocked Events,{_logParser.BlockedCount}");
                writer.WriteLine($"Export Date,{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
        }

        private void ExportToText(string filePath)
        {
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine("FIRELOG INSIGHT - LOG REPORT");
                writer.WriteLine("=" + new string('=', 50));
                writer.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"Data Source: {_selectedDataSource}");
                writer.WriteLine();

                writer.WriteLine("SUMMARY STATISTICS");
                writer.WriteLine("-" + new string('-', 30));
                writer.WriteLine($"Total Events: {_logParser.TotalCount}");
                writer.WriteLine($"Allowed Events: {_logParser.AllowedCount} ({(double)_logParser.AllowedCount / _logParser.TotalCount * 100:F1}%)");
                writer.WriteLine($"Blocked Events: {_logParser.BlockedCount} ({(double)_logParser.BlockedCount / _logParser.TotalCount * 100:F1}%)");
                writer.WriteLine();

                var topProtocols = _logParser.LogEntries
                    .GroupBy(e => e.Protocol)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .ToList();

                writer.WriteLine("TOP PROTOCOLS");
                writer.WriteLine("-" + new string('-', 30));
                foreach (var protocol in topProtocols)
                {
                    writer.WriteLine($"{protocol.Key}: {protocol.Count()} events");
                }
                writer.WriteLine();

                var suspiciousActivities = _logParser.IdentifySuspiciousActivity();
                if (suspiciousActivities.Count > 0)
                {
                    writer.WriteLine("SECURITY ALERTS");
                    writer.WriteLine("-" + new string('-', 30));
                    foreach (var activity in suspiciousActivities)
                    {
                        writer.WriteLine($"• {activity}");
                    }
                    writer.WriteLine();
                }

                writer.WriteLine("DETAILED LOG ENTRIES");
                writer.WriteLine("-" + new string('-', 50));
                writer.WriteLine($"{"Time",-20} {"Action",-10} {"Protocol",-10} {"Source IP",-15} {"Dest IP",-15} {"Src Port",-8} {"Dest Port",-8}");
                writer.WriteLine(new string('-', 100));

                foreach (var entry in _logParser.LogEntries.Take(1000)) 
                {
                    writer.WriteLine($"{entry.Timestamp:yyyy-MM-dd HH:mm:ss,-20} " +
                                   $"{entry.Action,-10} " +
                                   $"{entry.Protocol,-10} " +
                                   $"{entry.SrcIP,-15} " +
                                   $"{entry.DestIP,-15} " +
                                   $"{entry.SrcPort,-8} " +
                                   $"{entry.DestPort,-8}");
                }

                if (_logParser.LogEntries.Count > 1000)
                {
                    writer.WriteLine($"... and {_logParser.LogEntries.Count - 1000} more entries");
                }
            }
        }

        private static string EscapeCSV(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";

            return value;
        }
        private void UpdateSuspiciousActivities()
        {
            try
            {
                var suspiciousPanel = FindName("SuspiciousPanel") as StackPanel;
                if (suspiciousPanel != null)
                {
                    // Clear existing alerts
                    suspiciousPanel.Children.Clear();

                    // Add header
                    TextBlock headerText = new TextBlock
                    {
                        Text = "Security Alerts",
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White,
                        Margin = new Thickness(0, 0, 0, 10),
                        TextAlignment = TextAlignment.Center,
                        FontSize = 12
                    };
                    suspiciousPanel.Children.Add(headerText);

                    // Get list of suspicious activities
                    var suspiciousActivities = _logParser.IdentifySuspiciousActivity();

                    // If no suspicious activities, show message
                    if (suspiciousActivities.Count == 0)
                    {
                        TextBlock noAlertsText = new TextBlock
                        {
                            Text = "No security alerts detected",
                            Foreground = Brushes.LightGreen,
                            FontSize = 12,
                            Margin = new Thickness(0, 5, 0, 5)
                        };
                        suspiciousPanel.Children.Add(noAlertsText);
                    }
                    else
                    {
                        // Add TextBlocks for each alert
                        foreach (var activity in suspiciousActivities)
                        {
                            var textBlock = new TextBlock
                            {
                                Text = activity,
                                Foreground = Brushes.OrangeRed,
                                FontSize = 12,
                                Margin = new Thickness(0, 5, 0, 5),
                                TextWrapping = TextWrapping.Wrap
                            };
                            suspiciousPanel.Children.Add(textBlock);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating suspicious activities list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Error in UpdateSuspiciousActivities: {ex}");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Load logs when Extract button is clicked
            LoadLogs();
        }

        private void LoadLogs()
        {
            try
            {
                Debug.WriteLine($"Loading logs from source: {_selectedDataSource}");
                
                // Clear original log entries
                _originalLogEntries.Clear();

                // Check selected data source
                if (_selectedDataSource == "Database")
                {
                    try
                    {
                        Guid? userId = AppState.CurrentUserId;
                        if (userId == null)
                        {
                            MessageBox.Show("User is not logged in. Cannot load logs.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        Debug.WriteLine($"Loading logs for user ID: {userId}");

                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                        var logEntries = new ObservableCollection<FirewallLogEntry>();
                        int allowedCount = 0;
                        int blockedCount = 0;

                        using (var conn = new NpgsqlConnection(DbHelper.GetConnectionString()))
                        {
                            conn.Open();
                            Debug.WriteLine("PostgreSQL connection successful");

                            string query = "SELECT * FROM firewall_logs WHERE user_id = @UserID";

                            using (var cmd = new NpgsqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@UserID", userId);

                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        var entry = new FirewallLogEntry
                                        {
                                            Timestamp = reader["timestamp"] != DBNull.Value ?
                                                Convert.ToDateTime(reader["timestamp"]) : DateTime.MinValue,
                                            Action = reader["action"]?.ToString() ?? string.Empty,
                                            Protocol = reader["protocol"]?.ToString() ?? string.Empty,
                                            SrcIP = reader["src_ip"]?.ToString() ?? string.Empty,
                                            DestIP = reader["dst_ip"]?.ToString() ?? string.Empty,
                                            SrcPort = reader["src_port"] != DBNull.Value ?
                                                Convert.ToInt32(reader["src_port"]) : 0,
                                            DestPort = reader["dst_port"] != DBNull.Value ?
                                                Convert.ToInt32(reader["dst_port"]) : 0,
                                            UserID = reader["user_id"]?.ToString() ?? string.Empty
                                        };

                                        logEntries.Add(entry);

                                        if (entry.Action.ToLower().Contains("allow"))
                                            allowedCount++;
                                        else if (entry.Action.ToLower().Contains("drop") || entry.Action.ToLower().Contains("block"))
                                            blockedCount++;
                                    }
                                }
                            }
                        }

                        _logParser.UpdateFilteredEntries(logEntries, allowedCount, blockedCount);

                        var dataGrid = FindName("LogsDataGrid") as DataGrid;
                        if (dataGrid != null)
                        {
                            dataGrid.ItemsSource = _logParser.LogEntries;
                            dataGrid.Items.Refresh();
                        }

                        if (logEntries.Count > 0)
                        {
                            MessageBox.Show($"Successfully loaded {logEntries.Count} log entries from database.",
                                           "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("No log entries found for your user ID in the database.",
                                           "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception dbEx)
                    {
                        Debug.WriteLine($"Database error: {dbEx}");
                        MessageBox.Show($"Error loading logs from database: {dbEx.Message}",
                                       "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                else
                {
                    // Check if file exists
                    string logPath = @"C:\Windows\System32\LogFiles\Firewall\pfirewall.log";
                    
                    // For testing purposes, use the local log file in the project directory
                    string projectLogPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "pfirewall.log");
                    
                    if (File.Exists(projectLogPath))
                    {
                        logPath = projectLogPath;
                        Debug.WriteLine($"Using project log file: {logPath}");
                    }
                    else if (!File.Exists(logPath))
                    {
                        MessageBox.Show($"Log file not found: {logPath}\n\n" +
                                      "Make sure you have sufficient permissions to access the file.",
                                      "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Load logs from local file
                    _logParser.LoadLogs(logPath);
                    MessageBox.Show("Logs successfully loaded from local file.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Store original log entries
                foreach (var entry in _logParser.LogEntries)
                {
                    _originalLogEntries.Add(entry);
                }
                Debug.WriteLine($"Stored {_originalLogEntries.Count} original log entries");

                // Update list of suspicious activities
                UpdateSuspiciousActivities();
                
                // Update chart
                UpdateChart();
                
                Debug.WriteLine($"Logs loaded. Total: {_logParser.TotalCount}, Allowed: {_logParser.AllowedCount}, Blocked: {_logParser.BlockedCount}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Error in LoadLogs: {ex}");
            }
        }

        private void btn_enable_logs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get path to script
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string scriptPath = System.IO.Path.Combine(appDirectory, "enable_firewall_logs.ps1");

                // Check if script exists
                if (!File.Exists(scriptPath))
                {
                    MessageBox.Show("Script file not found:\n" + scriptPath, "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Setup the PowerShell process
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    UseShellExecute = true,
                    Verb = "runas", // Prompt for admin
                    WindowStyle = ProcessWindowStyle.Normal
                };

                var process = Process.Start(psi);
                if (process != null)
                {
                    // Add to tracked processes list
                    _runningProcesses.Add(process);
                    
                    process.WaitForExit(); // Wait for script to finish
                    
                    // Remove from tracked processes list after exit
                    _runningProcesses.Remove(process);
                    
                    if (process.ExitCode == 0)
                    {
                        MessageBox.Show("✅ Firewall logging has been enabled.", "Success",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"⚠️ Script exited with code {process.ExitCode}.", "Warning",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Failed to start the PowerShell process.", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error running script:\n{ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Error in btn_enable_logs_Click: {ex}");
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing) return;
            if (_changingSelection) return;

            var combo = sender as ComboBox;
            if (combo?.SelectedItem == null) return;

            var selectedText =
                (combo.SelectedItem as ComboBoxItem)?.Content?.ToString()
                ?? combo.SelectedItem.ToString()
                ?? string.Empty;

            if (string.Equals(selectedText, "Database", StringComparison.OrdinalIgnoreCase) && !_dbAvailable)
            {
                if (!_shownOfflineDbTip)
                {
                    MessageBox.Show(
                        "Database mode requires valid db_credentials.json or ENV config. Switching to Local.",
                        "Offline mode", MessageBoxButton.OK, MessageBoxImage.Information);
                    _shownOfflineDbTip = true;
                }

                _changingSelection = true;
                try
                {
                    _selectedDataSource = "Local";

                    var localItem = combo.Items
                        .OfType<ComboBoxItem>()
                        .FirstOrDefault(i => string.Equals(i.Content?.ToString(), "Local", StringComparison.OrdinalIgnoreCase));

                    if (localItem != null)
                        combo.SelectedItem = localItem;   
                    else
                        combo.SelectedIndex = Math.Max(0, combo.SelectedIndex - 1); // fallback without null
                }
                finally
                {
                    _changingSelection = false;
                }

                return;
            }

            _selectedDataSource = string.Equals(selectedText, "Database", StringComparison.OrdinalIgnoreCase)
                ? "Database"
                : "Local";
        }


        private async void btn_upload_to_db_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!AuthState.islogined)
                {
                    MessageBox.Show("You must be logged in to upload logs to the database.",
                                    "Authorization Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_logParser.LogEntries == null || !_logParser.LogEntries.Any())
                {
                    MessageBox.Show("No log entries loaded. Please load logs before uploading.",
                                    "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await Task.Run(() =>
                {
                    try
                    {
                        _logParser.SaveLogsToDatabase();
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("Logs successfully uploaded to the database.",
                                            "Upload Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Database upload error: " + ex);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"Error uploading logs:\n{ex.Message}",
                                            "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unexpected error: " + ex);
                MessageBox.Show($"Unexpected error:\n{ex.Message}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateChart()
        {
            try
            {
                Debug.WriteLine($"Updating chart with type: {_selectedChartType}");

                // Check if _logParser is null
                if (_logParser == null)
                {
                    Debug.WriteLine("Log parser is null, initializing new instance");
                    _logParser = new FirewallLogParser();
                }

                var chartContainer = FindName("ChartContainer") as Border;
                if (chartContainer == null)
                {
                    Debug.WriteLine("Chart container not found");
                    return;
                }

                // Clear existing chart
                chartContainer.Child = null;

                // Create chart based on selected type
                UIElement? chart = null;

                switch (_selectedChartType)
                {
                    case "Pie Chart":
                        chart = _logParser.CreatePieChart();
                        break;
                    case "Protocol Distribution":
                        chart = _logParser.CreateProtocolBarChart();
                        break;
                    default:
                        chart = _logParser.CreatePieChart(); // Default to pie chart
                        break;
                }

                if (chart != null)
                {
                    chartContainer.Child = chart;
                    Debug.WriteLine("Chart updated successfully");
                }
                else
                {
                    Debug.WriteLine("Chart creation returned null");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating chart: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Error in UpdateChart: {ex}");
            }
        }

        private void btn_export_chart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("Export chart button clicked");

                if (_logParser.LogEntries.Count == 0)
                {
                    MessageBox.Show("No data to export. Please load logs first.", "No Data",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                // Get chart from container
                var chartContainer = FindName("ChartContainer") as Border;
                if (chartContainer == null || chartContainer.Child == null)
                {
                    MessageBox.Show("No chart available to export.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Export chart to PNG
                BitmapSource? chartImage = _logParser.ExportChartToPng(chartContainer.Child as UIElement);
                if (chartImage == null)
                {
                    MessageBox.Show("Failed to create chart image.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // Create save file dialog
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png",
                    Title = "Save Chart Image",
                    FileName = $"FireLog_Chart_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                };
                
                if (saveDialog.ShowDialog() == true)
                {
                    // Save image to file
                    using (var fileStream = new FileStream(saveDialog.FileName, FileMode.Create))
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(chartImage));
                        encoder.Save(fileStream);
                    }
                    
                    MessageBox.Show($"Chart exported to {saveDialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting chart: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Error in btn_export_chart_Click: {ex}");
            }
        }

        public void ShowUsername(string username)
        {
            txtblock_loginshow.Text = $" User: {username}";
        }

        private void btn_logOut_Click(object sender, RoutedEventArgs e)
        {
            if (_dbAvailable)
            {
                login.Logout();

                var loginWindow = new login();

                Application.Current.MainWindow = loginWindow;
                Log.Information("User logged out");
                loginWindow.Show();
                this.Close(); 
            }
            else
            {
                MessageBox.Show("Offline mode: no authentication. You’re already using local features.",
                    "Offline", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        // Add event handlers for filter buttons
        private void btn_apply_filters_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Apply Filters button clicked");
            ApplyFilters();
        }

        private void btn_reset_filters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("Reset Filters button clicked");

                // Reset filter controls
                var ipAddressFilter = FindName("IpAddressFilter") as TextBox;
                var timeFromFilter = FindName("TimeFromFilter") as DatePicker;
                var timeToFilter = FindName("TimeToFilter") as DatePicker;
                var typeFilter = FindName("TypeFilter") as ComboBox;
                var portFilter = FindName("PortFilter") as TextBox;

                if (ipAddressFilter != null)
                    ipAddressFilter.Text = string.Empty;

                if (timeFromFilter != null)
                    timeFromFilter.SelectedDate = null;

                if (timeToFilter != null)
                    timeToFilter.SelectedDate = null;

                if (typeFilter != null)
                    typeFilter.SelectedIndex = 0; // Select "All"

                if (portFilter != null)
                    portFilter.Text = string.Empty;

                // Restore original log entries if available
                if (_originalLogEntries.Count > 0)
                {
                    // Update the log parser with original entries
                    int allowedCount = 0;
                    int blockedCount = 0;

                    foreach (var entry in _originalLogEntries)
                    {
                        if (TextUtil.ContainsIgnoreCase(entry.Action, "allow"))
                            allowedCount++;
                        else if (TextUtil.ContainsIgnoreCase(entry.Action, "drop") || TextUtil.ContainsIgnoreCase(entry.Action, "block"))
                            blockedCount++;
                    }


                    _logParser.UpdateFilteredEntries(_originalLogEntries, allowedCount, blockedCount);

                    // Update DataGrid
                    var dataGrid = FindName("LogsDataGrid") as DataGrid;
                    if (dataGrid != null)
                    {
                        dataGrid.ItemsSource = _logParser.LogEntries;
                        dataGrid.Items.Refresh();
                    }

                    // Update list of suspicious activities
                    UpdateSuspiciousActivities();

                    // Update chart
                    UpdateChart();

                    Debug.WriteLine($"Filters reset. Total: {_logParser.TotalCount}, Allowed: {_logParser.AllowedCount}, Blocked: {_logParser.BlockedCount}");
                }

                MessageBox.Show("Filters have been reset.", "Reset Filters", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting filters: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Error in btn_reset_filters_Click: {ex}");
            }
        }


        private Guid GetLoggedInUserId()
        {
            try
            {
                return login.LoggedInUserID;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting user ID: {ex}");
                throw new InvalidOperationException("Could not retrieve logged in user ID");
            }
        }

        private void ChartTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                var selectedItem = comboBox.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    _selectedChartType = selectedItem?.Content?.ToString() ?? "Unknown";
                    Debug.WriteLine($"Chart type changed to {_selectedChartType}");
                    UpdateChart();
                }
            }
        }

        private string GetDemoLogPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return System.IO.Path.Combine(baseDir, "assets", "demo", "pfirewall_demo.log");
        }

        private void btn_load_demo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _selectedDataSource = "Local";

                if (FindName("DataSourceCombo") is ComboBox ds)
                {
                    var localItem = ds.Items
                        .OfType<ComboBoxItem>()
                        .FirstOrDefault(i => string.Equals(i.Content?.ToString(), "Local", StringComparison.OrdinalIgnoreCase));
                    if (localItem != null) ds.SelectedItem = localItem;
                }

                var demoPath = GetDemoLogPath();
                if (!File.Exists(demoPath))
                {
                    MessageBox.Show($"Demo file not found:\n{demoPath}\n\nMake sure it is copied to output (Copy always).",
                                    "Missing demo file", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _originalLogEntries.Clear();
                _logParser.LoadLogs(demoPath);

                foreach (var entry in _logParser.LogEntries)
                    _originalLogEntries.Add(entry);

                UpdateSuspiciousActivities();
                UpdateChart();

                var dataGrid = FindName("LogsDataGrid") as DataGrid;
                dataGrid?.Items.Refresh();

                MessageBox.Show($"Demo data loaded: {_logParser.TotalCount} events.",
                                "Demo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading demo data:\n{ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

