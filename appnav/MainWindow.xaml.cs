using appnav.Utils;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

namespace appnav;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const string icoPath = "pack://application:,,,/appnav;component/images/navigate-outline.ico";
    private readonly HotKey _hotKey;

    private NotifyIcon TrayIcon;
    private readonly ContextMenuStrip TrayIconContextMenu = new();
    private readonly ToolStripMenuItem CloseMenuItem = new();
    WindowInfo? lastSelectedItem;

    private double _sOpacity = 0.9;

    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly string[] blackList = new string[] { "appnav", "TextInputHost", "OmApSvcBroker" };

    public double SOpacity
    {
        get { return _sOpacity; }
        set
        {
            if (_sOpacity != value)
            {
                _sOpacity = value;
                OnPropertyChanged(() => this.SOpacity);
            }
        }
    }
    public static string IcoPath => icoPath;

    public MainWindow()
    {
        InitializeComponent();

        CreateTryIcon();
        TrayIcon!.Visible = true;

        _hotKey = new HotKey(ModifierKeys.Windows, Key.OemBackslash, this);
        _hotKey.HotKeyPressed += OnHotKeyPressed;

        processTree.KeyUp += ProcessTree_KeyUp;
        processTree.MouseDoubleClick += ProcessTree_MouseDoubleClick;

        Loaded += MainWindow_Loaded;
        Unloaded += MainWindow_Unloaded;
        KeyUp += MainWindow_KeyUp;
        processTree.SelectedItemChanged += ProcessTree_SelectedItemChanged;

        this.Hide();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ReloadProcesses();
    }

    private void ReloadProcesses()
    {
        WindowWinApi.EnumAllWindows();

        processTree.Items.Clear();

        var items = WindowWinApi.WindowInfoList.Where(w => !blackList.Contains(w.ProcessName)).OrderBy(p => p.DisplayName).ToList();

        foreach (var item in items)
        {
            if (new string[] { "Program Manager" }.Contains(item.WindowText)) continue;

            var isSelected = lastSelectedItem != null &&
                            (item.WindowText == lastSelectedItem.WindowText && item.ProcessName == lastSelectedItem.ProcessName);

            processTree.Items.Add(new TreeViewItem() { Header = item.DisplayName, DataContext = item, IsSelected = isSelected });
        }

        processTree.Focus();
    }

    private void ProcessTree_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ActivateSelectedApplication();
        }
    }

    private void ProcessTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ActivateSelectedApplication();
    }

    private void ActivateSelectedApplication()
    {
        if (processTree.SelectedItem is not TreeViewItem selectedItem) return;

        if (selectedItem.DataContext is not WindowInfo windowInfo) return;

        lastSelectedItem = windowInfo;

        int style = WindowWinApi.GetWindowLong(windowInfo.WindowHadler, WindowWinApi.GWL_STYLE);
        if ((style & WindowWinApi.WS_MINIMIZE) == WindowWinApi.WS_MINIMIZE)
        {
            WindowWinApi.ShowWindow(windowInfo.WindowHadler, WindowWinApi.SW_RESTORE);
        }

        WindowWinApi.SetForegroundWindow(windowInfo.WindowHadler);

        this.WindowState = WindowState.Minimized;
    }

    private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
    {
        UnregisterHotKeys();
    }

    private void MainWindow_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            this.Hide();
        }
    }

    protected override void OnStateChanged(EventArgs e)
    {
        if (this.WindowState == WindowState.Minimized)
        {
            this.Hide();
        }
        else if (this.WindowState == WindowState.Normal)
        {
            processTree.Focus();
        }

        base.OnStateChanged(e);
    }

    private void UnregisterHotKeys()
    {
        if (_hotKey == null) return;

        _hotKey.HotKeyPressed -= OnHotKeyPressed;
        _hotKey.Dispose();
    }

    private void OnHotKeyPressed(HotKey hotKey)
    {
        ReloadProcesses();
        ShowApp();
    }

    private void ShowApp()
    {
        this.Show();
        this.WindowState = WindowState.Normal;

        using var currentProcess = Process.GetCurrentProcess();

        this.Activate();
        this.Focus();
        processTree.Focus();
    }

    private void ProcessTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (processTree.SelectedItem is not TreeViewItem selectedItem) return;

        if (selectedItem.DataContext is not WindowInfo windowInfo) return;

        lastSelectedItem = windowInfo;
    }

    protected virtual void OnPropertyChanged<T>(Expression<Func<T>> propertyExpression)
    {
        OnPropertyChanged(((MemberExpression)propertyExpression.Body).Member.Name);
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChangedEventHandler? propertyChanged = this.PropertyChanged;

        if (propertyChanged != null)
        {
            PropertyChangedEventArgs e = new(propertyName);
            propertyChanged(this, e);
        }
    }

    private void MinimizeToTray_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    public void DragWindow(object sender, MouseButtonEventArgs args)
    {
        DragMove();
    }

    private void CreateTryIcon()
    {
        TrayIcon = new NotifyIcon
        {
            BalloonTipIcon = ToolTipIcon.Info,
            BalloonTipText = "text",
            BalloonTipTitle = "title",
            Text = "AppNavG",
            Icon = TrayIconHelper.GetIcon(IcoPath)
        };

        TrayIcon.DoubleClick += (s, e) => { ShowApp(); };

        TrayIconContextMenu.SuspendLayout();

        this.TrayIconContextMenu.Items.AddRange(new ToolStripItem[]
            {
                    this.CloseMenuItem
            });
        this.TrayIconContextMenu.Name = "TrayIconContextMenu";
        this.TrayIconContextMenu.Size = new System.Drawing.Size(153, 70);

        // CloseMenuItem
        this.CloseMenuItem.Name = "CloseMenuItem";
        this.CloseMenuItem.Size = new System.Drawing.Size(152, 22);
        this.CloseMenuItem.Text = "Close";
        this.CloseMenuItem.Click += (s, e) =>
        {
            System.Windows.Application.Current.Shutdown();
        };

        TrayIconContextMenu.ResumeLayout(false);
        TrayIcon.ContextMenuStrip = TrayIconContextMenu;
    }
}
