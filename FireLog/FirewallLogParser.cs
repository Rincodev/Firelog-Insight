using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data.SqlClient;
using System.Data;
using Npgsql;
using System.Net;


namespace FireLog
{
    public class FirewallLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string? Action { get; set; } = string.Empty;
        public string? Protocol { get; set; } = string.Empty;
        public string? SrcIP { get; set; } = string.Empty;
        public string? DestIP { get; set; } = string.Empty;
        public int SrcPort { get; set; }
        public int DestPort { get; set; }
        //public string Interface { get; set; }
        public string? UserID { get; set; } = string.Empty;
    }

    public class FirewallLogParser : INotifyPropertyChanged
    {
        private ObservableCollection<FirewallLogEntry> _logEntries = new();
        private int _totalCount;
        private int _allowedCount;
        private int _blockedCount;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<FirewallLogEntry> LogEntries
        {
            get { return _logEntries; }
            set
            {
                _logEntries = value;
                OnPropertyChanged();
            }
        }

        public int TotalCount
        {
            get { return _totalCount; }
            set
            {
                _totalCount = value;
                OnPropertyChanged();
            }
        }

        public int AllowedCount
        {
            get { return _allowedCount; }
            set
            {
                _allowedCount = value;
                OnPropertyChanged();
            }
        }

        public int BlockedCount
        {
            get { return _blockedCount; }
            set
            {
                _blockedCount = value;
                OnPropertyChanged();
            }
        }

        public FirewallLogParser()
        {
            LogEntries = new ObservableCollection<FirewallLogEntry>();
            TotalCount = 0;
            AllowedCount = 0;
            BlockedCount = 0;
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? string.Empty));
        }
        public void SaveLogsToDatabase()
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbHelper.GetConnectionString()))
                {
                    conn.Open();

                    foreach (var entry in LogEntries)
                    {
                        using (var cmd = new NpgsqlCommand(@"
    INSERT INTO firewall_logs 
    (timestamp, action, protocol, src_ip, dst_ip, src_port, dst_port, user_id)
    VALUES 
    (@timestamp, @action, @protocol, @src_ip, @dst_ip, @src_port, @dst_port, @user_id)
", conn))
                        {
                            cmd.Parameters.AddWithValue("@timestamp", entry.Timestamp);

                            cmd.Parameters.Add("@action", NpgsqlTypes.NpgsqlDbType.Varchar).Value =
                                string.IsNullOrEmpty(entry.Action) ? DBNull.Value : entry.Action;

                            cmd.Parameters.Add("@protocol", NpgsqlTypes.NpgsqlDbType.Varchar).Value =
                                string.IsNullOrEmpty(entry.Protocol) ? DBNull.Value : entry.Protocol;

                            var srcIpParam = new NpgsqlParameter("@src_ip", NpgsqlTypes.NpgsqlDbType.Inet)
                            {
                                Value = IPAddress.TryParse(entry.SrcIP, out var srcIp) ? srcIp : DBNull.Value
                            };
                            cmd.Parameters.Add(srcIpParam);

                            var dstIpParam = new NpgsqlParameter("@dst_ip", NpgsqlTypes.NpgsqlDbType.Inet)
                            {
                                Value = IPAddress.TryParse(entry.DestIP, out var dstIp) ? dstIp : DBNull.Value
                            };
                            cmd.Parameters.Add(dstIpParam);
                            cmd.Parameters.AddWithValue("@src_port", entry.SrcPort);
                            cmd.Parameters.AddWithValue("@dst_port", entry.DestPort);
                            Guid? userId = null;

                            if (Guid.TryParse(entry.UserID, out var parsedUserId))
                            {
                                userId = parsedUserId;
                            }
                            else if (AppState.CurrentUserId.HasValue)
                            {
                                userId = AppState.CurrentUserId.Value;
                            }
                            cmd.Parameters.AddWithValue("@user_id", (object?)userId ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Logs have been successfully saved to the database.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while saving to the database: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Error in SaveLogsToDatabase: {ex}");
            }
        }

        public void LoadLogs(string filePath)
        {
            try
            {
                // Clear existing entries
                LogEntries.Clear();
                AllowedCount = 0;
                BlockedCount = 0;

                var lines = new List<string>();

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }

                // Parse each line
                foreach (string line in lines)
                {
                    // Skip comments and empty lines
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    // Split line by whitespace
                    string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length < 7)
                        continue;

                    // Create new log entry
                    FirewallLogEntry entry = new FirewallLogEntry();

                    // Parse timestamp
                    if (DateTime.TryParseExact(parts[0] + " " + parts[1], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timestamp))
                    {
                        entry.Timestamp = timestamp;
                    }
                    else
                    {
                        // Skip entries with invalid timestamps
                        continue;
                    }

                    // Parse other fields
                    entry.Action = parts[2];
                    entry.Protocol = parts[3];
                    entry.SrcIP = parts[4];
                    entry.DestIP = parts[5];

                    // Parse ports
                    if (int.TryParse(parts[6], out int srcPort))
                    {
                        entry.SrcPort = srcPort;
                    }

                    if (parts.Length > 7 && int.TryParse(parts[7], out int destPort))
                    {
                        entry.DestPort = destPort;
                    }

                    // Parse additional fields if available
                    if (parts.Length > 8)
                    {
                        // Check for key-value pairs
                        for (int i = 8; i < parts.Length; i++)
                        {
                            string part = parts[i];
                            if (part.Contains("="))
                            {
                                string[] keyValue = part.Split('=');
                                if (keyValue.Length == 2)
                                {
                                    string key = keyValue[0].ToLower();
                                    string value = keyValue[1];

                                    switch (key)
                                    {

                                        case "user":
                                        case "username":
                                            entry.UserID = value;
                                            break;

                                    }
                                }
                            }
                        }

                        // Add the record to the collection
                        LogEntries.Add(entry);

                        // Update statistics
                        if (entry.Action.ToLower().Contains("allow"))
                            AllowedCount++;
                        else if (entry.Action.ToLower().Contains("drop") || entry.Action.ToLower().Contains("block"))
                            BlockedCount++;
                    }
                }

                // Update total count
                TotalCount = LogEntries.Count;

                // Debug output
                Debug.WriteLine($"Loaded {TotalCount} log entries: {AllowedCount} allowed, {BlockedCount} blocked");
            }
            catch (IOException ex)
            {
                // Handle file access issues specifically
                MessageBox.Show($"Error accessing log file: {ex.Message}\n\nTry again later or use a different log file.",
                               "File Access Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"File access error in LoadLogs: {ex}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading log file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Error in LoadLogs: {ex}");
            }
        }

        // Method to update filtered entries
        public void UpdateFilteredEntries(ObservableCollection<FirewallLogEntry> filteredEntries, int allowedCount, int blockedCount)
        {
            try
            {
                // Update the log entries collection
                LogEntries = new ObservableCollection<FirewallLogEntry>(filteredEntries);

                // Update statistics
                AllowedCount = allowedCount;
                BlockedCount = blockedCount;
                TotalCount = LogEntries.Count;

                Debug.WriteLine($"Updated filtered entries: {TotalCount} log entries: {AllowedCount} allowed, {BlockedCount} blocked");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating filtered entries: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Error in UpdateFilteredEntries: {ex}");
            }
        }

        // Method to apply filters to the loaded logs
        public void ApplyFilters(DateTime? fromDate = null, DateTime? toDate = null, string? ipFilter = null, string? actionFilter = null)
        {
            try
            {
                Debug.WriteLine($"Applying filters - From: {fromDate}, To: {toDate}, IP: {ipFilter}, Action: {actionFilter}");

                // Reset counters
                AllowedCount = 0;
                BlockedCount = 0;

                // Create a filtered view of the log entries
                var filteredEntries = new ObservableCollection<FirewallLogEntry>();

                foreach (var entry in LogEntries)
                {
                    bool includeEntry = true;

                    // Apply date filters
                    if (fromDate.HasValue && entry.Timestamp < fromDate.Value)
                        includeEntry = false;

                    if (toDate.HasValue && entry.Timestamp > toDate.Value)
                        includeEntry = false;

                    // Apply IP filter
                    if (!string.IsNullOrWhiteSpace(ipFilter))
                    {
                        var src = entry.SrcIP ?? string.Empty;
                        var dst = entry.DestIP ?? string.Empty;

                        if (src.IndexOf(ipFilter, StringComparison.OrdinalIgnoreCase) < 0 &&
                            dst.IndexOf(ipFilter, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            includeEntry = false;
                        }
                    }

                    // Apply action filter
                    if (!string.IsNullOrWhiteSpace(actionFilter) &&
    !string.Equals(actionFilter, "All", StringComparison.OrdinalIgnoreCase) &&
    !TextUtil.ContainsIgnoreCase(entry.Action, actionFilter))
                        includeEntry = false;

                    // If the entry meets all filter criteria, include it
                    if (includeEntry)
                    {
                        filteredEntries.Add(entry);

                        // Update statistics
                        if (TextUtil.ContainsIgnoreCase(entry.Action, "allow"))
                            AllowedCount++;
                        else if (TextUtil.ContainsIgnoreCase(entry.Action, "drop") || TextUtil.ContainsIgnoreCase(entry.Action, "block"))
                            BlockedCount++;
                    }
                }

                // Update the collection with filtered entries
                LogEntries = filteredEntries;

                // Update total count
                TotalCount = LogEntries.Count;

                Debug.WriteLine($"After filtering: {TotalCount} log entries: {AllowedCount} allowed, {BlockedCount} blocked");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying filters: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Error in ApplyFilters: {ex}");
            }
        }

        public List<string> IdentifySuspiciousActivity()
        {
            List<string> suspiciousActivities = new List<string>();

            try
            {
                // Check for port scanning (multiple connections to different ports on the same IP)
                var portScanSuspects = LogEntries
                    .GroupBy(e => new { e.SrcIP, e.DestIP })
                    .Where(g => g.Select(e => e.DestPort).Distinct().Count() > 10)
                    .Select(g => new { g.Key.SrcIP, g.Key.DestIP, PortCount = g.Select(e => e.DestPort).Distinct().Count() });

                foreach (var suspect in portScanSuspects)
                {
                    suspiciousActivities.Add($"Possible port scan: {suspect.SrcIP} scanned {suspect.PortCount} ports on {suspect.DestIP}");
                }

                // Check for brute force attempts (multiple connections to the same port)
                var bruteForceTargets = LogEntries
                    .Where(e => e.DestPort == 22 || e.DestPort == 3389 || e.DestPort == 21 || e.DestPort == 23)
                    .GroupBy(e => new { e.SrcIP, e.DestIP, e.DestPort })
                    .Where(g => g.Count() > 5)
                    .Select(g => new { g.Key.SrcIP, g.Key.DestIP, g.Key.DestPort, Count = g.Count() });

                foreach (var target in bruteForceTargets)
                {
                    string service = target.DestPort == 22 ? "SSH" :
                                    target.DestPort == 3389 ? "RDP" :
                                    target.DestPort == 21 ? "FTP" :
                                    target.DestPort == 23 ? "Telnet" : $"Port {target.DestPort}";

                    suspiciousActivities.Add($"Possible brute force: {target.SrcIP} made {target.Count} connections to {service} on {target.DestIP}");
                }

                // Check for unusual ports
                var unusualPorts = new[] { 4444, 5555, 6666, 7777, 8888, 9999, 1337 };
                var unusualPortConnections = LogEntries
                    .Where(e => unusualPorts.Contains(e.DestPort) || unusualPorts.Contains(e.SrcPort))
                    .GroupBy(e => new { e.SrcIP, e.DestIP, SrcPort = e.SrcPort, DestPort = e.DestPort })
                    .Select(g => new { g.Key.SrcIP, g.Key.DestIP, g.Key.SrcPort, g.Key.DestPort, Count = g.Count() });

                foreach (var connection in unusualPortConnections)
                {
                    int suspiciousPort = unusualPorts.Contains(connection.SrcPort) ? connection.SrcPort : connection.DestPort;
                    suspiciousActivities.Add($"Unusual port activity: {connection.SrcIP}:{connection.SrcPort} to {connection.DestIP}:{connection.DestPort} ({connection.Count} connections)");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in IdentifySuspiciousActivity: {ex}");
            }

            return suspiciousActivities;
        }

        public UIElement CreatePieChart()
        {
            try
            {
                Debug.WriteLine("Creating pie chart");

                // Create a canvas for the chart
                Canvas canvas = new Canvas
                {
                    Width = 150,
                    Height = 150
                };

                // If no data, show message
                if (TotalCount == 0)
                {
                    TextBlock noDataText = new TextBlock
                    {
                        Text = "No data available",
                        Foreground = Brushes.White,
                        FontSize = 16,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    Canvas.SetLeft(noDataText, 400);
                    Canvas.SetTop(noDataText, 50);
                    canvas.Children.Add(noDataText);

                    Debug.WriteLine("No data for pie chart");
                    return canvas;
                }

                // Calculate percentages
                double allowedPercentage = (double)AllowedCount / TotalCount;
                double blockedPercentage = (double)BlockedCount / TotalCount;
                double otherPercentage = 1 - allowedPercentage - blockedPercentage;

                // Center of the pie
                double centerX = 135;
                double centerY = 95;
                double radius = 75;

                // Draw allowed slice
                if (allowedPercentage > 0)
                {
                    double allowedAngle = allowedPercentage * 360;
                    DrawPieSlice(canvas, centerX, centerY, radius, 0, allowedAngle, Brushes.Green, "Allowed");
                }

                // Draw blocked slice
                if (blockedPercentage > 0)
                {
                    double blockedStartAngle = allowedPercentage * 360;
                    double blockedAngle = blockedPercentage * 360;
                    DrawPieSlice(canvas, centerX, centerY, radius, blockedStartAngle, blockedAngle, Brushes.Red, "Blocked");
                }

                // Draw other slice if needed
                if (otherPercentage > 0)
                {
                    double otherStartAngle = (allowedPercentage + blockedPercentage) * 360;
                    double otherAngle = otherPercentage * 360;
                    DrawPieSlice(canvas, centerX, centerY, radius, otherStartAngle, otherAngle, Brushes.Gray, "Other");
                }

                // Add legend
                double legendY = 20;

                // Allowed legend
                if (allowedPercentage > 0)
                {
                    Rectangle allowedRect = new Rectangle
                    {
                        Width = 15,
                        Height = 15,
                        Fill = Brushes.Green
                    };
                    Canvas.SetLeft(allowedRect, -74);
                    Canvas.SetTop(allowedRect, legendY - 40);
                    canvas.Children.Add(allowedRect);

                    TextBlock allowedText = new TextBlock
                    {
                        Text = $"Allowed: {AllowedCount} ({allowedPercentage:P1})",
                        Foreground = Brushes.Black,
                        FontSize = 14
                    };
                    Canvas.SetLeft(allowedText, -58);
                    Canvas.SetTop(allowedText, legendY - 40);
                    canvas.Children.Add(allowedText);

                    legendY += 25;
                }

                // Blocked legend
                if (blockedPercentage > 0)
                {
                    Rectangle blockedRect = new Rectangle
                    {
                        Width = 15,
                        Height = 15,
                        Fill = Brushes.Red
                    };
                    Canvas.SetLeft(blockedRect, -74);
                    Canvas.SetTop(blockedRect, legendY - 35);
                    canvas.Children.Add(blockedRect);

                    TextBlock blockedText = new TextBlock
                    {
                        Text = $"Blocked: {BlockedCount} ({blockedPercentage:P1})",
                        Foreground = Brushes.Black,
                        FontSize = 14
                    };
                    Canvas.SetLeft(blockedText, -58);
                    Canvas.SetTop(blockedText, legendY - 35);
                    canvas.Children.Add(blockedText);

                    legendY += 25;
                }


                Debug.WriteLine("Pie chart created successfully");
                return canvas;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating pie chart: {ex}");

                // Return empty canvas with error message
                Canvas errorCanvas = new Canvas
                {
                    Width = 300,
                    Height = 300
                };

                TextBlock errorText = new TextBlock
                {
                    Text = "Error creating chart",
                    Foreground = Brushes.Red,
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Canvas.SetLeft(errorText, 100);
                Canvas.SetTop(errorText, 140);
                errorCanvas.Children.Add(errorText);

                return errorCanvas;
            }
        }

        // Helper method to draw a pie slice
        private void DrawPieSlice(Canvas canvas, double centerX, double centerY, double radius,
                                 double startAngle, double sweepAngle, Brush fill, string label)
        {
            try
            {
                // Convert angles to radians
                double startRad = startAngle * Math.PI / 180;
                double endRad = (startAngle + sweepAngle) * Math.PI / 180;

                // Calculate points
                double startX = centerX + radius * Math.Cos(startRad);
                double startY = centerY + radius * Math.Sin(startRad);
                double endX = centerX + radius * Math.Cos(endRad);
                double endY = centerY + radius * Math.Sin(endRad);

                // Create path geometry
                PathGeometry pathGeometry = new PathGeometry();
                PathFigure pathFigure = new PathFigure();
                pathFigure.StartPoint = new Point(centerX, centerY);
                pathFigure.Segments.Add(new LineSegment(new Point(startX, startY), true));

                // Add arc segment
                ArcSegment arcSegment = new ArcSegment
                {
                    Point = new Point(endX, endY),
                    Size = new Size(radius, radius),
                    SweepDirection = SweepDirection.Clockwise,
                    IsLargeArc = sweepAngle > 180
                };
                pathFigure.Segments.Add(arcSegment);

                // Close the path
                pathFigure.Segments.Add(new LineSegment(new Point(centerX, centerY), true));
                pathFigure.IsClosed = true;
                pathGeometry.Figures.Add(pathFigure);

                // Create path
                System.Windows.Shapes.Path path = new System.Windows.Shapes.Path
                {
                    Fill = fill,
                    Data = pathGeometry
                };

                canvas.Children.Add(path);

                // Add label at the middle of the slice if slice is large enough
                if (sweepAngle >= 20)
                {
                    double labelRad = startRad + (endRad - startRad) / 2;
                    double labelX = centerX + (radius * 0.7) * Math.Cos(labelRad);
                    double labelY = centerY + (radius * 0.7) * Math.Sin(labelRad);

                    TextBlock labelText = new TextBlock
                    {
                        Text = label,
                        Foreground = Brushes.White,
                        FontSize = 12
                    };

                    Canvas.SetLeft(labelText, labelX - 20);
                    Canvas.SetTop(labelText, labelY - 10);
                    canvas.Children.Add(labelText);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error drawing pie slice: {ex}");
            }
        }

        // Method to create a bar chart showing protocol distribution
        public UIElement CreateProtocolBarChart()
        {
            try
            {
                Debug.WriteLine("Creating protocol bar chart");

                // Create a canvas for the chart
                Canvas canvas = new Canvas
                {
                    Width = 300,
                    Height = 300
                };

                // If no data, show message
                if (TotalCount == 0)
                {
                    TextBlock noDataText = new TextBlock
                    {
                        Text = "No data available",
                        Foreground = Brushes.Black,
                        FontSize = 16,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    Canvas.SetLeft(noDataText, 100);
                    Canvas.SetTop(noDataText, 0);
                    canvas.Children.Add(noDataText);

                    Debug.WriteLine("No data for protocol bar chart");
                    return canvas;
                }

                // Group entries by protocol
                var protocolData = LogEntries
                    .Where(e => !string.IsNullOrEmpty(e.Protocol))
                    .GroupBy(e => e.Protocol)
                    .Select(g => new { Protocol = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5) // Limit to top 5 protocols
                    .ToList();

                // Find max count for scaling
                int maxCount = protocolData.Any() ? protocolData.Max(x => x.Count) : 0;
                if (maxCount == 0)
                {
                    TextBlock noDataText = new TextBlock
                    {
                        Text = "No protocol data available",
                        Foreground = Brushes.Black,
                        FontSize = 16,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    Canvas.SetLeft(noDataText, -30);
                    Canvas.SetTop(noDataText, 0);
                    canvas.Children.Add(noDataText);

                    Debug.WriteLine("No protocol data for bar chart");
                    return canvas;
                }

                // Chart dimensions
                double chartLeft = 50;
                double chartBottom = 175;
                double chartWidth = 230;
                double chartHeight = 150;

                // Draw X and Y axes
                Line xAxis = new Line
                {
                    X1 = chartLeft,
                    Y1 = chartBottom,
                    X2 = chartLeft + chartWidth,
                    Y2 = chartBottom,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                canvas.Children.Add(xAxis);

                Line yAxis = new Line
                {
                    X1 = chartLeft,
                    Y1 = chartBottom,
                    X2 = chartLeft,
                    Y2 = chartBottom - chartHeight,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                canvas.Children.Add(yAxis);

                // Draw Y axis labels (counts)
                int yStep = Math.Max(1, maxCount / 5);
                for (int count = 0; count <= maxCount; count += yStep)
                {
                    double y = chartBottom - (count / (double)maxCount) * chartHeight;

                    Line tickMark = new Line
                    {
                        X1 = chartLeft - 5,
                        Y1 = y,
                        X2 = chartLeft,
                        Y2 = y,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };
                    canvas.Children.Add(tickMark);

                    TextBlock countLabel = new TextBlock
                    {
                        Text = count.ToString(),
                        Foreground = Brushes.Black,
                        FontSize = 10
                    };
                    Canvas.SetLeft(countLabel, chartLeft - 30);
                    Canvas.SetTop(countLabel, y - 5);
                    canvas.Children.Add(countLabel);
                }

                // Draw bars
                double barWidth = chartWidth / (protocolData.Count * 2);

                // Define colors for bars
                Brush[] barColors = new Brush[]
                {
                    Brushes.SkyBlue,
                    Brushes.LightGreen,
                    Brushes.Orange,
                    Brushes.Pink,
                    Brushes.Yellow
                };

                for (int i = 0; i < protocolData.Count; i++)
                {
                    var data = protocolData[i];
                    double x = chartLeft + (i * 2 + 0.5) * barWidth;
                    double barHeight = (data.Count / (double)maxCount) * chartHeight;

                    // Draw bar
                    Rectangle bar = new Rectangle
                    {
                        Width = barWidth,
                        Height = barHeight,
                        Fill = barColors[i % barColors.Length]
                    };
                    Canvas.SetLeft(bar, x);
                    Canvas.SetTop(bar, chartBottom - barHeight);
                    canvas.Children.Add(bar);

                    // Draw protocol label
                    TextBlock protocolLabel = new TextBlock
                    {
                        Text = data.Protocol,
                        Foreground = Brushes.Black,
                        FontSize = 10
                    };
                    Canvas.SetLeft(protocolLabel, x - 5);
                    Canvas.SetTop(protocolLabel, chartBottom + 9);
                    canvas.Children.Add(protocolLabel);

                    // Draw count label
                    TextBlock countLabel = new TextBlock
                    {
                        Text = data.Count.ToString(),
                        Foreground = Brushes.Black,
                        FontSize = 10
                    };
                    Canvas.SetLeft(countLabel, x);
                    Canvas.SetTop(countLabel, chartBottom - barHeight - 5);
                    canvas.Children.Add(countLabel);
                }

                Debug.WriteLine("Protocol bar chart created successfully");
                return canvas;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating protocol bar chart: {ex}");

                // Return empty canvas with error message
                Canvas errorCanvas = new Canvas
                {
                    Width = 300,
                    Height = 300
                };

                TextBlock errorText = new TextBlock
                {
                    Text = "Error creating protocol chart",
                    Foreground = Brushes.Red,
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Canvas.SetLeft(errorText, 80);
                Canvas.SetTop(errorText, 140);
                errorCanvas.Children.Add(errorText);

                return errorCanvas;
            }
        }


        public BitmapSource? ExportChartToPng(UIElement chart)
        {
            if (chart == null) return null;

            chart.UpdateLayout();

            var contentBounds = VisualTreeHelper.GetDescendantBounds(chart);

            if (contentBounds.IsEmpty)
                contentBounds = new Rect(new Point(0, 0), chart.RenderSize);

            const double pad = 86; 
            var paddedBounds = new Rect(
                contentBounds.X - pad,
                contentBounds.Y - pad,
                contentBounds.Width + 2 * pad,
                contentBounds.Height + 2 * pad
            );

            var dpi = VisualTreeHelper.GetDpi(chart);
            int pxW = Math.Max(1, (int)Math.Ceiling(paddedBounds.Width * dpi.DpiScaleX));
            int pxH = Math.Max(1, (int)Math.Ceiling(paddedBounds.Height * dpi.DpiScaleY));

            var rtb = new RenderTargetBitmap(
                pxW, pxH,
                96 * dpi.DpiScaleX, 96 * dpi.DpiScaleY,
                PixelFormats.Pbgra32
            );

            var vb = new VisualBrush(chart)
            {
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = paddedBounds,         
                Stretch = Stretch.Fill,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };

            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, paddedBounds.Width, paddedBounds.Height));
                dc.DrawRectangle(vb, null, new Rect(0, 0, paddedBounds.Width, paddedBounds.Height));
            }

            rtb.Render(dv);
            return rtb;
        }
    }
}

