using System;
using System.Collections.Generic;

namespace Vexor.Core
{
    public sealed class ModProject
    {
        public string ProjectId;
        public string ApiId;
        public string Slug;
        public string Title;
        public string Description;
        public string Author;
        public string IconUrl;
        public long Downloads;
        public string Source = "modrinth";
        public List<string> Categories = new List<string>();
    }

    public sealed class ModFile
    {
        public string Url;
        public string FileName;
        public string Sha1;
        public long Size;
        public bool Primary;
        public bool Available = true;
        public string Id;
        public string ModId;
    }

    public sealed class ModDependency
    {
        public string ProjectId;
        public string VersionId;
        public string Type;
    }

    public sealed class ModVersion
    {
        public string Id;
        public string ProjectId;
        public string Name;
        public string VersionNumber;
        public string VersionType;
        public string Source = "modrinth";
        public List<string> GameVersions = new List<string>();
        public List<string> Loaders = new List<string>();
        public List<ModFile> Files = new List<ModFile>();
        public List<ModDependency> Dependencies = new List<ModDependency>();

        public ModFile PrimaryFile
        {
            get
            {
                for (int i = 0; i < Files.Count; i++) if (Files[i].Primary) return Files[i];
                return Files.Count > 0 ? Files[0] : null;
            }
        }
    }

    public sealed class ModSearchResult
    {
        public List<ModProject> Hits = new List<ModProject>();
        public int TotalHits;
        public int Offset;
        public int Limit;
    }

    public sealed class ModInstallResult
    {
        public int Installed;
        public int AlreadyPresent;
        public List<string> Files = new List<string>();
        public List<string> Warnings = new List<string>();
    }
}
