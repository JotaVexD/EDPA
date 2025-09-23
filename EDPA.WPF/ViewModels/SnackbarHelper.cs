// SnackbarHelper.cs
using EDPA.WPF;
using System;
using System.Windows;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace EDPA.WPF.ViewModels
{
    public static class SnackbarHelper
    {
        public static void Show(string title, string message, ControlAppearance appearance = ControlAppearance.Primary)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.SnackbarService.Show(
                    title: title,
                    message: message,
                    appearance: appearance,
                    timeout: TimeSpan.FromSeconds(4)
                );
            }
        }

        public static void ShowSuccess(string message)
        {
            Show("Success", message, ControlAppearance.Success);
        }

        public static void ShowError(string message)
        {
            Show("Error", message, ControlAppearance.Danger);
        }

        public static void ShowCaution(string message)
        {
            Show("Update", message, ControlAppearance.Caution);
        }

        public static void ShowInfo(string message)
        {
            Show("Information", message, ControlAppearance.Info);
        }
    }
}