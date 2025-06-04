using System.Linq;
using System.Windows;
using System.Windows.Input;
using DiplomProject.Models;
using Microsoft.EntityFrameworkCore;

namespace DiplomProject
{
    public partial class LoginWindow : Window
    {
        private readonly diplomContext _context;

        public LoginWindow()
        {
            InitializeComponent();
            _context = new diplomContext();
        }

        private void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = EmailTextBox.Text;
            string password = PasswordTextBox.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                ShowErrorMessage("Пожалуйста, введите логин и пароль");
                return;
            }

            var user = _context.Users.FirstOrDefault(u => u.Login == login);

            if (user != null && user.Password == password)
            {
                MainWindow mainWindow = new MainWindow(user.Role);
                mainWindow.Show();
                this.Close();
            }
            else
            {
                ShowErrorMessage("Неверный логин или пароль");
            }
        }

        private void ShowErrorMessage(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _context?.Dispose();
        }
    }
}