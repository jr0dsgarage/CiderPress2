﻿/*
 * Copyright 2023 faddenSoft
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using AppCommon;
using CommonUtil;
using cp2_wpf.WPFCommon;
using DiskArc;
using DiskArc.Multi;

namespace cp2_wpf {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged {
        private MainController mMainCtrl;

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Call this when a notification-worthy property changes value.
        /// The CallerMemberName attribute puts the calling property's name in the first arg.
        /// </summary>
        /// <param name="propertyName">Name of property that changed.</param>
        private void OnPropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Version string, for display.
        /// </summary>
        public string ProgramVersionString {
            get { return GlobalAppVersion.AppVersion.ToString(); }
        }

        /// <summary>
        /// Which panel are we showing, launchPanel or mainPanel?
        /// </summary>
        public bool ShowLaunchPanel {
            get { return mShowLaunchPanel; }
            set {
                mShowLaunchPanel = value;
                OnPropertyChanged("LaunchPanelVisibility");
                OnPropertyChanged("MainPanelVisibility");
            }
        }
        private bool mShowLaunchPanel = true;

        /// <summary>
        /// Returns the visibility status of the launch panel.
        /// (Intended for use from XAML.)
        /// </summary>
        public Visibility LaunchPanelVisibility {
            get { return mShowLaunchPanel ? Visibility.Visible : Visibility.Collapsed; }
        }

        /// <summary>
        /// Returns the visibility status of the main panel.
        /// (Intended for use from XAML.)
        /// </summary>
        public Visibility MainPanelVisibility {
            get { return mShowLaunchPanel ? Visibility.Collapsed : Visibility.Visible; }
        }

        public double LeftPanelWidth {
            get { return mainTriptychPanel.ColumnDefinitions[0].ActualWidth; }
            set { mainTriptychPanel.ColumnDefinitions[0].Width = new GridLength(value); }
        }
        //public double RightPanelWidth {
        //    get { return mainTriptychPanel.ColumnDefinitions[4].ActualWidth; }
        //    set { mainTriptychPanel.ColumnDefinitions[4].Width = new GridLength(value); }
        //}

        public bool ShowRecentProject1 {
            get { return !string.IsNullOrEmpty(mRecentProjectName1); }
        }
        public string RecentProjectName1 {
            get { return mRecentProjectName1; }
            set { mRecentProjectName1 = value; OnPropertyChanged();
                OnPropertyChanged("ShowRecentProject1"); }
        }
        public string RecentProjectPath1 {
            get { return mRecentProjectPath1; }
            set { mRecentProjectPath1 = value; OnPropertyChanged(); }
        }
        private string mRecentProjectName1 = string.Empty;
        private string mRecentProjectPath1 = string.Empty;

        public bool ShowRecentProject2 {
            get { return !string.IsNullOrEmpty(mRecentProjectName2); }
        }
        public string RecentProjectName2 {
            get { return mRecentProjectName2; }
            set {
                mRecentProjectName2 = value;
                OnPropertyChanged();
                OnPropertyChanged("ShowRecentProject2");
            }
        }
        public string RecentProjectPath2 {
            get { return mRecentProjectPath2; }
            set { mRecentProjectPath2 = value; OnPropertyChanged(); }
        }
        private string mRecentProjectName2 = string.Empty;
        private string mRecentProjectPath2 = string.Empty;


        /// <summary>
        /// Set to true if the DEBUG menu should be visible on the main menu strip.
        /// </summary>
        public bool ShowDebugMenu {
            get { return mShowDebugMenu; }
            set { mShowDebugMenu = value; OnPropertyChanged(); }
        }
        private bool mShowDebugMenu = true;


        /// <summary>
        /// Window constructor.
        /// </summary>
        public MainWindow() {
            Debug.WriteLine("START at " + DateTime.Now.ToLocalTime());

            InitializeComponent();
            DataContext = this;

            mMainCtrl = new MainController(this);

            //ICON_STATUS_OK = (ControlTemplate)FindResource("icon_StatusOK");
            //ICON_STATUS_DUBIOUS = (ControlTemplate)FindResource("icon_StatusInvalid");
            //ICON_STATUS_WARNING = (ControlTemplate)FindResource("icon_StatusWarning");
            //ICON_STATUS_DAMAGE = (ControlTemplate)FindResource("icon_StatusDamage");

            // Get an event when the splitters move.  Because of the way things are set up, it's
            // actually best to get an event when the grid row/column sizes change.
            // https://stackoverflow.com/a/22495586/294248
            DependencyPropertyDescriptor widthDesc = DependencyPropertyDescriptor.FromProperty(
                ColumnDefinition.WidthProperty, typeof(ItemsControl));
            DependencyPropertyDescriptor heightDesc = DependencyPropertyDescriptor.FromProperty(
                RowDefinition.HeightProperty, typeof(ItemsControl));
            // main window, left/right panels
            widthDesc.AddValueChanged(mainTriptychPanel.ColumnDefinitions[0], GridSizeChanged);
            widthDesc.AddValueChanged(mainTriptychPanel.ColumnDefinitions[4], GridSizeChanged);
            // vertical resize within side panels
            heightDesc.AddValueChanged(leftPanel.RowDefinitions[0], GridSizeChanged);

            // Add events that fire when column headers change size.  Set this up for
            // the FileList DataGrids.
            PropertyDescriptor pd = DependencyPropertyDescriptor.FromProperty(
                DataGridColumn.ActualWidthProperty, typeof(DataGridColumn));
            AddColumnWidthChangeCallback(pd, fileListDataGrid);
        }

        private void AddColumnWidthChangeCallback(PropertyDescriptor pd, DataGrid dg) {
            foreach (DataGridColumn col in dg.Columns) {
                pd.AddValueChanged(col, ColumnWidthChanged);
            }
        }

        /// <summary>
        /// Handles source-initialized event.  This happens before Loaded, before the window
        /// is visible, which makes it a good time to set the size and position.
        /// </summary>
        private void Window_SourceInitialized(object sender, EventArgs e) {
            mMainCtrl.WindowSourceInitialized();
        }

        /// <summary>
        /// Handles window-loaded event.  Window is ready to go, so we can start doing things
        /// that involve user interaction.
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mMainCtrl.WindowLoaded();
        }

        /// <summary>
        /// Handles window-close event.  The user has an opportunity to cancel.
        /// </summary>
        private void Window_Closing(object sender, CancelEventArgs e) {
            Debug.WriteLine("Main app window close requested");
            if (mMainCtrl == null) {
                // early failure?
                return;
            }
#if DEBUG
            // Give the library a chance to test assertions in finalizers.
            Debug.WriteLine("doing GC stuff");
            GC.Collect();
            GC.WaitForPendingFinalizers();
#endif
            if (!mMainCtrl.WindowClosing()) {
                e.Cancel = true;
            }
        }

        //
        // We record the location and size of the window, the sizes of the panels, and the
        // widths of the various columns.  These events may fire rapidly while the user is
        // resizing them, so we just want to set a flag noting that a change has been made.
        //
        private void Window_LocationChanged(object? sender, EventArgs e) {
            //Debug.WriteLine("Main window location changed");
        }
        private void Window_SizeChanged(object? sender, SizeChangedEventArgs e) {
            //Debug.WriteLine("Main window size changed");
        }
        private void GridSizeChanged(object? sender, EventArgs e) {
            //Debug.WriteLine("Grid size change: " + sender);
        }
        private void ColumnWidthChanged(object? sender, EventArgs e) {
            DataGridTextColumn? col = sender as DataGridTextColumn;
            if (col != null) {
                Debug.WriteLine("Column " + col.Header + " width now " + col.ActualWidth);
            }
        }

        #region Can-execute handlers

        private void IsFileOpen(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = (mMainCtrl != null && mMainCtrl.IsFileOpen);
        }

        private void IsFileAreaSelected(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = (mMainCtrl != null && mMainCtrl.IsFileOpen && ShowCenterFileList);
        }

        #endregion Can-execute handlers

        #region Command handlers

        private void AboutCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            mMainCtrl.About();
        }
        private void AddFilesCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            mMainCtrl.AddFiles();
        }
        private void CloseCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            if (!mMainCtrl.CloseWorkFile()) {
                Debug.WriteLine("Close canceled");
            }
        }
        private void CopyCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            Debug.WriteLine("Copy!");
        }
        private void CutCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            Debug.WriteLine("Cut!");
        }
        private void ExitCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            // Close the main window.  This operation can be cancelled by the user.
            Close();
        }
        private void ExtractFilesCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            mMainCtrl.ExtractFiles();
        }
        private void HelpCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            Debug.WriteLine("Help!");
        }
        private void NewDiskImageCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            mMainCtrl.NewDiskImage();
        }
        private void NewFileArchiveCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            mMainCtrl.NewFileArchive();
        }
        private void OpenCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            mMainCtrl.OpenWorkFile();
        }
        private void PasteCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            Debug.WriteLine("Paste!");
        }
        private void RecentProjectCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            int recentIndex;
            if (e.Parameter is int) {
                recentIndex = (int)e.Parameter;
            } else if (e.Parameter is string) {
                recentIndex = int.Parse((string)e.Parameter);
            } else {
                throw new Exception("Bad parameter: " + e.Parameter);
            }
            if (recentIndex < 0 || recentIndex >= MainController.MAX_RECENT_PROJECTS) {
                throw new Exception("Bad parameter: " + e.Parameter);
            }

            Debug.WriteLine("Recent project #" + recentIndex);
            mMainCtrl.OpenRecentProject(recentIndex);
        }
        private void ResetSortCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            fileListDataGrid.ResetSort();
        }
        private void ShowDirListCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            // TODO: reconfigure file list
            SetShowCenterInfo(false);
        }
        private void ShowFullListCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            // TODO: reconfigure file list
            SetShowCenterInfo(false);
        }
        private void ShowInfoCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            SetShowCenterInfo(true);
        }
        private void ViewFilesCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            mMainCtrl.ViewFiles();
        }

        private void Debug_BulkCompressTestCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            mMainCtrl.Debug_BulkCompressTest();
        }
        private void Debug_DiskArcLibTestCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            mMainCtrl.Debug_DiskArcLibTests();
        }
        private void Debug_FileConvLibTestCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            mMainCtrl.Debug_FileConvLibTests();
        }
        private void Debug_ShowDebugLogCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            mMainCtrl.Debug_ShowDebugLog();
        }

        #endregion Command handlers

        #region Misc

        private void DebugMenu_SubmenuOpened(object sender, RoutedEventArgs e) {
            debugShowDebugLogMenuItem.IsChecked = mMainCtrl.IsDebugLogOpen;
        }

        #endregion Misc

        #region Archive Tree

        //internal readonly ControlTemplate ICON_STATUS_OK;
        //internal readonly ControlTemplate ICON_STATUS_DUBIOUS;
        //internal readonly ControlTemplate ICON_STATUS_WARNING;
        //internal readonly ControlTemplate ICON_STATUS_DAMAGE;

        /// <summary>
        /// Data for archive tree TreeView control.
        /// </summary>
        public ObservableCollection<ArchiveTreeItem> ArchiveTreeRoot { get; private set; } =
            new ObservableCollection<ArchiveTreeItem>();

        /// <summary>
        /// Handles selection change in archive tree view.
        /// </summary>
        private void ArchiveTree_SelectedItemChanged(object sender,
                RoutedPropertyChangedEventArgs<object> e) {
            ArchiveTreeItem? item = e.NewValue as ArchiveTreeItem;
            mMainCtrl.ArchiveTree_SelectionChanged(item);
        }

        #endregion Archive Tree

        #region Directory Tree

        /// <summary>
        /// Data for directory tree TreeView control.
        /// </summary>
        public ObservableCollection<DirectoryTreeItem> DirectoryTreeRoot { get; private set; } =
            new ObservableCollection<DirectoryTreeItem>();

        /// <summary>
        /// Handles selection change in directory tree view.
        /// </summary>
        private void DirectoryTree_SelectedItemChanged(object sender,
                RoutedPropertyChangedEventArgs<object> e) {
            DirectoryTreeItem? item = e.NewValue as DirectoryTreeItem;
            mMainCtrl.DirectoryTree_SelectionChanged(item);
        }

        #endregion Directory Tree

        #region Center Panel

        public enum CenterPanelContents { Unknown = 0, FileArchive, DiskImage, InfoOnly }
        public void SetCenterPanelContents(CenterPanelContents cont) {
            mCenterPanelContents = cont;
            SetShowCenterInfo(cont == CenterPanelContents.InfoOnly);
        }
        private CenterPanelContents mCenterPanelContents = CenterPanelContents.Unknown;

        public bool ShowCenterFileList { get { return !mShowCenterInfo; } }
        public bool ShowCenterInfoPanel { get { return mShowCenterInfo; } }
        private void SetShowCenterInfo(bool value) {
            if (!value && mCenterPanelContents == CenterPanelContents.InfoOnly) {
                // No file list to switch to; ignore request.
                Debug.WriteLine("Ignoring attempt to switch to file list");
                return;
            }
            mShowCenterInfo = value;
            OnPropertyChanged("ShowCenterFileList");
            OnPropertyChanged("ShowCenterInfoPanel");
        }
        private bool mShowCenterInfo;

        // These control which columns are visible in the file list.  Some columns are always
        // visible, some are only shown for file archives or disks.  We want to set one
        // to Visible and one to Collapsed.
        public Visibility ArchiveColVis { get; internal set; } = Visibility.Collapsed;
        public Visibility DiskColVis { get; internal set; } = Visibility.Collapsed;
        public Visibility DOSDiskColVis { get; internal set; } = Visibility.Collapsed;
        public enum ColumnMode { Unknown = 0, FileArchive, DiskImage, DOSDiskImage };
        public void SetColumnConfig(ColumnMode mode) {
            switch (mode) {
                case ColumnMode.FileArchive:
                    ArchiveColVis = Visibility.Visible;
                    DiskColVis = Visibility.Collapsed;
                    DOSDiskColVis = Visibility.Collapsed;
                    break;
                case ColumnMode.DOSDiskImage:
                    ArchiveColVis = Visibility.Collapsed;
                    DiskColVis = Visibility.Visible;
                    DOSDiskColVis = Visibility.Visible;
                    break;
                case ColumnMode.DiskImage:
                default:
                    ArchiveColVis = Visibility.Collapsed;
                    DiskColVis = Visibility.Visible;
                    DOSDiskColVis = Visibility.Collapsed;
                    break;
            }
            OnPropertyChanged("ArchiveColVis");
            OnPropertyChanged("DiskColVis");
            OnPropertyChanged("DOSDiskColVis");
        }

        public ObservableCollection<FileListItem> FileList {
            get { return mFileList; }
            internal set { mFileList = value; OnPropertyChanged(); }
        }
        private ObservableCollection<FileListItem> mFileList =
            new ObservableCollection<FileListItem>();


        public string CenterInfoText1 {
            get { return mCenterInfoText1; }
            set { mCenterInfoText1 = value; OnPropertyChanged(); }
        }
        private string mCenterInfoText1 = string.Empty;
        public string CenterInfoText2 {
            get { return mCenterInfoText2; }
            set { mCenterInfoText2 = value; OnPropertyChanged(); }
        }
        private string mCenterInfoText2 = string.Empty;

        public bool ShowPartitionLayout {
            get { return mShowPartitionLayout; }
            set { mShowPartitionLayout = value; OnPropertyChanged(); }
        }
        private bool mShowPartitionLayout;
        public class PartitionListItem {
            public int Index { get; private set; }
            public long StartBlock { get; private set; }
            public long BlockCount { get; private set; }
            public string PartName { get; private set; }
            public string PartType { get; private set; }

            public PartitionListItem(int index, long startBlock, long blockCount, string partName,
                    string partType) {
                Index = index;
                StartBlock = startBlock;
                BlockCount = blockCount;
                PartName = partName;
                PartType = partType;
            }
            public override string ToString() {
                return "[Part: start=" + StartBlock + " count=" + BlockCount + " name=" +
                    PartName + "]";
            }
        }
        public ObservableCollection<PartitionListItem> PartitionList { get; } =
            new ObservableCollection<PartitionListItem>();

        public bool ShowMetadata {
            get { return mShowMetadata; }
            set { mShowMetadata = value; OnPropertyChanged(); }
        }
        private bool mShowMetadata;
        public class MetadataItem {
            public string Name { get; private set; }
            public string Value { get; private set; }

            public MetadataItem(string name, string value) {
                Name = name;
                Value = value;
            }
        }
        public ObservableCollection<MetadataItem> MetadataList { get; } =
            new ObservableCollection<MetadataItem>();

        /// <summary>
        /// Configures the metadata list, displayed on the info panel.
        /// </summary>
        public void SetMetadataList(string typeName) {
            // TODO: make this real
            MetadataList.Clear();
            MetadataItem item = new MetadataItem("put_some", "stuff for " + typeName);
            MetadataList.Add(item);
            ShowMetadata = true;
        }

        /// <summary>
        /// Configures the partition list, displayed on the info panel.
        /// </summary>
        /// <param name="parts">Partition container.</param>
        public void SetPartitionList(IMultiPart parts) {
            PartitionList.Clear();
            for (int index = 0; index < parts.Count; index++) {
                Partition part = parts[index];
                string name = string.Empty;
                string type = string.Empty;
                if (part is APM_Partition) {
                    name = ((APM_Partition)part).PartitionName;
                    type = ((APM_Partition)part).PartitionType;
                }
                PartitionListItem item =
                    new PartitionListItem(index + (ArchiveTreeItem.ONE_BASED_INDEX ? 1 : 0),
                        part.StartOffset / Defs.BLOCK_SIZE, part.Length / Defs.BLOCK_SIZE,
                        name, type);
                PartitionList.Add(item);
            }
            ShowPartitionLayout = (PartitionList.Count > 0);
        }

        public bool ShowNotes {
            get { return mShowNotes; }
            set { mShowNotes = value; OnPropertyChanged(); }
        }
        private bool mShowNotes;
        public ObservableCollection<Notes.Note> NotesList { get; } =
            new ObservableCollection<Notes.Note>();

        /// <summary>
        /// Sets the list of Notes to display on the center panel.
        /// </summary>
        public void SetNotesList(Notes notes) {
            NotesList.Clear();
            foreach (Notes.Note note in notes.GetNotes()) {
                NotesList.Add(note);
            }
            ShowNotes = (notes.Count > 0);
        }

        /// <summary>
        /// Clears the partition list and notes list displayed on the center panel.
        /// </summary>
        public void ClearCenterInfo() {
            PartitionList.Clear();
            ShowPartitionLayout = false;
            NotesList.Clear();
            ShowNotes = false;

            MetadataList.Clear();
            ShowMetadata = false;
        }

        private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            DataGrid grid = (DataGrid)sender;
            if (!grid.GetClickRowColItem(e, out int row, out int col, out object? item)) {
                // Header or empty area; ignore.
                return;
            }
            FileListItem fli = (FileListItem)item;

            ArchiveTreeItem? arcTreeSel = archiveTree.SelectedItem as ArchiveTreeItem;
            DirectoryTreeItem? dirTreeSel = directoryTree.SelectedItem as DirectoryTreeItem;
            if (arcTreeSel == null || dirTreeSel == null) {
                Debug.Assert(false, "tree is missing selection");
                return;
            }
            mMainCtrl.HandleFileListDoubleClick(fli, row, col, arcTreeSel, dirTreeSel);
        }

        private void FileListDataGrid_Drop(object sender, DragEventArgs e) {
            IFileEntry dropTarget = IFileEntry.NO_ENTRY;
            DataGrid grid = (DataGrid)sender;
            if (grid.GetDropRowColItem(e, out int row, out int col, out object? item)) {
                // Dropped on a specific item.  Do we want to do something with that fact?
                FileListItem fli = (FileListItem)item;
                dropTarget = fli.FileEntry;
            }
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                mMainCtrl.AddFileDrop(dropTarget, files);
            }
        }

        #endregion Center Panel

        #region Options Panel

        /// <summary>
        /// True if the options panel on the right side of the window should be fully visible.
        /// </summary>
        public bool ShowOptionsPanel {
            get { return mShowOptionsPanel; }
            set { mShowOptionsPanel = value; OnPropertyChanged(); }
        }
        private bool mShowOptionsPanel = true;

        private void ShowHideOptionsButton_Click(object sender, RoutedEventArgs e) {
            ShowOptionsPanel = !ShowOptionsPanel;
        }

        #endregion Options Panel

        #region Status Bar

        /// <summary>
        /// String to display at the center of the status bar (bottom of window).
        /// </summary>
        public string CenterStatusText {
            get { return mCenterStatusText; }
            set { mCenterStatusText = value; OnPropertyChanged(); }
        }
        private string mCenterStatusText = string.Empty;

        #endregion Status Bar
    }
}