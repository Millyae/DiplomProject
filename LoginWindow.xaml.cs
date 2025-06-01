using System;
using System.Configuration;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace DiplomProject
{
    public partial class LoginWindow : Window
    {
        private byte[] _encryptedEmail;
        private byte[] _encryptedPassword;

        public LoginWindow()
        {
            InitializeComponent();
            EmailTextBox.Focus();
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);
            InitializeCredentials();
        }

        private void InitializeCredentials()
        {
            string originalEmail = ConfigurationManager.AppSettings["Email"];
            string originalPassword = ConfigurationManager.AppSettings["Password"];

            _encryptedEmail = Protect(originalEmail);
            _encryptedPassword = Protect(originalPassword);
        }

        private void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text.Trim();
            string password = PasswordTextBox.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowError("Пожалуйста, введите email и пароль");
                return;
            }

            string decryptedEmail = Unprotect(_encryptedEmail);
            string decryptedPassword = Unprotect(_encryptedPassword);

            if (email != decryptedEmail || password != decryptedPassword)
            {
                ShowError("Неверный email или пароль");
                return;
            }

            MainWindow mainWindow = new MainWindow();
            mainWindow.WindowState = WindowState.Maximized; 
            mainWindow.Show();
            this.Close();
        }

        private byte[] Protect(string data)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                return ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка шифрования: {ex.Message}");
                return null;
            }
        }

        private string Unprotect(byte[] data)
        {
            try
            {
                byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка расшифровки: {ex.Message}");
                return string.Empty;
            }
        }

        private void HandleEsc(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) Application.Current.Shutdown(); }
        private void ShowError(string message) { ErrorMessage.Text = message; ErrorMessage.Visibility = Visibility.Visible; }
        private void TextBox_GotFocus(object sender, RoutedEventArgs e) { ErrorMessage.Visibility = Visibility.Collapsed; }
    }
}