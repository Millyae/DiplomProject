using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using DiplomProject.Models;

namespace DiplomProject
{
    public partial class ServiceWindow : Window
    {
        private readonly diplomContext _context;
        private readonly ComboBox _positionComboBox; 

        public ServiceWindow(diplomContext context, ComboBox positionComboBox)
        {
            InitializeComponent();
            _context = context;
            _positionComboBox = positionComboBox;
            LoadServices();
        }

        private void LoadServices()
        {
            try
            {
                var services = _context.Services
                    .OrderBy(s => s.ServiceName)
                    .ToList();

                ServicesDataGrid.ItemsSource = services;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки услуг: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddServiceButton_Click(object sender, RoutedEventArgs e)
        {
            string serviceName = NewServiceTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(serviceName))
            {
                MessageBox.Show("Введите название услуги", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                bool exists = await _context.Services
                    .AnyAsync(s => s.ServiceName.ToLower() == serviceName.ToLower());

                if (exists)
                {
                    MessageBox.Show("Услуга с таким названием уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newService = new Service { ServiceName = serviceName };
                _context.Services.Add(newService);
                await _context.SaveChangesAsync();

                NewServiceTextBox.Clear();
                LoadServices();
                UpdatePositionComboBox();

                MessageBox.Show("Услуга успешно добавлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении услуги: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteServiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (ServicesDataGrid.SelectedItem is not Service selectedService)
                return;

            bool isUsed = await _context.Rates.AnyAsync(r => r.IdService == selectedService.IdService);

            if (isUsed)
            {
                MessageBox.Show("Невозможно удалить услугу, так как она используется в ставках", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите удалить услугу '{selectedService.ServiceName}'?","Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _context.Services.Remove(selectedService);
                    await _context.SaveChangesAsync();

                    LoadServices();
                    UpdatePositionComboBox();

                    MessageBox.Show("Услуга успешно удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении услуги: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdatePositionComboBox()
        {
            try
            {
                var services = _context.Services
                    .OrderBy(s => s.ServiceName)
                    .ToList();

                if (_positionComboBox != null)
                {
                    _positionComboBox.ItemsSource = services;
                    _positionComboBox.DisplayMemberPath = "ServiceName";
                    _positionComboBox.SelectedValuePath = "IdService";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления списка услуг: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}