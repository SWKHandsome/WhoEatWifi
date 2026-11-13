using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using WhoEatWifi.Core;

namespace WhoEatWifi.UI
{
    public enum CloseActionChoice
    {
        Cancel,
        Minimize,
        Exit
    }

    public class CloseOptionDialog : Window
    {
        public CloseActionChoice SelectedAction { get; private set; }
        public bool RememberChoice { get; private set; }

        private CheckBox _chkRemember;

        public CloseOptionDialog()
        {
            SelectedAction = CloseActionChoice.Cancel;
            RememberChoice = false;

            InitializeDialog();
        }

        private void InitializeDialog()
        {
            this.Title = LanguageManager.T("CloseDialogTitle");
            this.Width = 460;
            this.SizeToContent = SizeToContent.Height;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.ShowInTaskbar = false;
            this.ResizeMode = ResizeMode.NoResize;

            // Outer Shadow & Container Border
            Border outerBorder = new Border();
            outerBorder.Background = Brushes.White;
            outerBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)); // #CBD5E1
            outerBorder.BorderThickness = new Thickness(1);
            outerBorder.CornerRadius = new CornerRadius(10);
            outerBorder.Margin = new Thickness(12);
            outerBorder.Padding = new Thickness(22, 18, 22, 18);
            outerBorder.Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 16,
                ShadowDepth = 2,
                Opacity = 0.22
            };

            // Allow dragging the dialog
            outerBorder.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    try { DragMove(); } catch { }
                }
            };

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Prompt Text
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // CheckBox
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

            // --- Row 0: Header (Icon + Title + Close Button) ---
            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock iconBlock = new TextBlock
            {
                Text = "❓",
                FontSize = 18,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(iconBlock, 0);

            TextBlock titleBlock = new TextBlock
            {
                Text = LanguageManager.T("CloseDialogTitle"),
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)), // #0F172A
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleBlock, 1);

            Button btnClose = new Button
            {
                Content = "✕",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)), // #64748B
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Padding = new Thickness(6, 2, 6, 2),
                FontWeight = FontWeights.Bold
            };
            btnClose.Click += delegate
            {
                SelectedAction = CloseActionChoice.Cancel;
                this.DialogResult = false;
                this.Close();
            };
            Grid.SetColumn(btnClose, 2);

            headerGrid.Children.Add(iconBlock);
            headerGrid.Children.Add(titleBlock);
            headerGrid.Children.Add(btnClose);
            Grid.SetRow(headerGrid, 0);

            // --- Row 1: Message Prompt ---
            TextBlock promptBlock = new TextBlock
            {
                Text = LanguageManager.T("CloseDialogPrompt"),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                LineHeight = 20,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)), // #334155
                Margin = new Thickness(0, 14, 0, 14)
            };
            Grid.SetRow(promptBlock, 1);

            // --- Row 2: Remember Choice CheckBox ---
            _chkRemember = new CheckBox
            {
                Content = LanguageManager.T("ChkRememberCloseChoice"),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)), // #64748B
                Margin = new Thickness(0, 0, 0, 18),
                Cursor = Cursors.Hand
            };
            Grid.SetRow(_chkRemember, 2);

            // --- Row 3: Buttons Panel ---
            StackPanel btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            // Button 1: Cancel
            Button btnCancel = CreateStyledButton(
                LanguageManager.T("BtnCancel"),
                new SolidColorBrush(Color.FromRgb(241, 245, 249)), // #F1F5F9
                new SolidColorBrush(Color.FromRgb(71, 85, 105)),   // #475569
                new SolidColorBrush(Color.FromRgb(226, 232, 240)), // #E2E8F0 border
                FontWeights.Normal);
            btnCancel.Margin = new Thickness(0, 0, 8, 0);
            btnCancel.Click += delegate
            {
                SelectedAction = CloseActionChoice.Cancel;
                this.DialogResult = false;
                this.Close();
            };

            // Button 2: Exit Program (Red)
            Button btnExit = CreateStyledButton(
                LanguageManager.T("BtnExitApp"),
                new SolidColorBrush(Color.FromRgb(254, 226, 226)), // #FEE2E2
                new SolidColorBrush(Color.FromRgb(220, 38, 38)),   // #DC2626
                new SolidColorBrush(Color.FromRgb(252, 165, 165)), // #FCA5A5 border
                FontWeights.SemiBold);
            btnExit.Margin = new Thickness(0, 0, 8, 0);
            btnExit.Click += delegate
            {
                SelectedAction = CloseActionChoice.Exit;
                RememberChoice = _chkRemember.IsChecked == true;
                this.DialogResult = true;
                this.Close();
            };

            // Button 3: Minimize to Tray (Primary Blue/Indigo)
            Button btnMinimize = CreateStyledButton(
                LanguageManager.T("BtnMinimizeTray"),
                new SolidColorBrush(Color.FromRgb(79, 70, 229)),   // #4F46E5
                Brushes.White,
                Brushes.Transparent,
                FontWeights.Bold);
            btnMinimize.Click += delegate
            {
                SelectedAction = CloseActionChoice.Minimize;
                RememberChoice = _chkRemember.IsChecked == true;
                this.DialogResult = true;
                this.Close();
            };

            btnPanel.Children.Add(btnCancel);
            btnPanel.Children.Add(btnExit);
            btnPanel.Children.Add(btnMinimize);
            Grid.SetRow(btnPanel, 3);

            mainGrid.Children.Add(headerGrid);
            mainGrid.Children.Add(promptBlock);
            mainGrid.Children.Add(_chkRemember);
            mainGrid.Children.Add(btnPanel);

            outerBorder.Child = mainGrid;
            this.Content = outerBorder;

            this.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Escape)
                {
                    SelectedAction = CloseActionChoice.Cancel;
                    this.DialogResult = false;
                    this.Close();
                }
            };
        }

        private static Button CreateStyledButton(string text, Brush bg, Brush fg, Brush border, FontWeight weight)
        {
            Button btn = new Button
            {
                Content = text,
                Background = bg,
                Foreground = fg,
                BorderBrush = border,
                BorderThickness = border == Brushes.Transparent ? new Thickness(0) : new Thickness(1),
                Padding = new Thickness(14, 7, 14, 7),
                Cursor = Cursors.Hand,
                FontWeight = weight,
                FontSize = 12
            };

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));

            FrameworkElementFactory presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);

            template.VisualTree = borderFactory;
            btn.Template = template;

            return btn;
        }
    }
}
