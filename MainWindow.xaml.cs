using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System;
using System.ComponentModel.Design.Serialization;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static EldenRingPractice.ERLink;

namespace EldenRingPractice;

public partial class MainWindow : Window, IDisposable
{
    ERLink _process;
    SettingsManager settingsManager;
    HotkeyManager hotkeyManager;
    ItemSpawner itemSpawner;
    FlagManager flagManager;
    //EventLog eventLog;
    System.Windows.Threading.DispatcherTimer uiTimer = new System.Windows.Threading.DispatcherTimer();

    // popup windows
    TargetDisplay targetDisplay = new TargetDisplay();
    InfoPanel infoPanel = new InfoPanel();

    //
    //
    //temp
    List<TargetDisplay> targetDisplays = new List<TargetDisplay>();
    //
    //
    //

    bool disposed = false;

    public MainWindow()
    {
        InitializeComponent();

        tabError.Visibility = Visibility.Hidden; // just so I can see it in the editor

        _process = new ERLink();

        if (_process.linkActive == true)
        {

        }
        else
        {
            // Eventually disable other tabs
            tabError.Visibility = Visibility.Visible;
        }

        // setup ui timer
        uiTimer.Tick += uiTimer_Tick;
        uiTimer.Interval = TimeSpan.FromSeconds(0.1);
        uiTimer.Start();

        //settingsManager = new SettingsManager();
        hotkeyManager = new HotkeyManager(this);
        itemSpawner = new ItemSpawner();
        flagManager = new FlagManager();

        //eventLog = new EventLog();
        //eventLog.Show();
        Closing += WindowClosed;
        //Closed += WindowClosed;
        Loaded += MainWindow_Loaded;

        SetupItemsTab();
        SetupFlagsTab();

    }

    void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        updateHotkeyText();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
            { return; }

        if (disposing)
        {
            _process.Dispose();
            //_process = null;
            hotkeyManager.Dispose();
            itemSpawner.Dispose();
            flagManager.Dispose();
            //settingsManager.Dispose();
            uiTimer.Stop();
            //uiTimer = null;
        }
        disposed = true;
    }


    void WindowClosed(object sender, EventArgs e)
    {
        targetDisplay.Close();
        infoPanel.Close();
        //eventLog.Close();
        Dispose();
    }

    public void AddLogEntry(string entry)
    {
        //eventLog.AddEntry(entry);
    }

    void updateHotkeyText()
    {
        foreach (KeyValuePair<(int hotkey, ModifierKeys modifiers), ERLink.GameOptions> hotkeyEntry in hotkeyManager.hotkeyList)
        {
            string hotkeyText = "";

            if (hotkeyEntry.Key.Item2 != ModifierKeys.None)
            {
                if (hotkeyEntry.Key.modifiers.HasFlag(ModifierKeys.Control)) { hotkeyText = "Ctrl+"; }
                if (hotkeyEntry.Key.modifiers.HasFlag(ModifierKeys.Alt)) { hotkeyText += "Alt+"; }
                if (hotkeyEntry.Key.modifiers.HasFlag(ModifierKeys.Shift)) { hotkeyText += "Shift+"; }
            }

            hotkeyText += (char)hotkeyEntry.Key.hotkey;

            switch (hotkeyEntry.Value)
            {
                case GameOptions.NO_DEATH_PLAYER:
                    textNoDeath_Player_Hotkey.Text = $" ({hotkeyText})"; break;
            }
        }
    }

    static Dictionary<int, ERLink.GameOptions> hotkeyList = new Dictionary<int, ERLink.GameOptions>();

    public void ActionHotkey(ERLink.GameOptions option)
    {
        switch (option)
        {
            case ERLink.GameOptions.RUNE_ARC:
                Dispatcher.Invoke(() => { chkRuneArc.IsChecked = !chkRuneArc.IsChecked; });
                break;
            case ERLink.GameOptions.NO_DEATH_PLAYER:
                Dispatcher.Invoke(() => { chkNoDeath_Player.IsChecked = !chkNoDeath_Player.IsChecked; });
                break;
            case ERLink.GameOptions.ONE_SHOT:
                Dispatcher.Invoke(() => { chkOneShot.IsChecked = !chkOneShot.IsChecked; });
                break;
            case ERLink.GameOptions.LOAD_SAVE:
                //_process.ActionOption(ERLink.GameOptions.FAST_QUIT, 1);
                Dispatcher.Invoke(() => LoadSaveFile(null, null));
                break;
            case ERLink.GameOptions.FAST_QUIT:
                _process.ActionOption(option, 1);
                
                break;
            case ERLink.GameOptions.DISABLE_AI_UPDATES:
                Dispatcher.Invoke(() => { chkDisableAI.IsChecked = !chkDisableAI.IsChecked; });
                break;
            case ERLink.GameOptions.NO_GRAVITY:
                //_process.getTargetStats(ERLink.TargetStats.POISE, Convert.ToSingle(textLockPoise.Text));
                //_process.getTargetStats(ERLink.TargetStats.HP, 30000);
                //_process.getTargetStats(ERLink.TargetStats.POISE_TIMER, 6);
                break;
            case ERLink.GameOptions.NUDGE_PLUS_Y:
                _process.nudgePlayer(ERLink.Coordinates.y, true);
                break;
            case ERLink.GameOptions.NUDGE_MINUS_Y:
                _process.nudgePlayer(ERLink.Coordinates.y, false);
                break;


        }
    }

    private void FastQuitout(object sender, RoutedEventArgs e)
    {
        _process.ActionOption(ERLink.GameOptions.FAST_QUIT, 1);
    }

    //
    // PLAYER TAB HANDLERS
    //
    private void player_NoDeath_On(object sender, RoutedEventArgs e)
    {
        //_process.ActionOption(ERLink.GameOptions.NO_DEATH_PLAYER, 1);
        _process.noDeathPlayer(1);
    }

    private void player_NoDeath_Off(object sender, RoutedEventArgs e)
    {
        //_process.ActionOption(ERLink.GameOptions.NO_DEATH_PLAYER, 0);
        _process.noDeathPlayer(0);
    }

    private void noDeathAll_On(object sender, RoutedEventArgs e)
    {
        //_process.ActionOption(ERLink.GameOptions.NO_DEATH_ALL, 1);
        _process.noDeathAll(1);
    }
    private void noDeathAll_Off(object sender, RoutedEventArgs e)
    {
        //_process.ActionOption(ERLink.GameOptions.NO_DEATH_ALL, 0);
        _process.noDeathAll(0);
    }

    private void oneShot_on(object sender, RoutedEventArgs e)
    {
        _process.ActionOption(ERLink.GameOptions.ONE_SHOT, 1);
    }

    private void oneShot_off(object sender, RoutedEventArgs e)
    {
        _process.ActionOption(ERLink.GameOptions.ONE_SHOT, 0);
    }

    private void runeArc_On(object sender, RoutedEventArgs e)
    {
        _process.ActionOption(ERLink.GameOptions.RUNE_ARC, 1);
    }
    private void runeArc_Off(object sender, RoutedEventArgs e)
    {
        _process.ActionOption(ERLink.GameOptions.RUNE_ARC, 0);
    }

    private void NoGravity_On(object sender, RoutedEventArgs e)
    {
        _process.ActionOption(ERLink.GameOptions.NO_GRAVITY, 1);
    }

    private void NoGravity_Off(object sender, RoutedEventArgs e)
    {
        _process.ActionOption(ERLink.GameOptions.NO_GRAVITY, 0);
    }

    private void InfiniteStamina_On(object sender, RoutedEventArgs e)
    {
        _process.InfiniteStamina(1);
    }

    private void InfiniteStamina_Off(object sender, RoutedEventArgs e)
    {
        _process.InfiniteStamina(0);
    }

    private void InfiniteFocus_On(object sender, RoutedEventArgs e)
    {
        _process.InfiniteFocus(1);
    }

    private void InfiniteFocus_Off(object sender, RoutedEventArgs e)
    {
        _process.InfiniteFocus(0);
    }

    private void InfiniteConsumables_On(object sender, RoutedEventArgs e)
    {
        _process.InfiniteConsumables(1);
    }

    private void InfiniteConsumables_Off(object sender, RoutedEventArgs e)
    {
        _process.InfiniteConsumables(0);
    }

    private void ShowAllGraces_On(object sender, RoutedEventArgs e)
    {
        _process.ShowAllGraces(1);
    }

    private void ShowAllGraces_Off(object sender, RoutedEventArgs e)
    {
        _process.ShowAllGraces(0);
    }

    private void ShowStableGround_On(object sender, RoutedEventArgs e)
    {
        _process.ShowStableGround(1);
    }

    private void ShowStableGround_Off(object sender, RoutedEventArgs e)
    {
        _process.ShowStableGround(0);
    }

    public enum TabItems
    {
        PLAYER,
        STATS,
        ENEMY,
        ITEMS,
        WARP,
        MENUS,
        RENDER,
        FLAGS,
        SAVES,
    }

    //
    // Loading tabs
    //
    private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is not TabControl)
        {
            return;
        }

        switch ((sender as TabControl).SelectedIndex)
        {
            case (int)TabItems.STATS:
                SetStatsTab();
                break;
            case (int)TabItems.ENEMY:
                setupEnemyTab(); break;
            case (int)TabItems.SAVES:
                SetupSavesTab();
                break;
            case (int)TabItems.ITEMS:
                //SetupItemsTab();
                break;
            case (int)TabItems.FLAGS:
                //SetupFlagsTab();
                break;
        }
    }

    //
    // textbox handlers and verifiers
    //
    private void TextBox_VerifyNumeric(object sender, TextCompositionEventArgs e)
    {
        foreach (char c in e.Text)
        {
            if (!char.IsDigit(c)) { e.Handled = true; break; }
        }
    }
    private void TextBox_VerifyFloat(object sender, TextCompositionEventArgs e)
    {
        foreach (char c in e.Text)
        {
            if ( !(char.IsDigit(c) || c.Equals('.')) ) { e.Handled = true; break; }
        }
    }

    private void TextBox_VerifyStatRange(object sender, RoutedEventArgs e)
    {
        int stat = 0;

        try
        {
            stat = int.Parse((sender as TextBox).Text);
        }
        catch
        {
            (sender as TextBox).Text = "1";
        }

        if (stat < 1)
        {
            (sender as TextBox).Text = "1";
        }
        else if ((stat > 99) && (sender != textPlayerLevel))
        {
            (sender as TextBox).Text = "99";
        }
        else if ((stat > 713) && (sender == textPlayerLevel))
        {
            (sender as TextBox).Text = "713";
        }
    }

    private void TextBox_VerifyDLCStatRange(object sender, RoutedEventArgs e)
    {
        int stat = 0;

        try
        {
            stat = int.Parse((sender as TextBox).Text);
        }
        catch
        {
            (sender as TextBox).Text = "0";
        }

        if (stat < 0)
        {
            (sender as TextBox).Text = "0";
        }
        else if (stat > 20)
        {
            (sender as TextBox).Text = "20";
        }
    }

    void SetStatsTab()
    {
        // Player Stats
        int[] stats = _process.getPlayerStats();

        if ((stats[0] > 0) && stats[0] < 714) { textPlayerLevel.Text = stats[0].ToString(); }
        if ((stats[1] > 0) && stats[1] < 100) { textVigor.Text = stats[1].ToString(); }
        textMind.Text = stats[2].ToString();
        textEndurance.Text = stats[3].ToString();
        textStrength.Text = stats[4].ToString();
        textDexterity.Text = stats[5].ToString();
        textIntelligence.Text = stats[6].ToString();
        textFaith.Text = stats[7].ToString();
        textArcane.Text = stats[8].ToString();

        if ((stats[9] > -1) && stats[9] < 21) { textScadu.Text = stats[9].ToString(); }
        if ((stats[10] > -1) && stats[10] < 21) { textAsh.Text = stats[10].ToString(); }

        // Runes
        textRunes.Text = _process.getRunes().ToString();

        // IGT
        //textIGTSeconds.Text = _process.getIGT().ToString();

        var igt = _process.getIGT();
        var IGTms = (igt % 1000) / 10;
        var IGTseconds = (igt / 1000) % 60;
        var IGTminutes = (igt / 1000) / 60 % 60;
        var IGThours = (igt / 1000) / 3600;

        textIGTSeconds.Text = $"{IGThours}:{IGTminutes}:{IGTseconds}.{IGTms}";

    }

    void setupEnemyTab()
    {
        _process.populateEntityList(listboxEntityList);
    }

    void SetupSavesTab()
    {

    }
    
    void SetupItemsTab()
    {
        itemSpawner.loadItemList();
        itemSpawner.loadAshOfWar();
        itemSpawner.populateItemList(listboxItems);
        itemSpawner.populateAffinities(comboAffinities);
        comboAffinities.SelectedIndex = 0;
        itemSpawner.populateAshOfWar(comboAshes);
        comboAshes.SelectedIndex = 0;
        itemSpawner.populateWeaponLevel(comboWeaponLevel, 25);
        comboWeaponLevel.SelectedIndex = 0;
    }

    void SetupFlagsTab()
    {
        flagManager.loadFlagList();
        flagManager.populateFlagList(listboxFlags);
    }

    void flagCheckToggle(object sender, RoutedEventArgs e)
    {
        //MessageBox.Show((sender as CheckBox));
    }


    //
    // ENEMY TAB HANDLERS
    //

    private void disableAIUpdates_On(object sender, RoutedEventArgs e)
    {
        _process.ActionOption(ERLink.GameOptions.DISABLE_AI_UPDATES, 1);
    }

    private void disableAIUpdates_Off(object sender, RoutedEventArgs e)
    {
        _process.ActionOption(ERLink.GameOptions.DISABLE_AI_UPDATES, 0);
    }

    private void repeatLastAction_On(object sender, RoutedEventArgs e)
    {
        _process.ActionOption(ERLink.GameOptions.REPEAT_LAST_ACTION, 1);
    }

    private void repeatLastAction_Off(object sender, RoutedEventArgs e)
    {
        _process.ActionOption(ERLink.GameOptions.REPEAT_LAST_ACTION, 0);
    }

    private void showTargetDisplay(object sender, RoutedEventArgs e)
    {
        _process.installTargetHook();
        targetDisplay.Show();
        targetDisplay.Topmost = (bool)chkTargetDisplayOnTop.IsChecked;
    }
    private void hideTargetDisplay(object sender, RoutedEventArgs e)
    {
        targetDisplay.Hide();
    }

    //
    // ERROR TAB HANDLERS
    //
    private void RetryLink(object sender, RoutedEventArgs e)
    {
        _process.InitGameLink();
    }

    private void TargetDisplay_TopOn(object sender, RoutedEventArgs e)
    {
        targetDisplay.Topmost = true;
    }

    private void TargetDisplay_TopOff(object sender, RoutedEventArgs e)
    {
        targetDisplay.Topmost = false;
    }

    //
    // UI Timer
    //

    private void uiTimer_Tick(object sender, EventArgs e)
    {
        if (targetDisplay.Visibility == Visibility.Visible)
        {
            UpdateTargetDisplay();
        }

        if (infoPanel.Visibility == Visibility.Visible)
        {
            UpdateInfoPanel();
        }

        if (targetDisplays.Count > 0)
        {
            updateEntityViews();
        }
    }

    void UpdateTargetDisplay()
    {
        //if hp is not valid, then data is nonsense
        double statCurrent = _process.getTargetStats(ERLink.TargetStats.HP);

        if (statCurrent > -1)
        {
            double statMax = _process.getTargetStats(ERLink.TargetStats.HP_MAX);
            targetDisplay.textHP.Text = $"{(int)statCurrent}/{(int)statMax}";
            targetDisplay.barHP.Value = statCurrent;
            targetDisplay.barHP.Maximum = statMax;

            statCurrent = _process.getTargetStats(ERLink.TargetStats.POISE);
            statMax = _process.getTargetStats(ERLink.TargetStats.POISE_MAX);
            double poiseTimer = _process.getTargetStats(ERLink.TargetStats.POISE_TIMER);
            targetDisplay.textPoise.Text = $"({poiseTimer:F1})s {statCurrent:F1}/{(int)statMax}";
            targetDisplay.barPoise.Value = statCurrent; // 'NaN' is not a valid value for property 'Value'. | Figure out why this happened.
            targetDisplay.barPoise.Maximum = statMax;

            statCurrent = _process.getTargetStats(ERLink.TargetStats.BLEED);
            statMax = _process.getTargetStats(ERLink.TargetStats.BLEED_MAX);
            targetDisplay.textBleed.Text = $"{(int)statCurrent}/{(int)statMax}";
            targetDisplay.barBleed.Value = statCurrent;
            targetDisplay.barBleed.Maximum = statMax;

            statCurrent = _process.getTargetStats(ERLink.TargetStats.FROST);
            statMax = _process.getTargetStats(ERLink.TargetStats.FROST_MAX);
            targetDisplay.textFrost.Text = $"{(int)statCurrent}/{(int)statMax}";
            targetDisplay.barFrost.Value = statCurrent;
            targetDisplay.barFrost.Maximum = statMax;

            statCurrent = _process.getTargetStats(ERLink.TargetStats.ROT);
            statMax = _process.getTargetStats(ERLink.TargetStats.ROT_MAX);
            targetDisplay.textRot.Text = $"{(int)statCurrent}/{(int)statMax}";
            targetDisplay.barRot.Value = statCurrent;
            targetDisplay.barRot.Maximum = statMax;

            statCurrent = _process.getTargetStats(ERLink.TargetStats.POISON);
            statMax = _process.getTargetStats(ERLink.TargetStats.POISON_MAX);
            targetDisplay.textPoison.Text = $"{(int)statCurrent}/{(int)statMax}";
            targetDisplay.barPoison.Value = statCurrent;
            targetDisplay.barPoison.Maximum = statMax;

            statCurrent = _process.getTargetStats(ERLink.TargetStats.SLEEP);
            statMax = _process.getTargetStats(ERLink.TargetStats.SLEEP_MAX);
            targetDisplay.textSleep.Text = $"{(int)statCurrent}/{(int)statMax}";
            targetDisplay.barSleep.Value = statCurrent;
            targetDisplay.barSleep.Maximum = statMax;

            statCurrent = _process.getTargetStats(ERLink.TargetStats.ANIMATION);
            targetDisplay.textCurrentAnimation.Text = statCurrent.ToString();
        }
        else
        {
            if (targetDisplay.textHP.Text != "N/A")
            {
                targetDisplay.textHP.Text = "N/A";
                targetDisplay.barHP.Maximum = 1;
                targetDisplay.barHP.Value = 1;

                targetDisplay.textPoise.Text = "N/A";
                targetDisplay.barPoise.Value = 1;
                targetDisplay.barPoise.Maximum = 1;

                targetDisplay.textBleed.Text = "N/A";
                targetDisplay.barBleed.Value = 1;
                targetDisplay.barBleed.Maximum = 1;

                targetDisplay.textFrost.Text = "N/A";
                targetDisplay.barFrost.Value = 1;
                targetDisplay.barFrost.Maximum = 1;

                targetDisplay.textRot.Text = "N/A";
                targetDisplay.barRot.Value = 1;
                targetDisplay.barRot.Maximum = 1;

                targetDisplay.textPoison.Text = "N/A";
                targetDisplay.barPoison.Value = 1;
                targetDisplay.barPoison.Maximum = 1;

                targetDisplay.textSleep.Text = "N/A";
                targetDisplay.barSleep.Value = 1;
                targetDisplay.barSleep.Maximum = 1;
            }
        }
    }

    void updateEntityViews()
    {
        foreach (TargetDisplay target in targetDisplays)
        {
            //if hp is not valid, then data is nonsense
            IntPtr pointer = target.getPointer();
            double statCurrent = _process.getEntityStats(pointer, ERLink.TargetStats.HP);

            if (statCurrent > -1)
            {
                double statMax = _process.getEntityStats(pointer, ERLink.TargetStats.HP_MAX);
                target.textHP.Text = $"{(int)statCurrent}/{(int)statMax}";
                target.barHP.Value = statCurrent;
                target.barHP.Maximum = statMax;

                statCurrent = _process.getEntityStats(pointer, ERLink.TargetStats.POISE);
                statMax = _process.getEntityStats(pointer, ERLink.TargetStats.POISE_MAX);
                double poiseTimer = _process.getEntityStats(pointer, ERLink.TargetStats.POISE_TIMER);
                target.textPoise.Text = $"({poiseTimer:F1})s {statCurrent:F1}/{(int)statMax}";
                target.barPoise.Value = statCurrent; // 'NaN' is not a valid value for property 'Value'. | Figure out why this happened.
                target.barPoise.Maximum = statMax;

                statCurrent = _process.getEntityStats(pointer, ERLink.TargetStats.BLEED);
                statMax = _process.getEntityStats(pointer, ERLink.TargetStats.BLEED_MAX);
                target.textBleed.Text = $"{(int)statCurrent}/{(int)statMax}";
                target.barBleed.Value = statCurrent;
                target.barBleed.Maximum = statMax;

                statCurrent = _process.getEntityStats(pointer, ERLink.TargetStats.FROST);
                statMax = _process.getEntityStats(pointer, ERLink.TargetStats.FROST_MAX);
                target.textFrost.Text = $"{(int)statCurrent}/{(int)statMax}";
                target.barFrost.Value = statCurrent;
                target.barFrost.Maximum = statMax;

                statCurrent = _process.getEntityStats(pointer, ERLink.TargetStats.ROT);
                statMax = _process.getEntityStats(pointer, ERLink.TargetStats.ROT_MAX);
                target.textRot.Text = $"{(int)statCurrent}/{(int)statMax}";
                target.barRot.Value = statCurrent;
                target.barRot.Maximum = statMax;
                
                statCurrent = _process.getEntityStats(pointer, ERLink.TargetStats.POISON);
                statMax = _process.getEntityStats(pointer, ERLink.TargetStats.POISON_MAX);
                target.textPoison.Text = $"{(int)statCurrent}/{(int)statMax}";
                target.barPoison.Value = statCurrent;
                target.barPoison.Maximum = statMax;

                statCurrent = _process.getEntityStats(pointer, ERLink.TargetStats.SLEEP);
                statMax = _process.getEntityStats(pointer, ERLink.TargetStats.SLEEP_MAX);
                target.textSleep.Text = $"{(int)statCurrent}/{(int)statMax}";
                target.barSleep.Value = statCurrent;
                target.barSleep.Maximum = statMax;

                statCurrent = _process.getEntityStats(pointer, ERLink.TargetStats.BLIGHT);
                statMax = _process.getEntityStats(pointer, ERLink.TargetStats.BLIGHT_MAX);
                target.textBlight.Text = $"{(int)statCurrent}/{(int)statMax}";
                target.barBlight.Value = statCurrent;
                target.barBlight.Maximum = statMax;

                statCurrent = _process.getEntityStats(pointer, ERLink.TargetStats.MADNESS);
                statMax = _process.getEntityStats(pointer, ERLink.TargetStats.MADNESS_MAX);
                target.textMadness.Text = $"{(int)statCurrent}/{(int)statMax}";
                target.barMadness.Value = statCurrent;
                target.barMadness.Maximum = statMax;

                statCurrent = _process.getEntityStats(pointer, ERLink.TargetStats.ANIMATION);
                var angle = _process.getEntityStats(pointer, ERLink.TargetStats.ANGLE);
                var x = _process.getEntityStats(pointer, ERLink.TargetStats.X_POS);
                var y = _process.getEntityStats(pointer, ERLink.TargetStats.Y_POS);
                var z = _process.getEntityStats(pointer, ERLink.TargetStats.Z_POS);
                //targetDisplay.textCurrentAnimation.Text = statCurrent.ToString();
                target.textCurrentAnimation.Text = $"{statCurrent}\nX: {x:F3} Y: {y:F3} Z: {z:F3} Angle: {angle:F3}";
            }
            else
            {
                if (target.textHP.Text != "N/A")
                {
                    target.textHP.Text = "N/A";
                    target.barHP.Maximum = 1;
                    target.barHP.Value = 1;

                    target.textPoise.Text = "N/A";
                    target.barPoise.Value = 1;
                    target.barPoise.Maximum = 1;

                    target.textBleed.Text = "N/A";
                    target.barBleed.Value = 1;
                    target.barBleed.Maximum = 1;

                    target.textFrost.Text = "N/A";
                    target.barFrost.Value = 1;
                    target.barFrost.Maximum = 1;

                    target.textRot.Text = "N/A";
                    target.barRot.Value = 1;
                    target.barRot.Maximum = 1;

                    target.textPoison.Text = "N/A";
                    target.barPoison.Value = 1;
                    target.barPoison.Maximum = 1;

                    target.textSleep.Text = "N/A";
                    target.barSleep.Value = 1;
                    target.barSleep.Maximum = 1;

                    target.textBlight.Text = "N/A";
                    target.barBlight.Value = 1;
                    target.barBlight.Maximum = 1;

                    target.textMadness.Text = "N/A";
                    target.barMadness.Value = 1;
                    target.barMadness.Maximum = 1;
                }
            }
        }
    }

    void UpdateInfoPanel()
    {
        string info = "";
        if ((bool)chkInfo_IGT.IsChecked)
        {
            (ulong hours, ulong minutes, ulong seconds, ulong ms) igt = _process.getIGTFormatted();
            info += $"{igt.hours.ToString()}:{igt.minutes.ToString()}:{igt.seconds.ToString()}.{igt.ms.ToString()}\n";
        }

        infoPanel.textInfo.Text = info;
    }

    //
    // Hotkey handlers
    //
    public enum hotkeyOptions
    {
        FAST_QUIT,
        NO_DEATH_ALL,
        RUNE_ARC,
        DISABLE_AI_UPDATES,
    }

    object loopLock = new object();

    private void buttonSetStats_Click(object sender, RoutedEventArgs e)
    {
        int[] stats = new int[11];

        try
        {
            stats[0] = int.Parse(textPlayerLevel.Text);
            stats[1] = int.Parse(textVigor.Text);
            stats[2] = int.Parse(textMind.Text);
            stats[3] = int.Parse(textEndurance.Text);
            stats[4] = int.Parse(textStrength.Text);
            stats[5] = int.Parse(textDexterity.Text);
            stats[6] = int.Parse(textIntelligence.Text);
            stats[7] = int.Parse(textFaith.Text);
            stats[8] = int.Parse(textArcane.Text);
            stats[9] = int.Parse(textScadu.Text);
            stats[10] = int.Parse(textAsh.Text);
        }
        catch
        {
            return;
        }

        _process.getPlayerStats(stats);
    }

    private void LockPoise_On(object sender, RoutedEventArgs e)
    {
        _process.AddLock(ERLink.StatLocks.TARGET_POISE);
    }

    private void LockPoise_Off(object sender, RoutedEventArgs e)
    {
        _process.RemoveLock(ERLink.StatLocks.TARGET_POISE);
    }

    private void DisablePoiseResetTimer_On(object sender, RoutedEventArgs e)
    {
        _process.AddLock(ERLink.StatLocks.TARGET_POISE_TIMER);
    }

    private void DisablePoiseResetTimer_Off(object sender, RoutedEventArgs e)
    {
        _process.RemoveLock(ERLink.StatLocks.TARGET_POISE_TIMER);
    }

    private void OpenMenu(object sender, RoutedEventArgs e)
    {
        switch ((sender as Button).Name)
        {
            case "buttonMenu_Grace":
                _process.openMenu((int)ERLink.GameMenus.LEVEL_UP); break;
            case "buttonMenu_Ash":
                _process.openMenu((int)ERLink.GameMenus.ASH_OF_WAR); break;
            case "buttonMenu_Physick":
                _process.openMenu((int)ERLink.GameMenus.PHYSICK); break;
            case "buttonMenu_Spells":
                _process.openMenu((int)ERLink.GameMenus.SPELLS); break;
            case "buttonMenu_Rune":
                _process.openMenu((int)ERLink.GameMenus.GREAT_RUNE); break;
            case "buttonMenu_Chest":
                _process.openMenu((int)ERLink.GameMenus.CHEST); break;
            case "buttonMenu_Rebirth":
                _process.openMenu((int)ERLink.GameMenus.REBIRTH); break;
        }
    }

    //
    // Render tab
    //
    private void WeaponHitboxes_On(object sender, RoutedEventArgs e)
    {
        _process.ToggleWeaponHitboxes(1);
    }

    private void WeaponHitboxes_Off(object sender, RoutedEventArgs e)
    {
        _process.ToggleWeaponHitboxes(0);
    }

    private void ModelHitboxes_On(object sender, RoutedEventArgs e)
    {
        _process.ToggleModelHitboxes(1);
    }

    private void ModelHitboxes_Off(object sender, RoutedEventArgs e)
    {
        _process.ToggleModelHitboxes(0);
    }

    private void TargetView_On(object sender, RoutedEventArgs e)
    {
        _process.ToggleTargetView(1);
    }

    private void TargetView_Off(object sender, RoutedEventArgs e)
    {
        _process.ToggleTargetView(0);
    }

    private void SoundView_On(object sender, RoutedEventArgs e)
    {
        _process.ToggleSoundView(true);
    }

    private void SoundView_Off(object sender, RoutedEventArgs e)
    {
        _process.ToggleSoundView(false);
    }

    //
    // Save tab options
    //
    private void SetERSaveFile(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\EldenRing";
        openFileDialog.Filter = "Elden Ring save|ER0000.sl2";

        if (openFileDialog.ShowDialog() == true)
        {
            textERSaveFile.Text = openFileDialog.FileName;
        }

    }

    private void SetBackupSaveDirectory(object sender, RoutedEventArgs e)
    {
        OpenFolderDialog openFolderDialog = new OpenFolderDialog();

        if (textSaveFolder.Text != "")
        {
            openFolderDialog.DefaultDirectory = textSaveFolder.Text;
        }
        else if (textERSaveFile.Text != "")
        {
            openFolderDialog.DefaultDirectory = textERSaveFile.Text;
        }
        else
        {
            openFolderDialog.DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\EldenRing";
        }

        if ( openFolderDialog.ShowDialog() == true )
        {
            textSaveFolder.Text = openFolderDialog.FolderName;
        }

    }

    private void SelectSaveFile(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        if (textSaveFolder.Text != "")
        {
            openFileDialog.DefaultDirectory = textSaveFolder.Text;
        }
        else if (textERSaveFile.Text != "")
        {
            openFileDialog.DefaultDirectory = textERSaveFile.Text;
        }
        else
        {
            openFileDialog.DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\EldenRing";
        }
        openFileDialog.Filter = "Elden Ring save (*.sl2) | *.sl2";

        if (openFileDialog.ShowDialog() == true)
        {
            textSaveFile.Text = openFileDialog.FileName;
        }
    }

    private void LoadSaveFile(object sender, RoutedEventArgs e)
    {
        if (File.Exists(textSaveFile.Text) && textERSaveFile.Text != "")
        {
            try
            {
                File.Copy(textSaveFile.Text, textERSaveFile.Text, true);
            }
            catch
            {
                MessageBox.Show("File copy failed");
            }
        }
    }

    private void CreateNewSave(object sender, RoutedEventArgs e)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\EldenRing";
        saveFileDialog.Filter = "Elden Ring save (*.sl2) | *.sl2";

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                File.Copy(textERSaveFile.Text, saveFileDialog.FileName, true);
            }
            catch
            {
                MessageBox.Show("File copy failed");
            }
        }
    }

    private void PopulateTreeView()
    {
        var directories = Directory.GetDirectories(textSaveFolder.Text);

        TreeViewItem rootNode = new TreeViewItem();
        rootNode.Header = "Root";
        treeSaveFolders.Items.Add(rootNode);

        foreach (var directory in directories)
        {
            TreeViewItem subFolder = new TreeViewItem();
            subFolder.Header = directory;
            rootNode.Items.Add(subFolder);
        }
    }

    private void LockFPS_On(object sender, RoutedEventArgs e)
    {
        _process.setFrameTime((float)1 / float.Parse(textFPS.Text));
    }

    private void LockFPS_Off(object sender, RoutedEventArgs e)
    {
        _process.setFrameTime(1 / 60);
    }

    private void GameSpeed_On(object sender, RoutedEventArgs e)
    {
        _process.setGameSpeed(float.Parse(textGameSpeed.Text));
    }

    private void GameSpeed_Off(object sender, RoutedEventArgs e)
    {
        _process.setGameSpeed(1);
    }

    private void showInfoPanel(object sender, RoutedEventArgs e)
    {
        infoPanel.Show();
        infoPanel.Topmost = (bool)chkInfoPanelOnTop.IsChecked;
    }

    private void hideInfoPanel(object sender, RoutedEventArgs e)
    {

    }

    private void NudgePlayer(object sender, RoutedEventArgs e)
    {
        switch ((sender as Button).Name)
        {
            case "buttonPlusY":
                _process.nudgePlayer(ERLink.Coordinates.y, true);
                break;
            case "buttonMinusY":
                _process.nudgePlayer(ERLink.Coordinates.y, false);
                break;
        }
    }

    private void listboxItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }

    private void itemSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        int index = itemSpawner.getListboxPositionByString(textItemSearch.Text);
        textTemp.Text = index.ToString();
        listboxItems.SelectedIndex = index;
        listboxItems.ScrollIntoView(listboxItems.Items[index]);
    }

    private void spawnItem(object sender, RoutedEventArgs e)
    {
        if (listboxItems.SelectedItem is KeyValuePair<string, (uint, ItemSpawner.ItemCategory)> selectedItem)
        {
            KeyValuePair<string, uint> selectedAsh = (KeyValuePair<string, uint>)comboAshes.SelectedItem;
            (uint itemID, uint ash, uint quantity) itemData = itemSpawner.getValidatedItemValues(
                                                                            selectedItem,
                                                                            (uint)comboWeaponLevel.SelectedIndex,
                                                                            (uint)comboAffinities.SelectedIndex,
                                                                            selectedAsh.Value,
                                                                            (uint)Convert.ToInt32(textSpawnQuantity.Text));
            _process.spawnItem(itemData.itemID, itemData.quantity, itemData.ash);
        }
    }

    private void openNewTargetWindow(object sender, RoutedEventArgs e)
    {
        //MessageBox.Show(((IntPtr)listboxEntityList.SelectedItem).ToString("X16"));
        targetDisplays.Add(new TargetDisplay((IntPtr)listboxEntityList.SelectedItem, true));
    }
}
