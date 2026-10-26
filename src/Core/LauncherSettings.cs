using System;
using System.IO;
using System.Text;

namespace Vexor.Core
{
    public sealed class LauncherSettings
    {
        public string Nickname = "Player";
        public string GameDir = AppPaths.DefaultGameDir;
        public int RamMb = 4096;
        public bool OptimizeJvm = true;
        public bool VisualEnabled = true;
        public string ExtraJvmArgs = "";
        public string JavaPath = "";
        public string SelectedVersion = "";
        public int ResolutionWidth = 0;
        public int ResolutionHeight = 0;
        public string CurseForgeKey = "";
        public string ModCatalogSource = "modrinth";
        public string MicrosoftClientId = "";
        public string UpdateManifestUrl = "";

        public static LauncherSettings Load()
        {
            var s = new LauncherSettings();
            try
            {
                if (File.Exists(AppPaths.SettingsFile))
                {
                    JsonValue j = JsonParser.Parse(File.ReadAllText(AppPaths.SettingsFile));
                    s.Nickname = j["nickname"].AsString(s.Nickname);
                    s.GameDir = j["gameDir"].AsString(s.GameDir);
                    s.RamMb = j["ramMb"].AsInt(s.RamMb);
                    s.OptimizeJvm = j["optimizeJvm"].AsBool(s.OptimizeJvm);
                    s.VisualEnabled = j["visualEnabled"].AsBool(s.VisualEnabled);
                    s.ExtraJvmArgs = j["extraJvmArgs"].AsString(s.ExtraJvmArgs);
                    s.JavaPath = j["javaPath"].AsString(s.JavaPath);
                    s.SelectedVersion = j["selectedVersion"].AsString(s.SelectedVersion);
                    s.ResolutionWidth = j["resolutionWidth"].AsInt(s.ResolutionWidth);
                    s.ResolutionHeight = j["resolutionHeight"].AsInt(s.ResolutionHeight);
                    s.CurseForgeKey = j["curseForgeKey"].AsString(s.CurseForgeKey);
                    s.ModCatalogSource = j["modCatalogSource"].AsString(s.ModCatalogSource);
                    s.MicrosoftClientId = j["microsoftClientId"].AsString(s.MicrosoftClientId);
                    s.UpdateManifestUrl = j["updateManifestUrl"].AsString(s.UpdateManifestUrl);
                }
            }
            catch
            {
            }
            s.Normalize();
            return s;
        }

        public void Normalize()
        {
            if (string.IsNullOrWhiteSpace(Nickname)) Nickname = "Player";
            Nickname = Nickname.Trim();
            if (string.IsNullOrWhiteSpace(GameDir)) GameDir = AppPaths.DefaultGameDir;
            if (RamMb < 1024) RamMb = 1024;
            if (RamMb > 32768) RamMb = 32768;
            RamMb = (RamMb / 512) * 512;
            if (ResolutionWidth < 0) ResolutionWidth = 0;
            if (ResolutionHeight < 0) ResolutionHeight = 0;
            if (ResolutionWidth > 0 && ResolutionHeight <= 0) ResolutionHeight = 720;
            if (ResolutionHeight > 0 && ResolutionWidth <= 0) ResolutionWidth = 1280;
            CurseForgeKey = (CurseForgeKey ?? "").Trim();
            MicrosoftClientId = (MicrosoftClientId ?? "").Trim();
            UpdateManifestUrl = (UpdateManifestUrl ?? "").Trim();
            if (!string.Equals(ModCatalogSource, "curseforge", StringComparison.OrdinalIgnoreCase))
                ModCatalogSource = "modrinth";
            else ModCatalogSource = "curseforge";
        }

        public void Save()
        {
            try
            {
                Normalize();
                AppPaths.EnsureDirectories();
                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"nickname\": " + JsonText.Quote(Nickname) + ",");
                sb.AppendLine("  \"gameDir\": " + JsonText.Quote(GameDir) + ",");
                sb.AppendLine("  \"ramMb\": " + RamMb.ToString() + ",");
                sb.AppendLine("  \"optimizeJvm\": " + (OptimizeJvm ? "true" : "false") + ",");
                sb.AppendLine("  \"visualEnabled\": " + (VisualEnabled ? "true" : "false") + ",");
                sb.AppendLine("  \"extraJvmArgs\": " + JsonText.Quote(ExtraJvmArgs) + ",");
                sb.AppendLine("  \"javaPath\": " + JsonText.Quote(JavaPath) + ",");
                sb.AppendLine("  \"selectedVersion\": " + JsonText.Quote(SelectedVersion) + ",");
                sb.AppendLine("  \"resolutionWidth\": " + ResolutionWidth.ToString() + ",");
                sb.AppendLine("  \"resolutionHeight\": " + ResolutionHeight.ToString() + ",");
                sb.AppendLine("  \"curseForgeKey\": " + JsonText.Quote(CurseForgeKey) + ",");
                sb.AppendLine("  \"modCatalogSource\": " + JsonText.Quote(ModCatalogSource) + ",");
                sb.AppendLine("  \"microsoftClientId\": " + JsonText.Quote(MicrosoftClientId) + ",");
                sb.AppendLine("  \"updateManifestUrl\": " + JsonText.Quote(UpdateManifestUrl));
                sb.AppendLine("}");
                File.WriteAllText(AppPaths.SettingsFile, sb.ToString(), new UTF8Encoding(false));
            }
            catch
            {
            }
        }
    }
}
