using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vexor.Core;

namespace Vexor.Ui
{
    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        private const int WmNcLButtonDown = 0xA1;
        private const int HtCaption = 0x2;

        private readonly LauncherSettings _settings;
        private readonly object _logGate = new object();

        private SidebarPanel _sidebar;
        private Panel _titleBar;
        private PagePanel _playPage;
        private PagePanel _modsPage;
        private PagePanel _packsPage;
        private PagePanel _resourcesPage;
        private PagePanel _skinsPage;
        private PagePanel _serversPage;
        private PagePanel _diagnosticsPage;
        private PagePanel _settingsPage;
        private NavButton _navPlay;
        private NavButton _navMods;
        private NavButton _navPacks;
        private NavButton _navResources;
        private NavButton _navShaders;
        private NavButton _navServers;
        private NavButton _navSkins;
        private NavButton _navDiagnostics;
        private NavButton _navSettings;

        private VersionManifest _manifest;
        private List<VersionEntry> _all = new List<VersionEntry>();
        private List<InstalledBuild> _builds = new List<InstalledBuild>();
        private string _filter = "all";
        private VersionListView _versionList;
        private TextField _search;
        private Label _versionCount;
        private PillButton _pillAll;
        private PillButton _pillRelease;
        private PillButton _pillSnapshot;
        private PillButton _pillOld;
        private RoundedButton _refreshButton;

        private Label _versionTitle;
        private Label _versionMeta;
        private Label _status;
        private Label _fabricStatus;
        private Label _forgeStatus;
        private RoundedButton _fabricAction;
        private RoundedButton _forgeAction;
        private CardPanel _modCatalog;
        private TextField _modSearch;
        private RoundedButton _modFind;
        private RoundedButton _modMore;
        private RoundedButton _modFabricChoice;
        private RoundedButton _modForgeChoice;
        private RoundedButton _modModrinthChoice;
        private RoundedButton _modCurseChoice;
        private RoundedButton _modCategoryButton;
        private Label _modCatalogTarget;
        private Label _modCatalogStatus;
        private InstalledBuild _modBuild;
        private List<ModProject> _modResults = new List<ModProject>();
        private List<Control> _modRowControls = new List<Control>();
        private HashSet<string> _modInstalled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string _modCatalogKey = "";
        private string _modSearchedKey = "";
        private string _modSource = "modrinth";
        private int _modCategoryId;
        private List<CfCategory> _cfCategories;
        private Panel _categoryOverlay;
        private CategoryWheelFilter _categoryWheelFilter;
        private string _categoryStatusText;
        private Color _categoryStatusColor = Theme.Muted2;
        private int _modOffset;
        private int _modTotal;
        private int _modToken;
        private bool _modBusy;

        private CardPanel _packCatalog;
        private TextField _packSearch;
        private TextField _packVersion;
        private RoundedButton _packFind;
        private RoundedButton _packMore;
        private RoundedButton _packAllChoice;
        private RoundedButton _packFabricChoice;
        private RoundedButton _packForgeChoice;
        private Label _packTarget;
        private Label _packStatus;
        private ProgressBarEx _packProgress;
        private List<ModProject> _packResults = new List<ModProject>();
        private List<Control> _packRowControls = new List<Control>();
        private HashSet<string> _packInstalled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string _packLoader = "";
        private string _packSearchedKey = "";
        private int _packOffset;
        private int _packTotal;
        private int _packToken;
        private bool _packBusy;
        private CardPanel _resCatalog;
        private Label _resHeader;
        private Label _resSubtitle;
        private Label _resCardTitle;
        private TextField _resSearch;
        private RoundedButton _resFind;
        private RoundedButton _resMore;
        private Label _resTarget;
        private Label _resStatus;
        private bool _shaderMode;
        private List<ModProject> _resResults = new List<ModProject>();
        private List<Control> _resRowControls = new List<Control>();
        private HashSet<string> _resInstalled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string _resTargetKey = "";
        private string _resGameDir = "";
        private string _resGameVersion = "";
        private string _resSearchedKey = "";
        private int _resOffset;
        private int _resTotal;
        private int _resToken;
        private bool _resBusy;

        private TextField _nickField;
        private RoundedButton _playButton;
        private MsaSession _msa;
        private Label _accountDot;
        private Label _accountName;
        private RoundedButton _accountButton;
        private ProgressBarEx _progress;

        private LogView _logView;
        private StreamWriter _logFile;

        private TextField _setNick;
        private TextField _gameDir;
        private TextField _javaPath;
        private TextField _cfKey;
        private TextField _jvmArgs;
        private TextField _resW;
        private TextField _resH;
        private RamSlider _ramSlider;
        private Label _ramValue;
        private Label _ramHint;
        private Label _javaStatus;
        private ToggleSwitch _optimize;
        private Label _saveHint;
        private RoundedButton _updateButton;
        private System.Windows.Forms.Timer _saveTimer;

        private SkinPreview _skinCurrentView;
        private SkinPreview _skinSelectedView;
        private Label _skinAccountName;
        private Label _skinAccountHint;
        private Label _skinCurrentNote;
        private Label _skinFileName;
        private Label _skinStatus;
        private TextField _skinNick;
        private RoundedButton _skinFind;
        private RoundedButton _skinChoose;
        private RoundedButton _skinApply;
        private RoundedButton _skinRefresh;
        private PillButton _skinClassicPill;
        private PillButton _skinSlimPill;
        private string _skinSelectedPath;
        private bool _skinSelectedSlim;
        private bool _skinBusy;
        private bool _skinOwnLoaded;
        private SkinInfo _skinOwn;

        private Label[] _serverPlayers;
        private Label[] _serverPing;
        private RoundedButton[] _serverConnect;
        private RoundedButton _serverRefresh;
        private Label _serverStatus;
        private Label _serverVersionHint;
        private Panel _serverViewport;
        private Panel _serverContent;
        private DarkScrollBar _serverScroll;
        private ServerWheelFilter _serverWheelFilter;
        private bool _serverBusy;
        private int _serverGeneration;
        private DateTime _serverPingedUtc = DateTime.MinValue;

        private RoundedButton _diagRefresh;
        private RoundedButton _diagFolder;
        private Label _diagTarget;
        private Label _diagSubtitle;
        private Label _diagRam;
        private Label _diagRamHint;
        private Label _diagVerdict;
        private Label _diagCounts;
        private Label _diagStamp;
        private Label _diagHeader;
        private Label _diagEmpty;
        private Label _diagStatus;
        private Panel _diagViewport;
        private Panel _diagContent;
        private Panel _diagMeterTrack;
        private Panel _diagMeterFill;
        private Panel _diagMeterMark;
        private DarkScrollBar _diagScroll;
        private ServerWheelFilter _diagWheelFilter;
        private List<Control> _diagRows = new List<Control>();
        private bool _diagBusy;
        private int _diagGeneration;

        private CancellationTokenSource _cts;
        private bool _busy;
        private bool _gameRunning;
        private bool _loadingManifest;
        private bool _updateCheckRunning;
        private bool _uiReady;
        private int _metaToken;

        public MainForm()
        {
            _settings = LauncherSettings.Load();
            _modSource = _settings.ModCatalogSource;
            CurseForgeApi.Configure(_settings.CurseForgeKey);
            AppPaths.EnsureDirectories();
            InitLogFile();

            SuspendLayout();
            Text = "Vexor Launcher";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(Theme.S(1120), Theme.S(720));
            MinimumSize = ClientSize;
            MaximumSize = ClientSize;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            DoubleBuffered = true;
            KeyPreview = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch
            {
            }

            BuildSidebar();
            BuildTitleBar();
            BuildPages();

            _saveTimer = new System.Windows.Forms.Timer();
            _saveTimer.Interval = 2200;
            _saveTimer.Tick += delegate
            {
                _saveTimer.Stop();
                if (_saveHint != null) _saveHint.Text = "";
            };

            LoadSettingsIntoUi();
            _uiReady = true;
            ShowPage(0);
            ResumeLayout(false);

            Log("Vexor Launcher " + UpdateService.CurrentVersion + " · MVP");
            Log("Папка игры: " + _settings.GameDir);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle = cp.ClassStyle | 0x20000;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int dark = 1;
                DwmSetWindowAttribute(Handle, 20, ref dark, Marshal.SizeOf(typeof(int)));
                int round = 2;
                DwmSetWindowAttribute(Handle, 33, ref round, Marshal.SizeOf(typeof(int)));
            }
            catch
            {
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdatePlayButton();
            RefreshJavaStatusAsync(false);
            LoadManifestAsync(false);
            RefreshMsaQuietAsync();

            var updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 2500;
            updateTimer.Tick += delegate
            {
                updateTimer.Stop();
                updateTimer.Dispose();
                CheckUpdatesAsync(false);
            };
            updateTimer.Start();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && _busy && !_gameRunning && _cts != null)
            {
                _cts.Cancel();
                SetStatus("Отмена…", Theme.Yellow);
                if (_packBusy) SetPackStatus("Отмена…", Theme.Yellow);
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                if (_cts != null) _cts.Cancel();
                if (_serverWheelFilter != null)
                {
                    Application.RemoveMessageFilter(_serverWheelFilter);
                    _serverWheelFilter = null;
                }
                if (_diagWheelFilter != null)
                {
                    Application.RemoveMessageFilter(_diagWheelFilter);
                    _diagWheelFilter = null;
                }
                if (_uiReady) SaveSettingsFromUi(false);
                if (_logFile != null)
                {
                    _logFile.Flush();
                    _logFile.Dispose();
                    _logFile = null;
                }
            }
            catch
            {
            }
            base.OnFormClosing(e);
        }

        private void InitLogFile()
        {
            try
            {
                AppPaths.EnsureDirectories();
                string path = Path.Combine(AppPaths.LogsDir, "launcher.log");
                try
                {
                    if (File.Exists(path) && new FileInfo(path).Length > 4 * 1024 * 1024) File.Delete(path);
                }
                catch
                {
                }
                _logFile = new StreamWriter(path, true, new UTF8Encoding(false));
                _logFile.AutoFlush = true;
            }
            catch
            {
                _logFile = null;
            }
        }

        private void Log(string text)
        {
            Log(text, Theme.Text);
        }

        private void Log(string text, Color color)
        {
            if (string.IsNullOrEmpty(text) || IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke((Action)delegate { Log(text, color); }); }
                catch
                {
                }
                return;
            }

            if (color == Theme.Text)
            {
                if (text.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("Exception", StringComparison.OrdinalIgnoreCase) >= 0)
                    color = Theme.Red;
                else if (text.IndexOf("WARN", StringComparison.OrdinalIgnoreCase) >= 0)
                    color = Theme.Yellow;
            }

            // Game output already carries a "[HH:mm:ss]" prefix; drop it so the console
            // shows only our own time column.
            if (text.Length > 12 && text[0] == '[' && text[3] == ':' && text[6] == ':')
            {
                int bracket = text.IndexOf("] [", StringComparison.Ordinal);
                if (bracket > 0) text = text.Substring(bracket + 2);
            }

            _logView.Append(text, color);
            if (_logFile != null)
            {
                try
                {
                    lock (_logGate)
                    {
                        _logFile.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "  " + text);
                    }
                }
                catch
                {
                }
            }
        }

        private void SetStatus(string text, Color color)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke((Action)delegate { SetStatus(text, color); }); }
                catch
                {
                }
                return;
            }
            _status.Text = text;
            _status.ForeColor = color;
        }

        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(Handle, WmNcLButtonDown, HtCaption, 0);
        }

        // ---------------------------------------------------------------- shell

        private void BuildSidebar()
        {
            _sidebar = new SidebarPanel();
            _sidebar.Bounds = new Rectangle(0, 0, Theme.S(236), Theme.S(720));
            _sidebar.SystemText = SystemInfo.TotalRamMb / 1024 + " ГБ ОЗУ · Windows 11";
            Controls.Add(_sidebar);

            _navPlay = AddNav(134, "Играть", "\uE768");
            _navMods = AddNav(184, "Моды", "\uE7FC");
            _navPacks = AddNav(234, "Модпаки", "\uE7B8");
            _navResources = AddNav(284, "Ресурспаки", "\uE8B7");
            _navShaders = AddNav(334, "Шейдеры", "\uE790");
            _navServers = AddNav(384, "Серверы", "\uE774");
            _navSkins = AddNav(434, "Скины", "\uE77B");
            _navDiagnostics = AddNav(484, "Диагностика", "\uE9D9");
            _navSettings = AddNav(534, "Настройки", "\uE713");

            _navPlay.Click += delegate { ShowPage(0); };
            _navMods.Click += delegate { ShowPage(1); };
            _navPacks.Click += delegate { ShowPage(2); };
            _navResources.Click += delegate { SetCatalogMode(false); };
            _navShaders.Click += delegate { SetCatalogMode(true); };
            _navServers.Click += delegate { ShowPage(5); };
            _navSkins.Click += delegate { ShowPage(4); };
            _navDiagnostics.Click += delegate { ShowPage(6); };
            _navSettings.Click += delegate { ShowPage(7); };
        }

        private NavButton AddNav(int y, string text, string glyph)
        {
            var nav = new NavButton();
            nav.Bounds = new Rectangle(Theme.S(16), Theme.S(y), Theme.S(204), Theme.S(44));
            nav.Text = text;
            nav.Glyph = glyph;
            _sidebar.Controls.Add(nav);
            return nav;
        }

        private void BuildTitleBar()
        {
            _titleBar = new Panel();
            _titleBar.Bounds = new Rectangle(Theme.S(236), 0, Theme.S(884), Theme.S(44));
            _titleBar.BackColor = Theme.Bg;
            _titleBar.MouseDown += DragWindow;
            Controls.Add(_titleBar);

            var caption = new Label();
            caption.Text = "VEXOR LAUNCHER";
            caption.Font = Theme.F(9f, FontStyle.Bold);
            caption.ForeColor = Theme.Muted2;
            caption.BackColor = Color.Transparent;
            caption.AutoSize = false;
            caption.Bounds = new Rectangle(Theme.S(28), 0, Theme.S(240), Theme.S(44));
            caption.TextAlign = ContentAlignment.MiddleLeft;
            caption.UseMnemonic = false;
            caption.MouseDown += DragWindow;
            _titleBar.Controls.Add(caption);

            var close = new WindowButton();
            close.CloseButton = true;
            close.Glyph = "\uE8BB";
            close.Bounds = new Rectangle(Theme.S(884 - 44), Theme.S(7), Theme.S(38), Theme.S(30));
            close.Click += delegate { Close(); };
            _titleBar.Controls.Add(close);

            var minimize = new WindowButton();
            minimize.Glyph = "\uE921";
            minimize.Bounds = new Rectangle(Theme.S(884 - 84), Theme.S(7), Theme.S(38), Theme.S(30));
            minimize.Click += delegate { WindowState = FormWindowState.Minimized; };
            _titleBar.Controls.Add(minimize);
        }

        private PagePanel NewPage()
        {
            var page = new PagePanel();
            page.Bounds = new Rectangle(Theme.S(236), Theme.S(44), Theme.S(884), Theme.S(676));
            page.Visible = false;
            Controls.Add(page);
            return page;
        }

        private void ShowPage(int index)
        {
            if (_uiReady && index != 7 && _settingsPage.Visible) SaveSettingsFromUi(false);

            if (index != 1) HideCategoryMenu();
            _playPage.Visible = index == 0;
            _modsPage.Visible = index == 1;
            _packsPage.Visible = index == 2;
            _resourcesPage.Visible = index == 3;
            _skinsPage.Visible = index == 4;
            _serversPage.Visible = index == 5;
            _diagnosticsPage.Visible = index == 6;
            _settingsPage.Visible = index == 7;
            _navPlay.Selected = index == 0;
            _navMods.Selected = index == 1;
            _navPacks.Selected = index == 2;
            _navResources.Selected = index == 3 && !_shaderMode;
            _navShaders.Selected = index == 3 && _shaderMode;
            _navSkins.Selected = index == 4;
            _navServers.Selected = index == 5;
            _navDiagnostics.Selected = index == 6;
            _navSettings.Selected = index == 7;

            if (_uiReady && index == 7) RefreshJavaStatusAsync(false);
            if (_uiReady && index == 6) RefreshDiagnosticsAsync(false);
            if (_uiReady && index == 1)
            {
                UpdateModsTarget();
                EnsureCatalogLoaded();
            }
            if (_uiReady && index == 2)
            {
                UpdatePacksTarget();
                EnsurePacksLoaded();
            }
            if (_uiReady && index == 3)
            {
                UpdateResourcesTarget();
                EnsureResourcesLoaded();
            }
            if (_uiReady && index == 4) RefreshSkinPageAsync(false);
            if (_uiReady && index == 5) RefreshServersAsync(false);
        }

        // ------------------------------------------------------------- builders

        private Label AddLabel(Control parent, string text, int x, int y, int w, int h, Font font, Color color)
        {
            var label = new Label();
            label.AutoSize = false;
            label.BackColor = Color.Transparent;
            label.ForeColor = color;
            label.Font = font;
            label.Text = text;
            label.UseMnemonic = false;
            label.Bounds = new Rectangle(Theme.S(x), Theme.S(y), Theme.S(w), Theme.S(h));
            label.TextAlign = ContentAlignment.MiddleLeft;
            parent.Controls.Add(label);
            return label;
        }

        private Label AddParagraph(Control parent, string text, int x, int y, int w, int h, float size, Color color)
        {
            Label label = AddLabel(parent, text, x, y, w, h, Theme.F(size), color);
            label.TextAlign = ContentAlignment.TopLeft;
            return label;
        }

        private RoundedButton AddButton(Control parent, string text, string glyph, int x, int y, int w, int h,
            RoundedButton.ButtonStyle style, float fontSize, EventHandler click)
        {
            var button = new RoundedButton();
            button.Text = text;
            button.Glyph = glyph;
            button.Style = style;
            button.Font = Theme.F(fontSize, FontStyle.Bold);
            button.Bounds = new Rectangle(Theme.S(x), Theme.S(y), Theme.S(w), Theme.S(h));
            if (click != null) button.Click += click;
            parent.Controls.Add(button);
            return button;
        }

        private void AddDivider(Control parent, int x, int y, int w)
        {
            var line = new Panel();
            line.BackColor = Theme.BorderSoft;
            line.Bounds = new Rectangle(Theme.S(x), Theme.S(y), Theme.S(w), Math.Max(1, Theme.S(1)));
            parent.Controls.Add(line);
        }

        private GlyphTile AddTile(Control parent, int x, int y, int size, string glyph, Color tint, float glyphSize)
        {
            var tile = new GlyphTile();
            tile.Glyph = glyph;
            tile.Tint = tint;
            tile.GlyphSize = glyphSize;
            tile.Bounds = new Rectangle(Theme.S(x), Theme.S(y), Theme.S(size), Theme.S(size));
            parent.Controls.Add(tile);
            return tile;
        }

        private CardPanel AddCard(Control parent, int x, int y, int w, int h)
        {
            var card = new CardPanel();
            card.Bounds = new Rectangle(Theme.S(x), Theme.S(y), Theme.S(w), Theme.S(h));
            parent.Controls.Add(card);
            return card;
        }

        private void BuildPages()
        {
            _playPage = NewPage();
            BuildPlayPage();

            _modsPage = NewPage();
            BuildModsPage();

            _packsPage = NewPage();
            BuildPacksPage();

            _resourcesPage = NewPage();
            BuildResourcesPage();

            _skinsPage = NewPage();
            BuildSkinsPage();

            _serversPage = NewPage();
            BuildServersPage();

            _diagnosticsPage = NewPage();
            BuildDiagnosticsPage();

            _settingsPage = NewPage();
            BuildSettingsPage();
        }

        private void BuildPlayPage()
        {
            AddLabel(_playPage, "Играть", 28, 8, 420, 40, Theme.Display(26f, FontStyle.Bold), Theme.Text);
            AddLabel(_playPage, "Любая версия Minecraft — от alpha до последнего релиза", 28, 48, 470, 20,
                Theme.F(11.5f), Theme.Muted);

            _pillAll = AddPill("Все", 474, 54, "all");
            _pillRelease = AddPill("Релизы", 536, 72, "release");
            _pillSnapshot = AddPill("Снапшоты", 616, 100, "snapshot");
            _pillOld = AddPill("Старые", 724, 76, "old");
            _pillAll.Selected = true;

            _refreshButton = AddButton(_playPage, "", "\uE72C", 816, 21, 40, 30,
                RoundedButton.ButtonStyle.Ghost, 11f, RefreshClicked);

            var tip = new ToolTip();
            tip.SetToolTip(_refreshButton, "Обновить список версий");

            var versions = AddCard(_playPage, 28, 78, 476, 378);
            AddLabel(versions, "Версии", 18, 12, 70, 24, Theme.F(14f, FontStyle.Bold), Theme.Text);
            _versionCount = AddLabel(versions, "—", 84, 14, 200, 20, Theme.F(11.5f), Theme.Muted);

            _search = new TextField();
            _search.Placeholder = "Поиск версии…";
            _search.Bounds = new Rectangle(Theme.S(18), Theme.S(48), Theme.S(440), Theme.S(36));
            _search.ValueChanged += delegate { ApplyFilter(false); };
            versions.Controls.Add(_search);

            _versionList = new VersionListView();
            _versionList.Bounds = new Rectangle(Theme.S(10), Theme.S(94), Theme.S(456), Theme.S(268));
            _versionList.SelectionChanged += OnVersionSelected;
            versions.Controls.Add(_versionList);

            var launch = AddCard(_playPage, 524, 78, 332, 378);
            AddLabel(launch, "ЗАПУСК", 20, 12, 200, 18, Theme.F(9.5f, FontStyle.Bold), Theme.Muted2);
            _versionTitle = AddLabel(launch, "—", 20, 30, 292, 36, Theme.Display(24f, FontStyle.Bold), Theme.Text);
            _versionMeta = AddLabel(launch, "Загрузка списка версий…", 20, 68, 292, 18, Theme.F(10.5f), Theme.Muted);
            AddDivider(launch, 20, 96, 292);

            AddLabel(launch, "НИКНЕЙМ", 20, 110, 292, 16, Theme.F(9.5f, FontStyle.Bold), Theme.Muted2);
            _nickField = new TextField();
            _nickField.Placeholder = "Ваш ник";
            _nickField.MaxLength = 16;
            _nickField.Bounds = new Rectangle(Theme.S(20), Theme.S(130), Theme.S(292), Theme.S(38));
            _nickField.ValueChanged += delegate { NickChanged(_nickField); };
            launch.Controls.Add(_nickField);

            _playButton = AddButton(launch, "ИГРАТЬ", "\uE768", 20, 184, 292, 52,
                RoundedButton.ButtonStyle.Accent, 14f, PlayClicked);

            _progress = new ProgressBarEx();
            _progress.Bounds = new Rectangle(Theme.S(20), Theme.S(252), Theme.S(292), Theme.S(8));
            launch.Controls.Add(_progress);

            _status = AddLabel(launch, "Выберите версию слева", 20, 268, 292, 48, Theme.F(10f), Theme.Muted);
            _status.TextAlign = ContentAlignment.TopLeft;

            _accountDot = AddLabel(launch, "●", 20, 332, 12, 18, Theme.F(9f), Theme.Muted2);
            _accountName = AddLabel(launch, "Офлайн-профиль", 34, 326, 176, 30, Theme.F(9.5f), Theme.Muted2);
            _accountButton = AddButton(launch, "MICROSOFT", "\uE77B", 210, 326, 102, 30,
                RoundedButton.ButtonStyle.Accent, 9f, AccountClicked);
            var accountTip = new ToolTip();
            accountTip.SetToolTip(_accountButton, "Войти через Microsoft");

            var logCard = AddCard(_playPage, 28, 472, 828, 164);
            AddLabel(logCard, "Консоль", 18, 10, 120, 22, Theme.F(13f, FontStyle.Bold), Theme.Text);
            AddButton(logCard, "Очистить", "", 726, 9, 84, 28, RoundedButton.ButtonStyle.Ghost, 10f,
                delegate { _logView.Clear(); });

            _logView = new LogView();
            _logView.Bounds = new Rectangle(Theme.S(12), Theme.S(38), Theme.S(804), Theme.S(114));
            logCard.Controls.Add(_logView);
        }

        private PillButton AddPill(string text, int x, int w, string filter)
        {
            var pill = new PillButton();
            pill.Text = text;
            pill.Font = Theme.F(10.5f);
            pill.Bounds = new Rectangle(Theme.S(x), Theme.S(21), Theme.S(w), Theme.S(30));
            pill.Click += delegate { SetFilter(filter); };
            _playPage.Controls.Add(pill);
            return pill;
        }

        private void BuildModsPage()
        {
            AddLabel(_modsPage, "Моды", 28, 8, 420, 40, Theme.Display(26f, FontStyle.Bold), Theme.Text);
            AddLabel(_modsPage, "Загрузчики, сборки и каталоги Modrinth и CurseForge", 28, 48, 700, 20,
                Theme.F(11.5f), Theme.Muted);

            var fabric = AddCard(_modsPage, 28, 74, 404, 150);
            GlyphTile fabricTile = AddTile(fabric, 18, 16, 34, "\uE7FC", Theme.AccentLight, 18f);
            fabricTile.PictureInset = 1f;
            LoadLoaderLogo(fabricTile, LoaderInstaller.Fabric);
            AddLabel(fabric, "Fabric", 62, 18, 200, 28, Theme.Display(16f, FontStyle.Bold), Theme.Text);
            AddLabel(fabric, "Лёгкий загрузчик модов для современных сборок.", 18, 62, 368, 18,
                Theme.F(10f), Theme.Muted);
            _fabricStatus = AddLabel(fabric, "Не установлено", 18, 84, 368, 18, Theme.F(9.5f), Theme.Muted2);
            _fabricAction = AddButton(fabric, "Установить Fabric", "", 18, 106, 368, 30,
                RoundedButton.ButtonStyle.Accent, 10.5f, delegate { LoaderAction(LoaderInstaller.Fabric); });

            var forge = AddCard(_modsPage, 452, 74, 404, 150);
            GlyphTile forgeTile = AddTile(forge, 18, 16, 34, "\uE896", Theme.Yellow, 18f);
            forgeTile.PictureBackdrop = Color.FromArgb(255, 232, 176, 102);
            forgeTile.PictureInset = 3f;
            LoadLoaderLogo(forgeTile, LoaderInstaller.Forge);
            AddLabel(forge, "Forge", 62, 18, 200, 28, Theme.Display(16f, FontStyle.Bold), Theme.Text);
            AddLabel(forge, "Классические мод-сборки для старых и новых версий.", 18, 62, 368, 18,
                Theme.F(10f), Theme.Muted);
            _forgeStatus = AddLabel(forge, "Не установлено", 18, 84, 368, 18, Theme.F(9.5f), Theme.Muted2);
            _forgeAction = AddButton(forge, "Установить Forge", "", 18, 106, 368, 30,
                RoundedButton.ButtonStyle.Surface, 10.5f, delegate { LoaderAction(LoaderInstaller.Forge); });

            _modCatalog = AddCard(_modsPage, 28, 236, 828, 404);
            AddLabel(_modCatalog, "Каталог модов", 20, 14, 300, 26, Theme.F(13f, FontStyle.Bold), Theme.Text);
            _modModrinthChoice = AddButton(_modCatalog, "Modrinth", "", 340, 14, 92, 28,
                RoundedButton.ButtonStyle.Ghost, 9.5f, delegate { SelectCatalogSource("modrinth"); });
            _modCurseChoice = AddButton(_modCatalog, "CurseForge", "", 440, 14, 108, 28,
                RoundedButton.ButtonStyle.Ghost, 9.5f, delegate { SelectCatalogSource("curseforge"); });
            _modCatalogTarget = AddLabel(_modCatalog, "Выберите сборку", 20, 38, 540, 18,
                Theme.F(9.5f), Theme.Muted2);

            _modFabricChoice = AddButton(_modCatalog, "Fabric", "", 624, 14, 88, 28,
                RoundedButton.ButtonStyle.Ghost, 9.5f,
                delegate { SelectCatalogBuild(FindBuild(LoaderInstaller.Fabric, TargetGameId())); });
            _modForgeChoice = AddButton(_modCatalog, "Forge", "", 720, 14, 88, 28,
                RoundedButton.ButtonStyle.Ghost, 9.5f,
                delegate { SelectCatalogBuild(FindBuild(LoaderInstaller.Forge, TargetGameId())); });

            _modSearch = new TextField();
            _modSearch.Placeholder = "Найти мод: Sodium, Iris, JEI…";
            _modSearch.Bounds = new Rectangle(Theme.S(20), Theme.S(64), Theme.S(560), Theme.S(38));
            _modSearch.Submitted += delegate { CatalogSearch(false); };
            _modCatalog.Controls.Add(_modSearch);

            _modCategoryButton = AddButton(_modCatalog, "Категория: все", "", 414, 64, 170, 38,
                RoundedButton.ButtonStyle.Surface, 9.5f, delegate { CategoryClicked(); });
            _modCategoryButton.Visible = false;

            _modFind = AddButton(_modCatalog, "Найти", "", 592, 64, 100, 38,
                RoundedButton.ButtonStyle.Accent, 10.5f, delegate { CatalogSearch(false); });
            _modMore = AddButton(_modCatalog, "Ещё", "", 700, 64, 108, 38,
                RoundedButton.ButtonStyle.Surface, 10.5f, delegate { CatalogSearch(true); });
            _modMore.Enabled = false;
            UpdateCatalogSourceState();

            _modCatalogStatus = AddLabel(_modCatalog, "", 20, 366, 788, 16, Theme.F(9.5f), Theme.Muted2);

            ShowCatalogHint("Установите Fabric или Forge, чтобы ставить моды.");
        }

        private void BuildPacksPage()
        {
            AddLabel(_packsPage, "Модпаки", 28, 8, 420, 40, Theme.Display(26f, FontStyle.Bold), Theme.Text);
            AddLabel(_packsPage, "Готовые сборки Modrinth: моды, конфиги и загрузчик в один клик", 28, 48, 700, 20,
                Theme.F(11.5f), Theme.Muted);

            _packCatalog = AddCard(_packsPage, 28, 74, 828, 566);
            AddLabel(_packCatalog, "Каталог модпаков", 20, 14, 300, 26, Theme.F(13f, FontStyle.Bold), Theme.Text);

            _packAllChoice = AddButton(_packCatalog, "Все", "", 566, 14, 54, 28,
                RoundedButton.ButtonStyle.Ghost, 9f, delegate { SelectPackLoader(""); });
            _packFabricChoice = AddButton(_packCatalog, "Fabric", "", 626, 14, 84, 28,
                RoundedButton.ButtonStyle.Ghost, 9f, delegate { SelectPackLoader(LoaderInstaller.Fabric); });
            _packForgeChoice = AddButton(_packCatalog, "Forge", "", 716, 14, 80, 28,
                RoundedButton.ButtonStyle.Ghost, 9f, delegate { SelectPackLoader(LoaderInstaller.Forge); });

            _packTarget = AddLabel(_packCatalog, "Minecraft · поиск сборок Modrinth", 20, 38, 740, 18,
                Theme.F(9.5f), Theme.Muted2);

            _packSearch = new TextField();
            _packSearch.Placeholder = "Найти модпак: Cobblemon, Better MC…";
            _packSearch.Bounds = new Rectangle(Theme.S(20), Theme.S(64), Theme.S(400), Theme.S(38));
            _packSearch.Submitted += delegate { PacksSearch(false); };
            _packCatalog.Controls.Add(_packSearch);

            _packVersion = new TextField();
            _packVersion.Placeholder = "1.21.1";
            _packVersion.Bounds = new Rectangle(Theme.S(428), Theme.S(64), Theme.S(130), Theme.S(38));
            _packVersion.Submitted += delegate { PacksSearch(false); };
            _packCatalog.Controls.Add(_packVersion);

            _packFind = AddButton(_packCatalog, "Найти", "", 566, 64, 100, 38,
                RoundedButton.ButtonStyle.Accent, 10.5f, delegate { PacksSearch(false); });
            _packMore = AddButton(_packCatalog, "Ещё", "", 674, 64, 122, 38,
                RoundedButton.ButtonStyle.Surface, 10.5f, delegate { PacksSearch(true); });
            _packMore.Enabled = false;

            _packProgress = new ProgressBarEx();
            _packProgress.Bounds = new Rectangle(Theme.S(20), Theme.S(496), Theme.S(788), Theme.S(8));
            _packCatalog.Controls.Add(_packProgress);

            _packStatus = AddLabel(_packCatalog, "", 20, 512, 788, 16, Theme.F(9.5f), Theme.Muted2);

            UpdatePackChoiceState();
            ShowPackHint("Загрузка каталога Modrinth…");
        }

        private void BuildResourcesPage()
        {
            _resHeader = AddLabel(_resourcesPage, "Ресурспаки", 28, 8, 420, 40,
                Theme.Display(26f, FontStyle.Bold), Theme.Text);
            _resSubtitle = AddLabel(_resourcesPage,
                "Текстуры из каталога Modrinth — в выбранную сборку или ваниль", 28, 48, 700, 20,
                Theme.F(11.5f), Theme.Muted);

            _resCatalog = AddCard(_resourcesPage, 28, 74, 828, 566);
            _resCardTitle = AddLabel(_resCatalog, "Каталог ресурспаков", 20, 14, 300, 26,
                Theme.F(13f, FontStyle.Bold), Theme.Text);
            _resTarget = AddLabel(_resCatalog, "Версия не выбрана", 20, 38, 788, 18, Theme.F(9.5f), Theme.Muted2);

            _resSearch = new TextField();
            _resSearch.Placeholder = "Найти ресурспак: Faithful, Fresh Animations…";
            _resSearch.Bounds = new Rectangle(Theme.S(20), Theme.S(64), Theme.S(560), Theme.S(38));
            _resSearch.Submitted += delegate { ResourceSearch(false); };
            _resCatalog.Controls.Add(_resSearch);

            _resFind = AddButton(_resCatalog, "Найти", "", 592, 64, 100, 38,
                RoundedButton.ButtonStyle.Accent, 10.5f, delegate { ResourceSearch(false); });
            _resMore = AddButton(_resCatalog, "Ещё", "", 700, 64, 108, 38,
                RoundedButton.ButtonStyle.Surface, 10.5f, delegate { ResourceSearch(true); });
            _resMore.Enabled = false;

            _resStatus = AddLabel(_resCatalog, "", 20, 527, 788, 16, Theme.F(9.5f), Theme.Muted2);

            ShowResourceHint("Загрузка каталога Modrinth…");
        }

        private void BuildSkinsPage()
        {
            AddLabel(_skinsPage, "Скины", 28, 8, 340, 40, Theme.Display(26f, FontStyle.Bold), Theme.Text);
            AddLabel(_skinsPage, "Свой PNG или скин любого игрока — через профиль Microsoft",
                28, 48, 700, 20, Theme.F(11.5f), Theme.Muted);

            var current = AddCard(_skinsPage, 28, 78, 404, 566);
            AddLabel(current, "Текущий скин", 20, 14, 300, 24, Theme.F(13f, FontStyle.Bold), Theme.Text);

            _skinCurrentView = new SkinPreview();
            _skinCurrentView.Bounds = new Rectangle(Theme.S(20), Theme.S(40), Theme.S(364), Theme.S(296));
            current.Controls.Add(_skinCurrentView);

            _skinAccountName = AddLabel(current, "Офлайн-профиль", 20, 342, 364, 24,
                Theme.F(12.5f, FontStyle.Bold), Theme.Muted2);
            _skinAccountHint = AddLabel(current, "Скины в игре доступны после входа Microsoft", 20, 368,
                364, 18, Theme.F(9.5f), Theme.Muted2);
            _skinCurrentNote = AddLabel(current,
                "Скин привязывается к лицензионному профилю и подтягивается игрой при запуске.",
                20, 398, 364, 64, Theme.F(9.5f), Theme.Muted2);

            AddDivider(current, 20, 470, 364);

            _skinRefresh = AddButton(current, "Обновить", "\uE72C", 20, 486, 140, 38,
                RoundedButton.ButtonStyle.Ghost, 10.5f, delegate { RefreshSkinPageAsync(true); });
            AddButton(current, "Папка скинов", "", 172, 486, 192, 38,
                RoundedButton.ButtonStyle.Surface, 10.5f, delegate { SkinService.OpenSkinsFolder(); });

            AddLabel(current, "Библиотека: " + SkinService.SkinsDir, 20, 534, 364, 18,
                Theme.F(8.5f), Theme.Muted2).AutoEllipsis = true;

            var source = AddCard(_skinsPage, 452, 78, 404, 566);
            AddLabel(source, "Выбрать скин", 20, 14, 300, 24, Theme.F(13f, FontStyle.Bold), Theme.Text);

            AddLabel(source, "ФАЙЛ PNG · 64×64 ИЛИ 64×32", 20, 44, 300, 16,
                Theme.F(9.5f, FontStyle.Bold), Theme.Muted2);
            _skinChoose = AddButton(source, "Выбрать файл…", "", 20, 62, 160, 38,
                RoundedButton.ButtonStyle.Surface, 10.5f, delegate { ChooseSkinFile(); });
            _skinFileName = AddLabel(source, "Файл не выбран", 192, 62, 192, 38,
                Theme.F(9.5f), Theme.Muted2);
            _skinFileName.AutoEllipsis = true;

            AddDivider(source, 20, 116, 364);

            AddLabel(source, "ИЛИ ПО НИКУ ИГРОКА", 20, 132, 300, 16,
                Theme.F(9.5f, FontStyle.Bold), Theme.Muted2);
            _skinNick = new TextField();
            _skinNick.Placeholder = "Ник игрока";
            _skinNick.MaxLength = 16;
            _skinNick.Bounds = new Rectangle(Theme.S(20), Theme.S(152), Theme.S(250), Theme.S(38));
            _skinNick.Submitted += delegate { FindSkinByNickname(); };
            source.Controls.Add(_skinNick);
            _skinFind = AddButton(source, "Найти", "", 282, 152, 102, 38,
                RoundedButton.ButtonStyle.Accent, 10.5f, delegate { FindSkinByNickname(); });

            _skinStatus = AddLabel(source, "Выберите PNG или найдите скин по нику.", 20, 198, 364, 34,
                Theme.F(9.5f), Theme.Muted);
            _skinStatus.TextAlign = ContentAlignment.TopLeft;

            AddDivider(source, 20, 240, 364);

            AddLabel(source, "МОДЕЛЬ РУК", 20, 256, 300, 16, Theme.F(9.5f, FontStyle.Bold), Theme.Muted2);

            _skinClassicPill = new PillButton();
            _skinClassicPill.Text = "Классический";
            _skinClassicPill.Font = Theme.F(10f);
            _skinClassicPill.Bounds = new Rectangle(Theme.S(20), Theme.S(276), Theme.S(150), Theme.S(30));
            _skinClassicPill.Selected = true;
            _skinClassicPill.Click += delegate { SetSkinModel(false); };
            source.Controls.Add(_skinClassicPill);

            _skinSlimPill = new PillButton();
            _skinSlimPill.Text = "Slim";
            _skinSlimPill.Font = Theme.F(10f);
            _skinSlimPill.Bounds = new Rectangle(Theme.S(180), Theme.S(276), Theme.S(100), Theme.S(30));
            _skinSlimPill.Click += delegate { SetSkinModel(true); };
            source.Controls.Add(_skinSlimPill);

            _skinSelectedView = new SkinPreview();
            _skinSelectedView.EmptyText = "Здесь появится выбранный скин";
            _skinSelectedView.Bounds = new Rectangle(Theme.S(20), Theme.S(318), Theme.S(364), Theme.S(160));
            source.Controls.Add(_skinSelectedView);

            _skinApply = AddButton(source, "ПРИМЕНИТЬ В ИГРЕ", "", 20, 490, 364, 42,
                RoundedButton.ButtonStyle.Accent, 11f, delegate { ApplySkin(); });

            AddLabel(source, "Лицензионный аккаунт Microsoft · появляется при следующем запуске",
                20, 540, 364, 18, Theme.F(8.5f), Theme.Muted2);

            UpdateSkinAccountUi();
            UpdateSkinButtons();
        }

        private void BuildServersPage()
        {
            AddLabel(_serversPage, "Серверы", 28, 8, 420, 40, Theme.Display(26f, FontStyle.Bold), Theme.Text);
            AddLabel(_serversPage, "Популярные проекты — пинг, онлайн и подключение в один клик", 28, 48, 700, 20,
                Theme.F(11.5f), Theme.Muted);

            _serverRefresh = AddButton(_serversPage, "", "\uE72C", 816, 21, 40, 30,
                RoundedButton.ButtonStyle.Ghost, 11f, delegate { RefreshServersAsync(true); });

            var tip = new ToolTip();
            tip.SetToolTip(_serverRefresh, "Обновить пинг и онлайн");

            var card = AddCard(_serversPage, 28, 78, 828, 510);
            card.FillTop = Theme.Surface;
            card.FillBottom = Theme.Surface;
            AddLabel(card, "ПРОЕКТЫ", 18, 12, 200, 20, Theme.F(9.5f, FontStyle.Bold), Theme.Muted2);
            Label count = AddLabel(card, ServerCatalog.All.Length + " " +
                ServersWord(ServerCatalog.All.Length) + " · Minecraft Java Edition",
                350, 12, 462, 20, Theme.F(9.5f), Theme.Muted2);
            count.TextAlign = ContentAlignment.MiddleRight;

            int total = ServerCatalog.All.Length;

            _serverViewport = new Panel();
            _serverViewport.Bounds = new Rectangle(Theme.S(14), Theme.S(40), Theme.S(790), Theme.S(462));
            _serverViewport.BackColor = Theme.Surface;
            card.Controls.Add(_serverViewport);

            _serverContent = new Panel();
            _serverContent.BackColor = Theme.Surface;
            _serverViewport.Controls.Add(_serverContent);

            int rowStride = Theme.S(92);
            int contentHeight = total * rowStride - Theme.S(8);
            _serverContent.Bounds = new Rectangle(0, 0, Theme.S(790), contentHeight);

            _serverScroll = new DarkScrollBar();
            _serverScroll.Bounds = new Rectangle(Theme.S(808), Theme.S(40), Theme.S(6), Theme.S(462));
            _serverScroll.Minimum = 0;
            _serverScroll.Maximum = Math.Max(0, contentHeight - _serverViewport.Height);
            _serverScroll.LargeChange = _serverViewport.Height;
            _serverScroll.SmallChange = Theme.S(184);
            _serverScroll.ValueChanged += delegate { _serverContent.Top = -_serverScroll.Value; };
            _serverScroll.Visible = _serverScroll.Maximum > 0;
            card.Controls.Add(_serverScroll);
            if (_serverScroll.Maximum > 0)
            {
                _serverWheelFilter = new ServerWheelFilter(_serversPage, _serverViewport, _serverScroll);
                Application.AddMessageFilter(_serverWheelFilter);
            }

            _serverPlayers = new Label[total];
            _serverPing = new Label[total];
            _serverConnect = new RoundedButton[total];
            Color[] tints =
            {
                Theme.Accent,
                Theme.Blend(Theme.Accent, Theme.Green, 0.55f),
                Theme.Green,
                Theme.Yellow,
                Theme.AccentLight
            };

            for (int i = 0; i < total; i++)
            {
                ServerEntry server = ServerCatalog.All[i];
                CardPanel row = AddCard(_serverContent, 0, i * 92, 790, 84);
                row.Radius = 14;
                row.Shadow = false;
                row.Stroke = Theme.BorderSoft;
                row.FillTop = Theme.SurfaceAlt;
                row.FillBottom = Theme.SurfaceAlt;

                AddTile(row, 16, 18, 48, "\uE774", tints[i % tints.Length], 20f);
                AddLabel(row, server.Name, 78, 8, 314, 24, Theme.F(13f, FontStyle.Bold), Theme.Text);
                AddLabel(row, server.Address, 78, 32, 314, 18, Theme.F(9.5f), Theme.Muted2);
                Label motd = AddLabel(row, server.Description, 78, 52, 314, 18, Theme.F(9f), Theme.Muted);
                motd.AutoEllipsis = true;

                _serverPlayers[i] = AddLabel(row, "Игроки: —", 400, 12, 228, 20, Theme.F(10f), Theme.Muted);
                _serverPlayers[i].TextAlign = ContentAlignment.MiddleRight;
                _serverPing[i] = AddLabel(row, "Проверка…", 400, 40, 228, 20, Theme.F(10f), Theme.Muted);
                _serverPing[i].TextAlign = ContentAlignment.MiddleRight;

                int index = i;
                _serverConnect[i] = AddButton(row, "Подключиться", "\uE768", 642, 20, 134, 44,
                    RoundedButton.ButtonStyle.Accent, 9.5f, delegate { QuickConnect(index); });

                var rowTip = new ToolTip();
                rowTip.SetToolTip(row, server.Name + " · " + server.Address + Environment.NewLine + server.Description);
            }

            _serverStatus = AddLabel(_serversPage, "Пинг серверов выполняется автоматически.",
                28, 598, 540, 20, Theme.F(9.5f), Theme.Muted2);
            _serverVersionHint = AddLabel(_serversPage, "", 576, 598, 280, 20, Theme.F(9f), Theme.Muted2);
            _serverVersionHint.TextAlign = ContentAlignment.MiddleRight;
            UpdateServerVersionHint();
        }

        private static string ServersWord(int count)
        {
            int mod100 = count % 100;
            int mod10 = count % 10;
            if (mod100 >= 11 && mod100 <= 14) return "серверов";
            if (mod10 == 1) return "сервер";
            if (mod10 >= 2 && mod10 <= 4) return "сервера";
            return "серверов";
        }

        private sealed class ServerWheelFilter : IMessageFilter
        {
            private readonly Control _page;
            private readonly Control _viewport;
            private readonly DarkScrollBar _bar;

            public ServerWheelFilter(Control page, Control viewport, DarkScrollBar bar)
            {
                _page = page;
                _viewport = viewport;
                _bar = bar;
            }

            public bool PreFilterMessage(ref Message message)
            {
                if (message.Msg != 0x020A) return false;
                if (_page == null || _page.IsDisposed || !_page.Visible) return false;
                if (_bar == null || _bar.IsDisposed || !_bar.Visible || _bar.Maximum <= 0) return false;
                Point cursor = Cursor.Position;
                if (!_viewport.RectangleToScreen(_viewport.ClientRectangle).Contains(cursor)) return false;
                int delta = (short)((long)message.WParam >> 16);
                if (delta == 0) return false;
                _bar.ScrollBy(-Math.Sign(delta) * _bar.SmallChange);
                return true;
            }
        }

        private void BuildDiagnosticsPage()
        {
            AddLabel(_diagnosticsPage, "Диагностика", 28, 8, 420, 40, Theme.Display(26f, FontStyle.Bold), Theme.Text);
            AddLabel(_diagnosticsPage, "Совместимость установленных модов и запас оперативной памяти", 28, 48, 700, 20,
                Theme.F(11.5f), Theme.Muted);

            _diagRefresh = AddButton(_diagnosticsPage, "", "\uE72C", 816, 21, 40, 30,
                RoundedButton.ButtonStyle.Ghost, 11f, delegate { RefreshDiagnosticsAsync(true); });
            _diagFolder = AddButton(_diagnosticsPage, "Папка mods", "", 688, 21, 118, 30,
                RoundedButton.ButtonStyle.Ghost, 9.5f, delegate { OpenDiagnosticFolder(); });

            var refreshTip = new ToolTip();
            refreshTip.SetToolTip(_diagRefresh, "Проверить моды и память");
            refreshTip.SetToolTip(_diagFolder, "Открыть папку mods выбранной сборки");

            var summary = AddCard(_diagnosticsPage, 28, 76, 828, 156);
            summary.FillTop = Theme.Surface;
            summary.FillBottom = Theme.Surface;
            AddLabel(summary, "СБОРКА", 18, 10, 300, 16, Theme.F(9.5f, FontStyle.Bold), Theme.Muted2);
            _diagTarget = AddLabel(summary, "Определяю сборку…", 18, 28, 520, 24,
                Theme.F(13f, FontStyle.Bold), Theme.Text);
            _diagTarget.AutoEllipsis = true;
            _diagSubtitle = AddLabel(summary, "", 18, 52, 520, 18, Theme.F(9.5f), Theme.Muted2);
            _diagSubtitle.AutoEllipsis = true;

            AddLabel(summary, "ОПЕРАТИВНАЯ ПАМЯТЬ", 18, 80, 300, 16, Theme.F(9.5f, FontStyle.Bold), Theme.Muted2);
            _diagRam = AddLabel(summary, "—", 18, 98, 400, 22, Theme.F(12.5f, FontStyle.Bold), Theme.Text);

            _diagMeterTrack = new Panel();
            _diagMeterTrack.Bounds = new Rectangle(Theme.S(18), Theme.S(126), Theme.S(400), Theme.S(8));
            _diagMeterTrack.BackColor = Theme.SurfaceAlt;
            summary.Controls.Add(_diagMeterTrack);

            _diagMeterFill = new Panel();
            _diagMeterFill.Bounds = new Rectangle(0, 0, 0, Theme.S(8));
            _diagMeterFill.BackColor = Theme.Green;
            _diagMeterTrack.Controls.Add(_diagMeterFill);

            _diagMeterMark = new Panel();
            _diagMeterMark.Bounds = new Rectangle(0, 0, Math.Max(1, Theme.S(2)), Theme.S(8));
            _diagMeterMark.BackColor = Theme.AccentLight;
            _diagMeterMark.Visible = false;
            _diagMeterTrack.Controls.Add(_diagMeterMark);

            _diagRamHint = AddLabel(summary, "", 18, 138, 520, 16, Theme.F(9f), Theme.Muted2);
            _diagRamHint.AutoEllipsis = true;

            _diagVerdict = AddLabel(summary, "Проверка…", 548, 12, 262, 30,
                Theme.Display(15f, FontStyle.Bold), Theme.Muted);
            _diagVerdict.TextAlign = ContentAlignment.MiddleRight;
            _diagCounts = AddLabel(summary, "", 548, 44, 262, 46, Theme.F(9.5f), Theme.Muted2);
            _diagCounts.TextAlign = ContentAlignment.TopRight;
            _diagStamp = AddLabel(summary, "", 548, 120, 262, 16, Theme.F(8.5f), Theme.Muted2);
            _diagStamp.TextAlign = ContentAlignment.MiddleRight;

            var card = AddCard(_diagnosticsPage, 28, 244, 828, 402);
            card.FillTop = Theme.Surface;
            card.FillBottom = Theme.Surface;
            AddLabel(card, "РЕЗУЛЬТАТЫ ПРОВЕРКИ", 18, 12, 300, 20, Theme.F(9.5f, FontStyle.Bold), Theme.Muted2);
            _diagHeader = AddLabel(card, "", 330, 12, 482, 20, Theme.F(9.5f), Theme.Muted2);
            _diagHeader.TextAlign = ContentAlignment.MiddleRight;

            _diagViewport = new Panel();
            _diagViewport.Bounds = new Rectangle(Theme.S(14), Theme.S(40), Theme.S(790), Theme.S(318));
            _diagViewport.BackColor = Theme.Surface;
            card.Controls.Add(_diagViewport);

            _diagContent = new Panel();
            _diagContent.Bounds = new Rectangle(0, 0, Theme.S(790), Theme.S(318));
            _diagContent.BackColor = Theme.Surface;
            _diagViewport.Controls.Add(_diagContent);

            _diagEmpty = AddLabel(card, "Проблем не найдено — моды совместимы, памяти достаточно.",
                20, 150, 788, 30, Theme.F(11.5f), Theme.Green);
            _diagEmpty.TextAlign = ContentAlignment.MiddleCenter;
            _diagEmpty.Visible = false;

            _diagScroll = new DarkScrollBar();
            _diagScroll.Bounds = new Rectangle(Theme.S(808), Theme.S(40), Theme.S(6), Theme.S(318));
            _diagScroll.Minimum = 0;
            _diagScroll.Maximum = 0;
            _diagScroll.LargeChange = _diagViewport.Height;
            _diagScroll.SmallChange = Theme.S(184);
            _diagScroll.ValueChanged += delegate { _diagContent.Top = -_diagScroll.Value; };
            _diagScroll.Visible = false;
            card.Controls.Add(_diagScroll);

            _diagWheelFilter = new ServerWheelFilter(_diagnosticsPage, _diagViewport, _diagScroll);
            Application.AddMessageFilter(_diagWheelFilter);

            _diagStatus = AddLabel(_diagnosticsPage, "Проверка выполняется автоматически при открытии страницы.",
                28, 650, 828, 18, Theme.F(9.5f), Theme.Muted2);
        }

        private sealed class DiagnosticTarget
        {
            public string ModsDir = "";
            public string Loader = "";
            public string LoaderVersion = "";
            public string GameVersion = "";
            public string Title = "";
            public string Subtitle = "";
            public bool IsModpack;
        }

        private DiagnosticTarget ResolveDiagnosticTarget()
        {
            var target = new DiagnosticTarget();
            VersionEntry entry = _versionList != null ? _versionList.SelectedEntry : null;
            InstalledBuild build = entry != null ? LoaderInstaller.Find(_builds, entry.Id) : null;
            if (entry == null && build == null && _builds.Count > 0) build = _builds[0];

            if (build != null)
            {
                target.ModsDir = build.ModsDir;
                target.Loader = build.Loader ?? "";
                target.LoaderVersion = build.LoaderVersion ?? "";
                target.GameVersion = build.GameVersion ?? "";
                target.IsModpack = build.IsModpack;
                target.Title = build.Summary;
                target.Subtitle = "mods: " + build.ModsDir;
                return target;
            }

            target.ModsDir = Path.Combine(_settings.GameDir, "mods");
            target.GameVersion = entry != null ? entry.Id : (_settings.SelectedVersion ?? "");
            target.Title = entry != null ? "Ваниль · " + entry.Id : "Сборка не выбрана";
            target.Subtitle = entry != null
                ? "Выберите сборку с Fabric или Forge, чтобы проверить именно её моды"
                : "mods: " + target.ModsDir;
            return target;
        }

        private void OpenDiagnosticFolder()
        {
            DiagnosticTarget target = ResolveDiagnosticTarget();
            OpenFolder(target.ModsDir);
        }

        private async void RefreshDiagnosticsAsync(bool manual)
        {
            if (_diagBusy || _diagnosticsPage == null) return;

            _diagBusy = true;
            int generation = ++_diagGeneration;
            if (_diagRefresh != null) _diagRefresh.Enabled = false;
            SetDiagStatus("Читаю манифесты модов и оцениваю память…", Theme.Muted);
            if (_diagVerdict != null)
            {
                _diagVerdict.Text = "Проверка…";
                _diagVerdict.ForeColor = Theme.Muted;
            }

            DiagnosticTarget target = ResolveDiagnosticTarget();
            if (_diagTarget != null)
            {
                _diagTarget.Text = target.Title;
                _diagSubtitle.Text = target.Subtitle;
            }
            if (manual) Log("Диагностика: проверяю " + target.ModsDir);

            int configured = _settings.RamMb;
            string extra = _settings.ExtraJvmArgs;
            int total = SystemInfo.TotalRamMb;
            int available = SystemInfo.AvailableRamMb;

            try
            {
                DiagnosticReport report = await Task.Run(() => ModDiagnostics.Analyze(target.ModsDir,
                    target.Loader, target.LoaderVersion, target.GameVersion, configured, extra, total,
                    available, target.IsModpack, target.Title, target.Subtitle));
                if (IsDisposed || generation != _diagGeneration) return;
                RenderDiagnosticReport(report);
                if (manual)
                    Log("Диагностика: ошибок " + report.ErrorCount + ", предупреждений " + report.WarningCount + ".");
            }
            catch (Exception ex)
            {
                if (generation == _diagGeneration)
                    SetDiagStatus("Ошибка проверки: " + ShortMessage(ex), Theme.Red);
            }
            finally
            {
                if (generation == _diagGeneration)
                {
                    _diagBusy = false;
                    if (_diagRefresh != null) _diagRefresh.Enabled = true;
                }
            }
        }

        private void RenderDiagnosticReport(DiagnosticReport report)
        {
            ClearDiagnosticRows();
            int count = report.Issues.Count;
            for (int i = 0; i < count; i++) AddDiagnosticRow(report.Issues[i], i);

            _diagEmpty.Visible = count == 0;
            if (count == 0) _diagEmpty.BringToFront();

            int contentHeight = Math.Max(_diagViewport.Height,
                count * Theme.S(92) - (count > 0 ? Theme.S(8) : 0));
            _diagContent.Bounds = new Rectangle(0, 0, Theme.S(790), contentHeight);
            _diagScroll.Maximum = Math.Max(0, contentHeight - _diagViewport.Height);
            _diagScroll.LargeChange = _diagViewport.Height;
            _diagScroll.SmallChange = Theme.S(184);
            _diagScroll.Value = 0;
            _diagScroll.Visible = _diagScroll.Maximum > 0;
            _diagContent.Top = 0;

            _diagTarget.Text = report.TargetTitle;
            _diagSubtitle.Text = report.TargetSubtitle;
            _diagRam.Text = Theme.FormatRam(report.EffectiveRamMb) + " из " + Theme.FormatRam(report.TotalRamMb);
            _diagRam.ForeColor = MemoryColor(report.MemoryLevel);

            string hint = "Рекомендуется " + Theme.FormatRam(report.RecommendedRamMb) + " · " +
                report.ModCount + " " + Plural(report.ModCount, "мод", "мода", "модов");
            if (report.DisabledCount > 0) hint += " (+" + report.DisabledCount + " отключено)";
            hint += " · свободно " + Theme.FormatRam(report.AvailableRamMb);
            if (report.RamOverridden) hint += " · -Xmx из аргументов JVM";
            _diagRamHint.Text = hint;

            int trackWidth = Math.Max(1, _diagMeterTrack.Width);
            int fillWidth = report.TotalRamMb > 0
                ? (int)Math.Round(trackWidth * Math.Min(1.0, report.EffectiveRamMb / (double)report.TotalRamMb))
                : 0;
            _diagMeterFill.Width = Math.Max(0, Math.Min(trackWidth, fillWidth));
            _diagMeterFill.BackColor = MemoryColor(report.MemoryLevel);
            bool showMark = report.TotalRamMb > 0 && report.RecommendedRamMb < report.TotalRamMb;
            _diagMeterMark.Visible = showMark;
            if (showMark)
            {
                int markLeft = (int)Math.Round(trackWidth * (report.RecommendedRamMb / (double)report.TotalRamMb));
                _diagMeterMark.Left = Math.Max(0, Math.Min(trackWidth - _diagMeterMark.Width, markLeft));
            }

            if (report.ErrorCount > 0)
            {
                _diagVerdict.Text = "Есть проблемы";
                _diagVerdict.ForeColor = Theme.Red;
            }
            else if (report.WarningCount > 0)
            {
                _diagVerdict.Text = "Есть предупреждения";
                _diagVerdict.ForeColor = Theme.Yellow;
            }
            else
            {
                _diagVerdict.Text = "Можно играть";
                _diagVerdict.ForeColor = Theme.Green;
            }

            if (report.ErrorCount == 0 && report.WarningCount == 0)
            {
                _diagCounts.Text = "Совместимость и память в норме";
            }
            else
            {
                var parts = new List<string>();
                if (report.ErrorCount > 0)
                    parts.Add(report.ErrorCount + " " + Plural(report.ErrorCount, "ошибка", "ошибки", "ошибок"));
                if (report.WarningCount > 0)
                    parts.Add(report.WarningCount + " " +
                        Plural(report.WarningCount, "предупреждение", "предупреждения", "предупреждений"));
                if (report.InfoCount > 0 && parts.Count == 0)
                    parts.Add(report.InfoCount + " " + Plural(report.InfoCount, "заметка", "заметки", "заметок"));
                _diagCounts.Text = string.Join("\n", parts.ToArray());
            }

            _diagStamp.Text = "Проверено в " + report.ScannedAt.ToString("HH:mm:ss");
            _diagHeader.Text = count == 0 ? "Проблем не найдено" :
                count + " " + Plural(count, "замечание", "замечания", "замечаний");

            Color statusColor = report.ErrorCount > 0 ? Theme.Red
                : (report.WarningCount > 0 ? Theme.Yellow : Theme.Green);
            string status = report.ModCount + " " + Plural(report.ModCount, "мод", "мода", "модов") +
                " проверено · " + report.ErrorCount + " " +
                Plural(report.ErrorCount, "ошибка", "ошибки", "ошибок") + " · " +
                report.WarningCount + " " +
                Plural(report.WarningCount, "предупреждение", "предупреждения", "предупреждений");
            if (!report.ModsDirExists) status += " · папка mods ещё не создана";
            else if (report.DisabledCount > 0) status += " · " + report.DisabledCount + " отключено";
            SetDiagStatus(status, statusColor);
        }

        private void AddDiagnosticRow(ModIssue issue, int index)
        {
            CardPanel row = AddCard(_diagContent, 0, index * 92, 790, 84);
            row.Radius = 14;
            row.Shadow = false;
            row.Stroke = Theme.BorderSoft;
            row.FillTop = Theme.SurfaceAlt;
            row.FillBottom = Theme.SurfaceAlt;

            Color tint;
            string glyph;
            if (issue.Level == DiagnosticLevel.Error)
            {
                tint = Theme.Red;
                glyph = "\uEA39";
            }
            else if (issue.Level == DiagnosticLevel.Warning)
            {
                tint = Theme.Yellow;
                glyph = "\uE7BA";
            }
            else
            {
                tint = Theme.AccentLight;
                glyph = "\uE946";
            }

            AddTile(row, 16, 18, 48, glyph, tint, 20f);
            Label title = AddLabel(row, issue.Title, 78, 8, 690, 22, Theme.F(12.5f, FontStyle.Bold), Theme.Text);
            title.AutoEllipsis = true;
            Label detail = AddLabel(row, issue.Detail, 78, 30, 690, 36, Theme.F(9.5f), Theme.Muted);
            detail.AutoEllipsis = true;
            Label file = AddLabel(row, issue.File, 78, 68, 690, 14, Theme.F(8.5f), Theme.Muted2);
            file.AutoEllipsis = true;
            _diagRows.Add(row);
        }

        private void ClearDiagnosticRows()
        {
            for (int i = 0; i < _diagRows.Count; i++)
            {
                try { _diagRows[i].Dispose(); }
                catch
                {
                }
            }
            _diagRows.Clear();
        }

        private void SetDiagStatus(string text, Color color)
        {
            if (_diagStatus == null || IsDisposed) return;
            _diagStatus.Text = text;
            _diagStatus.ForeColor = color;
        }

        private static Color MemoryColor(DiagnosticLevel level)
        {
            if (level == DiagnosticLevel.Error) return Theme.Red;
            if (level == DiagnosticLevel.Warning) return Theme.Yellow;
            return Theme.Green;
        }

        private static string Plural(int count, string one, string few, string many)
        {
            int mod100 = count % 100;
            int mod10 = count % 10;
            if (mod100 >= 11 && mod100 <= 14) return many;
            if (mod10 == 1) return one;
            if (mod10 >= 2 && mod10 <= 4) return few;
            return many;
        }

        private void BuildSettingsPage()
        {
            AddLabel(_settingsPage, "Настройки", 28, 8, 340, 40, Theme.Display(26f, FontStyle.Bold), Theme.Text);
            AddLabel(_settingsPage, "Память, Java, каталоги и папка игры", 28, 48, 480, 20, Theme.F(11.5f), Theme.Muted);

            _updateButton = AddButton(_settingsPage, "Обновления", "\uE895", 452, 18, 136, 34,
                RoundedButton.ButtonStyle.Surface, 10.5f, delegate { CheckUpdatesAsync(true); });

            _saveHint = AddLabel(_settingsPage, "", 596, 18, 120, 30, Theme.F(10.5f, FontStyle.Bold), Theme.Green);
            _saveHint.TextAlign = ContentAlignment.MiddleRight;
            AddButton(_settingsPage, "Сохранить", "", 736, 18, 120, 34,
                RoundedButton.ButtonStyle.Accent, 11.5f, delegate { SaveSettingsFromUi(true); });

            var profile = AddCard(_settingsPage, 28, 78, 404, 286);
            AddLabel(profile, "Профиль и память", 20, 14, 300, 24, Theme.F(13f, FontStyle.Bold), Theme.Text);
            AddLabel(profile, "НИКНЕЙМ", 20, 48, 300, 16, Theme.F(9.5f, FontStyle.Bold), Theme.Muted2);

            _setNick = new TextField();
            _setNick.Placeholder = "Ваш ник";
            _setNick.MaxLength = 16;
            _setNick.Bounds = new Rectangle(Theme.S(20), Theme.S(66), Theme.S(364), Theme.S(38));
            _setNick.ValueChanged += delegate { NickChanged(_setNick); };
            profile.Controls.Add(_setNick);

            AddLabel(profile, "ОПЕРАТИВНАЯ ПАМЯТЬ", 20, 118, 240, 16, Theme.F(9.5f, FontStyle.Bold), Theme.Muted2);
            _ramValue = AddLabel(profile, "4 ГБ", 280, 114, 104, 22, Theme.F(13f, FontStyle.Bold), Theme.AccentLight);
            _ramValue.TextAlign = ContentAlignment.MiddleRight;

            _ramSlider = new RamSlider();
            int totalRam = SystemInfo.TotalRamMb;
            int maxRam = (totalRam / 512) * 512;
            if (maxRam > 32768) maxRam = 32768;
            if (maxRam < 2048) maxRam = 2048;
            _ramSlider.Maximum = maxRam;
            _ramSlider.Bounds = new Rectangle(Theme.S(12), Theme.S(140), Theme.S(380), Theme.S(28));
            _ramSlider.ValueChanged += delegate { _ramValue.Text = Theme.FormatRam(_ramSlider.Value); };
            profile.Controls.Add(_ramSlider);

            _ramHint = AddLabel(profile, "Всего ОЗУ: " + (totalRam / 1024) + " ГБ · рекомендуется 4–8 ГБ",
                20, 170, 364, 18, Theme.F(9.5f), Theme.Muted2);

            AddLabel(profile, "ПАПКА ИГРЫ", 20, 198, 300, 16, Theme.F(9.5f, FontStyle.Bold), Theme.Muted2);
            _gameDir = new TextField();
            _gameDir.ReadOnly = true;
            _gameDir.Bounds = new Rectangle(Theme.S(20), Theme.S(216), Theme.S(250), Theme.S(38));
            profile.Controls.Add(_gameDir);
            AddButton(profile, "Сменить", "", 282, 216, 102, 38, RoundedButton.ButtonStyle.Surface, 10.5f,
                ChooseGameFolder);

            var perf = AddCard(_settingsPage, 28, 380, 404, 256);
            AddLabel(perf, "Производительность", 20, 14, 300, 24, Theme.F(13f, FontStyle.Bold), Theme.Text);
            AddLabel(perf, "Оптимизация JVM", 20, 48, 260, 20, Theme.F(12f), Theme.Text);
            AddLabel(perf, "G1GC и короткие паузы сборщика мусора", 20, 68, 300, 18, Theme.F(9.5f), Theme.Muted2);

            _optimize = new ToggleSwitch();
            _optimize.Bounds = new Rectangle(Theme.S(340), Theme.S(46), Theme.S(44), Theme.S(24));
            perf.Controls.Add(_optimize);

            AddLabel(perf, "ДОП. АРГУМЕНТЫ JVM", 20, 100, 300, 16, Theme.F(9.5f, FontStyle.Bold), Theme.Muted2);
            _jvmArgs = new TextField();
            _jvmArgs.Placeholder = "-XX:+UseZGC";
            _jvmArgs.Bounds = new Rectangle(Theme.S(20), Theme.S(118), Theme.S(364), Theme.S(38));
            perf.Controls.Add(_jvmArgs);

            AddLabel(perf, "РАЗРЕШЕНИЕ ОКНА", 20, 168, 300, 16, Theme.F(9.5f, FontStyle.Bold), Theme.Muted2);
            _resW = new TextField();
            _resW.Placeholder = "1280";
            _resW.MaxLength = 4;
            _resW.Bounds = new Rectangle(Theme.S(20), Theme.S(186), Theme.S(104), Theme.S(38));
            perf.Controls.Add(_resW);

            AddLabel(perf, "×", 126, 186, 24, 38, Theme.F(12f), Theme.Muted2).TextAlign = ContentAlignment.MiddleCenter;

            _resH = new TextField();
            _resH.Placeholder = "720";
            _resH.MaxLength = 4;
            _resH.Bounds = new Rectangle(Theme.S(150), Theme.S(186), Theme.S(104), Theme.S(38));
            perf.Controls.Add(_resH);

            AddLabel(perf, "0 — авто", 266, 196, 118, 20, Theme.F(9.5f), Theme.Muted2);

            var java = AddCard(_settingsPage, 452, 78, 404, 270);
            AddLabel(java, "Java", 20, 14, 300, 24, Theme.F(13f, FontStyle.Bold), Theme.Text);
            _javaStatus = AddParagraph(java, "Поиск установленных Java…", 20, 46, 364, 46, 10.5f, Theme.Muted);
            AddParagraph(java, "Нужная версия скачивается с Adoptium автоматически при первом запуске.",
                20, 96, 364, 34, 9.5f, Theme.Muted2);

            AddLabel(java, "СВОЙ ПУТЬ К JAVA", 20, 136, 300, 16, Theme.F(9.5f, FontStyle.Bold), Theme.Muted2);
            _javaPath = new TextField();
            _javaPath.Placeholder = "Авто — найти автоматически";
            _javaPath.Bounds = new Rectangle(Theme.S(20), Theme.S(154), Theme.S(364), Theme.S(38));
            java.Controls.Add(_javaPath);

            AddButton(java, "Найти Java", "", 20, 208, 150, 38, RoundedButton.ButtonStyle.Surface, 10.5f,
                delegate { RefreshJavaStatusAsync(true); });
            AddButton(java, "Папка Java", "", 182, 208, 130, 38, RoundedButton.ButtonStyle.Ghost, 10.5f,
                delegate { OpenFolder(AppPaths.RuntimeDir); });

            var curse = AddCard(_settingsPage, 452, 356, 404, 124);
            AddLabel(curse, "CurseForge", 20, 12, 300, 24, Theme.F(13f, FontStyle.Bold), Theme.Text);
            AddLabel(curse, "API-КЛЮЧ ДЛЯ КАТАЛОГА", 20, 38, 300, 14, Theme.F(9f, FontStyle.Bold), Theme.Muted2);
            _cfKey = new TextField();
            _cfKey.Placeholder = "Вставьте ключ CurseForge API";
            _cfKey.Bounds = new Rectangle(Theme.S(20), Theme.S(56), Theme.S(250), Theme.S(36));
            curse.Controls.Add(_cfKey);
            AddButton(curse, "Получить", "", 282, 56, 102, 36, RoundedButton.ButtonStyle.Surface, 9.5f,
                delegate { OpenUrl("https://console.curseforge.com/"); });
            AddLabel(curse, "Хранится локально · console.curseforge.com", 20, 98, 364, 16,
                Theme.F(8.5f), Theme.Muted2);

            var folders = AddCard(_settingsPage, 452, 492, 404, 144);
            AddLabel(folders, "Папки", 20, 12, 300, 24, Theme.F(13f, FontStyle.Bold), Theme.Text);
            AddButton(folders, "Игра", "", 20, 42, 112, 36, RoundedButton.ButtonStyle.Surface, 10.5f,
                delegate { OpenFolder(_gameDir.Value); });
            AddButton(folders, "Логи", "", 144, 42, 112, 36, RoundedButton.ButtonStyle.Surface, 10.5f,
                delegate { OpenFolder(AppPaths.LogsDir); });
            AddButton(folders, "Лаунчер", "", 268, 42, 112, 36, RoundedButton.ButtonStyle.Surface, 10.5f,
                delegate { OpenFolder(AppPaths.LauncherDir); });
            AddParagraph(folders, "Vexor Launcher " + UpdateService.CurrentVersion + "\n" + AppPaths.LauncherDir,
                20, 88, 364, 44, 9.5f, Theme.Muted2);
        }

        // -------------------------------------------------------------- settings

        private void LoadSettingsIntoUi()
        {
            MsaAuth.Configure(_settings.MicrosoftClientId);
            _nickField.Value = _settings.Nickname;
            _setNick.Value = _settings.Nickname;
            _gameDir.Value = _settings.GameDir;
            _ramSlider.Value = _settings.RamMb;
            _ramValue.Text = Theme.FormatRam(_ramSlider.Value);
            _optimize.Checked = _settings.OptimizeJvm;
            _jvmArgs.Value = _settings.ExtraJvmArgs;
            _javaPath.Value = _settings.JavaPath;
            _cfKey.Value = _settings.CurseForgeKey;
            _resW.Value = _settings.ResolutionWidth > 0 ? _settings.ResolutionWidth.ToString() : "";
            _resH.Value = _settings.ResolutionHeight > 0 ? _settings.ResolutionHeight.ToString() : "";
            _msa = MsaStore.Load();
            UpdateAccountUi();
            UpdateSkinAccountUi();
            UpdateSkinButtons();
        }

        private void NickChanged(TextField source)
        {
            if (_msa != null) return;
            string value = source.Value;
            if (source != _nickField) _nickField.Value = value;
            if (source != _setNick) _setNick.Value = value;
            string trimmed = value.Trim();
            _settings.Nickname = trimmed.Length > 0 ? trimmed : "Player";
            _sidebar.AccountName = _settings.Nickname;
            _sidebar.Invalidate();
        }

        private static bool ValidNick(string nick)
        {
            return Regex.IsMatch(nick ?? "", "^[A-Za-z0-9_]{1,16}$");
        }

        private void UpdateAccountUi()
        {
            bool online = _msa != null && !string.IsNullOrEmpty(_msa.Name);
            if (online)
            {
                _nickField.Value = _msa.Name;
                _nickField.ReadOnly = true;
                _accountName.Text = "Microsoft · " + _msa.Name;
                _accountName.ForeColor = Theme.Green;
                _accountDot.ForeColor = Theme.Green;
                _accountButton.Text = "ВЫЙТИ";
                _accountButton.Glyph = "";
                _accountButton.Style = RoundedButton.ButtonStyle.Ghost;
                _sidebar.AccountName = _msa.Name;
                _sidebar.AccountSubtitle = "Microsoft · лицензия";
                _sidebar.AccountOnline = true;
            }
            else
            {
                _nickField.ReadOnly = false;
                _nickField.Value = _settings.Nickname;
                _accountName.Text = "Офлайн-профиль";
                _accountName.ForeColor = Theme.Muted2;
                _accountDot.ForeColor = Theme.Muted2;
                _accountButton.Text = "MICROSOFT";
                _accountButton.Glyph = "\uE77B";
                _accountButton.Style = RoundedButton.ButtonStyle.Accent;
                _sidebar.AccountName = _settings.Nickname;
                _sidebar.AccountSubtitle = "Офлайн-аккаунт";
                _sidebar.AccountOnline = false;
            }
            _sidebar.Invalidate();
        }

        private void AccountClicked(object sender, EventArgs e)
        {
            if (_msa != null)
            {
                LogoutMsa();
                return;
            }
            PromptMicrosoftLogin();
        }

        private bool PromptMicrosoftLogin()
        {
            using (var dialog = new MsaLoginForm())
            {
                DialogResult result = dialog.ShowDialog(this);
                if (result == DialogResult.OK && dialog.Account != null)
                {
                    _msa = dialog.Account;
                    bool saved = MsaStore.Save(_msa);
                    _skinOwn = null;
                    _skinOwnLoaded = false;
                    UpdateAccountUi();
                    UpdateSkinAccountUi();
                    UpdateSkinButtons();
                    Log("Microsoft: вход выполнен · " + _msa.Name + (saved ? "" : " · сессия не сохранена"));
                    SetStatus("Аккаунт Microsoft подключён.", Theme.Green);
                    return true;
                }
                if (!string.IsNullOrEmpty(dialog.ErrorMessage))
                {
                    Log("Вход Microsoft: " + dialog.ErrorMessage, Theme.Red);
                }
            }
            return false;
        }

        private void LogoutMsa()
        {
            MsaStore.Clear();
            _msa = null;
            _skinOwn = null;
            _skinOwnLoaded = false;
            if (_skinCurrentView != null) _skinCurrentView.Clear();
            UpdateAccountUi();
            UpdateSkinAccountUi();
            UpdateSkinButtons();
            Log("Выход из Microsoft. Используется офлайн-профиль.");
            SetStatus("Вы вышли из аккаунта Microsoft.", Theme.Muted);
        }

        private async Task<MsaSession> EnsureMsaAsync()
        {
            if (_msa == null) return null;
            if (!_msa.NeedsRefresh) return _msa;

            SetStatus("Обновляем сессию Microsoft…", Theme.Muted);
            try
            {
                MsaSession refreshed = await MsaAuth.RefreshAsync(_msa, null, CancellationToken.None);
                _msa = refreshed;
                if (!MsaStore.Save(refreshed))
                    Log("Не удалось сохранить сессию Microsoft локально.", Theme.Yellow);
                UpdateAccountUi();
                Log("Сессия Microsoft обновлена · " + refreshed.Name);
                return refreshed;
            }
            catch (MsaException ex)
            {
                if (ex.SessionExpired)
                {
                    MsaStore.Clear();
                    _msa = null;
                    UpdateAccountUi();
                    Log("Сессия Microsoft истекла — требуется новый вход.", Theme.Yellow);
                }
                throw;
            }
        }

        private async void RefreshMsaQuietAsync()
        {
            if (_msa == null || !_msa.NeedsRefresh) return;
            try
            {
                MsaSession refreshed = await MsaAuth.RefreshAsync(_msa, null, CancellationToken.None);
                _msa = refreshed;
                MsaStore.Save(refreshed);
                UpdateAccountUi();
                Log("Сессия Microsoft обновлена · " + refreshed.Name);
            }
            catch (MsaException ex)
            {
                if (ex.SessionExpired)
                {
                    MsaStore.Clear();
                    _msa = null;
                    UpdateAccountUi();
                    Log("Сессия Microsoft истекла — требуется новый вход.", Theme.Yellow);
                }
            }
            catch
            {
            }
        }

        // ---------------------------------------------------------------- skins

        private async void RefreshSkinPageAsync(bool force)
        {
            UpdateSkinAccountUi();
            UpdateSkinButtons();
            if (_skinBusy) return;

            if (_msa == null || !_msa.IsLicensed)
            {
                _skinOwn = null;
                _skinOwnLoaded = false;
                _skinCurrentView.Clear();
                _skinCurrentNote.Text =
                    "Скины в игре доступны после входа в лицензионный аккаунт Microsoft.";
                return;
            }
            if (_skinOwnLoaded && !force) return;

            _skinBusy = true;
            UpdateSkinButtons();
            SetSkinStatus("Загружаю текущий скин…", Theme.Muted);
            try
            {
                await LoadOwnSkinAsync(true);
                SetSkinStatus("Профиль готов. Выберите скин справа.", Theme.Muted);
            }
            catch (Exception ex)
            {
                HandleSkinError(ex);
            }
            finally
            {
                _skinBusy = false;
                UpdateSkinButtons();
            }
        }

        private async Task LoadOwnSkinAsync(bool force)
        {
            if (_msa == null || !_msa.IsLicensed) return;
            if (_skinOwnLoaded && !force) return;

            MsaSession session = await EnsureMsaAsync();
            SkinInfo info = await SkinService.GetOwnAsync(session, CancellationToken.None);
            _skinOwn = info;
            _skinOwnLoaded = true;
            UpdateSkinAccountUi();
            Log("Скины: профиль " + info.Name + " · " +
                (info.HasSkin ? (info.Slim ? "Slim" : "Classic") : "стандартный скин"));

            if (info.HasSkin)
            {
                Image image = await SkinService.LoadPreviewAsync(info, CancellationToken.None);
                if (image != null)
                {
                    _skinCurrentView.SetSkin(image, info.Slim);
                }
                else
                {
                    _skinCurrentView.Clear();
                }
                _skinCurrentNote.Text = "Активный скин · " + (info.Slim ? "Slim" : "Classic") +
                                        "\nОбновляется автоматически после загрузки.";
            }
            else
            {
                _skinCurrentView.Clear();
                _skinCurrentNote.Text = "У профиля стандартный скин.\nВыберите PNG или найдите скин по нику справа.";
            }
        }

        private void UpdateSkinAccountUi()
        {
            if (_skinAccountName == null) return;
            bool licensed = _msa != null && _msa.IsLicensed;
            if (licensed)
            {
                _skinAccountName.Text = "Microsoft · " + _msa.Name;
                _skinAccountName.ForeColor = Theme.Green;
                _skinAccountHint.Text = "Лицензионный профиль · скин синхронизируется с Mojang";
                _skinAccountHint.ForeColor = Theme.Green;
            }
            else
            {
                _skinAccountName.Text = "Офлайн-профиль";
                _skinAccountName.ForeColor = Theme.Muted2;
                _skinAccountHint.Text = "Скины в игре доступны после входа Microsoft";
                _skinAccountHint.ForeColor = Theme.Muted2;
            }
        }

        private void UpdateSkinButtons()
        {
            if (_skinApply == null) return;
            bool licensed = _msa != null && _msa.IsLicensed;
            bool hasFile = !string.IsNullOrEmpty(_skinSelectedPath) && File.Exists(_skinSelectedPath);
            bool idle = !_skinBusy;

            _skinChoose.Enabled = idle;
            _skinFind.Enabled = idle;
            _skinNick.Enabled = idle;
            _skinRefresh.Enabled = idle;
            _skinClassicPill.Enabled = idle;
            _skinSlimPill.Enabled = idle && !IsLegacySkin();

            if (licensed)
            {
                _skinApply.Text = "ПРИМЕНИТЬ В ИГРЕ";
                _skinApply.Glyph = "\uE73E";
                _skinApply.Style = RoundedButton.ButtonStyle.Accent;
                _skinApply.Enabled = idle && hasFile;
            }
            else
            {
                _skinApply.Text = "ВОЙТИ ЧЕРЕЗ MICROSOFT";
                _skinApply.Glyph = "\uE77B";
                _skinApply.Style = RoundedButton.ButtonStyle.Accent;
                _skinApply.Enabled = idle;
            }
        }

        private bool IsLegacySkin()
        {
            if (string.IsNullOrEmpty(_skinSelectedPath) || !File.Exists(_skinSelectedPath)) return false;
            try
            {
                return SkinService.InspectFile(_skinSelectedPath).Height == 32;
            }
            catch
            {
                return false;
            }
        }

        private void SetSkinModel(bool slim)
        {
            if (slim && IsLegacySkin())
            {
                _skinSelectedSlim = false;
                _skinClassicPill.Selected = true;
                _skinSlimPill.Selected = false;
                SetSkinStatus("Slim-модель требует скин 64×64.", Theme.Yellow);
                return;
            }
            _skinSelectedSlim = slim;
            _skinClassicPill.Selected = !slim;
            _skinSlimPill.Selected = slim;
            RefreshSelectedSkinPreview();
        }

        private void RefreshSelectedSkinPreview()
        {
            if (_skinSelectedView == null) return;
            if (string.IsNullOrEmpty(_skinSelectedPath) || !File.Exists(_skinSelectedPath))
            {
                _skinSelectedView.Clear();
                return;
            }
            try
            {
                Image image = SkinService.LoadImageCopy(_skinSelectedPath);
                _skinSelectedView.SetSkin(image, _skinSelectedSlim);
            }
            catch (Exception ex)
            {
                _skinSelectedView.Clear();
                SetSkinStatus(ShortMessage(ex), Theme.Red);
            }
        }

        private void ChooseSkinFile()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Выберите скин Minecraft";
                dialog.Filter = "PNG-скины (*.png)|*.png";
                dialog.CheckFileExists = true;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    SkinFile file = SkinService.InspectFile(dialog.FileName);
                    _skinSelectedPath = file.Path;
                    _skinSelectedSlim = file.Height == 64 ? _skinSelectedSlim : false;
                    _skinFileName.Text = Path.GetFileName(file.Path);
                    _skinFileName.ForeColor = Theme.Text;
                    _skinClassicPill.Selected = !_skinSelectedSlim;
                    _skinSlimPill.Selected = _skinSelectedSlim;
                    RefreshSelectedSkinPreview();
                    SetSkinStatus("Файл готов · " + file.Width + "×" + file.Height +
                                  (file.Height == 32 ? " · только классическая модель" : ""), Theme.Green);
                    UpdateSkinButtons();
                }
                catch (SkinException ex)
                {
                    SetSkinStatus(ex.Message, Theme.Red);
                }
            }
        }

        private async void FindSkinByNickname()
        {
            if (_skinBusy) return;
            string nick = (_skinNick.Value ?? "").Trim();
            if (!SkinService.IsValidNickname(nick))
            {
                SetSkinStatus("Ник: 1–16 символов, латиница, цифры и _.", Theme.Red);
                return;
            }

            _skinBusy = true;
            UpdateSkinButtons();
            SetSkinStatus("Ищу игрока " + nick + "…", Theme.Muted);
            try
            {
                SkinInfo info = await SkinService.FetchByNicknameAsync(nick, CancellationToken.None);
                string path = await SkinService.DownloadToLibraryAsync(info, CancellationToken.None);
                _skinSelectedPath = path;
                _skinSelectedSlim = info.Slim;
                _skinFileName.Text = "Скин игрока " + info.Name;
                _skinFileName.ForeColor = Theme.Text;
                _skinClassicPill.Selected = !info.Slim;
                _skinSlimPill.Selected = info.Slim;
                RefreshSelectedSkinPreview();
                SetSkinStatus("Скин игрока " + info.Name + " · " + (info.Slim ? "Slim" : "Classic"),
                    Theme.Green);
                Log("Скин игрока " + info.Name + " сохранён в библиотеку.");
            }
            catch (Exception ex)
            {
                SetSkinStatus(ShortMessage(ex), Theme.Red);
                Log("Поиск скина: " + ShortMessage(ex), Theme.Muted2);
            }
            finally
            {
                _skinBusy = false;
                UpdateSkinButtons();
            }
        }

        private async void ApplySkin()
        {
            if (_skinBusy) return;

            if (_msa == null || !_msa.IsLicensed)
            {
                if (!PromptMicrosoftLogin()) return;
                UpdateSkinAccountUi();
                UpdateSkinButtons();
                if (string.IsNullOrEmpty(_skinSelectedPath))
                {
                    SetSkinStatus("Аккаунт подключён. Выберите скин справа.", Theme.Green);
                    RefreshSkinPageAsync(false);
                    return;
                }
            }

            if (string.IsNullOrEmpty(_skinSelectedPath) || !File.Exists(_skinSelectedPath))
            {
                SetSkinStatus("Сначала выберите PNG или найдите скин по нику.", Theme.Yellow);
                return;
            }

            _skinBusy = true;
            UpdateSkinButtons();
            SetSkinStatus("Загружаю скин в профиль Minecraft…", Theme.Muted);
            try
            {
                MsaSession session = await EnsureMsaAsync();
                await SkinService.UploadAsync(session, _skinSelectedPath, _skinSelectedSlim, CancellationToken.None);
                _skinOwnLoaded = false;
                SetSkinStatus("Скин применён. Он появится в игре при следующем запуске.", Theme.Green);
                SetStatus("Скин обновлён · " + _msa.Name, Theme.Green);
                Log("Скин загружен в профиль Microsoft · " + _msa.Name);
                try
                {
                    await LoadOwnSkinAsync(true);
                }
                catch (Exception previewError)
                {
                    Log("Скин применён, но превью не обновилось: " + ShortMessage(previewError), Theme.Yellow);
                }
            }
            catch (Exception ex)
            {
                HandleSkinError(ex);
            }
            finally
            {
                _skinBusy = false;
                UpdateSkinButtons();
            }
        }

        private void HandleSkinError(Exception ex)
        {
            string message = ShortMessage(ex);
            SetSkinStatus(message, Theme.Red);

            bool expired = false;
            var skin = ex as SkinException;
            if (skin != null) expired = skin.SessionExpired;
            var msa = ex as MsaException;
            if (msa != null) expired = msa.SessionExpired;

            if (expired)
            {
                MsaStore.Clear();
                _msa = null;
                _skinOwn = null;
                _skinOwnLoaded = false;
                if (_skinCurrentView != null) _skinCurrentView.Clear();
                _skinCurrentNote.Text = "Сессия истекла. Войдите через Microsoft снова.";
                UpdateAccountUi();
                UpdateSkinAccountUi();
                UpdateSkinButtons();
            }
            Log("Скины: " + ExceptionChain(ex), expired ? Theme.Yellow : Theme.Red);
        }

        private static string ExceptionChain(Exception ex)
        {
            var sb = new StringBuilder();
            int depth = 0;
            while (ex != null && depth < 5)
            {
                if (depth > 0) sb.Append(" ← ");
                sb.Append(ex.GetType().Name).Append(": ").Append(ex.Message);
                ex = ex.InnerException;
                depth++;
            }
            return sb.ToString();
        }

        private void SetSkinStatus(string text, Color color)
        {
            if (_skinStatus == null || IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke((Action)delegate { SetSkinStatus(text, color); }); }
                catch
                {
                }
                return;
            }
            _skinStatus.Text = text;
            _skinStatus.ForeColor = color;
        }

        private void SetServerStatus(string text, Color color)
        {
            if (_serverStatus == null || IsDisposed) return;
            _serverStatus.Text = text;
            _serverStatus.ForeColor = color;
        }

        private void UpdateServerVersionHint()
        {
            if (_serverVersionHint == null) return;
            VersionEntry entry = _versionList != null ? _versionList.SelectedEntry : null;
            if (entry == null)
            {
                _serverVersionHint.Text = "Версия не выбрана — откройте «Играть»";
                _serverVersionHint.ForeColor = Theme.Yellow;
            }
            else
            {
                _serverVersionHint.Text = "Запуск версии " + entry.Id;
                _serverVersionHint.ForeColor = Theme.Muted2;
            }
        }

        private async void RefreshServersAsync(bool manual)
        {
            if (_serverBusy || _serversPage == null) return;
            if (!manual && (DateTime.UtcNow - _serverPingedUtc).TotalSeconds < 20) return;

            _serverBusy = true;
            _serverPingedUtc = DateTime.UtcNow;
            int generation = ++_serverGeneration;
            if (_serverRefresh != null) _serverRefresh.Enabled = false;
            UpdateServerVersionHint();
            SetServerStatus("Проверяю пинг и онлайн…", Theme.Muted);
            for (int i = 0; i < _serverPing.Length; i++)
            {
                _serverPlayers[i].Text = "Игроки: —";
                _serverPlayers[i].ForeColor = Theme.Muted2;
                _serverPing[i].Text = "Проверка…";
                _serverPing[i].ForeColor = Theme.Muted;
            }
            if (manual) Log("Серверы: проверяю пинг…");

            int online = 0;
            int total = ServerCatalog.All.Length;
            const int batchSize = 6;
            for (int start = 0; start < total; start += batchSize)
            {
                var tasks = new List<Task>();
                int end = Math.Min(total, start + batchSize);
                for (int i = start; i < end; i++)
                {
                    int index = i;
                    tasks.Add(RefreshOneServerAsync(generation, index, delegate(bool ok)
                    {
                        if (ok) Interlocked.Increment(ref online);
                    }));
                }
                await Task.WhenAll(tasks);
                if (generation != _serverGeneration || IsDisposed) return;
            }

            _serverBusy = false;
            if (_serverRefresh != null) _serverRefresh.Enabled = true;
            SetServerStatus(online + " из " + total + " серверов отвечают · " +
                DateTime.Now.ToString("HH:mm:ss"), online > 0 ? Theme.Green : Theme.Red);
            if (manual) Log("Серверы: отвечают " + online + " из " + total + ".");
        }

        private async Task RefreshOneServerAsync(int generation, int index, Action<bool> onResult)
        {
            ServerStatus status;
            try
            {
                status = await ServerPing.PingAsync(ServerCatalog.All[index], ServerPing.DefaultTimeoutMs,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                status = new ServerStatus();
                status.Error = ShortMessage(ex);
            }
            if (onResult != null) onResult(status.Online);
            if (IsDisposed) return;
            try
            {
                BeginInvoke((Action)delegate
                {
                    if (generation == _serverGeneration) ApplyServerStatus(index, status);
                });
            }
            catch
            {
            }
        }

        private void ApplyServerStatus(int index, ServerStatus status)
        {
            if (_serverPlayers == null || index < 0 || index >= _serverPlayers.Length) return;
            if (!status.Online)
            {
                _serverPlayers[index].Text = "Игроки: —";
                _serverPlayers[index].ForeColor = Theme.Muted2;
                _serverPing[index].Text = string.IsNullOrEmpty(status.Error)
                    ? "Недоступен"
                    : "Недоступен · " + status.Error;
                _serverPing[index].ForeColor = Theme.Red;
                return;
            }

            _serverPlayers[index].Text = "Игроки: " + FormatCount(status.OnlinePlayers) + " / " +
                FormatCount(status.MaxPlayers);
            _serverPlayers[index].ForeColor = Theme.Text;
            _serverPing[index].Text = "Пинг: " + status.PingMs + " мс";
            _serverPing[index].ForeColor = status.PingMs < 90 ? Theme.Green
                : (status.PingMs < 180 ? Theme.Yellow : Theme.Red);
        }

        private static string FormatCount(int value)
        {
            return value.ToString("N0", CultureInfo.GetCultureInfo("ru-RU"));
        }

        private async void QuickConnect(int index)
        {
            if (index < 0 || index >= ServerCatalog.All.Length) return;
            ServerEntry server = ServerCatalog.All[index];
            if (_busy)
            {
                SetServerStatus("Лаунчер занят: дождитесь завершения текущей операции.", Theme.Yellow);
                return;
            }

            VersionEntry entry = _versionList != null ? _versionList.SelectedEntry : null;
            if (entry == null)
            {
                SetServerStatus("Сначала выберите версию на странице «Играть».", Theme.Yellow);
                return;
            }

            LaunchAccount account = await ResolveLaunchAccountAsync(delegate(string message)
            {
                SetServerStatus(message, Theme.Red);
            }, false);
            if (!account.Ok) return;

            SetServerStatus("Подключение к " + server.Name + " · " + server.Address + "…", Theme.Muted);
            Log("Быстрое подключение: " + server.Name + " (" + server.Address + ") · версия " + entry.Id);
            ShowPage(0);
            await StartGameAsync(entry, account.Session, server.Host, server.Port);
        }

        private static string UpdateManifestUrl(LauncherSettings settings)
        {
            string url = settings != null ? (settings.UpdateManifestUrl ?? "").Trim() : "";
            if (url.Length == 0)
                url = (Environment.GetEnvironmentVariable("VEXOR_UPDATE_URL") ?? "").Trim();
            return url;
        }

        private async void CheckUpdatesAsync(bool manual)
        {
            if (_updateCheckRunning) return;

            string url = UpdateManifestUrl(_settings);
            if (url.Length == 0)
            {
                if (manual)
                {
                    Log("Источник обновлений не задан: updateManifestUrl в settings.json или VEXOR_UPDATE_URL.",
                        Theme.Yellow);
                    SetStatus("Источник обновлений не настроен.", Theme.Yellow);
                }
                return;
            }

            if (_busy || _gameRunning)
            {
                if (manual) SetStatus("Дождитесь завершения текущей операции.", Theme.Yellow);
                return;
            }

            _updateCheckRunning = true;
            if (manual)
            {
                SetStatus("Проверяю обновления…", Theme.Muted);
                if (_updateButton != null) _updateButton.Enabled = false;
            }

            try
            {
                UpdateManifest manifest = await UpdateService.CheckAsync(url, CancellationToken.None);
                if (!UpdateService.IsNewer(manifest.Version))
                {
                    if (manual)
                    {
                        Log("Обновления: установлена актуальная версия " + UpdateService.CurrentVersion + ".");
                        SetStatus("Установлена последняя версия " + UpdateService.CurrentVersion, Theme.Green);
                    }
                    return;
                }

                Log("Доступно обновление " + UpdateService.CurrentVersion + " → " + manifest.Version,
                    Theme.AccentLight);
                using (var dialog = new UpdateForm(manifest))
                {
                    dialog.ShowDialog(this);
                    if (dialog.RestartRequested) Close();
                }
            }
            catch (Exception ex)
            {
                Log("Проверка обновлений: " + ShortMessage(ex), manual ? Theme.Red : Theme.Muted2);
                if (manual) SetStatus("Обновления: " + ShortMessage(ex), Theme.Red);
            }
            finally
            {
                _updateCheckRunning = false;
                if (_updateButton != null) _updateButton.Enabled = true;
            }
        }

        private bool SaveSettingsFromUi(bool notify)
        {
            string nick = _setNick.Value.Trim();
            if (!ValidNick(nick))
            {
                _saveHint.Text = "Ник: A–Z, 0–9, _";
                _saveHint.ForeColor = Theme.Red;
                _saveTimer.Start();
                if (notify) _setNick.FocusBox();
                return false;
            }

            string oldDir = _settings.GameDir;
            string oldJava = _settings.JavaPath;
            string oldCfKey = _settings.CurseForgeKey;

            _settings.Nickname = nick;
            string dir = _gameDir.Value.Trim();
            _settings.GameDir = string.IsNullOrEmpty(dir) ? AppPaths.DefaultGameDir : dir;
            _settings.RamMb = _ramSlider.Value;
            _settings.OptimizeJvm = _optimize.Checked;
            _settings.ExtraJvmArgs = _jvmArgs.Value.Trim();
            _settings.JavaPath = _javaPath.Value.Trim();
            _settings.CurseForgeKey = _cfKey.Value.Trim();

            int w;
            int h;
            if (!int.TryParse(_resW.Value.Trim(), out w)) w = 0;
            if (!int.TryParse(_resH.Value.Trim(), out h)) h = 0;
            if (w < 0) w = 0;
            if (h < 0) h = 0;
            if (w > 7680) w = 7680;
            if (h > 4320) h = 4320;
            _settings.ResolutionWidth = w;
            _settings.ResolutionHeight = h;
            _settings.Normalize();
            _settings.Save();
            MsaAuth.Configure(_settings.MicrosoftClientId);

            _nickField.Value = _msa != null ? _msa.Name : _settings.Nickname;
            _gameDir.Value = _settings.GameDir;
            _resW.Value = _settings.ResolutionWidth > 0 ? _settings.ResolutionWidth.ToString() : "";
            _resH.Value = _settings.ResolutionHeight > 0 ? _settings.ResolutionHeight.ToString() : "";
            _ramSlider.Value = _settings.RamMb;
            _ramValue.Text = Theme.FormatRam(_settings.RamMb);
            UpdateAccountUi();

            if (!string.Equals(oldCfKey, _settings.CurseForgeKey, StringComparison.Ordinal))
            {
                CurseForgeApi.Configure(_settings.CurseForgeKey);
                _cfCategories = null;
                _modCategoryId = 0;
                _modCatalogKey = "";
                _modSearchedKey = "";
                UpdateCategoryButton();
                UpdateCatalogSourceState();
            }

            if (!string.Equals(oldDir, _settings.GameDir, StringComparison.OrdinalIgnoreCase))
            {
                ReloadBuilds();
                ApplyFilter(true);
                VersionEntry entry = _versionList.SelectedEntry;
                if (entry != null)
                {
                    int token = ++_metaToken;
                    LoadVersionMetaAsync(entry, token);
                }
            }
            if (!string.Equals(oldJava, _settings.JavaPath, StringComparison.OrdinalIgnoreCase))
            {
                RefreshJavaStatusAsync(true);
            }

            if (notify)
            {
                _saveHint.Text = "Сохранено";
                _saveHint.ForeColor = Theme.Green;
                _saveTimer.Start();
                Log("Настройки сохранены.");
            }
            return true;
        }

        private void ChooseGameFolder(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Папка для файлов Minecraft";
                dialog.SelectedPath = _settings.GameDir;
                dialog.ShowNewFolderButton = true;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _gameDir.Value = dialog.SelectedPath;
                    _settings.GameDir = dialog.SelectedPath;
                    _settings.Save();
                    Log("Папка игры: " + dialog.SelectedPath);
                    VersionEntry entry = _versionList.SelectedEntry;
                    if (entry != null)
                    {
                        int token = ++_metaToken;
                        LoadVersionMetaAsync(entry, token);
                    }
                }
            }
        }

        private void OpenFolder(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return;
                Directory.CreateDirectory(path);
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + path + "\""));
            }
            catch (Exception ex)
            {
                Log("Не удалось открыть папку: " + ex.Message, Theme.Red);
            }
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log("Не удалось открыть ссылку: " + ex.Message, Theme.Red);
            }
        }

        private async void RefreshJavaStatusAsync(bool force)
        {
            if (_javaStatus == null) return;
            _javaStatus.ForeColor = Theme.Muted;
            _javaStatus.Text = "Поиск установленных Java…";
            try
            {
                List<JavaInfo> found = await JavaManager.DetectAsync(_settings, force);
                if (found.Count == 0)
                {
                    _javaStatus.Text = "Java не найдена — при первом запуске скачаю нужную версию автоматически.";
                    _javaStatus.ForeColor = Theme.Muted;
                }
                else
                {
                    var majors = new List<string>();
                    foreach (JavaInfo info in found)
                    {
                        string label = "Java " + info.Major;
                        if (!majors.Contains(label)) majors.Add(label);
                    }
                    _javaStatus.Text = "Найдено: " + string.Join(", ", majors.ToArray()) + ".";
                    _javaStatus.ForeColor = Theme.Green;
                }
            }
            catch (Exception ex)
            {
                _javaStatus.Text = "Не удалось проверить Java: " + ex.Message;
                _javaStatus.ForeColor = Theme.Red;
            }
        }

        // ---------------------------------------------------------------- versions

        private async void LoadManifestAsync(bool force)
        {
            if (_loadingManifest || _busy) return;
            _loadingManifest = true;
            if (_refreshButton != null) _refreshButton.Enabled = false;
            if (_status.ForeColor != Theme.Red) SetStatus("Получаю список версий Mojang…", Theme.Muted);

            try
            {
                VersionManifest manifest = await MojangApi.GetManifestAsync(force, CancellationToken.None);
                _manifest = manifest;
                ReloadBuilds();
                Log("Список версий: " + manifest.Versions.Count + " · сборок: " + _builds.Count +
                    " · последний релиз " + manifest.LatestRelease);
                ApplyFilter(true);
                if (_packVersion != null && string.IsNullOrWhiteSpace(_packVersion.Value))
                    _packVersion.Value = manifest.LatestRelease;
                if (!_busy) SetStatus("Готово к запуску", Theme.Muted);
            }
            catch (Exception ex)
            {
                Log("Не удалось загрузить список версий: " + ex.Message, Theme.Red);
                SetStatus("Нет связи с Mojang. Проверьте интернет и нажмите обновление.", Theme.Red);
                if (_all.Count == 0)
                {
                    _versionList.SetItems(new List<VersionEntry>());
                    _versionCount.Text = "0";
                    _versionTitle.Text = "—";
                    _versionMeta.Text = "Список недоступен";
                    _playButton.Enabled = false;
                }
            }
            finally
            {
                _loadingManifest = false;
                if (_refreshButton != null) _refreshButton.Enabled = true;
            }
        }

        private void RefreshClicked(object sender, EventArgs e)
        {
            LoadManifestAsync(true);
        }

        private void SetFilter(string filter)
        {
            _filter = filter;
            _pillAll.Selected = filter == "all";
            _pillRelease.Selected = filter == "release";
            _pillSnapshot.Selected = filter == "snapshot";
            _pillOld.Selected = filter == "old";
            ApplyFilter(false);
        }

        private bool MatchesFilter(VersionEntry entry)
        {
            switch (_filter)
            {
                case "release":
                    return entry.Type == "release" || entry.Type == "fabric" || entry.Type == "forge";
                case "snapshot":
                    return entry.Type == "snapshot";
                case "old":
                    return entry.Type == "old_beta" || entry.Type == "old_alpha";
                default:
                    return true;
            }
        }

        private static VersionEntry FindIn(List<VersionEntry> list, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].Id, id, StringComparison.OrdinalIgnoreCase)) return list[i];
            }
            return null;
        }

        private void ApplyFilter(bool initial)
        {
            if (_versionList == null) return;

            string keepId = _settings.SelectedVersion;
            VersionEntry current = _versionList.SelectedEntry;
            if (current != null) keepId = current.Id;

            string query = (_search.Value ?? "").Trim();
            var filtered = new List<VersionEntry>();
            for (int i = 0; i < _all.Count; i++)
            {
                VersionEntry entry = _all[i];
                if (!MatchesFilter(entry)) continue;
                if (query.Length > 0 &&
                    entry.Id.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
                    entry.DisplayNameOrId.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
                filtered.Add(entry);
            }

            _versionList.SetItems(filtered);
            _versionCount.Text = filtered.Count + " из " + _all.Count;

            if (filtered.Count == 0)
            {
                _versionTitle.Text = "—";
                _versionMeta.Text = "Ничего не найдено";
                _playButton.Enabled = false;
                return;
            }

            VersionEntry chosen = null;
            if (initial && _manifest != null) chosen = FindIn(filtered, _settings.SelectedVersion);
            if (chosen == null) chosen = FindIn(filtered, keepId);
            if (chosen == null && _manifest != null) chosen = FindIn(filtered, _manifest.LatestRelease);
            if (chosen == null) chosen = filtered[0];

            _versionList.SelectIndex(filtered.IndexOf(chosen));
            UpdatePlayButton();
        }

        private void OnVersionSelected(object sender, EventArgs e)
        {
            VersionEntry entry = _versionList.SelectedEntry;
            if (entry == null) return;

            _settings.SelectedVersion = entry.Id;
            _versionTitle.Text = entry.DisplayNameOrId;
            _versionMeta.Text = entry.TypeLabel + " · " + entry.ReleaseDateText;
            if (!_busy) SetStatus("Готово к запуску", Theme.Muted);
            UpdatePlayButton();
            UpdateModsTarget();
            UpdateResourcesTarget();
            UpdateServerVersionHint();

            int token = ++_metaToken;
            LoadVersionMetaAsync(entry, token);
        }

        private async void LoadVersionMetaAsync(VersionEntry entry, int token)
        {
            string gameDir = _settings.GameDir;
            try
            {
                GameVersion version = await MojangApi.EnsureVersionJsonAsync(entry, gameDir, null, CancellationToken.None);
                if (token != _metaToken) return;
                _versionMeta.Text = entry.TypeLabel + " · " + entry.ReleaseDateText + " · Java " + version.JavaMajor;
            }
            catch (Exception ex)
            {
                if (token != _metaToken) return;
                Log("Не удалось получить описание " + entry.Id + ": " + ex.Message, Theme.Red);
            }
        }

        private void UpdatePlayButton()
        {
            if (_playButton == null || _busy) return;
            bool hasVersion = _versionList != null && _versionList.SelectedEntry != null;
            _playButton.Enabled = hasVersion;
        }

        // ------------------------------------------------------------------ play

        private IProgress<BatchProgress> CreateProgress()
        {
            return new Progress<BatchProgress>(delegate(BatchProgress p)
            {
                if (p == null || !_busy) return;
                _progress.Value = p.Fraction;
                string text = p.Status;
                if (p.FilesTotal > 1) text += "  ·  " + p.FilesDone + " / " + p.FilesTotal;
                if (p.BytesTotal > 0)
                    text += "  ·  " + BatchProgress.FormatMb(p.BytesDone) + " из " + BatchProgress.FormatMb(p.BytesTotal);
                _status.Text = text;
                _status.ForeColor = Theme.Muted;
            });
        }

        private void SetGameRunning(bool running, string versionId, int javaMajor)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke((Action)delegate { SetGameRunning(running, versionId, javaMajor); }); }
                catch
                {
                }
                return;
            }
            _gameRunning = running;
            if (running)
            {
                _playButton.Text = "ИГРА ЗАПУЩЕНА";
                _playButton.Glyph = "\uE768";
                _playButton.Enabled = false;
                SetStatus("Minecraft " + versionId + " запущен · Java " + javaMajor, Theme.Green);
            }
        }

        private static string ShortMessage(Exception ex)
        {
            string text = ex.Message ?? "неизвестная ошибка";
            int nl = text.IndexOf('\n');
            if (nl >= 0) text = text.Substring(0, nl);
            text = text.Trim();
            if (text.Length > 150) text = text.Substring(0, 147) + "…";
            return text;
        }

        // ------------------------------------------------------------- loaders

        private void ReloadBuilds()
        {
            _builds = LoaderInstaller.Scan(_settings.GameDir);
            var combined = new List<VersionEntry>();
            foreach (InstalledBuild build in _builds)
            {
                var entry = new VersionEntry();
                entry.Id = build.Id;
                entry.Type = build.Loader;
                entry.ReleaseTime = build.CreatedTime;
                entry.IsModpack = build.IsModpack;
                if (build.IsModpack) entry.DisplayName = build.DisplayName;
                combined.Add(entry);
            }
            if (_manifest != null) combined.AddRange(_manifest.Versions);
            _all = combined;
        }

        private string TargetGameId()
        {
            VersionEntry entry = _versionList != null ? _versionList.SelectedEntry : null;
            if (entry == null) return _settings.SelectedVersion;
            InstalledBuild build = LoaderInstaller.Find(_builds, entry.Id);
            return build != null ? build.GameVersion : entry.Id;
        }

        private InstalledBuild FindBuild(string loader, string gameId)
        {
            foreach (InstalledBuild build in _builds)
            {
                if (build.IsModpack) continue;
                if (!string.Equals(build.Loader, loader, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(build.GameVersion, gameId, StringComparison.OrdinalIgnoreCase)) continue;
                return build;
            }
            return null;
        }

        private void UpdateModsTarget()
        {
            if (_fabricAction == null) return;
            string gameId = TargetGameId();
            ApplyLoaderState(FindBuild(LoaderInstaller.Fabric, gameId), _fabricStatus, _fabricAction, "Установить Fabric");
            ApplyLoaderState(FindBuild(LoaderInstaller.Forge, gameId), _forgeStatus, _forgeAction, "Установить Forge");
            UpdateCatalogTarget(gameId);
        }

        private static void ApplyLoaderState(InstalledBuild build, Label status, RoundedButton action, string installText)
        {
            if (build != null)
            {
                status.Text = "Установлено · " + build.LoaderLabel + " " + build.LoaderVersion;
                status.ForeColor = Theme.Green;
                action.Text = "Папка mods";
            }
            else
            {
                status.Text = "Не установлено";
                status.ForeColor = Theme.Muted2;
                action.Text = installText;
            }
        }

        // ------------------------------------------------------------ modrinth

        private void UpdateCatalogTarget(string gameId)
        {
            if (_modCatalogTarget == null) return;

            InstalledBuild fabric = FindBuild(LoaderInstaller.Fabric, gameId);
            InstalledBuild forge = FindBuild(LoaderInstaller.Forge, gameId);
            bool keep = _modBuild != null && (_modBuild == fabric || _modBuild == forge);
            if (!keep) _modBuild = fabric != null ? fabric : forge;

            _modFabricChoice.Style = _modBuild != null && _modBuild == fabric
                ? RoundedButton.ButtonStyle.Accent : RoundedButton.ButtonStyle.Ghost;
            _modForgeChoice.Style = _modBuild != null && _modBuild == forge
                ? RoundedButton.ButtonStyle.Accent : RoundedButton.ButtonStyle.Ghost;
            UpdateCatalogChoiceState();

            bool has = _modBuild != null;
            _modSearch.Enabled = has && !_modBusy;
            _modFind.Enabled = has && !_modBusy;

            if (!has)
            {
                _modCatalogTarget.Text = string.IsNullOrEmpty(gameId)
                    ? "Версия не выбрана · выберите её на вкладке «Играть»"
                    : "Для " + gameId + " нет сборки · сначала установите Fabric или Forge";
                _modCatalogKey = "";
                _modSearch.Value = "";
                _modInstalled.Clear();
                _modResults.Clear();
                _modMore.Enabled = false;
                SetModStatus("Каталог недоступен без загрузчика.", Theme.Muted2);
                ShowCatalogHint("Установите Fabric или Forge — моды ставятся в папку сборки.");
                return;
            }

            _modCatalogTarget.Text = "Установка в " + _modBuild.LoaderLabel + " " + _modBuild.LoaderVersion +
                " · Minecraft " + _modBuild.GameVersion;

            if (_modCatalogKey == _modBuild.Id) return;

            _modCatalogKey = _modBuild.Id;
            _modSearchedKey = "";
            _modSearch.Value = "";
            _modOffset = 0;
            _modTotal = 0;
            _modResults.Clear();
            HideCategoryMenu();
            _modInstalled = ModrinthApi.ReadInstalledSlugs(_modBuild);
            SetModStatus("Моды для " + _modBuild.Summary, Theme.Muted2);
            ShowCatalogHint("Введите название мода или оставьте строку пустой.");
            if (_modsPage != null && _modsPage.Visible) CatalogSearch(false);
        }

        private void UpdateCatalogChoiceState()
        {
            if (_modFabricChoice == null) return;
            string gameId = TargetGameId();
            _modFabricChoice.Enabled = !_modBusy && FindBuild(LoaderInstaller.Fabric, gameId) != null;
            _modForgeChoice.Enabled = !_modBusy && FindBuild(LoaderInstaller.Forge, gameId) != null;
            if (_modModrinthChoice != null) _modModrinthChoice.Enabled = !_modBusy;
            if (_modCurseChoice != null) _modCurseChoice.Enabled = !_modBusy;
            if (_modCategoryButton != null)
                _modCategoryButton.Enabled = _modSource == "curseforge" && !_modBusy;
        }

        private void UpdateCatalogSourceState()
        {
            if (_modModrinthChoice == null) return;
            bool curse = _modSource == "curseforge";
            _modModrinthChoice.Style = curse
                ? RoundedButton.ButtonStyle.Ghost : RoundedButton.ButtonStyle.Accent;
            _modCurseChoice.Style = curse
                ? RoundedButton.ButtonStyle.Accent : RoundedButton.ButtonStyle.Ghost;
            _modSearch.Bounds = new Rectangle(Theme.S(20), Theme.S(64),
                Theme.S(curse ? 384 : 560), Theme.S(38));
            _modCategoryButton.Visible = curse;
            _modFind.Bounds = new Rectangle(Theme.S(curse ? 596 : 592), Theme.S(64),
                Theme.S(curse ? 96 : 100), Theme.S(38));
            UpdateCategoryButton();
            UpdateCatalogChoiceState();
        }

        private void UpdateCategoryButton()
        {
            if (_modCategoryButton == null) return;
            string name = "все";
            if (_modCategoryId > 0 && _cfCategories != null)
            {
                foreach (CfCategory category in _cfCategories)
                {
                    if (category.Id == _modCategoryId)
                    {
                        name = category.Name;
                        break;
                    }
                }
            }
            _modCategoryButton.Text = "Категория: " + name;
        }

        private void SelectCatalogSource(string source)
        {
            if (_modBusy || string.Equals(_modSource, source, StringComparison.OrdinalIgnoreCase)) return;
            _modSource = source;
            _settings.ModCatalogSource = source;
            _modCategoryId = 0;
            _modCatalogKey = "";
            _modSearchedKey = "";
            _modOffset = 0;
            _modTotal = 0;
            _modResults.Clear();
            HideCategoryMenu();
            UpdateCatalogSourceState();
            UpdateCatalogTarget(TargetGameId());
            EnsureCatalogLoaded();
        }

        private async void CategoryClicked()
        {
            if (_modSource != "curseforge" || _modBusy) return;
            if (!CurseForgeApi.HasKey)
            {
                SetModStatus("CurseForge: укажите API-ключ в Настройках.", Theme.Yellow);
                ShowCatalogHint("Ключ CurseForge API вводится в Настройках → CurseForge.");
                return;
            }
            if (_categoryOverlay != null)
            {
                HideCategoryMenu();
                return;
            }

            if (_cfCategories == null)
            {
                _modCategoryButton.Enabled = false;
                _modCategoryButton.Text = "Загрузка категорий…";
                SetModStatus("Загрузка категорий CurseForge…", Theme.Muted2);
                try
                {
                    _cfCategories = await CurseForgeApi.GetCategoriesAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    string message = ShortMessage(ex);
                    if (!message.StartsWith("CurseForge", StringComparison.OrdinalIgnoreCase))
                        message = "CurseForge: " + message;
                    SetModStatus(message, Theme.Red);
                    string logText = ex.Message;
                    if (!logText.StartsWith("CurseForge", StringComparison.OrdinalIgnoreCase))
                        logText = "CurseForge: " + logText;
                    Log(logText, Theme.Red);
                    UpdateCatalogSourceState();
                    return;
                }
                UpdateCatalogSourceState();
                SetModStatus("Категорий CurseForge: " + _cfCategories.Count + ". Выберите нужную.", Theme.Muted2);
            }
            ShowCategoryMenu();
        }

        private void ShowCategoryMenu()
        {
            HideCategoryMenu();
            if (_cfCategories == null || _modCatalog == null) return;

            if (_modCatalogStatus != null)
            {
                _categoryStatusText = _modCatalogStatus.Text;
                _categoryStatusColor = _modCatalogStatus.ForeColor;
            }

            var backdrop = new Panel();
            backdrop.BackColor = Theme.Bg;
            backdrop.Bounds = new Rectangle(Theme.S(8), Theme.S(106), Theme.S(812), Theme.S(254));
            backdrop.Click += delegate { HideCategoryMenu(); };
            _modCatalog.Controls.Add(backdrop);
            _categoryOverlay = backdrop;

            var menu = new CardPanel();
            menu.Bounds = new Rectangle(Theme.S(402), Theme.S(4), Theme.S(340), Theme.S(246));
            menu.Radius = 14;
            menu.FillTop = Theme.SurfaceAlt;
            menu.FillBottom = Theme.SurfaceAlt;
            menu.Stroke = Theme.Border;
            menu.Shadow = true;
            backdrop.Controls.Add(menu);
            menu.Click += delegate { HideCategoryMenu(); };

            AddLabel(menu, "КАТЕГОРИИ CURSEFORGE", 16, 10, 300, 16,
                Theme.F(9f, FontStyle.Bold), Theme.Muted2);

            var viewport = new Panel();
            viewport.Bounds = new Rectangle(Theme.S(8), Theme.S(32), Theme.S(308), Theme.S(206));
            viewport.BackColor = Theme.SurfaceAlt;
            menu.Controls.Add(viewport);

            var content = new Panel();
            content.BackColor = Theme.SurfaceAlt;
            viewport.Controls.Add(content);

            int index = 0;
            AddCategoryRow(content, 0, "Все категории", _modCategoryId == 0, index++);
            foreach (CfCategory category in _cfCategories)
                AddCategoryRow(content, category.Id, category.Name, _modCategoryId == category.Id, index++);

            int contentHeight = Theme.S(4 + index * 30);
            content.Bounds = new Rectangle(0, 0, Theme.S(306), contentHeight);

            var bar = new DarkScrollBar();
            bar.Bounds = new Rectangle(Theme.S(324), Theme.S(32), Theme.S(6), Theme.S(206));
            bar.Minimum = 0;
            bar.Maximum = Math.Max(0, contentHeight - viewport.Height);
            bar.LargeChange = viewport.Height;
            bar.SmallChange = Theme.S(30);
            bar.Visible = bar.Maximum > 0;
            bar.ValueChanged += delegate { content.Top = -bar.Value; };
            menu.Controls.Add(bar);

            SetCatalogRowsVisible(false);
            SetModStatus("Выберите категорию CurseForge.", Theme.Muted2);

            if (bar.Visible)
            {
                _categoryWheelFilter = new CategoryWheelFilter(backdrop, bar);
                Application.AddMessageFilter(_categoryWheelFilter);
            }
        }

        private void SetCatalogRowsVisible(bool visible)
        {
            for (int i = 0; i < _modRowControls.Count; i++)
            {
                Control control = _modRowControls[i];
                if (control != null && !control.IsDisposed) control.Visible = visible;
            }
        }

        private sealed class CategoryWheelFilter : IMessageFilter
        {
            private readonly Panel _overlay;
            private readonly DarkScrollBar _bar;

            public CategoryWheelFilter(Panel overlay, DarkScrollBar bar)
            {
                _overlay = overlay;
                _bar = bar;
            }

            public bool PreFilterMessage(ref Message message)
            {
                if (message.Msg != 0x020A) return false;
                if (_overlay == null || _overlay.IsDisposed || !_overlay.Visible) return false;
                if (_bar == null || _bar.IsDisposed || !_bar.Visible) return false;
                Point cursor = Cursor.Position;
                if (!_overlay.RectangleToScreen(_overlay.ClientRectangle).Contains(cursor)) return false;
                int delta = (short)((long)message.WParam >> 16);
                if (delta == 0) return false;
                _bar.ScrollBy(-Math.Sign(delta) * _bar.SmallChange);
                return true;
            }
        }

        private void AddCategoryRow(Panel parent, int id, string title, bool selected, int index)
        {
            var row = new RoundedButton();
            row.Text = title;
            row.Style = selected ? RoundedButton.ButtonStyle.Accent : RoundedButton.ButtonStyle.Ghost;
            row.Font = Theme.F(9.5f, FontStyle.Bold);
            row.Bounds = new Rectangle(Theme.S(2), Theme.S(2 + index * 30), Theme.S(300), Theme.S(28));
            row.Click += delegate { SelectCategory(id); };
            parent.Controls.Add(row);
        }

        private void SelectCategory(int id)
        {
            if (_modBusy) return;
            _modCategoryId = id;
            HideCategoryMenu();
            UpdateCategoryButton();
            _modSearchedKey = "";
            _modOffset = 0;
            _modTotal = 0;
            _modResults.Clear();
            CatalogSearch(false);
        }

        private void HideCategoryMenu()
        {
            if (_categoryOverlay == null) return;
            Panel overlay = _categoryOverlay;
            _categoryOverlay = null;
            if (_categoryWheelFilter != null)
            {
                Application.RemoveMessageFilter(_categoryWheelFilter);
                _categoryWheelFilter = null;
            }
            if (overlay.Parent != null) overlay.Parent.Controls.Remove(overlay);
            overlay.Dispose();
            SetCatalogRowsVisible(true);
            if (_categoryStatusText != null)
            {
                SetModStatus(_categoryStatusText, _categoryStatusColor);
                _categoryStatusText = null;
            }
        }

        private void SelectCatalogBuild(InstalledBuild build)
        {
            if (build == null || _modBusy) return;
            _modBuild = build;
            _modCatalogKey = "";
            UpdateCatalogTarget(TargetGameId());
            EnsureCatalogLoaded();
        }

        private void EnsureCatalogLoaded()
        {
            if (_modsPage == null || !_modsPage.Visible) return;
            if (_modBuild == null || _modBusy || _modSearch == null) return;
            string key = CatalogKey(_modBuild, (_modSearch.Value ?? "").Trim());
            if (_modSearchedKey == key) return;
            CatalogSearch(false);
        }

        private string CatalogKey(InstalledBuild build, string query)
        {
            return build.Id + "|" + _modSource + "|" + _modCategoryId + "|" + query;
        }

        private async void CatalogSearch(bool more)
        {
            if (_modBusy || _modBuild == null) return;
            InstalledBuild build = _modBuild;
            string source = _modSource;
            string sourceLabel = source == "curseforge" ? "CurseForge" : "Modrinth";
            string query = (_modSearch.Value ?? "").Trim();
            int offset = more ? _modOffset + 4 : 0;
            if (more && offset >= _modTotal) return;

            if (source == "curseforge" && !CurseForgeApi.HasKey)
            {
                _modResults.Clear();
                HideCategoryMenu();
                ShowCatalogHint("CurseForge требует API-ключ. Вставьте его в Настройках → CurseForge.");
                SetModStatus("CurseForge: ключ API не задан.", Theme.Yellow);
                _modMore.Enabled = false;
                return;
            }

            int token = ++_modToken;
            _modBusy = true;
            _modFind.Enabled = false;
            _modMore.Enabled = false;
            HideCategoryMenu();
            UpdateCatalogChoiceState();
            SetModStatus(query.Length == 0
                ? "Популярные моды " + sourceLabel + " для " + build.GameVersion + "…"
                : "Поиск в " + sourceLabel + ": «" + TrimText(query, 48) + "»…", Theme.Muted2);
            if (!more) ShowCatalogHint("Загрузка каталога " + sourceLabel + "…");

            try
            {
                ModSearchResult found;
                if (source == "curseforge")
                {
                    found = await CurseForgeApi.SearchAsync(query, build.GameVersion, build.Loader,
                        _modCategoryId, offset, 4, CancellationToken.None);
                }
                else
                {
                    found = await ModrinthApi.SearchAsync(query, build.GameVersion, build.Loader,
                        offset, 4, CancellationToken.None);
                }
                if (token != _modToken) return;

                _modResults = found.Hits;
                _modOffset = offset;
                _modTotal = found.TotalHits;
                _modSearchedKey = CatalogKey(build, query);

                if (_modResults.Count == 0)
                {
                    ShowCatalogHint(query.Length == 0
                        ? "Пока нет модов для " + build.GameVersion + " · " + build.LoaderLabel
                        : "Ничего не найдено. Попробуйте другое название.");
                    SetModStatus(sourceLabel + " · найдено 0 модов для " + build.Summary + ".", Theme.Yellow);
                }
                else
                {
                    RenderModRows();
                    int first = offset + 1;
                    int last = offset + _modResults.Count;
                    SetModStatus(sourceLabel + " · " + (query.Length == 0 ? "популярное" : "найдено " + _modTotal) +
                        " · " + first + "–" + last + " из " + _modTotal, Theme.Muted2);
                }
            }
            catch (Exception ex)
            {
                if (token != _modToken) return;
                ShowCatalogHint("Не удалось загрузить каталог " + sourceLabel + ".");
                string message = ShortMessage(ex);
                if (!message.StartsWith(sourceLabel, StringComparison.OrdinalIgnoreCase))
                    message = sourceLabel + ": " + message;
                SetModStatus(message, Theme.Red);
                string logText = ex.Message;
                if (!logText.StartsWith(sourceLabel, StringComparison.OrdinalIgnoreCase))
                    logText = sourceLabel + ": " + logText;
                Log(logText, Theme.Red);
            }
            finally
            {
                if (token == _modToken)
                {
                    _modBusy = false;
                    bool has = _modBuild != null;
                    _modFind.Enabled = has;
                    _modSearch.Enabled = has;
                    _modMore.Enabled = has && _modResults.Count > 0 && _modOffset + _modResults.Count < _modTotal;
                    UpdateCatalogChoiceState();
                }
            }
        }

        private void RenderModRows()
        {
            ClearModRows();
            int y = 112;
            for (int i = 0; i < _modResults.Count; i++)
            {
                ModProject project = _modResults[i];

                var row = new CardPanel();
                row.Bounds = new Rectangle(Theme.S(16), Theme.S(y), Theme.S(796), Theme.S(57));
                row.Radius = 12;
                row.FillTop = Theme.SurfaceAlt;
                row.FillBottom = Theme.SurfaceAlt;
                row.Stroke = Theme.BorderSoft;
                row.Shadow = false;
                _modCatalog.Controls.Add(row);
                _modRowControls.Add(row);

                var icon = new ModIcon();
                icon.Letter = project.Title;
                icon.Tint = ModTint(project.Title);
                icon.Bounds = new Rectangle(Theme.S(12), Theme.S(12), Theme.S(32), Theme.S(32));
                row.Controls.Add(icon);
                if (!string.IsNullOrEmpty(project.IconUrl))
                    LoadModIconAsync(icon, project.IconUrl);

                Label title = AddLabel(row, project.Title, 54, 7, 610, 18,
                    Theme.F(11.5f, FontStyle.Bold), Theme.Text);
                title.AutoEllipsis = true;
                Label description = AddLabel(row, project.Description, 54, 25, 610, 15,
                    Theme.F(9.5f), Theme.Muted);
                description.AutoEllipsis = true;
                AddLabel(row, FormatDownloads(project.Downloads) + " загрузок · " + project.Author,
                    54, 41, 610, 13, Theme.F(8.5f), Theme.Muted2);

                bool installed = _modInstalled.Contains(project.ProjectId) ||
                    _modInstalled.Contains(project.Slug);
                var action = new RoundedButton();
                action.Text = installed ? "Готово" : "Установить";
                action.Style = installed ? RoundedButton.ButtonStyle.Ghost : RoundedButton.ButtonStyle.Surface;
                action.Font = Theme.F(9.5f, FontStyle.Bold);
                action.Bounds = new Rectangle(Theme.S(684), Theme.S(13), Theme.S(100), Theme.S(30));
                action.Enabled = !installed;
                ModProject captured = project;
                RoundedButton button = action;
                action.Click += delegate { InstallMod(captured, button); };
                row.Controls.Add(action);

                y += 63;
            }
        }

        private async void InstallMod(ModProject project, RoundedButton button)
        {
            if (_modBusy || _busy || _modBuild == null)
            {
                SetModStatus("Дождитесь завершения текущей операции.", Theme.Yellow);
                return;
            }

            InstalledBuild build = _modBuild;
            string source = _modSource;
            string sourceLabel = source == "curseforge" ? "CurseForge" : "Modrinth";
            _modBusy = true;
            _modFind.Enabled = false;
            _modMore.Enabled = false;
            HideCategoryMenu();
            UpdateCatalogChoiceState();
            if (button != null)
            {
                button.Enabled = false;
                button.Text = "Загрузка…";
            }
            SetModStatus("Подбор версии: " + project.Title + "…", Theme.Muted2);

            try
            {
                ModVersion version;
                if (source == "curseforge")
                {
                    version = await CurseForgeApi.ResolveAsync(project, build.GameVersion, build.Loader,
                        CancellationToken.None);
                }
                else
                {
                    version = await ModrinthApi.ResolveAsync(project, build.GameVersion, build.Loader,
                        CancellationToken.None);
                }
                SetModStatus(project.Title + " " + version.VersionNumber + " · загрузка…", Theme.Muted2);

                Action<string> writeLog = delegate(string message)
                {
                    Log(sourceLabel + ": " + message);
                    SetModStatus(message, Theme.Muted);
                };
                ModInstallResult installed;
                if (source == "curseforge")
                {
                    installed = await CurseForgeApi.InstallAsync(version, build, writeLog,
                        CancellationToken.None);
                }
                else
                {
                    installed = await ModrinthApi.InstallAsync(version, build, writeLog,
                        CancellationToken.None);
                }

                _modInstalled = ModrinthApi.ReadInstalledSlugs(build);
                bool already = installed.Installed == 0 && installed.AlreadyPresent > 0;
                if (button != null && !button.IsDisposed)
                {
                    button.Text = "Готово";
                    button.Style = RoundedButton.ButtonStyle.Ghost;
                    button.Enabled = false;
                }

                if (already)
                {
                    SetModStatus(project.Title + " уже установлен в " + build.LoaderLabel + ".", Theme.Muted);
                }
                else if (installed.Warnings.Count > 0)
                {
                    SetModStatus("Установлено: " + installed.Installed + " · предупреждений: " +
                        installed.Warnings.Count + " · " +
                        TrimText(installed.Warnings[installed.Warnings.Count - 1], 120),
                        Theme.Yellow);
                    Log(sourceLabel + ": " + project.Title + " установлен с предупреждениями", Theme.Yellow);
                }
                else
                {
                    SetModStatus("Установлено файлов: " + installed.Installed + " · " + build.ModsDir, Theme.Green);
                    Log(sourceLabel + ": " + project.Title + " установлен (" + installed.Installed + " файлов)",
                        Theme.Green);
                }
            }
            catch (Exception ex)
            {
                if (button != null && !button.IsDisposed)
                {
                    button.Text = "Установить";
                    button.Enabled = true;
                }
                SetModStatus("Ошибка: " + ShortMessage(ex), Theme.Red);
                Log(sourceLabel + ": " + ex.Message, Theme.Red);
            }
            finally
            {
                _modBusy = false;
                bool has = _modBuild != null;
                _modFind.Enabled = has;
                _modSearch.Enabled = has;
                _modMore.Enabled = has && _modResults.Count > 0 && _modOffset + _modResults.Count < _modTotal;
                UpdateCatalogChoiceState();
            }
        }

        private async void LoadModIconAsync(ModIcon icon, string url)
        {
            if (icon == null || string.IsNullOrEmpty(url)) return;
            Image picture = ImageCache.Cached(url);
            if (picture != null)
            {
                if (!icon.IsDisposed) icon.SetPicture(picture);
                return;
            }

            try
            {
                picture = await ImageCache.LoadAsync(url, CancellationToken.None);
                if (picture != null && !icon.IsDisposed) icon.SetPicture(picture);
            }
            catch
            {
            }
        }

        private void LoadLoaderLogo(GlyphTile tile, string loader)
        {
            if (tile == null) return;
            string resource = string.Equals(loader, LoaderInstaller.Forge, StringComparison.OrdinalIgnoreCase)
                ? "Vexor.Assets.loader-forge.png"
                : "Vexor.Assets.loader-fabric.png";
            Image picture = ImageCache.LoadEmbedded("loader:" + loader, resource);
            if (picture != null && !tile.IsDisposed) tile.SetPicture(picture);
        }

        private void ClearModRows()
        {
            for (int i = 0; i < _modRowControls.Count; i++)
            {
                Control control = _modRowControls[i];
                if (_modCatalog != null && control.Parent == _modCatalog) _modCatalog.Controls.Remove(control);
                control.Dispose();
            }
            _modRowControls.Clear();
        }

        private void ShowCatalogHint(string text)
        {
            HideCategoryMenu();
            ClearModRows();
            Label hint = AddLabel(_modCatalog, text, 20, 206, 788, 20, Theme.F(10.5f), Theme.Muted2);
            hint.TextAlign = ContentAlignment.MiddleCenter;
            hint.AutoEllipsis = true;
            _modRowControls.Add(hint);
        }

        private void SetModStatus(string text, Color color)
        {
            if (_modCatalogStatus == null || IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke((Action)delegate { SetModStatus(text, color); }); }
                catch
                {
                }
                return;
            }
            _modCatalogStatus.Text = text;
            _modCatalogStatus.ForeColor = color;
        }

        // -------------------------------------------------------------- modpacks

        private void UpdatePacksTarget()
        {
            if (_packVersion == null) return;
            if (string.IsNullOrWhiteSpace(_packVersion.Value))
            {
                string target = TargetGameId();
                if (string.IsNullOrEmpty(target) && _manifest != null) target = _manifest.LatestRelease;
                if (!string.IsNullOrEmpty(target)) _packVersion.Value = target;
            }

            _packInstalled = PackInstalledSet();
            string version = (_packVersion.Value ?? "").Trim();
            _packTarget.Text = string.IsNullOrEmpty(version)
                ? "Укажите версию Minecraft для поиска сборок"
                : "Minecraft " + version + " · установка в отдельный профиль лаунчера";
        }

        private HashSet<string> PackInstalledSet()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (InstalledBuild build in _builds)
            {
                if (!build.IsModpack) continue;
                if (!string.IsNullOrEmpty(build.PackProjectId)) set.Add(build.PackProjectId);
                if (!string.IsNullOrEmpty(build.PackSlug)) set.Add(build.PackSlug);
            }
            return set;
        }

        private void SelectPackLoader(string loader)
        {
            if (_packBusy || _busy) return;
            if (string.Equals(_packLoader, loader, StringComparison.OrdinalIgnoreCase)) return;
            _packLoader = loader ?? "";
            _packSearchedKey = "";
            _packOffset = 0;
            _packTotal = 0;
            _packResults.Clear();
            UpdatePackChoiceState();
            PacksSearch(false);
        }

        private void UpdatePackChoiceState()
        {
            if (_packAllChoice == null) return;
            _packAllChoice.Style = _packLoader.Length == 0
                ? RoundedButton.ButtonStyle.Accent : RoundedButton.ButtonStyle.Ghost;
            _packFabricChoice.Style = _packLoader == LoaderInstaller.Fabric
                ? RoundedButton.ButtonStyle.Accent : RoundedButton.ButtonStyle.Ghost;
            _packForgeChoice.Style = _packLoader == LoaderInstaller.Forge
                ? RoundedButton.ButtonStyle.Accent : RoundedButton.ButtonStyle.Ghost;

            bool enabled = !_packBusy && !_busy;
            _packAllChoice.Enabled = enabled;
            _packFabricChoice.Enabled = enabled;
            _packForgeChoice.Enabled = enabled;
            _packFind.Enabled = enabled;
            _packSearch.Enabled = enabled;
            _packVersion.Enabled = enabled;
            _packMore.Enabled = enabled && _packResults.Count > 0 && _packOffset + _packResults.Count < _packTotal;
        }

        private void EnsurePacksLoaded()
        {
            if (_packsPage == null || !_packsPage.Visible) return;
            if (_packBusy || _packFind == null) return;
            if (string.IsNullOrWhiteSpace(_packVersion.Value)) return;
            if (_packSearchedKey == PackKey()) return;
            PacksSearch(false);
        }

        private string PackKey()
        {
            return _packLoader + "|" + (_packVersion.Value ?? "").Trim() + "|" + (_packSearch.Value ?? "").Trim();
        }

        private async void PacksSearch(bool more)
        {
            if (_packBusy || _busy) return;
            string version = (_packVersion.Value ?? "").Trim();
            if (version.Length == 0)
            {
                SetPackStatus("Укажите версию Minecraft — например, 1.21.1.", Theme.Yellow);
                _packVersion.FocusBox();
                return;
            }

            string query = (_packSearch.Value ?? "").Trim();
            int offset = more ? _packOffset + 6 : 0;
            if (more && offset >= _packTotal) return;

            int token = ++_packToken;
            _packBusy = true;
            _packFind.Enabled = false;
            _packMore.Enabled = false;
            UpdatePackChoiceState();
            SetPackStatus(query.Length == 0
                ? "Популярные модпаки Modrinth для " + version + "…"
                : "Поиск модпаков: «" + TrimText(query, 40) + "»…", Theme.Muted2);
            if (!more) ShowPackHint("Загрузка каталога Modrinth…");

            try
            {
                ModSearchResult found = await ModpacksApi.SearchAsync(query, version, _packLoader, offset, 6,
                    CancellationToken.None);
                if (token != _packToken) return;

                _packResults = found.Hits;
                _packOffset = offset;
                _packTotal = found.TotalHits;
                _packSearchedKey = PackKey();
                _packInstalled = PackInstalledSet();

                if (_packResults.Count == 0)
                {
                    ShowPackHint(query.Length == 0
                        ? "Пока нет модпаков для Minecraft " + version
                        : "Ничего не найдено. Попробуйте другое название.");
                    SetPackStatus("Modrinth · модпаков не найдено для " + version + ".", Theme.Yellow);
                }
                else
                {
                    RenderPackRows();
                    int first = offset + 1;
                    int last = offset + _packResults.Count;
                    SetPackStatus("Modrinth · " + (query.Length == 0 ? "популярное" : "найдено " + _packTotal) +
                        " · " + first + "–" + last + " из " + _packTotal, Theme.Muted2);
                }
            }
            catch (Exception ex)
            {
                if (token != _packToken) return;
                ShowPackHint("Не удалось загрузить каталог модпаков.");
                string message = ShortMessage(ex);
                if (!message.StartsWith("Modrinth", StringComparison.OrdinalIgnoreCase))
                    message = "Modrinth: " + message;
                SetPackStatus(message, Theme.Red);
                Log("Modrinth: " + ex.Message, Theme.Red);
            }
            finally
            {
                if (token == _packToken)
                {
                    _packBusy = false;
                    UpdatePackChoiceState();
                }
            }
        }

        private void RenderPackRows()
        {
            ClearPackRows();
            int y = 112;
            for (int i = 0; i < _packResults.Count; i++)
            {
                ModProject project = _packResults[i];

                var row = new CardPanel();
                row.Bounds = new Rectangle(Theme.S(16), Theme.S(y), Theme.S(796), Theme.S(57));
                row.Radius = 12;
                row.FillTop = Theme.SurfaceAlt;
                row.FillBottom = Theme.SurfaceAlt;
                row.Stroke = Theme.BorderSoft;
                row.Shadow = false;
                _packCatalog.Controls.Add(row);
                _packRowControls.Add(row);

                var icon = new ModIcon();
                icon.Letter = project.Title;
                icon.Tint = ModTint(project.Title);
                icon.Bounds = new Rectangle(Theme.S(12), Theme.S(12), Theme.S(32), Theme.S(32));
                row.Controls.Add(icon);
                if (!string.IsNullOrEmpty(project.IconUrl))
                    LoadModIconAsync(icon, project.IconUrl);

                Label title = AddLabel(row, project.Title, 54, 7, 610, 18,
                    Theme.F(11.5f, FontStyle.Bold), Theme.Text);
                title.AutoEllipsis = true;
                Label description = AddLabel(row, project.Description, 54, 25, 610, 15,
                    Theme.F(9.5f), Theme.Muted);
                description.AutoEllipsis = true;
                AddLabel(row, PackMeta(project), 54, 41, 610, 13, Theme.F(8.5f), Theme.Muted2);

                bool installed = _packInstalled.Contains(project.ProjectId) ||
                    _packInstalled.Contains(project.Slug);
                var action = new RoundedButton();
                action.Text = installed ? "Готово" : "Установить";
                action.Style = installed ? RoundedButton.ButtonStyle.Ghost : RoundedButton.ButtonStyle.Surface;
                action.Font = Theme.F(9.5f, FontStyle.Bold);
                action.Bounds = new Rectangle(Theme.S(684), Theme.S(13), Theme.S(100), Theme.S(30));
                action.Enabled = !installed;
                ModProject captured = project;
                RoundedButton button = action;
                action.Click += delegate { InstallPack(captured, button); };
                row.Controls.Add(action);

                y += 63;
            }
        }

        private async void InstallPack(ModProject project, RoundedButton button)
        {
            if (_packBusy || _busy)
            {
                SetPackStatus("Дождитесь завершения текущей операции.", Theme.Yellow);
                return;
            }

            string minecraft = (_packVersion.Value ?? "").Trim();
            if (minecraft.Length == 0)
            {
                SetPackStatus("Укажите версию Minecraft — например, 1.21.1.", Theme.Yellow);
                _packVersion.FocusBox();
                return;
            }

            _packBusy = true;
            _busy = true;
            _cts = new CancellationTokenSource();
            _playButton.Enabled = false;
            UpdatePackChoiceState();
            if (button != null)
            {
                button.Enabled = false;
                button.Text = "Загрузка…";
            }
            SetPackStatus("Подбор версии: " + project.Title + "…", Theme.Muted2);
            Log("Модпак: " + project.Title + " для Minecraft " + minecraft);
            bool switchToPlay = false;

            try
            {
                ModVersion packVersion = await ModpacksApi.ResolveAsync(project, minecraft, _packLoader,
                    _cts.Token);
                SetPackStatus(project.Title + " " + packVersion.VersionNumber + " · подготовка…", Theme.Muted2);

                IProgress<BatchProgress> progress = new Progress<BatchProgress>(delegate(BatchProgress p)
                {
                    if (p == null || IsDisposed) return;
                    _packProgress.Value = p.Fraction;
                    string text = p.Status;
                    if (p.FilesTotal > 1) text += "  ·  " + p.FilesDone + " / " + p.FilesTotal;
                    if (p.BytesTotal > 0)
                        text += "  ·  " + BatchProgress.FormatMb(p.BytesDone) + " из " +
                            BatchProgress.FormatMb(p.BytesTotal);
                    SetPackStatus(text, Theme.Muted);
                });
                Action<string> writeLog = delegate(string message) { Log("Модпак: " + message); };

                PackInstallResult result = await ModpacksApi.InstallAsync(project, packVersion, _settings, progress,
                    writeLog, _cts.Token);

                ReloadBuilds();
                SetFilter("all");
                _packInstalled = PackInstalledSet();
                _versionList.SelectId(result.Build.Id);
                RenderPackRows();
                _packSearchedKey = PackKey();

                string fileInfo = result.Files + " файлов" +
                    (result.Overrides > 0 ? ", overrides " + result.Overrides : "");
                if (result.Warnings.Count > 0)
                {
                    SetPackStatus("Установлено: " + fileInfo + " · предупреждений: " + result.Warnings.Count,
                        Theme.Yellow);
                    Log("Модпак: " + result.Warnings[result.Warnings.Count - 1], Theme.Yellow);
                }
                else
                {
                    SetPackStatus("«" + result.Build.DisplayName + "» установлен · " + fileInfo, Theme.Green);
                    Log("Модпак: установлен в " + result.Build.ProfileDir, Theme.Green);
                }
                switchToPlay = true;
            }
            catch (OperationCanceledException)
            {
                SetPackStatus("Установка модпака отменена.", Theme.Muted);
                Log("Модпак: установка отменена.");
            }
            catch (Exception ex)
            {
                SetPackStatus("Ошибка: " + ShortMessage(ex), Theme.Red);
                Log("Модпак: " + ex.Message, Theme.Red);
                if (button != null && !button.IsDisposed)
                {
                    button.Text = "Установить";
                    button.Enabled = true;
                }
            }
            finally
            {
                _packBusy = false;
                _busy = false;
                if (_cts != null)
                {
                    try { _cts.Dispose(); }
                    catch
                    {
                    }
                    _cts = null;
                }
                _packProgress.Value = 0;
                _packInstalled = PackInstalledSet();
                UpdatePackChoiceState();
                UpdatePlayButton();
                if (switchToPlay)
                {
                    ShowPage(0);
                    SetStatus("Модпак установлен — нажмите ИГРАТЬ", Theme.Green);
                }
            }
        }

        private static string PackMeta(ModProject project)
        {
            var labels = new List<string>();
            foreach (string category in project.Categories)
            {
                string label = null;
                if (string.Equals(category, "fabric", StringComparison.OrdinalIgnoreCase)) label = "Fabric";
                else if (string.Equals(category, "forge", StringComparison.OrdinalIgnoreCase)) label = "Forge";
                else if (string.Equals(category, "quilt", StringComparison.OrdinalIgnoreCase)) label = "Quilt";
                else if (string.Equals(category, "neoforge", StringComparison.OrdinalIgnoreCase)) label = "NeoForge";
                if (label != null && !labels.Contains(label)) labels.Add(label);
            }
            string loaders = labels.Count > 0 ? string.Join(" · ", labels.ToArray()) + " · " : "";
            return loaders + FormatDownloads(project.Downloads) + " загрузок · " + project.Author;
        }

        private void ClearPackRows()
        {
            for (int i = 0; i < _packRowControls.Count; i++)
            {
                Control control = _packRowControls[i];
                if (_packCatalog != null && control.Parent == _packCatalog) _packCatalog.Controls.Remove(control);
                control.Dispose();
            }
            _packRowControls.Clear();
        }

        private void ShowPackHint(string text)
        {
            ClearPackRows();
            Label hint = AddLabel(_packCatalog, text, 20, 206, 788, 20, Theme.F(10.5f), Theme.Muted2);
            hint.TextAlign = ContentAlignment.MiddleCenter;
            hint.AutoEllipsis = true;
            _packRowControls.Add(hint);
        }

        private void SetPackStatus(string text, Color color)
        {
            if (_packStatus == null || IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke((Action)delegate { SetPackStatus(text, color); }); }
                catch
                {
                }
                return;
            }
            _packStatus.Text = text;
            _packStatus.ForeColor = color;
        }

        // ------------------------------------------------ resource packs & shaders

        private bool ShaderLoaderInstalled(InstalledBuild build)
        {
            if (build == null) return false;
            try
            {
                string dir = build.ModsDir;
                if (!Directory.Exists(dir)) return false;
                foreach (string file in Directory.GetFiles(dir, "*.jar"))
                {
                    string name = Path.GetFileName(file);
                    if (name.IndexOf("iris", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("oculus", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            catch
            {
            }
            return false;
        }

        private string CatalogNoun()
        {
            return _shaderMode ? "шейдеров" : "ресурспаков";
        }

        private string CatalogInputNoun()
        {
            return _shaderMode ? "шейдера" : "ресурспака";
        }

        private string CatalogFolder()
        {
            return _shaderMode ? ShadersApi.FolderName : ResourcePacksApi.FolderName;
        }

        private HashSet<string> ReadCatalogInstalled(string gameDir)
        {
            return _shaderMode ? ShadersApi.ReadInstalled(gameDir) : ResourcePacksApi.ReadInstalled(gameDir);
        }

        private void SetCatalogMode(bool shaders)
        {
            if (_shaderMode == shaders)
            {
                ShowPage(3);
                return;
            }

            _shaderMode = shaders;
            _resToken++;
            _resTargetKey = "";
            _resGameDir = "";
            _resGameVersion = "";
            _resSearchedKey = "";
            _resOffset = 0;
            _resTotal = 0;
            _resResults.Clear();
            _resInstalled.Clear();
            _resSearch.Value = "";
            ClearResourceRows();
            ApplyCatalogModeText();
            ShowPage(3);
        }

        private void ApplyCatalogModeText()
        {
            if (_resHeader == null) return;
            _resHeader.Text = _shaderMode ? "Шейдеры" : "Ресурспаки";
            _resSubtitle.Text = _shaderMode
                ? "Шейдеры из каталога Modrinth для Iris и Optifine"
                : "Текстуры из каталога Modrinth — в выбранную сборку или ваниль";
            _resCardTitle.Text = _shaderMode ? "Каталог шейдеров" : "Каталог ресурспаков";
            _resSearch.Placeholder = _shaderMode
                ? "Найти шейдер: Complementary, BSL…"
                : "Найти ресурспак: Faithful, Fresh Animations…";
        }

        private void UpdateResourcesTarget()
        {
            if (_resTarget == null) return;
            VersionEntry entry = _versionList != null ? _versionList.SelectedEntry : null;
            if (entry == null)
            {
                _resTargetKey = "";
                _resGameDir = "";
                _resGameVersion = "";
                _resInstalled.Clear();
                _resResults.Clear();
                _resSearch.Enabled = false;
                _resFind.Enabled = false;
                _resMore.Enabled = false;
                _resTarget.ForeColor = Theme.Muted2;
                _resTarget.Text = "Версия не выбрана · выберите её на вкладке «Играть»";
                ShowResourceHint("Сначала выберите версию Minecraft.");
                SetResourceStatus("Каталог недоступен без версии.", Theme.Muted2);
                return;
            }

            InstalledBuild build = LoaderInstaller.Find(_builds, entry.Id);
            string gameDir = build != null ? build.ProfileDir : _settings.GameDir;
            string gameVersion = build != null ? build.GameVersion : entry.Id;
            string key = (_shaderMode ? "shader|" : "resource|") + gameDir + "|" + gameVersion;

            string where = build != null
                ? build.LoaderLabel + (build.IsModpack ? " · модпак" : "") + " · профиль"
                : "ваниль";
            string suffix = "";
            bool missingLoader = false;
            if (_shaderMode)
            {
                bool ready = ShaderLoaderInstalled(build);
                missingLoader = !ready;
                if (build == null) suffix = " · нужен Iris или Optifine";
                else if (string.Equals(build.Loader, LoaderInstaller.Fabric, StringComparison.OrdinalIgnoreCase))
                    suffix = ready ? " · Iris" : " · Iris не найден";
                else if (string.Equals(build.Loader, LoaderInstaller.Forge, StringComparison.OrdinalIgnoreCase))
                    suffix = ready ? " · Oculus" : " · Oculus не найден";
                else suffix = ready ? " · шейдеры" : " · нужен Iris";
            }
            _resTarget.Text = "Minecraft " + gameVersion + " · " + where + " · папка " +
                CatalogFolder() + suffix;
            _resTarget.ForeColor = missingLoader ? Theme.Yellow : Theme.Muted2;

            if (key == _resTargetKey)
            {
                _resSearch.Enabled = !_resBusy;
                _resFind.Enabled = !_resBusy && !_busy;
                return;
            }

            _resTargetKey = key;
            _resGameDir = gameDir;
            _resGameVersion = gameVersion;
            _resSearchedKey = "";
            _resOffset = 0;
            _resTotal = 0;
            _resResults.Clear();
            _resSearch.Value = "";
            _resInstalled = ReadCatalogInstalled(gameDir);
            _resSearch.Enabled = !_resBusy;
            _resFind.Enabled = !_resBusy && !_busy;
            _resMore.Enabled = false;
            SetResourceStatus((_shaderMode ? "Шейдеры для Minecraft " : "Ресурспаки для Minecraft ") +
                gameVersion, Theme.Muted2);
            ShowResourceHint("Введите название " + CatalogInputNoun() + " или оставьте строку пустой.");
            if (_resourcesPage != null && _resourcesPage.Visible && !_resBusy && !_busy)
                ResourceSearch(false);
        }

        private void EnsureResourcesLoaded()
        {
            if (_resourcesPage == null || !_resourcesPage.Visible) return;
            if (_resBusy || _busy || _resFind == null) return;
            if (string.IsNullOrEmpty(_resGameDir)) return;
            if (_resSearchedKey == ResourceKey()) return;
            ResourceSearch(false);
        }

        private string ResourceKey()
        {
            return _resTargetKey + "|" + (_resSearch.Value ?? "").Trim();
        }

        private async void ResourceSearch(bool more)
        {
            if (_resBusy || _busy) return;
            if (string.IsNullOrEmpty(_resGameDir) || string.IsNullOrEmpty(_resGameVersion))
            {
                SetResourceStatus("Выберите версию на вкладке «Играть».", Theme.Yellow);
                return;
            }

            string query = (_resSearch.Value ?? "").Trim();
            int offset = more ? _resOffset + 6 : 0;
            if (more && offset >= _resTotal) return;

            int token = ++_resToken;
            _resBusy = true;
            _resFind.Enabled = false;
            _resMore.Enabled = false;
            _resSearch.Enabled = false;
            SetResourceStatus(query.Length == 0
                ? (_shaderMode ? "Популярные шейдеры" : "Популярные ресурспаки") +
                    " Modrinth для " + _resGameVersion + "…"
                : (_shaderMode ? "Поиск шейдеров: «" : "Поиск ресурспаков: «") +
                    TrimText(query, 40) + "»…", Theme.Muted2);
            if (!more) ShowResourceHint("Загрузка каталога Modrinth…");

            bool shaders = _shaderMode;
            try
            {
                ModSearchResult found;
                if (shaders)
                    found = await ShadersApi.SearchAsync(query, _resGameVersion, offset, 6,
                        CancellationToken.None);
                else
                    found = await ResourcePacksApi.SearchAsync(query, _resGameVersion, offset, 6,
                        CancellationToken.None);
                if (token != _resToken) return;

                _resResults = found.Hits;
                _resOffset = offset;
                _resTotal = found.TotalHits;
                _resSearchedKey = ResourceKey();
                _resInstalled = ReadCatalogInstalled(_resGameDir);

                if (_resResults.Count == 0)
                {
                    ShowResourceHint(query.Length == 0
                        ? "Пока нет " + CatalogNoun() + " для Minecraft " + _resGameVersion
                        : "Ничего не найдено. Попробуйте другое название.");
                    SetResourceStatus("Modrinth · " + CatalogNoun() + " не найдено для " +
                        _resGameVersion + ".", Theme.Yellow);
                }
                else
                {
                    RenderResourceRows();
                    int first = offset + 1;
                    int last = offset + _resResults.Count;
                    SetResourceStatus("Modrinth · " + (query.Length == 0 ? "популярное" : "найдено " + _resTotal) +
                        " · " + first + "–" + last + " из " + _resTotal, Theme.Muted2);
                }
            }
            catch (Exception ex)
            {
                if (token != _resToken) return;
                ShowResourceHint(shaders
                    ? "Не удалось загрузить каталог шейдеров."
                    : "Не удалось загрузить каталог ресурспаков.");
                string message = ShortMessage(ex);
                if (!message.StartsWith("Modrinth", StringComparison.OrdinalIgnoreCase))
                    message = "Modrinth: " + message;
                SetResourceStatus(message, Theme.Red);
                Log((shaders ? "Шейдеры: " : "Ресурспаки: ") + ex.Message, Theme.Red);
            }
            finally
            {
                if (token == _resToken)
                {
                    _resBusy = false;
                    bool has = !string.IsNullOrEmpty(_resGameDir) && !_busy;
                    _resSearch.Enabled = has;
                    _resFind.Enabled = has;
                    _resMore.Enabled = has && _resResults.Count > 0 &&
                        _resOffset + _resResults.Count < _resTotal;
                }
            }
        }

        private void RenderResourceRows()
        {
            ClearResourceRows();
            int y = 112;
            for (int i = 0; i < _resResults.Count; i++)
            {
                ModProject project = _resResults[i];

                var row = new CardPanel();
                row.Bounds = new Rectangle(Theme.S(16), Theme.S(y), Theme.S(796), Theme.S(57));
                row.Radius = 12;
                row.FillTop = Theme.SurfaceAlt;
                row.FillBottom = Theme.SurfaceAlt;
                row.Stroke = Theme.BorderSoft;
                row.Shadow = false;
                _resCatalog.Controls.Add(row);
                _resRowControls.Add(row);

                var icon = new ModIcon();
                icon.Letter = project.Title;
                icon.Tint = ModTint(project.Title);
                icon.Bounds = new Rectangle(Theme.S(12), Theme.S(12), Theme.S(32), Theme.S(32));
                row.Controls.Add(icon);
                if (!string.IsNullOrEmpty(project.IconUrl))
                    LoadModIconAsync(icon, project.IconUrl);

                Label title = AddLabel(row, project.Title, 54, 7, 610, 18,
                    Theme.F(11.5f, FontStyle.Bold), Theme.Text);
                title.AutoEllipsis = true;
                Label description = AddLabel(row, project.Description, 54, 25, 610, 15,
                    Theme.F(9.5f), Theme.Muted);
                description.AutoEllipsis = true;
                AddLabel(row, ResourceMeta(project), 54, 41, 610, 13, Theme.F(8.5f), Theme.Muted2);

                bool installed = _resInstalled.Contains(project.ProjectId) ||
                    _resInstalled.Contains(project.Slug);
                var action = new RoundedButton();
                action.Text = installed ? "Готово" : "Установить";
                action.Style = installed ? RoundedButton.ButtonStyle.Ghost : RoundedButton.ButtonStyle.Surface;
                action.Font = Theme.F(9.5f, FontStyle.Bold);
                action.Bounds = new Rectangle(Theme.S(684), Theme.S(13), Theme.S(100), Theme.S(30));
                action.Enabled = !installed;
                ModProject captured = project;
                RoundedButton button = action;
                action.Click += delegate { InstallResource(captured, button); };
                row.Controls.Add(action);

                y += 63;
            }
        }

        private async void InstallResource(ModProject project, RoundedButton button)
        {
            if (_resBusy || _busy)
            {
                SetResourceStatus("Дождитесь завершения текущей операции.", Theme.Yellow);
                return;
            }
            string gameDir = _resGameDir;
            string gameVersion = _resGameVersion;
            bool shaders = _shaderMode;
            if (string.IsNullOrEmpty(gameDir) || string.IsNullOrEmpty(gameVersion))
            {
                SetResourceStatus("Выберите версию на вкладке «Играть».", Theme.Yellow);
                return;
            }

            _resBusy = true;
            _resFind.Enabled = false;
            _resMore.Enabled = false;
            _resSearch.Enabled = false;
            if (button != null)
            {
                button.Enabled = false;
                button.Text = "Загрузка…";
            }
            SetResourceStatus("Подбор версии: " + project.Title + "…", Theme.Muted2);

            try
            {
                ModVersion version;
                if (shaders)
                    version = await ShadersApi.ResolveAsync(project, gameVersion, CancellationToken.None);
                else
                    version = await ResourcePacksApi.ResolveAsync(project, gameVersion, CancellationToken.None);
                SetResourceStatus(project.Title + " " + version.VersionNumber + " · скачивание…", Theme.Muted2);

                string logPrefix = shaders ? "Шейдер" : "Ресурспак";
                Action<string> writeLog = delegate(string message)
                {
                    Log(logPrefix + ": " + message);
                    SetResourceStatus(message, Theme.Muted);
                };
                ModInstallResult installed;
                if (shaders)
                    installed = await ShadersApi.InstallAsync(project, version, gameDir, writeLog,
                        CancellationToken.None);
                else
                    installed = await ResourcePacksApi.InstallAsync(project, version, gameDir, writeLog,
                        CancellationToken.None);

                _resInstalled = ReadCatalogInstalled(gameDir);
                if (button != null && !button.IsDisposed)
                {
                    button.Text = "Готово";
                    button.Style = RoundedButton.ButtonStyle.Ghost;
                    button.Enabled = false;
                }

                bool already = installed.Installed == 0 && installed.AlreadyPresent > 0;
                if (already)
                {
                    SetResourceStatus(project.Title + " уже установлен.", Theme.Muted);
                }
                else
                {
                    string fileName = installed.Files.Count > 0 ? installed.Files[0] : version.VersionNumber;
                    string folder = shaders ? ShadersApi.FolderName : ResourcePacksApi.FolderName;
                    SetResourceStatus("Установлен: " + fileName + " · папка " + folder, Theme.Green);
                    Log(logPrefix + ": " + project.Title + " установлен в " + gameDir, Theme.Green);
                }
            }
            catch (Exception ex)
            {
                if (button != null && !button.IsDisposed)
                {
                    button.Text = "Установить";
                    button.Enabled = true;
                }
                SetResourceStatus("Ошибка: " + ShortMessage(ex), Theme.Red);
                Log((shaders ? "Шейдер: " : "Ресурспак: ") + ex.Message, Theme.Red);
            }
            finally
            {
                _resBusy = false;
                bool has = !string.IsNullOrEmpty(_resGameDir) && !_busy;
                _resSearch.Enabled = has;
                _resFind.Enabled = has;
                _resMore.Enabled = has && _resResults.Count > 0 &&
                    _resOffset + _resResults.Count < _resTotal;
            }
        }

        private static string ResourceMeta(ModProject project)
        {
            var labels = new List<string>();
            for (int i = 0; i < project.Categories.Count && labels.Count < 3; i++)
            {
                string category = project.Categories[i];
                if (!string.IsNullOrEmpty(category)) labels.Add(category);
            }
            string prefix = labels.Count > 0 ? string.Join(" · ", labels.ToArray()) + " · " : "";
            return prefix + FormatDownloads(project.Downloads) + " загрузок · " + project.Author;
        }

        private void ClearResourceRows()
        {
            for (int i = 0; i < _resRowControls.Count; i++)
            {
                Control control = _resRowControls[i];
                if (_resCatalog != null && control.Parent == _resCatalog) _resCatalog.Controls.Remove(control);
                control.Dispose();
            }
            _resRowControls.Clear();
        }

        private void ShowResourceHint(string text)
        {
            ClearResourceRows();
            Label hint = AddLabel(_resCatalog, text, 20, 206, 788, 20, Theme.F(10.5f), Theme.Muted2);
            hint.TextAlign = ContentAlignment.MiddleCenter;
            hint.AutoEllipsis = true;
            _resRowControls.Add(hint);
        }

        private void SetResourceStatus(string text, Color color)
        {
            if (_resStatus == null || IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke((Action)delegate { SetResourceStatus(text, color); }); }
                catch
                {
                }
                return;
            }
            _resStatus.Text = text;
            _resStatus.ForeColor = color;
        }

        private static string FormatDownloads(long downloads)
        {
            if (downloads >= 1000000000L)
                return (downloads / 1000000000.0).ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',') + " млрд";
            if (downloads >= 1000000L)
                return (downloads / 1000000.0).ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',') + " млн";
            if (downloads >= 1000L)
                return (downloads / 1000.0).ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',') + " тыс.";
            return downloads.ToString(CultureInfo.InvariantCulture);
        }

        private static string TrimText(string text, int max)
        {
            if (text == null) return "";
            if (text.Length <= max) return text;
            return text.Substring(0, max - 1) + "…";
        }

        private static Color ModTint(string title)
        {
            Color[] palette = new[]
            {
                Theme.Accent, Theme.AccentLight, Theme.Green, Theme.Yellow,
                Color.FromArgb(86, 204, 242), Color.FromArgb(244, 114, 182)
            };
            int hash = 0;
            if (!string.IsNullOrEmpty(title))
            {
                for (int i = 0; i < title.Length; i++) hash = unchecked(hash * 31 + title[i]);
            }
            return palette[(hash & 0x7fffffff) % palette.Length];
        }

        private async void LoaderAction(string loader)
        {
            string gameId = TargetGameId();
            if (string.IsNullOrEmpty(gameId))
            {
                SetStatus("Сначала выберите версию на вкладке «Играть».", Theme.Red);
                return;
            }

            InstalledBuild existing = FindBuild(loader, gameId);
            if (existing != null)
            {
                OpenMods(existing);
                return;
            }
            if (_busy)
            {
                SetStatus("Дождитесь завершения текущей операции.", Theme.Yellow);
                return;
            }

            List<VersionEntry> source = _manifest != null ? _manifest.Versions : new List<VersionEntry>();
            VersionEntry entry = FindIn(source, gameId);
            if (entry == null)
            {
                SetStatus("Версия " + gameId + " не найдена в списке Mojang.", Theme.Red);
                return;
            }

            bool isFabric = string.Equals(loader, LoaderInstaller.Fabric, StringComparison.OrdinalIgnoreCase);
            Label status = isFabric ? _fabricStatus : _forgeStatus;
            string loaderLabel = isFabric ? "Fabric" : "Forge";
            string gameDir = _settings.GameDir;

            _busy = true;
            _cts = new CancellationTokenSource();
            _playButton.Enabled = false;
            _progress.Value = 0;
            status.ForeColor = Theme.Muted;
            status.Text = "Подготовка…";
            Log("Установка " + loaderLabel + " для " + gameId + "…");

            try
            {
                IProgress<BatchProgress> progress = new Progress<BatchProgress>(delegate(BatchProgress p)
                {
                    if (p == null) return;
                    _progress.Value = p.Fraction;
                    string text = p.Status;
                    if (p.FilesTotal > 1) text += "  ·  " + p.FilesDone + " / " + p.FilesTotal;
                    else if (p.BytesTotal > 0)
                        text += "  ·  " + BatchProgress.FormatMb(p.BytesDone) + " из " + BatchProgress.FormatMb(p.BytesTotal);
                    _status.Text = text;
                    _status.ForeColor = Theme.Muted;
                    status.Text = text;
                });

                GameVersion vanilla = await MojangApi.EnsureVersionJsonAsync(entry, gameDir, Log, _cts.Token);
                string javaPath = await JavaManager.EnsureJavaAsync(vanilla.JavaMajor, _settings, progress, Log, _cts.Token);
                _cts.Token.ThrowIfCancellationRequested();

                var features = new FeatureSet();
                await GameInstaller.InstallAsync(vanilla, gameDir, features, progress, Log, _cts.Token);
                _cts.Token.ThrowIfCancellationRequested();

                InstalledBuild build;
                if (isFabric)
                    build = await LoaderInstaller.EnsureFabricAsync(gameId, gameDir, null, progress, Log, _cts.Token);
                else
                    build = await LoaderInstaller.EnsureForgeAsync(gameId, gameDir, javaPath, progress, Log, _cts.Token);

                ReloadBuilds();
                ApplyFilter(false);
                UpdateModsTarget();

                Log("Сборка готова: " + build.Summary);
                Log("Папка модов: " + build.ModsDir);
                SetStatus("Сборка " + build.LoaderLabel + " " + build.LoaderVersion +
                    " готова — выберите её в списке версий", Theme.Green);
            }
            catch (OperationCanceledException)
            {
                SetStatus("Установка отменена.", Theme.Muted);
                Log("Установка отменена.");
            }
            catch (Exception ex)
            {
                SetStatus("Ошибка установки: " + ShortMessage(ex), Theme.Red);
                Log("Ошибка установки: " + ex.Message, Theme.Red);
            }
            finally
            {
                _busy = false;
                if (_cts != null)
                {
                    try { _cts.Dispose(); }
                    catch
                    {
                    }
                    _cts = null;
                }
                _progress.Value = 0;
                _playButton.Enabled = _versionList.SelectedEntry != null;
                UpdateModsTarget();
            }
        }

        private void OpenMods(InstalledBuild build)
        {
            try
            {
                LoaderInstaller.PrepareProfile(build);
                Process.Start("explorer.exe", "\"" + build.ModsDir + "\"");
                Log("Папка модов: " + build.ModsDir);
            }
            catch (Exception ex)
            {
                Log("Не удалось открыть папку mods: " + ex.Message, Theme.Red);
            }
        }

        private sealed class LaunchAccount
        {
            public MsaSession Session;
            public bool Ok;
        }

        private async Task<LaunchAccount> ResolveLaunchAccountAsync(Action<string> report, bool focusNick)
        {
            var result = new LaunchAccount();
            if (_msa != null)
            {
                try
                {
                    MsaSession account = await EnsureMsaAsync();
                    if (account == null)
                    {
                        report("Сессия Microsoft истекла. Войдите снова.");
                        return result;
                    }
                    result.Session = account;
                    result.Ok = true;
                    return result;
                }
                catch (Exception ex)
                {
                    report("Microsoft: " + ShortMessage(ex));
                    Log("Ошибка Microsoft: " + ex.Message, Theme.Red);
                    return result;
                }
            }

            string nick = _nickField.Value.Trim();
            if (!ValidNick(nick))
            {
                report("Ник: 1–16 символов, латиница, цифры и _");
                if (focusNick) _nickField.FocusBox();
                return result;
            }

            _settings.Nickname = nick;
            _setNick.Value = nick;
            _sidebar.AccountName = nick;
            _sidebar.Invalidate();
            result.Ok = true;
            return result;
        }

        private async void PlayClicked(object sender, EventArgs e)
        {
            if (_busy)
            {
                if (!_gameRunning && _cts != null)
                {
                    _cts.Cancel();
                    SetStatus("Отмена…", Theme.Yellow);
                }
                return;
            }

            VersionEntry entry = _versionList.SelectedEntry;
            if (entry == null)
            {
                SetStatus("Сначала выберите версию.", Theme.Red);
                return;
            }

            LaunchAccount resolved = await ResolveLaunchAccountAsync(delegate(string message)
            {
                SetStatus(message, Theme.Red);
            }, true);
            if (!resolved.Ok) return;

            await StartGameAsync(entry, resolved.Session, null, 0);
        }

        private async Task StartGameAsync(VersionEntry entry, MsaSession account, string connectHost, int connectPort)
        {
            _settings.SelectedVersion = entry.Id;
            _settings.Normalize();
            _settings.Save();

            _busy = true;
            _gameRunning = false;
            _cts = new CancellationTokenSource();
            _playButton.Style = RoundedButton.ButtonStyle.Danger;
            _playButton.Text = "ОТМЕНА";
            _playButton.Glyph = "";
            _playButton.Enabled = true;
            _progress.Value = 0;
            SetStatus(string.IsNullOrEmpty(connectHost)
                ? "Подготовка к запуску…"
                : "Подготовка к подключению…", Theme.Muted);

            string gameDir = _settings.GameDir;
            InstalledBuild build = LoaderInstaller.Find(_builds, entry.Id);
            string launchGameDir = gameDir;
            if (build != null)
            {
                LoaderInstaller.PrepareProfile(build);
                launchGameDir = build.ProfileDir;
                Log("Сборка: " + build.Summary + " · mods: " + build.ModsDir);
            }

            try
            {
                GameVersion version = await MojangApi.EnsureVersionJsonAsync(entry, gameDir, Log, _cts.Token);
                Log("Версия " + version.Id + " · требуется Java " + version.JavaMajor);

                IProgress<BatchProgress> progress = CreateProgress();
                string javaPath = await JavaManager.EnsureJavaAsync(version.JavaMajor, _settings, progress, Log,
                    _cts.Token);
                _cts.Token.ThrowIfCancellationRequested();

                var features = new FeatureSet();
                features.HasCustomResolution = _settings.ResolutionWidth > 0 && _settings.ResolutionHeight > 0;

                InstallResult install = await GameInstaller.InstallAsync(version, gameDir, features, progress, Log,
                    _cts.Token);
                _cts.Token.ThrowIfCancellationRequested();

                _cts.Dispose();
                _cts = null;
                _playButton.Style = RoundedButton.ButtonStyle.Accent;
                _playButton.Text = "ЗАПУСК…";
                _playButton.Enabled = false;
                _progress.Value = 1;
                SetStatus(string.IsNullOrEmpty(connectHost)
                    ? "Запускаю Minecraft…"
                    : "Подключаюсь к " + connectHost + "…", Theme.Muted);

                int exitCode = await GameLauncher.LaunchAsync(version, entry, install, _settings, account, javaPath,
                    launchGameDir, features, Log,
                    delegate { SetGameRunning(true, version.Id, version.JavaMajor); },
                    CancellationToken.None, connectHost, connectPort);

                if (_gameRunning) SetGameRunning(false, version.Id, version.JavaMajor);
                SetStatus("Игра завершена · код " + exitCode, Theme.Muted);
                Log("Игра завершена с кодом " + exitCode + ".");
            }
            catch (OperationCanceledException)
            {
                SetStatus("Запуск отменён.", Theme.Muted);
                Log("Операция отменена.");
            }
            catch (Exception ex)
            {
                SetStatus("Ошибка: " + ShortMessage(ex), Theme.Red);
                Log("Ошибка: " + ex.Message, Theme.Red);
            }
            finally
            {
                _busy = false;
                _gameRunning = false;
                if (_cts != null)
                {
                    try { _cts.Dispose(); }
                    catch
                    {
                    }
                    _cts = null;
                }
                _progress.Value = 0;
                _playButton.Style = RoundedButton.ButtonStyle.Accent;
                _playButton.Text = "ИГРАТЬ";
                _playButton.Glyph = "\uE768";
                _playButton.Enabled = _versionList.SelectedEntry != null;
            }
        }
    }
}
