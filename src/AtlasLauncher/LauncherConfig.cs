using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AtlasLauncher
{
    public class LauncherConfig
    {
        public string ShardName { get; set; } = "Project Atlas";
        public string ServerIP { get; set; } = "70.163.211.165";
        public int ServerPort { get; set; } = 2593;
        public string InternalIP { get; set; } = "127.0.0.1";
        public int InternalPort { get; set; } = 2594;
        public bool UseInternalIP { get; set; } = false;
        public string ClientVersion { get; set; } = "7.0.117.1";
        public string UODataPath { get; set; } = "UOFiles";
        public string PatchManifestUrl { get; set; } = "";
        public CommunityLinks Links { get; set; } = new CommunityLinks();
        public Dictionary<string, LaunchProfile> LaunchProfiles { get; set; } = new Dictionary<string, LaunchProfile>();

        public string ActiveIP => UseInternalIP ? InternalIP : ServerIP;
        public int ActivePort => UseInternalIP ? InternalPort : ServerPort;

        public static LauncherConfig Load(string path)
        {
            if (!File.Exists(path))
                return new LauncherConfig();

            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<LauncherConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new LauncherConfig();
        }
    }

    public class CommunityLinks
    {
        public string Discord { get; set; } = "";
        public string Wiki { get; set; } = "";
        public string Donate { get; set; } = "";
    }

    public class LaunchProfile
    {
        public string ClientExe { get; set; } = "";
        public string SettingsFile { get; set; } = "";
        public string PluginPath { get; set; } = "";
    }

    public class PatchManifest
    {
        public string Version { get; set; } = "1.0.0";
        public List<ManifestFileEntry> Files { get; set; } = new List<ManifestFileEntry>();
    }

    public class ManifestFileEntry
    {
        public string Path { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public long Size { get; set; }
    }
}
