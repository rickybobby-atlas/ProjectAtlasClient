using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AtlasLauncher
{
    public class MainForm : Form
    {
        private readonly string _baseDir;
        private readonly string _configPath;
        private readonly string _devMarkerPath;
        private LauncherConfig _config;

        private Label _titleLabel;
        private Label _serverStatusLabel;
        private Label _patchStatusLabel;
        private CheckBox _devModeCheckBox;
        private ProgressBar _progressBar;
        private Button _checkUpdatesBtn;
        private Panel _launchPanel;

        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };

        public MainForm()
        {
            _baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _configPath = Path.Combine(_baseDir, "atlaslauncher.cfg");
            _devMarkerPath = Path.Combine(_baseDir, "dev.mode");
            _config = LauncherConfig.Load(_configPath);

            if (File.Exists(_devMarkerPath))
            {
                _config.UseInternalIP = true;
            }

            InitializeUI();
            Shown += async (s, e) => await RunUpdateCheckAsync();
        }

        private void InitializeUI()
        {
            AutoScaleMode = AutoScaleMode.None;
            Text = $"{_config.ShardName} Launcher";
            Size = new Size(960, 620);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(18, 20, 28);
            DoubleBuffered = true;
            KeyPreview = true;

            // Secret Admin Hotkey: Ctrl + Shift + D reveals/toggles Dev Server checkbox
            KeyDown += (s, e) =>
            {
                if (e.Control && e.Shift && e.KeyCode == Keys.D)
                {
                    _devModeCheckBox.Visible = true;
                    _devModeCheckBox.Checked = !_devModeCheckBox.Checked;
                }
            };

            string iconPath = Path.Combine(_baseDir, "ProjectAtlas.ico");
            if (File.Exists(iconPath))
            {
                try { Icon = new Icon(iconPath); } catch { }
            }

            string bgPath = Path.Combine(_baseDir, "launcher-bg.jpg");
            if (File.Exists(bgPath))
            {
                try
                {
                    BackgroundImage = Image.FromFile(bgPath);
                    BackgroundImageLayout = ImageLayout.Stretch;
                }
                catch { }
            }

            // Top Banner Panel
            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.FromArgb(190, 12, 14, 22)
            };

            _titleLabel = new Label
            {
                Text = _config.ShardName.ToUpperInvariant(),
                Font = new Font("Segoe UI", 21f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = Color.FromArgb(230, 195, 115),
                AutoSize = true,
                Location = new Point(20, 14),
                BackColor = Color.Transparent
            };
            topBar.Controls.Add(_titleLabel);

            var discordBtn = CreateLinkButton("Discord", 620, 12, () => OpenUrl(_config.Links?.Discord, "Discord"));
            var wikiBtn = CreateLinkButton("Wiki", 730, 12, () => OpenUrl(_config.Links?.Wiki, "Wiki"));
            var donateBtn = CreateLinkButton("Donate", 840, 12, () => OpenUrl(_config.Links?.Donate, "Donate"));
            donateBtn.ForeColor = Color.FromArgb(255, 215, 100);

            topBar.Controls.Add(discordBtn);
            topBar.Controls.Add(wikiBtn);
            topBar.Controls.Add(donateBtn);
            Controls.Add(topBar);

            // Compact Left Launch Panel (Tucked into upper-left archway)
            _launchPanel = new Panel
            {
                Location = new Point(16, 66),
                Size = new Size(204, 198),
                BackColor = Color.FromArgb(175, 14, 17, 26)
            };

            var launchHeader = new Label
            {
                Text = "LAUNCH CLIENT",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = Color.FromArgb(200, 210, 230),
                Location = new Point(10, 8),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _launchPanel.Controls.Add(launchHeader);

            AddLaunchButton("Razor Enhanced", "RazorEnhanced", 28);
            AddLaunchButton("Razor", "Razor", 60);
            AddLaunchButton("ClassicAssist", "ClassicAssist", 92);
            AddLaunchButton("TazUO (Modern)", "TazUO", 124);
            AddLaunchButton("TazUO + Razor Enhanced", "TazUORazorEnhanced", 156);

            Controls.Add(_launchPanel);

            // Bottom Patch Status & Server Info Dock
            var bottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 82,
                BackColor = Color.FromArgb(215, 12, 14, 22)
            };

            _serverStatusLabel = new Label
            {
                Location = new Point(20, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 12f, FontStyle.Regular, GraphicsUnit.Pixel),
                ForeColor = Color.FromArgb(160, 180, 205),
                BackColor = Color.Transparent
            };
            bottomBar.Controls.Add(_serverStatusLabel);

            // Developer Mode Tick Box (Only visible if dev.mode exists or Ctrl+Shift+D pressed)
            _devModeCheckBox = new CheckBox
            {
                Text = $"Dev Server ({_config.InternalIP}:{_config.InternalPort})",
                Location = new Point(560, 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = Color.FromArgb(120, 235, 150),
                BackColor = Color.Transparent,
                Checked = _config.UseInternalIP,
                Visible = File.Exists(_devMarkerPath) || _config.UseInternalIP,
                Cursor = Cursors.Hand
            };
            _devModeCheckBox.CheckedChanged += (s, e) =>
            {
                _config.UseInternalIP = _devModeCheckBox.Checked;
                UpdateServerLabel();
            };
            bottomBar.Controls.Add(_devModeCheckBox);

            UpdateServerLabel();

            _patchStatusLabel = new Label
            {
                Text = "Initializing patch check...",
                Location = new Point(20, 30),
                AutoSize = true,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = Color.FromArgb(230, 230, 235),
                BackColor = Color.Transparent
            };
            bottomBar.Controls.Add(_patchStatusLabel);

            _progressBar = new ProgressBar
            {
                Location = new Point(20, 54),
                Size = new Size(760, 16),
                Style = ProgressBarStyle.Continuous
            };
            bottomBar.Controls.Add(_progressBar);

            _checkUpdatesBtn = new Button
            {
                Text = "Check Updates",
                Location = new Point(800, 26),
                Size = new Size(125, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(35, 45, 65),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel),
                Cursor = Cursors.Hand
            };
            _checkUpdatesBtn.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 145);
            _checkUpdatesBtn.Click += async (s, e) => await RunUpdateCheckAsync();
            bottomBar.Controls.Add(_checkUpdatesBtn);

            Controls.Add(bottomBar);
        }

        private void UpdateServerLabel()
        {
            string mode = _config.UseInternalIP ? "LOCAL DEV SERVER" : "LIVE SHARD";
            _serverStatusLabel.Text = $"Target Server: {_config.ActiveIP}:{_config.ActivePort} ({mode})  |  UO Version: {_config.ClientVersion}";
            if (_serverStatusLabel != null)
            {
                _serverStatusLabel.ForeColor = _config.UseInternalIP
                    ? Color.FromArgb(120, 235, 150)
                    : Color.FromArgb(160, 180, 205);
            }
        }

        private Button CreateLinkButton(string text, int x, int y, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(95, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(32, 38, 54),
                ForeColor = Color.FromArgb(220, 225, 235),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(100, 120, 155);
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void AddLaunchButton(string label, string profileKey, int yPos)
        {
            var btn = new Button
            {
                Text = label,
                Location = new Point(10, yPos),
                Size = new Size(184, 28),
                Padding = Padding.Empty,
                TextAlign = ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 38, 56),
                ForeColor = Color.FromArgb(240, 240, 245),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(185, 150, 80);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 66, 96);
            btn.Click += (s, e) => LaunchGameProfile(profileKey);
            _launchPanel.Controls.Add(btn);
        }

        private void OpenUrl(string url, string name)
        {
            if (string.IsNullOrWhiteSpace(url) || url.Contains("YOUR_"))
            {
                MessageBox.Show($"The {name} link will be available soon!", _config.ShardName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool IsProtectedPlayerFile(string relPath)
        {
            string norm = relPath.Replace('\\', '/').ToLowerInvariant();
            if (norm.EndsWith("settings.json") || norm.EndsWith("settings.dev.json") || norm.Equals("dev.mode"))
                return true;

            if (norm.Contains("/profiles/") || norm.Contains("/macros/") || norm.Contains("/scripts/"))
                return true;

            return false;
        }

        private async Task RunUpdateCheckAsync()
        {
            _checkUpdatesBtn.Enabled = false;
            _launchPanel.Enabled = false;
            _progressBar.Value = 0;

            try
            {
                if (string.IsNullOrWhiteSpace(_config.PatchManifestUrl))
                {
                    _patchStatusLabel.Text = "Ready to launch (No patch URL configured).";
                    _progressBar.Value = 100;
                    return;
                }

                _patchStatusLabel.Text = "Checking for updates...";
                string manifestJson = await _httpClient.GetStringAsync(_config.PatchManifestUrl);
                var manifest = JsonSerializer.Deserialize<PatchManifest>(manifestJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (manifest?.Files == null || manifest.Files.Count == 0)
                {
                    _patchStatusLabel.Text = "Client is up to date.";
                    _progressBar.Value = 100;
                    return;
                }

                string basePatchUrl = _config.PatchManifestUrl.Substring(0, _config.PatchManifestUrl.LastIndexOf('/') + 1);
                var filesToUpdate = new System.Collections.Generic.List<ManifestFileEntry>();

                foreach (var entry in manifest.Files)
                {
                    if (IsProtectedPlayerFile(entry.Path))
                        continue;

                    string localPath = Path.Combine(_baseDir, entry.Path.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(localPath))
                    {
                        filesToUpdate.Add(entry);
                    }
                    else
                    {
                        string localHash = await Task.Run(() => ComputeSha256(localPath));
                        if (!string.Equals(localHash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
                        {
                            filesToUpdate.Add(entry);
                        }
                    }
                }

                if (filesToUpdate.Count == 0)
                {
                    _patchStatusLabel.Text = $"All files verified and up to date (Manifest v{manifest.Version}).";
                    _progressBar.Value = 100;
                    return;
                }

                int completed = 0;
                foreach (var entry in filesToUpdate)
                {
                    _patchStatusLabel.Text = $"Updating {entry.Path} ({completed + 1}/{filesToUpdate.Count})...";
                    string localPath = Path.Combine(_baseDir, entry.Path.Replace('/', Path.DirectorySeparatorChar));
                    string dir = Path.GetDirectoryName(localPath);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);

                    string fileUrl = basePatchUrl + entry.Path.Replace('\\', '/');
                    byte[] data = await _httpClient.GetByteArrayAsync(fileUrl);
                    await File.WriteAllBytesAsync(localPath, data);

                    if (entry.Path.Equals("atlaslauncher.cfg", StringComparison.OrdinalIgnoreCase))
                    {
                        bool wasDev = _devModeCheckBox.Checked;
                        _config = LauncherConfig.Load(_configPath);
                        _config.UseInternalIP = wasDev;
                        UpdateServerLabel();
                    }

                    completed++;
                    _progressBar.Value = (int)((completed / (double)filesToUpdate.Count) * 100);
                }

                _patchStatusLabel.Text = $"Update complete ({filesToUpdate.Count} file(s) updated). Ready to play!";
                _progressBar.Value = 100;
            }
            catch (Exception)
            {
                _patchStatusLabel.Text = "Patch server unreachable — Offline/Dev Mode Ready.";
                _progressBar.Value = 100;
            }
            finally
            {
                _checkUpdatesBtn.Enabled = true;
                _launchPanel.Enabled = true;
            }
        }

        private static string ComputeSha256(string filePath)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static void SyncClientSettingsJson(string settingsPath, string ip, int port, string uoDataPath, string clientVersion, string pluginFullPath)
        {
            JsonObject root = null;
            if (File.Exists(settingsPath))
            {
                try
                {
                    root = JsonNode.Parse(File.ReadAllText(settingsPath)) as JsonObject;
                }
                catch { }
            }

            root ??= new JsonObject();
            root["ip"] = ip;
            root["port"] = port;
            root["ultimaonlinedirectory"] = uoDataPath;
            root["clientversion"] = clientVersion;
            root["encryption"] = 0;

            if (string.IsNullOrWhiteSpace(pluginFullPath))
            {
                root["plugins"] = new JsonArray();
            }
            else
            {
                root["plugins"] = new JsonArray(pluginFullPath);
            }

            string dir = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(settingsPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        private void LaunchGameProfile(string profileKey)
        {
            if (_config.LaunchProfiles == null || !_config.LaunchProfiles.TryGetValue(profileKey, out var profile))
            {
                MessageBox.Show($"Launch profile '{profileKey}' is not configured in atlaslauncher.cfg.", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string clientExe = Path.GetFullPath(Path.Combine(_baseDir, profile.ClientExe));
            if (!File.Exists(clientExe))
            {
                MessageBox.Show($"Client executable not found at:\n{clientExe}", "Missing Client File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string relSettings = _config.UseInternalIP
                ? profile.SettingsFile.Replace("settings.json", "settings.dev.json")
                : profile.SettingsFile;

            string settingsPath = Path.GetFullPath(Path.Combine(_baseDir, relSettings));
            string uoDataPath = Path.GetFullPath(Path.Combine(_baseDir, _config.UODataPath));

            string pluginFullPath = "";
            if (!string.IsNullOrWhiteSpace(profile.PluginPath))
            {
                pluginFullPath = Path.GetFullPath(Path.Combine(_baseDir, profile.PluginPath));
                if (!File.Exists(pluginFullPath))
                {
                    MessageBox.Show($"Assistant plugin not found at:\n{pluginFullPath}", "Missing Assistant", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            SyncClientSettingsJson(settingsPath, _config.ActiveIP, _config.ActivePort, uoDataPath, _config.ClientVersion, pluginFullPath);

            string args = $"-settings \"{settingsPath}\" -uopath \"{uoDataPath}\" -ip \"{_config.ActiveIP}\" -port {_config.ActivePort} -clientversion \"{_config.ClientVersion}\" -encryption 0";
            if (!string.IsNullOrWhiteSpace(pluginFullPath))
            {
                args += $" -plugins \"{pluginFullPath}\"";
            }

            try
            {
                _patchStatusLabel.Text = $"Launching {profileKey} ({_config.ActiveIP}:{_config.ActivePort})...";
                var psi = new ProcessStartInfo
                {
                    FileName = clientExe,
                    Arguments = args,
                    WorkingDirectory = Path.GetDirectoryName(clientExe),
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start client:\n{ex.Message}", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
