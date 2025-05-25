﻿using Microsoft.UI.Windowing;

namespace Test.Views;
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        ((OverlappedPresenter)AppWindow.Presenter).PreferredMinimumWidth = 800;
        ((OverlappedPresenter)AppWindow.Presenter).PreferredMinimumHeight = 600;
    }
}

