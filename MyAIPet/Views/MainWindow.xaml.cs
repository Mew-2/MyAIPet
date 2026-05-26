using System.Windows;
using System.Windows.Input;
using MyAIPet.ViewModels;
using MyAIPet.Views;

namespace MyAIPet.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var screenWidth = SystemParameters.WorkArea.Width;
            var screenHeight = SystemParameters.WorkArea.Height;
            Left = screenWidth - Width - 20;
            Top = screenHeight - Height - 20;

            if (DataContext is MainWindowViewModel vm)
            {
                vm.UpdatePosition(this);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Window_LocationChanged(object sender, System.EventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.UpdatePosition(this);
            }
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is MainWindowViewModel vm)
            {
                if (vm.SendMessageCommand.CanExecute())
                {
                    vm.SendMessageCommand.Execute();
                }
            }
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MenuItem_Gobang_Click(object sender, RoutedEventArgs e)
        {
            var gobangWindow = new GobangWindow();
            gobangWindow.Show();
        }

        private void ChatBubble_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.BubbleClickCommand.Execute();
            }
            e.Handled = true;
        }

        private void PetImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.ToggleBubbleCommand.Execute();
                }
            }
            else if (DataContext is MainWindowViewModel vm)
            {
                vm.BubbleClickCommand.Execute();
            }
        }
    }
}
