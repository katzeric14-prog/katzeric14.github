using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Vexor.Core
{
    public enum DiagnosticLevel
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    public sealed class ModIssue
    {
        public DiagnosticLevel Level;
        public string Title;
        public string Detail;
        public string File;

        public ModIssue(DiagnosticLevel level, string title, string detail)
        {
            Level = level;
            Title = title;
            Detail = detail;
            File = "";
        }

        public ModIssue(DiagnosticLevel level, string title, string detail, string file)
        {
            Level = level;
            Title = title;
            Detail = detail;
            File = file ?? "";
        }
    }

    public sealed class ModDependencyInfo
    {
        public string Id;
        public string Range;
        public bool Required = true;
    }

    public sealed class ScannedMod
    {
        public string File;
        public string Id;
        public string Name;
        public string Version;
        public string Loader;
        public string Environment = "*";
        public List<ModDependencyInfo> Requires = new List<ModDependencyInfo>();
        public List<ModDependencyInfo> Breaks = new List<ModDependencyInfo>();
        public Dictionary<string, string> Provided = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class DiagnosticReport
    {
        public string TargetTitle;
        public string TargetSubtitle;
        public string ModsDir;
        public string Loader;
        public string LoaderVersion;
        public string GameVersion;
        public bool IsModpack;
        public bool ModsDirExists;
        public int ModCount;
        public int DisabledCount;
        public int FabricMods;
        public int ForgeMods;
        public int UnknownMods;
        public int EffectiveRamMb;
        public int ConfiguredRamMb;
        public int RecommendedRamMb;
        public int TotalRamMb;
        public int AvailableRamMb;
        public bool RamOverridden;
        public DiagnosticLevel MemoryLevel;
        public List<ModIssue> Issues = new List<ModIssue>();
        public DateTime ScannedAt;

        public int ErrorCount { get { return Count(DiagnosticLevel.Error); } }
        public int WarningCount { get { return Count(DiagnosticLevel.Warning); } }
        public int InfoCount { get { return Count(DiagnosticLevel.Info); } }

        private int Count(DiagnosticLevel level)
        {
            int count = 0;
            for (int i = 0; i < Issues.Count; i++) if (Issues[i].Level == level) count++;
            return count;
        }
    }

    public static class ModDiagnostics
    {
        private static readonly HashSet<string> BuiltIn = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "minecraft", "java", "fabric", "fabricloader", "fabric-loader", "fabric_loader",
            "forge", "neoforge", "quilt_loader", "quilt-loader", "javafml", "lowcodefml",
            "mcp", "modlauncher"
        };

        private sealed class ConflictRule
        {
            public string First;
            public string Second;
            public string Message;

            public ConflictRule(string first, string second, string message)
            {
                First = first;
                Second = second;
                Message = message;
            }
        }

        private static readonly ConflictRule[] Conflicts =
        {
            new ConflictRule("sodium", "optifine",
                "Sodium и OptiFine заменяют один и тот же рендер и вместе не работают."),
            new ConflictRule("sodium", "optifabric",
                "OptiFabric требует OptiFine, который конфликтует с Sodium."),
            new ConflictRule("iris", "optifine",
                "Iris и OptiFine — взаимоисключающие шейдерные движки."),
            new ConflictRule("iris", "optifabric",
                "OptiFabric (OptiFine) несовместим с Iris."),
            new ConflictRule("embeddium", "rubidium",
                "Embeddium и Rubidium — два форка одного рендера, оставьте один."),
            new ConflictRule("sodium", "embeddium",
                "Sodium и Embeddium одновременно устанавливать нельзя.")
        };

        public static DiagnosticReport Analyze(string modsDir, string loader, string loaderVersion,
            string gameVersion, int configuredRamMb, string extraJvmArgs, int totalRamMb, int availableRamMb,
            bool isModpack, string targetTitle, string targetSubtitle)
        {
            var report = new DiagnosticReport();
            report.ModsDir = modsDir ?? "";
            report.Loader = loader ?? "";
            report.LoaderVersion = loaderVersion ?? "";
            report.GameVersion = gameVersion ?? "";
            report.IsModpack = isModpack;
            report.TargetTitle = targetTitle ?? "";
            report.TargetSubtitle = targetSubtitle ?? "";
            report.ConfiguredRamMb = configuredRamMb;
            report.TotalRamMb = totalRamMb;
            report.AvailableRamMb = availableRamMb;
            report.ScannedAt = DateTime.Now;
            report.ModsDirExists = !string.IsNullOrEmpty(modsDir) && Directory.Exists(modsDir);

            var mods = new List<ScannedMod>();
            if (report.ModsDirExists) ScanFolder(modsDir, mods, report);
            report.ModCount = mods.Count;

            for (int i = 0; i < mods.Count; i++)
            {
                if (string.Equals(mods[i].Loader, "fabric", StringComparison.OrdinalIgnoreCase)) report.FabricMods++;
                else if (string.Equals(mods[i].Loader, "forge", StringComparison.OrdinalIgnoreCase)) report.ForgeMods++;
                else report.UnknownMods++;
            }

            var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < mods.Count; i++)
            {
                if (!string.IsNullOrEmpty(mods[i].Id) && !versions.ContainsKey(mods[i].Id))
                    versions[mods[i].Id] = mods[i].Version ?? "";
                foreach (var pair in mods[i].Provided)
                    if (!versions.ContainsKey(pair.Key)) versions[pair.Key] = pair.Value ?? "";
            }

            CheckDuplicates(mods, report);
            CheckLoaders(mods, report);
            CheckDependencies(mods, versions, report);
            CheckEnvironment(mods, report);
            CheckConflicts(versions, report);
            CheckModCount(report);
            CheckMemory(report, extraJvmArgs, totalRamMb, availableRamMb);

            report.Issues.Sort(delegate(ModIssue a, ModIssue b)
            {
                if (a.Level != b.Level) return b.Level.CompareTo(a.Level);
                return string.Compare(a.Title, b.Title, StringComparison.CurrentCulture);
            });
            return report;
        }

        private static void ScanFolder(string dir, List<ScannedMod> mods, DiagnosticReport report)
        {
            string[] files;
            try
            {
                files = Directory.GetFiles(dir);
            }
            catch (Exception ex)
            {
                report.Issues.Add(new ModIssue(DiagnosticLevel.Error, "Нет доступа к папке mods", ex.Message, dir));
                return;
            }

            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                string name = Path.GetFileName(file);
                if (name.EndsWith(".jar.disabled", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
                {
                    report.DisabledCount++;
                    continue;
                }
                if (!name.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
                {
                    string ext = Path.GetExtension(name);
                    if (string.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(ext, ".rar", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(ext, ".7z", StringComparison.OrdinalIgnoreCase))
                        report.Issues.Add(new ModIssue(DiagnosticLevel.Warning,
                            "Архив в папке mods", "«" + name + "» не мод: Minecraft загружает только .jar.", name));
                    continue;
                }

                ScannedMod mod = ReadMod(file);
                if (mod == null)
                {
                    report.Issues.Add(new ModIssue(DiagnosticLevel.Error,
                        "Повреждённый мод", "Не удалось прочитать «" + name + "» — файл скачан не полностью или повреждён.",
                        name));
                    continue;
                }
                if (string.IsNullOrEmpty(mod.Id))
                {
                    report.Issues.Add(new ModIssue(DiagnosticLevel.Warning,
                        "Неизвестный формат мода", "В «" + name + "» нет fabric.mod.json, mods.toml или mcmod.info.", name));
                    mod.Id = "";
                }
                mods.Add(mod);
            }
        }

        private static ScannedMod ReadMod(string path)
        {
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(path))
                {
                    ZipArchiveEntry fabric = FindEntry(archive, "fabric.mod.json");
                    if (fabric != null)
                    {
                        ScannedMod mod = ReadFabric(archive, fabric);
                        if (mod != null)
                        {
                            mod.File = Path.GetFileName(path);
                            ReadNestedJars(archive, mod);
                        }
                        return mod;
                    }

                    ZipArchiveEntry forge = FindEntry(archive, "META-INF/mods.toml");
                    if (forge == null) forge = FindEntry(archive, "META-INF/neoforge.mods.toml");
                    if (forge != null)
                    {
                        ScannedMod mod = ReadForgeToml(forge);
                        if (mod != null)
                        {
                            mod.File = Path.GetFileName(path);
                            ReadNestedForgeJars(archive, mod);
                        }
                        return mod;
                    }

                    ZipArchiveEntry legacy = FindEntry(archive, "mcmod.info");
                    if (legacy != null)
                    {
                        ScannedMod mod = ReadMcmodInfo(legacy);
                        if (mod != null) mod.File = Path.GetFileName(path);
                        return mod;
                    }

                    var unknown = new ScannedMod();
                    unknown.File = Path.GetFileName(path);
                    unknown.Loader = "unknown";
                    unknown.Name = Path.GetFileNameWithoutExtension(path);
                    unknown.Version = "";
                    return unknown;
                }
            }
            catch
            {
                return null;
            }
        }

        private static ScannedMod ReadFabric(ZipArchive archive, ZipArchiveEntry entry)
        {
            try
            {
                JsonValue root = JsonParser.Parse(ReadEntryText(entry));
                var mod = new ScannedMod();
                mod.Loader = "fabric";
                mod.Id = root["id"].AsString("");
                mod.Name = root["name"].AsString(mod.Id);
                mod.Version = root["version"].AsString("");
                mod.Environment = root["environment"].AsString("*");
                ReadFabricDependencies(root["depends"], true, mod);
                ReadFabricDependencies(root["breaks"], false, mod);

                foreach (JsonValue provided in root["provides"].Items)
                {
                    string id = provided.AsString("");
                    if (!string.IsNullOrEmpty(id)) mod.Provided[id] = mod.Version;
                }
                if (string.IsNullOrEmpty(mod.Id)) mod.Id = "";
                return mod;
            }
            catch
            {
                return null;
            }
        }

        private static void ReadFabricDependencies(JsonValue value, bool required, ScannedMod mod)
        {
            if (!value.IsObject) return;
            foreach (var pair in value.Properties)
            {
                var dependency = new ModDependencyInfo();
                dependency.Id = pair.Key;
                dependency.Required = required;
                dependency.Range = ValueAsRange(pair.Value);
                if (required) mod.Requires.Add(dependency);
                else mod.Breaks.Add(dependency);
            }
        }

        private static string ValueAsRange(JsonValue value)
        {
            if (value == null) return "";
            if (value.IsString) return value.AsString("");
            if (value.IsArray && value.Count > 0) return value[0].AsString("");
            return "";
        }

        private static void ReadNestedJars(ZipArchive archive, ScannedMod mod)
        {
            for (int i = 0; i < archive.Entries.Count; i++)
            {
                ZipArchiveEntry nestedEntry = archive.Entries[i];
                string nestedName = nestedEntry.FullName ?? "";
                if (!nestedName.StartsWith("META-INF/jars/", StringComparison.OrdinalIgnoreCase) ||
                    !nestedName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                    using (Stream stream = nestedEntry.Open())
                    using (var nested = new ZipArchive(stream, ZipArchiveMode.Read))
                    {
                        ZipArchiveEntry fabric = FindEntry(nested, "fabric.mod.json");
                        if (fabric == null) continue;
                        JsonValue root = JsonParser.Parse(ReadEntryText(fabric));
                        string id = root["id"].AsString("");
                        string version = root["version"].AsString("");
                        if (!string.IsNullOrEmpty(id) && !mod.Provided.ContainsKey(id))
                            mod.Provided[id] = version;
                        foreach (JsonValue provided in root["provides"].Items)
                        {
                            string providedId = provided.AsString("");
                            if (!string.IsNullOrEmpty(providedId) && !mod.Provided.ContainsKey(providedId))
                                mod.Provided[providedId] = version;
                        }
                    }
                }
                catch
                {
                }
            }
        }

        private static void ReadNestedForgeJars(ZipArchive archive, ScannedMod mod)
        {
            for (int i = 0; i < archive.Entries.Count; i++)
            {
                ZipArchiveEntry nestedEntry = archive.Entries[i];
                string nestedName = nestedEntry.FullName ?? "";
                if (!nestedName.StartsWith("META-INF/jarjar/", StringComparison.OrdinalIgnoreCase) ||
                    !nestedName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                    using (Stream stream = nestedEntry.Open())
                    using (var nested = new ZipArchive(stream, ZipArchiveMode.Read))
                    {
                        ZipArchiveEntry toml = FindEntry(nested, "META-INF/mods.toml");
                        if (toml == null) toml = FindEntry(nested, "META-INF/neoforge.mods.toml");
                        if (toml == null) continue;
                        ScannedMod embedded = ReadForgeToml(toml);
                        if (embedded == null || string.IsNullOrEmpty(embedded.Id)) continue;
                        if (!mod.Provided.ContainsKey(embedded.Id)) mod.Provided[embedded.Id] = embedded.Version;
                    }
                }
                catch
                {
                }
            }
        }

        private static ScannedMod ReadForgeToml(ZipArchiveEntry entry)
        {
            try
            {
                string text = ReadEntryText(entry);
                var mod = new ScannedMod();
                mod.Loader = "forge";
                string section = "";
                ModDependencyInfo dependency = null;

                foreach (string raw in text.Replace("\r", "").Split('\n'))
                {
                    string line = StripTomlComment(raw).Trim();
                    if (line.Length == 0) continue;
                    if (line.StartsWith("[[", StringComparison.Ordinal))
                    {
                        if (dependency != null)
                        {
                            AddForgeDependency(mod, dependency);
                            dependency = null;
                        }
                        if (line.StartsWith("[[mods]]", StringComparison.OrdinalIgnoreCase))
                        {
                            section = "mods";
                        }
                        else if (line.StartsWith("[[dependencies", StringComparison.OrdinalIgnoreCase))
                        {
                            section = "dependency";
                            dependency = new ModDependencyInfo();
                        }
                        else
                        {
                            section = "other";
                        }
                        continue;
                    }
                    if (line.StartsWith("[", StringComparison.Ordinal)) { section = "other"; continue; }

                    string key;
                    string value;
                    if (!SplitTomlPair(line, out key, out value)) continue;
                    if (string.Equals(section, "mods", StringComparison.Ordinal))
                    {
                        if (string.Equals(key, "modId", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(mod.Id))
                            mod.Id = value;
                        else if (string.Equals(key, "displayName", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(mod.Name))
                            mod.Name = value;
                        else if (string.Equals(key, "version", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(mod.Version))
                            mod.Version = value;
                    }
                    else if (string.Equals(section, "dependency", StringComparison.Ordinal) && dependency != null)
                    {
                        if (string.Equals(key, "modId", StringComparison.OrdinalIgnoreCase)) dependency.Id = value;
                        else if (string.Equals(key, "mandatory", StringComparison.OrdinalIgnoreCase))
                            dependency.Required = !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
                        else if (string.Equals(key, "versionRange", StringComparison.OrdinalIgnoreCase)) dependency.Range = value;
                    }
                }

                if (dependency != null) AddForgeDependency(mod, dependency);
                if (string.IsNullOrEmpty(mod.Name)) mod.Name = mod.Id;
                return mod;
            }
            catch
            {
                return null;
            }
        }

        private static void AddForgeDependency(ScannedMod mod, ModDependencyInfo dependency)
        {
            if (dependency == null || string.IsNullOrEmpty(dependency.Id)) return;
            mod.Requires.Add(dependency);
        }

        private static ScannedMod ReadMcmodInfo(ZipArchiveEntry entry)
        {
            try
            {
                JsonValue root = JsonParser.Parse(ReadEntryText(entry));
                JsonValue info = root;
                if (root.IsArray)
                {
                    if (root.Count == 0) return null;
                    info = root[0];
                }
                var mod = new ScannedMod();
                mod.Loader = "forge";
                mod.Id = info["modid"].AsString(info["modId"].AsString(""));
                mod.Name = info["name"].AsString(mod.Id);
                mod.Version = info["version"].AsString("");
                return mod;
            }
            catch
            {
                return null;
            }
        }

        private static void CheckDuplicates(List<ScannedMod> mods, DiagnosticReport report)
        {
            var byId = new Dictionary<string, List<ScannedMod>>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < mods.Count; i++)
            {
                ScannedMod mod = mods[i];
                if (string.IsNullOrEmpty(mod.Id)) continue;
                List<ScannedMod> list;
                if (!byId.TryGetValue(mod.Id, out list))
                {
                    list = new List<ScannedMod>();
                    byId[mod.Id] = list;
                }
                list.Add(mod);
            }

            foreach (var pair in byId)
            {
                if (pair.Value.Count < 2) continue;
                var files = new List<string>();
                for (int i = 0; i < pair.Value.Count; i++) files.Add(pair.Value[i].File);
                report.Issues.Add(new ModIssue(DiagnosticLevel.Error,
                    "Дубликат мода «" + pair.Key + "»",
                    "Один и тот же мод установлен несколько раз: " + string.Join(", ", files.ToArray()) +
                    ". Оставьте только одну версию.", files[0]));
            }
        }

        private static void CheckLoaders(List<ScannedMod> mods, DiagnosticReport report)
        {
            if (string.IsNullOrEmpty(report.Loader)) return;
            for (int i = 0; i < mods.Count; i++)
            {
                ScannedMod mod = mods[i];
                if (string.Equals(mod.Loader, "unknown", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(report.Loader, "fabric", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(mod.Loader, "forge", StringComparison.OrdinalIgnoreCase))
                    report.Issues.Add(new ModIssue(DiagnosticLevel.Error,
                        "Мод для Forge в Fabric-сборке", "«" + mod.Name + "» не загрузится: нужен Forge.", mod.File));
                else if (string.Equals(report.Loader, "forge", StringComparison.OrdinalIgnoreCase) &&
                         string.Equals(mod.Loader, "fabric", StringComparison.OrdinalIgnoreCase))
                    report.Issues.Add(new ModIssue(DiagnosticLevel.Error,
                        "Мод для Fabric в Forge-сборке", "«" + mod.Name + "» не загрузится: нужен Fabric.", mod.File));
            }
        }

        private static void CheckDependencies(List<ScannedMod> mods, Dictionary<string, string> versions,
            DiagnosticReport report)
        {
            for (int i = 0; i < mods.Count; i++)
            {
                ScannedMod mod = mods[i];
                for (int d = 0; d < mod.Requires.Count; d++)
                {
                    ModDependencyInfo dependency = mod.Requires[d];
                    if (string.IsNullOrEmpty(dependency.Id)) continue;
                    bool builtIn = BuiltIn.Contains(dependency.Id);
                    string installed;
                    versions.TryGetValue(dependency.Id, out installed);

                    if (string.Equals(dependency.Id, "minecraft", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(report.GameVersion) && !string.IsNullOrEmpty(dependency.Range) &&
                            !VersionRange.Matches(dependency.Range, report.GameVersion))
                            report.Issues.Add(new ModIssue(DiagnosticLevel.Error,
                                "Мод не поддерживает Minecraft " + report.GameVersion,
                                "«" + mod.Name + "» требует " + dependency.Range + ".", mod.File));
                        continue;
                    }

                    if (string.Equals(dependency.Id, "forge", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(dependency.Id, "fabricloader", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(dependency.Id, "fabric-loader", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(report.LoaderVersion) && !string.IsNullOrEmpty(dependency.Range) &&
                            !VersionRange.Matches(dependency.Range, report.LoaderVersion))
                            report.Issues.Add(new ModIssue(DiagnosticLevel.Error,
                                "Нужен другой загрузчик", "«" + mod.Name + "» требует " + dependency.Id + " " +
                                dependency.Range + ", установлен " + report.LoaderVersion + ".", mod.File));
                        continue;
                    }

                    if (builtIn) continue;

                    if (installed == null)
                    {
                        if (dependency.Required)
                            report.Issues.Add(new ModIssue(DiagnosticLevel.Error,
                                "Не хватает зависимости «" + dependency.Id + "»",
                                "«" + mod.Name + "» не запустится без " + dependency.Id +
                                (string.IsNullOrEmpty(dependency.Range) ? "." : " " + dependency.Range + "."), mod.File));
                        continue;
                    }

                    if (!string.IsNullOrEmpty(dependency.Range) && !string.IsNullOrEmpty(installed) &&
                        !VersionRange.Matches(dependency.Range, installed))
                        report.Issues.Add(new ModIssue(DiagnosticLevel.Warning,
                            "Версия «" + dependency.Id + "» не подходит",
                            "«" + mod.Name + "» требует " + dependency.Range + ", установлено " + installed + ".", mod.File));
                }

                for (int b = 0; b < mod.Breaks.Count; b++)
                {
                    ModDependencyInfo broken = mod.Breaks[b];
                    if (string.IsNullOrEmpty(broken.Id) || broken.Id == "*") continue;
                    string ignored;
                    if (!versions.TryGetValue(broken.Id, out ignored)) continue;
                    if (!string.IsNullOrEmpty(broken.Range) && !VersionRange.Matches(broken.Range, ignored)) continue;
                    report.Issues.Add(new ModIssue(DiagnosticLevel.Error,
                        "Конфликт с «" + broken.Id + "»",
                        "«" + mod.Name + "» явно несовместим с " + broken.Id +
                        (string.IsNullOrEmpty(broken.Range) ? "." : " " + broken.Range + "."), mod.File));
                }
            }
        }

        private static void CheckEnvironment(List<ScannedMod> mods, DiagnosticReport report)
        {
            for (int i = 0; i < mods.Count; i++)
            {
                ScannedMod mod = mods[i];
                if (string.Equals(mod.Environment, "server", StringComparison.OrdinalIgnoreCase))
                    report.Issues.Add(new ModIssue(DiagnosticLevel.Warning,
                        "Мод только для сервера", "«" + mod.Name + "» в клиенте не нужен и может вызвать ошибку.", mod.File));
            }
        }

        private static void CheckConflicts(Dictionary<string, string> versions, DiagnosticReport report)
        {
            for (int i = 0; i < Conflicts.Length; i++)
            {
                ConflictRule rule = Conflicts[i];
                if (!versions.ContainsKey(rule.First) || !versions.ContainsKey(rule.Second)) continue;
                report.Issues.Add(new ModIssue(DiagnosticLevel.Error,
                    "Несовместимые моды", rule.First + " + " + rule.Second + ": " + rule.Message));
            }
        }

        private static void CheckModCount(DiagnosticReport report)
        {
            if (report.ModCount > 250)
                report.Issues.Add(new ModIssue(DiagnosticLevel.Error, "Слишком много модов",
                    report.ModCount + " модов — высокий риск конфликтов и долгая загрузка. Соберите более лёгкую сборку."));
            else if (report.ModCount > 150)
                report.Issues.Add(new ModIssue(DiagnosticLevel.Warning, "Много модов",
                    report.ModCount + " модов — проверяйте конфликты и следите за производительностью."));
        }

        private static void CheckMemory(DiagnosticReport report, string extraJvmArgs, int totalRamMb, int availableRamMb)
        {
            report.EffectiveRamMb = ParseXmx(extraJvmArgs, report.ConfiguredRamMb);
            report.RamOverridden = report.EffectiveRamMb != report.ConfiguredRamMb;
            report.RecommendedRamMb = RecommendRam(report.ModCount, report.IsModpack, totalRamMb);
            report.MemoryLevel = DiagnosticLevel.Info;

            int effective = report.EffectiveRamMb;
            int recommended = report.RecommendedRamMb;

            if (effective < recommended * 3 / 4)
            {
                report.MemoryLevel = DiagnosticLevel.Error;
                report.Issues.Add(new ModIssue(DiagnosticLevel.Error, "Мало оперативной памяти",
                    "Выделено " + FormatRam(effective) + ", для " + report.ModCount + " модов рекомендуется " +
                    FormatRam(recommended) + ". Увеличьте память в «Настройках»."));
            }
            else if (effective < recommended)
            {
                report.MemoryLevel = DiagnosticLevel.Warning;
                report.Issues.Add(new ModIssue(DiagnosticLevel.Warning, "Память на нижней границе",
                    "Выделено " + FormatRam(effective) + " при рекомендации " + FormatRam(recommended) +
                    ". Возможны подвисания при загрузке."));
            }

            if (totalRamMb > 0 && effective > totalRamMb * 9 / 10)
            {
                report.MemoryLevel = DiagnosticLevel.Error;
                report.Issues.Add(new ModIssue(DiagnosticLevel.Error, "Слишком много памяти для системы",
                    "Выделено " + FormatRam(effective) + " из " + FormatRam(totalRamMb) +
                    " — Windows может остаться без памяти. Уменьшите до " +
                    FormatRam(Math.Max(2048, totalRamMb - 4096)) + "."));
            }
            else if (totalRamMb > 0 && effective > totalRamMb * 4 / 5)
            {
                if (report.MemoryLevel < DiagnosticLevel.Warning) report.MemoryLevel = DiagnosticLevel.Warning;
                report.Issues.Add(new ModIssue(DiagnosticLevel.Warning, "Почти вся память системе",
                    "Выделено " + FormatRam(effective) + " из " + FormatRam(totalRamMb) +
                    " — оставьте Windows хотя бы 4 ГБ."));
            }

            if (availableRamMb > 0 && availableRamMb < effective / 2)
            {
                if (report.MemoryLevel < DiagnosticLevel.Warning) report.MemoryLevel = DiagnosticLevel.Warning;
                report.Issues.Add(new ModIssue(DiagnosticLevel.Warning, "Сейчас мало свободной памяти",
                    "Свободно " + FormatRam(availableRamMb) + " при выделенных " + FormatRam(effective) +
                    ". Закройте лишние программы перед запуском."));
            }

            if (!Environment.Is64BitOperatingSystem && effective > 1024)
            {
                report.MemoryLevel = DiagnosticLevel.Error;
                report.Issues.Add(new ModIssue(DiagnosticLevel.Error, "32-битная Windows",
                    "Система не сможет выделить больше ~1,5 ГБ: нужна 64-битная Windows и 64-битная Java."));
            }
        }

        public static int RecommendRam(int modCount, bool modpack, int totalRamMb)
        {
            int recommended = 1024;
            if (modCount > 0) recommended += 256;
            recommended += modCount * 48;
            if (modpack) recommended += 512;
            if (modCount > 0) recommended = Math.Max(recommended, 2048);
            if (modCount > 200) recommended = Math.Max(recommended, 10240);
            if (modCount > 300) recommended = Math.Max(recommended, 12288);
            recommended = ((recommended + 511) / 512) * 512;
            if (recommended > 8192 && modCount <= 200) recommended = 8192;
            if (totalRamMb > 0)
            {
                int limit = ((Math.Max(2048, totalRamMb - 4096) + 511) / 512) * 512;
                if (recommended > limit) recommended = limit;
                if (recommended < 2048) recommended = 2048;
            }
            return recommended;
        }

        private static int ParseXmx(string extraJvmArgs, int fallback)
        {
            List<string> args = GameLauncher.SplitArguments(extraJvmArgs);
            int value = fallback;
            for (int i = 0; i < args.Count; i++)
            {
                string arg = args[i];
                if (arg == null || arg.Length < 5) continue;
                if (!arg.StartsWith("-Xmx", StringComparison.OrdinalIgnoreCase)) continue;
                int mb = ParseMemoryMb(arg.Substring(4));
                if (mb > 0) value = mb;
            }
            return value;
        }

        private static int ParseMemoryMb(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            string value = text.Trim();
            int multiplier = 1;
            char suffix = value[value.Length - 1];
            if (suffix == 'k' || suffix == 'K') { multiplier = 1; value = value.Substring(0, value.Length - 1); }
            else if (suffix == 'm' || suffix == 'M') { multiplier = 1; value = value.Substring(0, value.Length - 1); }
            else if (suffix == 'g' || suffix == 'G') { multiplier = 1024; value = value.Substring(0, value.Length - 1); }
            int number;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return 0;
            if (number <= 0) return 0;
            if (multiplier == 1 && (suffix == 'k' || suffix == 'K')) return Math.Max(1, number / 1024);
            return number * multiplier;
        }

        public static string FormatRam(int mb)
        {
            if (mb >= 1024)
                return (mb / 1024.0).ToString("0.#", CultureInfo.GetCultureInfo("ru-RU")) + " ГБ";
            return mb.ToString(CultureInfo.InvariantCulture) + " МБ";
        }

        private static ZipArchiveEntry FindEntry(ZipArchive archive, string name)
        {
            ZipArchiveEntry entry = archive.GetEntry(name);
            if (entry != null) return entry;
            for (int i = 0; i < archive.Entries.Count; i++)
            {
                if (string.Equals(archive.Entries[i].FullName, name, StringComparison.OrdinalIgnoreCase))
                    return archive.Entries[i];
            }
            return null;
        }

        private static string ReadEntryText(ZipArchiveEntry entry)
        {
            using (Stream stream = entry.Open())
            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                return reader.ReadToEnd();
        }

        private static string StripTomlComment(string line)
        {
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"' && (i == 0 || line[i - 1] != '\\')) quoted = !quoted;
                if (!quoted && c == '#') return line.Substring(0, i);
            }
            return line;
        }

        private static bool SplitTomlPair(string line, out string key, out string value)
        {
            key = "";
            value = "";
            int eq = line.IndexOf('=');
            if (eq <= 0) return false;
            key = line.Substring(0, eq).Trim();
            value = line.Substring(eq + 1).Trim();
            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                value = value.Substring(1, value.Length - 2);
            return key.Length > 0;
        }

        private static class VersionRange
        {
            public static bool Matches(string range, string version)
            {
                if (string.IsNullOrEmpty(range)) return true;
                string text = range.Trim();
                if (text.Length == 0 || text == "*") return true;
                string[] alternatives = text.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < alternatives.Length; i++)
                    if (MatchesAll(alternatives[i].Trim(), version)) return true;
                return alternatives.Length == 0;
            }

            private static bool MatchesAll(string range, string version)
            {
                if (range.Length == 0) return true;
                if (range[0] == '[' || range[0] == '(') return MatchesMaven(range, version);

                string[] tokens = range.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0) return true;
                for (int i = 0; i < tokens.Length; i++)
                    if (!MatchesToken(tokens[i], version)) return false;
                return true;
            }

            private static bool MatchesToken(string token, string version)
            {
                if (token.Length == 0) return true;
                if (token[0] == '[' || token[0] == '(') return MatchesMaven(token, version);

                if (token.StartsWith(">=", StringComparison.Ordinal)) return Compare(version, Clean(token.Substring(2))) >= 0;
                if (token.StartsWith("<=", StringComparison.Ordinal)) return Compare(version, Clean(token.Substring(2))) <= 0;
                if (token.StartsWith(">", StringComparison.Ordinal)) return Compare(version, Clean(token.Substring(1))) > 0;
                if (token.StartsWith("<", StringComparison.Ordinal)) return Compare(version, Clean(token.Substring(1))) < 0;
                if (token.StartsWith("=", StringComparison.Ordinal)) return Compare(version, Clean(token.Substring(1))) == 0;

                if (token.StartsWith("^", StringComparison.Ordinal))
                {
                    string baseVersion = Clean(token.Substring(1));
                    int[] parts = Parse(baseVersion);
                    int major = parts.Length > 0 ? parts[0] : 0;
                    int minor = parts.Length > 1 ? parts[1] : 0;
                    int[] upper;
                    if (major > 0) upper = new[] { major + 1, 0, 0 };
                    else if (minor > 0) upper = new[] { 0, minor + 1, 0 };
                    else upper = new[] { 0, 0, (parts.Length > 2 ? parts[2] : 0) + 1 };
                    return Compare(version, baseVersion) >= 0 && CompareParts(Parse(version), upper) < 0;
                }

                if (token.StartsWith("~", StringComparison.Ordinal))
                {
                    string baseVersion = Clean(token.Substring(1));
                    int[] parts = Parse(baseVersion);
                    int[] upper = new[] { parts.Length > 0 ? parts[0] : 0, (parts.Length > 1 ? parts[1] : 0) + 1, 0 };
                    return Compare(version, baseVersion) >= 0 && CompareParts(Parse(version), upper) < 0;
                }

                if (token.EndsWith(".x", StringComparison.OrdinalIgnoreCase) ||
                    token.EndsWith(".*", StringComparison.OrdinalIgnoreCase))
                    return MatchesPrefix(token.Substring(0, token.Length - 1), version);

                return Compare(version, Clean(token)) == 0;
            }

            private static bool MatchesPrefix(string prefix, string version)
            {
                int[] wanted = Parse(prefix);
                int[] actual = Parse(version);
                if (wanted.Length > actual.Length) return false;
                for (int i = 0; i < wanted.Length; i++) if (wanted[i] != actual[i]) return false;
                return true;
            }

            private static bool MatchesMaven(string range, string version)
            {
                if (range.Length < 2) return true;
                char first = range[0];
                char last = range[range.Length - 1];
                if ((first != '[' && first != '(') || (last != ']' && last != ')')) return true;
                string inner = range.Substring(1, range.Length - 2).Trim();
                string[] parts = inner.Split(',');
                if (parts.Length == 1)
                    return Compare(version, Clean(parts[0])) == 0;

                string low = parts[0].Trim();
                string high = parts[1].Trim();
                if (low.Length > 0)
                {
                    int cmp = Compare(version, Clean(low));
                    if (first == '[' ? cmp < 0 : cmp <= 0) return false;
                }
                if (high.Length > 0)
                {
                    int cmp = Compare(version, Clean(high));
                    if (last == ']' ? cmp > 0 : cmp >= 0) return false;
                }
                return true;
            }

            private static string Clean(string text)
            {
                if (text == null) return "";
                string value = text.Trim();
                while (value.EndsWith("-", StringComparison.Ordinal)) value = value.Substring(0, value.Length - 1);
                int plus = value.IndexOf('+');
                if (plus > 0) value = value.Substring(0, plus);
                if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase) && value.Length > 1 &&
                    char.IsDigit(value[1])) value = value.Substring(1);
                return value.Trim();
            }

            private static int Compare(string left, string right)
            {
                return CompareParts(Parse(left), Parse(right));
            }

            private static int CompareParts(int[] left, int[] right)
            {
                int count = Math.Max(left.Length, right.Length);
                for (int i = 0; i < count; i++)
                {
                    int a = i < left.Length ? left[i] : 0;
                    int b = i < right.Length ? right[i] : 0;
                    if (a != b) return a < b ? -1 : 1;
                }
                return 0;
            }

            private static int[] Parse(string version)
            {
                string text = Clean(version);
                int dash = text.IndexOf('-');
                if (dash > 0) text = text.Substring(0, dash);
                if (text.Length == 0) return new int[0];
                string[] segments = text.Split('.');
                var parts = new List<int>();
                for (int i = 0; i < segments.Length; i++)
                {
                    string segment = segments[i];
                    int number = 0;
                    int digits = 0;
                    while (digits < segment.Length && char.IsDigit(segment[digits]))
                    {
                        number = number * 10 + (segment[digits] - '0');
                        digits++;
                    }
                    if (digits == 0) break;
                    parts.Add(number);
                }
                return parts.ToArray();
            }
        }
    }
}
