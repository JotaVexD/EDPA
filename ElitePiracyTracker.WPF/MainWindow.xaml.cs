// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows;
using ElitePiracyTracker.WPF.Views.Pages;

namespace ElitePiracyTracker.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        DataContext = this;

        Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);

        InitializeComponent();

        Loaded += (_, _) => MainFrame.Navigate(typeof(DashboardPage));
    }
}


