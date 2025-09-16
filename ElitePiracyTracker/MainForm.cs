using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
//using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using ElitePiracyTracker.Models;
using ElitePiracyTracker.Services;

namespace ElitePiracyTracker
{
    //public partial class MainForm : Form
    //{
    //    private PiracyScoringService _scoringService;
    //    private EDSMService _edsmService;
    //    private List<PiracyScoreResult> _currentResults = new List<PiracyScoreResult>();
    //    private bool _isAnalyzing = false;
    //    private HttpClient _httpClient;
    //    private SpanshSystemSearch _spanshSearcher;
    //    private readonly Dictionary<string, SystemData> _systemCache = new Dictionary<string, SystemData>();
    //    private readonly Dictionary<string, PiracyScoreResult> _resultCache = new Dictionary<string, PiracyScoreResult>();

    //    public MainForm()
    //    {
    //        InitializeComponent();
    //        InitializeServices();
    //        SetupDataGridView();

    //        // Initialize HttpClient
    //        _httpClient = new HttpClient();
    //        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ElitePiracyTracker/1.0.0");

    //        _spanshSearcher = new SpanshSystemSearch();
    //    }

    //    private void InitializeServices()
    //    {
    //        try
    //        {
    //            // Set up configuration
    //            var configuration = new ConfigurationBuilder()
    //                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    //                .AddJsonFile("Config/appsettings.json", optional: false, reloadOnChange: true)
    //                .Build();

    //            // Set up dependency injection
    //            var serviceProvider = new ServiceCollection()
    //                .AddSingleton<IConfiguration>(configuration)
    //                .AddSingleton<HttpClient>()
    //                .AddMemoryCache()
    //                .AddSingleton<EDSMService>()
    //                .AddSingleton<SpanshSystemSearch>() // Add this
    //                .AddSingleton<PiracyScoringService>()
    //                .BuildServiceProvider();

    //            // Get the services
    //            _edsmService = serviceProvider.GetService<EDSMService>();
    //            _spanshSearcher = serviceProvider.GetService<SpanshSystemSearch>(); // Add this
    //            _scoringService = serviceProvider.GetService<PiracyScoringService>();
    //            progressBar.Style = ProgressBarStyle.Continuous;

    //            statusLabel.Text = "Services initialized. Ready to analyze systems.";
    //        }
    //        catch (Exception ex)
    //        {
    //            MessageBox.Show($"Failed to initialize services: {ex.Message}", "Initialization Error",
    //                MessageBoxButtons.OK, MessageBoxIcon.Error);
    //            statusLabel.Text = "Service initialization failed. Check appsettings.json.";
    //        }
    //    }

    //    private void SetupDataGridView()
    //    {
    //        // Configure the results grid view
    //        resultsDataGridView.AutoGenerateColumns = false;
    //        resultsDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    //        resultsDataGridView.MultiSelect = false;

    //        // Add columns
    //        resultsDataGridView.Columns.Add(new DataGridViewTextBoxColumn()
    //        {
    //            Name = "SystemName",
    //            HeaderText = "System",
    //            DataPropertyName = "SystemName",
    //            Width = 150
    //        });

    //        resultsDataGridView.Columns.Add(new DataGridViewTextBoxColumn()
    //        {
    //            Name = "FinalScore",
    //            HeaderText = "Score",
    //            DataPropertyName = "FinalScore",
    //            Width = 60,
    //            DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" }
    //        });

    //        resultsDataGridView.Columns.Add(new DataGridViewTextBoxColumn()
    //        {
    //            Name = "EconomyScore",
    //            HeaderText = "Economy",
    //            DataPropertyName = "EconomyScore",
    //            Width = 70,
    //            DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" }
    //        });

    //        resultsDataGridView.Columns.Add(new DataGridViewTextBoxColumn()
    //        {
    //            Name = "NoRingsScore",
    //            HeaderText = "No Rings",
    //            DataPropertyName = "NoRingsScore",
    //            Width = 70,
    //            DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" }
    //        });

    //        resultsDataGridView.Columns.Add(new DataGridViewTextBoxColumn()
    //        {
    //            Name = "GovernmentScore",
    //            HeaderText = "Government",
    //            DataPropertyName = "GovernmentScore",
    //            Width = 80,
    //            DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" }
    //        });

    //        resultsDataGridView.Columns.Add(new DataGridViewTextBoxColumn()
    //        {
    //            Name = "SecurityScore",
    //            HeaderText = "Security",
    //            DataPropertyName = "SecurityScore",
    //            Width = 70,
    //            DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" }
    //        });

    //        resultsDataGridView.Columns.Add(new DataGridViewTextBoxColumn()
    //        {
    //            Name = "FactionStateScore",
    //            HeaderText = "Faction State",
    //            DataPropertyName = "FactionStateScore",
    //            Width = 90,
    //            DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" }
    //        });

    //        resultsDataGridView.Columns.Add(new DataGridViewTextBoxColumn()
    //        {
    //            Name = "MarketDemandScore",
    //            HeaderText = "Market Demand",
    //            DataPropertyName = "MarketDemandScore",
    //            Width = 100,
    //            DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" }
    //        });

    //        resultsDataGridView.CellFormatting += resultsDataGridView_CellFormatting;
    //    }

    //    private void resultsDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
    //    {
    //        if (resultsDataGridView.Columns[e.ColumnIndex].Name == "MarketDemandScore")
    //        {
    //            var row = resultsDataGridView.Rows[e.RowIndex];
    //            var result = row.DataBoundItem as PiracyScoreResult;
    //            if (result != null && result.SkippedMarket)
    //            {
    //                e.Value = "Skipped";
    //                e.FormattingApplied = true;
    //            }
    //        }
    //    }

    //    private void clearCacheButton_Click(object sender, EventArgs e)
    //    {
    //        _systemCache.Clear();
    //        _resultCache.Clear();
    //        statusLabel.Text = "Cache cleared.";
    //    }

    //    private async void searchButton_Click(object sender, EventArgs e)
    //    {
    //        try
    //        {
    //            SetUiEnabled(false);

    //            // Clear previous results
    //            _currentResults.Clear();
    //            resultsDataGridView.DataSource = null;
    //            detailsTextBox.Text = "";

    //            statusLabel.Text = "Searching for systems...";

    //            // Get reference system and distance
    //            string referenceSystem = referenceSystemTextBox.Text.Trim();
    //            if (string.IsNullOrEmpty(referenceSystem))
    //            {
    //                MessageBox.Show("Please enter a reference system.", "Input Error",
    //                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
    //                return;
    //            }

    //            if (!int.TryParse(maxDistanceTextBox.Text, out int maxDistance) || maxDistance <= 0)
    //            {
    //                MessageBox.Show("Please enter a valid maximum distance.", "Input Error",
    //                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
    //                return;
    //            }

    //            // Use the SpanshSystemSearch class to get complete system data
    //            var systems = await _spanshSearcher.SearchSystemsNearReference(referenceSystem, maxDistance);

    //            statusLabel.Text = $"Found {systems.Count} systems near {referenceSystem} (within {maxDistance} ly). Analyzing...";

    //            // Analyze the systems using the SystemData objects we already have
    //            await AnalyzeSystems(systems);
    //        }
    //        catch (Exception ex)
    //        {
    //            MessageBox.Show($"Error searching for systems: {ex.Message}", "Search Error",
    //                MessageBoxButtons.OK, MessageBoxIcon.Error);
    //            statusLabel.Text = "Search failed.";
    //        }
    //        finally
    //        {
    //            SetUiEnabled(true);
    //        }
    //    }

    //    private void clearResultsButton_Click(object sender, EventArgs e)
    //    {
    //        _currentResults.Clear();
    //        resultsDataGridView.DataSource = null;
    //        detailsTextBox.Text = "";
    //        statusLabel.Text = "Results cleared. Ready to search.";
    //    }


    //    private async Task AnalyzeSystems(List<SystemData> systemsData)
    //    {
    //        // Set up progress
    //        progressBar.Value = 0;
    //        progressBar.Maximum = systemsData.Count;
    //        statusLabel.Text = $"Analyzing {systemsData.Count} systems...";

    //        // Use a semaphore to limit concurrent requests
    //        var semaphore = new SemaphoreSlim(5, 5);
    //        var tasks = new List<Task>();

    //        foreach (var systemData in systemsData)
    //        {
    //            // Wait for an available slot
    //            await semaphore.WaitAsync();

    //            tasks.Add(Task.Run(async () =>
    //            {
    //                try
    //                {
    //                    // Update UI on the main thread
    //                    this.Invoke((MethodInvoker)delegate {
    //                        statusLabel.Text = $"Analyzing {systemData.Name}...";
    //                    });

    //                    // Use the updated method with systemData parameter
    //                    var result = await _scoringService.CalculateSystemScore(systemData: systemData);
    //                    if (result != null)
    //                    {
    //                        // Scale final score to 0-100
    //                        result.FinalScore *= 100;

    //                        // Update UI on the main thread
    //                        this.Invoke((MethodInvoker)delegate {
    //                            _currentResults.Add(result);

    //                            // Update the data grid
    //                            resultsDataGridView.DataSource = null;
    //                            resultsDataGridView.DataSource = _currentResults
    //                                .OrderByDescending(r => r.FinalScore)
    //                                .ToList();

    //                            // Auto-select the best result
    //                            if (resultsDataGridView.Rows.Count > 0)
    //                            {
    //                                resultsDataGridView.Rows[0].Selected = true;
    //                            }

    //                            progressBar.Value++;
    //                        });
    //                    }
    //                }
    //                catch (Exception ex)
    //                {
    //                    this.Invoke((MethodInvoker)delegate {
    //                        MessageBox.Show($"Error analyzing {systemData.Name}: {ex.Message}", "Analysis Error",
    //                            MessageBoxButtons.OK, MessageBoxIcon.Error);
    //                    });
    //                }
    //                finally
    //                {
    //                    semaphore.Release();
    //                }
    //            }));
    //        }

    //        // Wait for all tasks to complete
    //        await Task.WhenAll(tasks);

    //        statusLabel.Text = $"Analysis complete. Found {_currentResults.Count} results.";

    //        // Highlight the best system
    //        if (_currentResults.Count > 0)
    //        {
    //            var bestSystem = _currentResults.OrderByDescending(r => r.FinalScore).First();
    //            statusLabel.Text += $" Best system: {bestSystem.SystemName} ({bestSystem.FinalScore:F2}/100)";
    //        }
    //    }

    //    private void SetUiEnabled(bool enabled)
    //    {
    //        exportButton.Enabled = enabled && _currentResults.Count > 0;
    //        searchButton.Enabled = enabled;
    //        referenceSystemTextBox.Enabled = enabled;
    //        maxDistanceTextBox.Enabled = enabled;
    //        progressBar.Visible = !enabled;
    //    }

    //    private void resultsDataGridView_SelectionChanged(object sender, EventArgs e)
    //    {
    //        if (resultsDataGridView.SelectedRows.Count > 0)
    //        {
    //            var selectedResult = resultsDataGridView.SelectedRows[0].DataBoundItem as PiracyScoreResult;
    //            if (selectedResult != null)
    //            {
    //                detailsTextBox.Text = selectedResult.ToString();

    //                // Add recommendation
    //                if (selectedResult.FinalScore >= 90)
    //                    detailsTextBox.Text += "\n\n⭐ EXCELLENT PIRACY SPOT - Highly recommended!";
    //                else if (selectedResult.FinalScore >= 80)
    //                    detailsTextBox.Text += "\n\n✓ Good piracy spot - Worth checking out";
    //                else if (selectedResult.FinalScore >= 70)
    //                    detailsTextBox.Text += "\n\n~ Moderate piracy spot - Some potential";
    //                else
    //                    detailsTextBox.Text += "\n\n✗ Poor piracy spot - Not recommended";
    //            }
    //        }
    //    }

    //    private void exportButton_Click(object sender, EventArgs e)
    //    {
    //        if (_currentResults.Count == 0)
    //        {
    //            MessageBox.Show("No results to export.", "Export",
    //                MessageBoxButtons.OK, MessageBoxIcon.Information);
    //            return;
    //        }

    //        using (var saveDialog = new SaveFileDialog())
    //        {
    //            saveDialog.Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
    //            saveDialog.Title = "Export Results";
    //            saveDialog.FileName = $"piracy_analysis_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

    //            if (saveDialog.ShowDialog() == DialogResult.OK)
    //            {
    //                try
    //                {
    //                    using (var writer = new StreamWriter(saveDialog.FileName))
    //                    {
    //                        // Write header
    //                        writer.WriteLine("System,Score,Economy,No Rings,Government,Security,Faction State,Market Demand");

    //                        // Write data
    //                        foreach (var result in _currentResults.OrderByDescending(r => r.FinalScore))
    //                        {
    //                            writer.WriteLine($"\"{result.SystemName}\",{result.FinalScore:F2},{result.EconomyScore:F2},{result.NoRingsScore:F2},{result.GovernmentScore:F2},{result.SecurityScore:F2},{result.FactionStateScore:F2},{(result.MarketDemandScore == 0 ? "Skipped" : result.MarketDemandScore.ToString("F2"))}");
    //                        }
    //                    }

    //                    MessageBox.Show($"Results exported to {saveDialog.FileName}", "Export Successful",
    //                        MessageBoxButtons.OK, MessageBoxIcon.Information);
    //                }
    //                catch (Exception ex)
    //                {
    //                    MessageBox.Show($"Error exporting results: {ex.Message}", "Export Error",
    //                        MessageBoxButtons.OK, MessageBoxIcon.Error);
    //                }
    //            }
    //        }
    //    }

    //    private void MainForm_Load(object sender, EventArgs e)
    //    {
    //        // Only set up the search reference
    //        referenceSystemTextBox.Text = "Sol";
    //        maxDistanceTextBox.Text = "10";
    //    }
    //}
}