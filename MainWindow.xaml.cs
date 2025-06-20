﻿using DiplomProject.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Data;
using System.IO;
using OfficeOpenXml;
using Microsoft.Win32;
using System.Windows.Input;
using DiplomProject.ViewModels;
using DiplomProject.Validation;
using System.Text;

namespace DiplomProject
{
    public partial class MainWindow : Window
    {
        private string _currentUserRole;
        private diplomContext _context = new diplomContext();
        private RateViewModel _selectedRate;
        public ObservableCollection<Models.Object> Objects { get; set; }
        public ObservableCollection<Service> Services { get; set; }
        public ObservableCollection<RateViewModel> Rates { get; set; }

        public MainWindow(string userRole)
        {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            InitializeComponent();
            ConfigureDataGridColumns();
            _currentUserRole = userRole;
            DataContext = this;
            LoadData();
            SetPermissionsBasedOnRole();
        }

        private void LoadData()
        {
            Objects = new ObservableCollection<Models.Object>(_context.Objects.Include(o => o.IdAddressNavigation).ToList());
            Services = new ObservableCollection<Service>(_context.Services.OrderBy(s => s.ServiceName).ToList());
            LoadEmployees();
            LoadObjects();
            LoadRates();
            LoadEmployeesPanel();
        }

        private void SetPermissionsBasedOnRole()
        {
            if (_currentUserRole == "Manager")
            {
                DeleteButton.Visibility = Visibility.Collapsed;
                DeleteObjectButton.Visibility = Visibility.Collapsed;
                DeleteRateButton.Visibility = Visibility.Collapsed;
            }
            else if (_currentUserRole == "Admin")
            {
                DeleteButton.Visibility = Visibility.Visible;
                DeleteObjectButton.Visibility = Visibility.Visible;
                DeleteRateButton.Visibility = Visibility.Visible;
            }
        }

        private void ConfigureDataGridColumns()
        {
            EmployeeDataGrid.AutoGeneratingColumn += (sender, e) =>
            {
                if (e.PropertyName == "WorkSchedules")
                {
                    e.Cancel = true;
                }
                if (e.PropertyName == "IdFio")
                {
                    e.Cancel = true;
                }
                switch (e.PropertyName)
                {
                    case "IdEmployee": e.Column.Header = "№"; break;
                    case "LastName": e.Column.Header = "Фамилия"; break;
                    case "FirstName": e.Column.Header = "Имя"; break;
                    case "MiddleName": e.Column.Header = "Отчество"; break;
                    case "Phone": e.Column.Header = "Телефон"; break;
                    case "Email": e.Column.Header = "Почта"; break;
                    case "Metro": e.Column.Header = "Метро"; break;
                    case "HireDate": e.Column.Header = "Дата приема"; break;
                    case "Experience": e.Column.Header = "Опыт"; break;
                    case "Schedules": e.Column.Header = "График"; break;
                    case "Notes": e.Column.Header = "Заметки"; break;
                    case "Comments": e.Column.Header = "Комментарии"; break;
                }
            };

            ObjectDataGrid.AutoGeneratingColumn += (sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case "IdObject": e.Column.Header = "№"; break;
                    case "ObjectName": e.Column.Header = "Название объекта"; break;
                    case "PostalCode": e.Column.Header = "Индекс"; break;
                    case "Country": e.Column.Header = "Страна"; break;
                    case "City": e.Column.Header = "Город"; break;
                    case "Street": e.Column.Header = "Улица"; break;
                    case "Building": e.Column.Header = "Дом"; break;
                    case "Corpus": e.Column.Header = "Корпус"; break;
                    case "Office": e.Column.Header = "Офис"; break;
                    case "FullAddress": e.Column.Header = "Полный Адрес"; break;
                }
            };


            RateDataGrid.AutoGeneratingColumn += (sender, e) =>
            {
                if (e.PropertyName == "IdObject" || e.PropertyName == "IdAddress" || e.PropertyName == "IdRate")
                {
                    e.Cancel = true;
                }

                switch (e.PropertyName)
                {
                    case "ObjectName": e.Column.Header = "Объект"; break;
                    case "Address": e.Column.Header = "Адрес"; break;
                    case "ServiceName": e.Column.Header = "Услуга"; break;
                    case "HourlyRate": e.Column.Header = "Ставка"; break;
                }
            };
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadEmployees();
        }

        private void LoadEmployeesPanel()
        {
            EmployeesStackPanel.Children.Clear();

            var employees = _context.Employees
                .Include(e => e.IdFullnameNavigation)
                .OrderBy(e => e.IdFullnameNavigation.LastName)
                .ToList();

            foreach (var employee in employees)
            {
                string fullName = $"{employee.IdFullnameNavigation?.LastName} {employee.IdFullnameNavigation?.FirstName} {employee.IdFullnameNavigation?.MiddleName}";

                Border employeeBorder = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(5),
                    Padding = new Thickness(10),
                    CornerRadius = new CornerRadius(5),
                    Background = Brushes.White,
                        Cursor = Cursors.Hand,
                        Tag = employee
                };

                StackPanel employeeInfoPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical
                };

                employeeInfoPanel.Children.Add(new TextBlock
                {
                    Text = $"{fullName}",
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 5)
                });

                employeeInfoPanel.Children.Add(new TextBlock
                {
                    Text = $"Почта: {employee.Email}",
                    Margin = new Thickness(0, 0, 0, 5)
                });

                employeeInfoPanel.Children.Add(new TextBlock
                {
                    Text = $"Телефон: {employee.Phone}"
                });

                employeeInfoPanel.Children.Add(new TextBlock
                {
                    Text = $"Метро: {employee.Metro}"
                });

                employeeInfoPanel.Children.Add(new TextBlock
                {
                    Text = $"График: {employee.Schedules}"
                });

                employeeBorder.Child = employeeInfoPanel;
                employeeBorder.MouseLeftButtonUp += (s, e) => OpenInformation(employee.IdEmployee, _currentUserRole);
                EmployeesStackPanel.Children.Add(employeeBorder);
            }
        }

        private void SearchPanelTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchPanelTextBox.Text.ToLower().Trim();

            foreach (Border employeeBorder in EmployeesStackPanel.Children)
            {
                bool isMatch = false;

                if (employeeBorder.Tag is Employee employee)
                {
                    string fullName = $"{employee.IdFullnameNavigation?.LastName} {employee.IdFullnameNavigation?.FirstName} {employee.IdFullnameNavigation?.MiddleName}".ToLower();
                    string email = employee.Email?.ToLower() ?? "";
                    string phone = employee.Phone?.ToLower() ?? "";
                    string metro = employee.Metro?.ToLower() ?? "";
                    string schedule = employee.Schedules?.ToLower() ?? "";

                    if (fullName.Contains(searchText) ||
                        email.Contains(searchText) ||
                        phone.Contains(searchText) ||
                        metro.Contains(searchText) ||
                        schedule.Contains(searchText))
                    {
                        isMatch = true;
                    }
                }

                employeeBorder.Visibility = isMatch || string.IsNullOrEmpty(searchText)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void OpenInformation(int employeeId, string userRoles)
        {
            InformationWindow detailsWindow = new InformationWindow(employeeId, userRoles);
            detailsWindow.Owner = this;
            detailsWindow.ShowDialog();
        }

        private void LoadEmployees()
        {
            try
            {
                var employees = _context.Employees
                    .Include(e => e.IdFullnameNavigation)
                    .Select(e => new EmployeeViewModel
                    {
                        IdEmployee = e.IdEmployee,
                        LastName = e.IdFullnameNavigation.LastName,
                        FirstName = e.IdFullnameNavigation.FirstName,
                        MiddleName = e.IdFullnameNavigation.MiddleName,
                        Email = e.Email,
                        Phone = e.Phone,
                        Metro = e.Metro,
                        Experience = e.Experience,
                        Schedules = e.Schedules,
                        Notes = e.Notes,
                        Comments = e.Comments,
                        HireDate = e.HireDate,
                        IdFio = e.IdFullname
                    })
                    .ToList();

                EmployeeDataGrid.ItemsSource = employees;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сотрудников: {ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var validationErrors = new List<string>();

                foreach (var item in EmployeeDataGrid.Items)
                {
                    if (item is EmployeeViewModel employeeVM)
                    {
                        var emailValidation = EmployeeValidation.ValidateEmail(employeeVM.Email, _context, employeeVM.IdEmployee);
                        if (!emailValidation.IsValid)
                        {
                            validationErrors.Add($"Сотрудник {employeeVM.LastName} {employeeVM.FirstName}: {emailValidation.ErrorMessage}");
                        }

                        var phoneValidation = EmployeeValidation.ValidatePhone(employeeVM.Phone, _context, employeeVM.IdEmployee);
                        if (!phoneValidation.IsValid)
                        {
                            validationErrors.Add($"Сотрудник {employeeVM.LastName} {employeeVM.FirstName}: {phoneValidation.ErrorMessage}");
                        }

                        var hireDateValidation = EmployeeValidation.ValidateHireDate(employeeVM.HireDate);
                        if (!hireDateValidation.IsValid)
                        {
                            validationErrors.Add($"Сотрудник {employeeVM.LastName} {employeeVM.FirstName}: {hireDateValidation.ErrorMessage}");
                        }
                    }
                }

                if (validationErrors.Any())
                {
                    MessageBox.Show($"Обнаружены ошибки:\n\n{string.Join("\n", validationErrors)}","Ошибки валидации",MessageBoxButton.OK,MessageBoxImage.Warning);
                    return;
                }

                foreach (var item in EmployeeDataGrid.Items)
                {
                    if (item is EmployeeViewModel employeeVM)
                    {
                        var employee = _context.Employees
                            .Include(e => e.IdFullnameNavigation)
                            .FirstOrDefault(e => e.IdEmployee == employeeVM.IdEmployee);

                        if (employee != null)
                        {
                            employee.Email = employeeVM.Email;
                            employee.Phone = employeeVM.Phone;
                            employee.HireDate = employeeVM.HireDate;
                            employee.Metro = employeeVM.Metro;
                            employee.Experience = employeeVM.Experience;
                            employee.Schedules = employeeVM.Schedules;
                            employee.Notes = employeeVM.Notes;
                            employee.Comments = employeeVM.Comments;

                            if (employee.IdFullnameNavigation == null)
                            {
                                employee.IdFullnameNavigation = new Fullname();
                            }

                            employee.IdFullnameNavigation.LastName = employeeVM.LastName;
                            employee.IdFullnameNavigation.FirstName = employeeVM.FirstName;
                            employee.IdFullnameNavigation.MiddleName = employeeVM.MiddleName;
                        }
                    }
                }

                _context.SaveChanges();
                MessageBox.Show("Изменения сохранены", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch (DbUpdateException ex)
            {
                string errorDetails = GetExceptionDetails(ex);
                MessageBox.Show($"Ошибка сохранения в базе данных: {errorDetails}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Непредвиденная ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUserRole != "Admin")
            {
                MessageBox.Show("У вас нет прав для выполнения этого действия", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EmployeeDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите сотрудника для удаления", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedEmployee = (EmployeeViewModel)EmployeeDataGrid.SelectedItem;
            var result = MessageBox.Show("Вы уверены, что хотите удалить этого сотрудника?", "Подтверждение", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var employee = _context.Employees
                        .Include(emp => emp.IdFullnameNavigation)
                        .FirstOrDefault(emp => emp.IdEmployee == selectedEmployee.IdEmployee);

                    if (employee != null)
                    {
                        employee.IdFullnameNavigation = null;
                        _context.Employees.Remove(employee);
                        _context.SaveChanges();

                        LoadEmployees();
                        MessageBox.Show("Сотрудник удален", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (DbUpdateException ex)
                {
                    MessageBox.Show($"Ошибка при удалении сотрудника: {ex.InnerException?.Message ?? ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newFio = new Fullname()
                {
                    LastName = "Фамилия",
                    FirstName = "Имя",
                    MiddleName = "Отчество"
                };

                var newEmployee = new Employee()
                {
                    Email = "example@mail.com",
                    Phone = "000-000-0000",
                    Metro = "Название метро",
                    Experience = "Опыт",
                    Schedules = "График",
                    Notes = "Заметки",
                    Comments = "Комментарии",
                    HireDate = DateOnly.FromDateTime(DateTime.Now),
                    IdFullnameNavigation = newFio
                };

                _context.Fullnames.Add(newFio);
                _context.Employees.Add(newEmployee);
                _context.SaveChanges();
                LoadEmployees();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadObjects()
        {
            try
            {
                var objects = _context.Objects
                    .Include(o => o.IdAddressNavigation)
                    .Select(o => new ObjectViewModel
                    {
                        IdObject = o.IdObject,
                        ObjectName = o.ObjectName,
                        PostalCode = o.IdAddressNavigation.PostalCode,
                        Country = o.IdAddressNavigation.Country,
                        City = o.IdAddressNavigation.City,
                        Street = o.IdAddressNavigation.Street,
                        Building = o.IdAddressNavigation.Building,
                        Corpus = o.IdAddressNavigation.Corpus,
                        Office = o.IdAddressNavigation.Office,
                        FullAddress = $"{o.IdAddressNavigation.PostalCode}, {o.IdAddressNavigation.Country}, {o.IdAddressNavigation.City}, {o.IdAddressNavigation.Street}, {o.IdAddressNavigation.Building}"
                    })
                    .ToList();

                ObjectDataGrid.ItemsSource = objects;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки объектов: {ex.Message}");
            }
        }

        private void AddObjectButton_Click(object sender, RoutedEventArgs e)
        {
            var newObject = new ObjectViewModel
            {
                ObjectName = "Новый объект",
                PostalCode = "000000",
                Country = "Россия",
                City = "Москва",
                Street = "Новая улица",
                Building = "1",
                Corpus = "",
                Office = ""
            };

            if (ObjectDataGrid.ItemsSource is ObservableCollection<ObjectViewModel> collection)
            {
                collection.Add(newObject);
            }
            else
            {
                var list = new List<ObjectViewModel>();
                if (ObjectDataGrid.ItemsSource != null)
                {
                    list = ((IEnumerable<ObjectViewModel>)ObjectDataGrid.ItemsSource).ToList();
                }
                list.Add(newObject);
                ObjectDataGrid.ItemsSource = list;
            }

            ObjectDataGrid.ScrollIntoView(newObject);
            ObjectDataGrid.SelectedItem = newObject;
            ObjectDataGrid.BeginEdit();
            LoadRates();
        }

        private void SaveObjectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ObjectDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

                var objectsToSave = (IEnumerable<ObjectViewModel>)ObjectDataGrid.ItemsSource;

                foreach (var obj in objectsToSave)
                {
                    var existingObject = _context.Objects.Include(o => o.IdAddressNavigation)
                        .FirstOrDefault(o => o.IdObject == obj.IdObject);

                    if (existingObject == null)
                    {
                        var newAddress = new Address
                        {
                            PostalCode = obj.PostalCode,
                            Country = obj.Country,
                            City = obj.City,
                            Street = obj.Street,
                            Building = obj.Building,
                            Corpus = obj.Corpus,
                            Office = obj.Office
                        };

                        var newObject = new Models.Object
                        {
                            ObjectName = obj.ObjectName,
                            IdAddressNavigation = newAddress
                        };

                        _context.Objects.Add(newObject);
                    }
                    else
                    {
                        existingObject.ObjectName = obj.ObjectName;
                        existingObject.IdAddressNavigation.PostalCode = obj.PostalCode;
                        existingObject.IdAddressNavigation.Country = obj.Country;
                        existingObject.IdAddressNavigation.City = obj.City;
                        existingObject.IdAddressNavigation.Street = obj.Street;
                        existingObject.IdAddressNavigation.Building = obj.Building;
                        existingObject.IdAddressNavigation.Corpus = obj.Corpus;
                        existingObject.IdAddressNavigation.Office = obj.Office;
                    }
                }

                _context.SaveChanges();
                LoadData();
                MessageBox.Show("Данные успешно сохранены");
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException;
                while (innerException != null)
                {
                    MessageBox.Show($"Ошибка при сохранении: {innerException.Message}");
                    innerException = innerException.InnerException;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
            }
        }

        private void DeleteObjectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUserRole != "Admin")
            {
                MessageBox.Show("У вас нет прав для выполнения этого действия", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ObjectDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите объект для удаления");
                return;
            }

            var selected = (ObjectViewModel)ObjectDataGrid.SelectedItem;
            int id = selected.IdObject;

            var result = MessageBox.Show("Вы уверены, что хотите удалить этот объект?", "Подтверждение", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var obj = _context.Objects.Find(id);
                    if (obj != null)
                    {
                        _context.Objects.Remove(obj);
                        _context.SaveChanges();
                        LoadObjects();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}");
                }
            }
        }

        private void LoadRates()
        {
            Objects = new ObservableCollection<Models.Object>(_context.Objects.Include(o => o.IdAddressNavigation).ToList());
            Services = new ObservableCollection<Service>(_context.Services.ToList());

            var rates = _context.Rates
                .Include(r => r.IdServiceNavigation)
                .Include(r => r.IdObjectNavigation)
                .ThenInclude(o => o.IdAddressNavigation)
                .Select(r => new RateViewModel
                {
                    IdRate = r.IdRate,
                    IdObject = r.IdObject,
                    IdAddress = r.IdObjectNavigation.IdAddress,
                    IdService = r.IdService,
                    ObjectName = r.IdObjectNavigation.ObjectName,
                    Address = $"{r.IdObjectNavigation.IdAddressNavigation.PostalCode}, " +
                              $"{r.IdObjectNavigation.IdAddressNavigation.Country}, " +
                              $"{r.IdObjectNavigation.IdAddressNavigation.City}, " +
                              $"{r.IdObjectNavigation.IdAddressNavigation.Street}, " +
                              $"{r.IdObjectNavigation.IdAddressNavigation.Building}",
                    ServiceName = r.IdServiceNavigation.ServiceName,
                    HourlyRate = r.HourlyRate
                })
                .ToList();

            Rates = new ObservableCollection<RateViewModel>(rates);
            RateDataGrid.ItemsSource = Rates;
        }

        private void AddRateButton_Click(object sender, RoutedEventArgs e)
        {
            var newRate = new RateViewModel
            {
                HourlyRate = 0.00m,
                ServiceName = "Выберите услугу",
                ObjectName = "Выберите объект",
                Address = "Адрес будет выбран автоматически"
            };

            Rates.Add(newRate);
            RateDataGrid.ScrollIntoView(newRate);
            RateDataGrid.CurrentCell = new DataGridCellInfo(RateDataGrid.Items[RateDataGrid.Items.Count - 1], RateDataGrid.Columns[0]);
            RateDataGrid.BeginEdit();
        }

        public ComboBox PositionComboBox { get; set; }

        private void AddNewRate_Click(object sender, RoutedEventArgs e)
        {
            ServiceWindow serviceWindow = new ServiceWindow(_context, PositionComboBox); 
            serviceWindow.Owner = this;
            serviceWindow.ShowDialog();
        }

        private async void UpdateRateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ratesToUpdate = RateDataGrid.Items.OfType<RateViewModel>().ToList();

                if (!ratesToUpdate.Any())
                {
                    MessageBox.Show("Нет данных для обновления", "Информация",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        int updatedCount = 0;
                        int addedCount = 0;

                        foreach (var rateVM in ratesToUpdate)
                        {
                            if (rateVM.IdObject == 0 || rateVM.IdService == 0)
                            {
                                MessageBox.Show($"Укажите объект и услугу для ставки (ID: {rateVM.IdRate})",
                                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                continue;
                            }

                            if (rateVM.IdRate > 0)
                            {
                                var existingRate = await _context.Rates.FindAsync(rateVM.IdRate);
                                if (existingRate != null)
                                {
                                    if (existingRate.IdObject != rateVM.IdObject ||
                                        existingRate.IdService != rateVM.IdService ||
                                        existingRate.HourlyRate != rateVM.HourlyRate)
                                    {
                                        existingRate.IdObject = rateVM.IdObject;
                                        existingRate.IdService = rateVM.IdService;
                                        existingRate.HourlyRate = rateVM.HourlyRate;
                                        updatedCount++;
                                    }
                                }
                            }
                            else
                            {
                                bool exists = await _context.Rates
                                    .AnyAsync(r => r.IdObject == rateVM.IdObject &&
                                                 r.IdService == rateVM.IdService);

                                if (exists)
                                {
                                    MessageBox.Show($"Ставка для этого объекта и услуги уже существует (ID объекта: {rateVM.IdObject}, ID услуги: {rateVM.IdService})",
                                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    continue;
                                }

                                _context.Rates.Add(new Rate
                                {
                                    IdObject = rateVM.IdObject,
                                    IdService = rateVM.IdService,
                                    HourlyRate = rateVM.HourlyRate
                                });
                                addedCount++;
                            }
                        }

                        int changes = await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        LoadRates();

                        MessageBox.Show($"Успешно обновлено!","Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (DbUpdateException ex)
                    {
                        await transaction.RollbackAsync();
                        MessageBox.Show($"Ошибка базы данных: {ex.InnerException?.Message ?? ex.Message}","Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка: {ex.Message}", "Ошибка",MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                    return (T)child;
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private void SaveRateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ratesToSave = (IEnumerable<RateViewModel>)RateDataGrid.ItemsSource;

                foreach (var rate in ratesToSave)
                {
                    var existingRate = _context.Rates
                        .AsNoTracking()
                        .FirstOrDefault(r => r.IdObject == rate.IdObject &&
                                           r.IdService == rate.IdService &&
                                           r.IdRate != rate.IdRate);

                    if (existingRate != null)
                    {
                        var objectName = _context.Objects.Find(rate.IdObject)?.ObjectName ?? "Объект";
                        var serviceName = _context.Services.Find(rate.IdService)?.ServiceName ?? "Услуга";

                        MessageBox.Show($"Ставка для {objectName} и {serviceName} уже существует", "Дублирование данных");
                        return;
                    }

                    if (rate.IdRate == 0)
                    {
                        var newRate = new Rate
                        {
                            IdObject = rate.IdObject,
                            IdService = rate.IdService,
                            HourlyRate = rate.HourlyRate
                        };
                        _context.Rates.Add(newRate);
                    }
                    else
                    {
                        var rateToUpdate = _context.Rates.Find(rate.IdRate);
                        if (rateToUpdate != null)
                        {
                            rateToUpdate.IdObject = rate.IdObject;
                            rateToUpdate.IdService = rate.IdService;
                            rateToUpdate.HourlyRate = rate.HourlyRate;
                        }
                    }
                }

                _context.SaveChanges();
                MessageBox.Show("Данные успешно сохранены");
                LoadRates();
            }
            catch (DbUpdateException ex)
            {
                string errorDetails = GetExceptionDetails(ex);
                MessageBox.Show($"Ошибка сохранения: {errorDetails}", "Ошибка");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        private void RateDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedRate = RateDataGrid.SelectedItem as RateViewModel;
        }

        private void DeleteRateButton_Click_1(object sender, RoutedEventArgs e)
        {
            if (_currentUserRole != "Admin")
            {
                MessageBox.Show("У вас нет прав для выполнения этого действия", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selected = _selectedRate ?? RateDataGrid.SelectedItem as RateViewModel;

            if (selected == null)
            {
                MessageBox.Show("Выберите ставку для удаления");
                return;
            }

            var result = MessageBox.Show("Вы уверены, что хотите удалить эту ставку?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (selected.IdRate > 0)
                    {
                        var rateToDelete = _context.Rates.Find(selected.IdRate);
                        if (rateToDelete != null)
                        {
                            _context.Rates.Remove(rateToDelete);
                        }
                    }

                    if (RateDataGrid.ItemsSource is ObservableCollection<RateViewModel> collection)
                    {
                        collection.Remove(selected);
                    }
                    else if (RateDataGrid.ItemsSource is IList<RateViewModel> list)
                    {
                        list.Remove(selected);
                        RateDataGrid.Items.Refresh();
                    }

                    _context.SaveChanges();
                    MessageBox.Show("Ставка успешно удалена");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении ставки: {ex.Message}");
                }
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var employees = EmployeeDataGrid.ItemsSource.Cast<EmployeeViewModel>().ToList();
                if (employees == null || !employees.Any())
                {
                    MessageBox.Show("Нет данных для экспорта", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    FileName = $"Сотрудники_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    DefaultExt = ".xlsx",
                    Title = "Выберите место для сохранения файла"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Сотрудники");

                        string[] headers = { "№", "Фамилия", "Имя", "Отчество", "Телефон", "Почта", "Метро",
                          "Дата приема", "Опыт", "График", "Заметки", "Комментарии" };

                        for (int i = 0; i < headers.Length; i++)
                        {
                            worksheet.Cells[1, i + 1].Value = headers[i];
                            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        }

                        int row = 2;
                        foreach (var emp in employees)
                        {
                            worksheet.Cells[row, 1].Value = emp.IdEmployee;
                            worksheet.Cells[row, 2].Value = emp.LastName;
                            worksheet.Cells[row, 3].Value = emp.FirstName;
                            worksheet.Cells[row, 4].Value = emp.MiddleName;
                            worksheet.Cells[row, 5].Value = emp.Phone;
                            worksheet.Cells[row, 6].Value = emp.Email;
                            worksheet.Cells[row, 7].Value = emp.Metro;
                            worksheet.Cells[row, 8].Value = emp.HireDate?.ToShortDateString();
                            worksheet.Cells[row, 9].Value = emp.Experience;
                            worksheet.Cells[row, 10].Value = emp.Schedules;
                            worksheet.Cells[row, 11].Value = emp.Notes;
                            worksheet.Cells[row, 12].Value = emp.Comments;
                            row++;
                        }

                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                        FileInfo excelFile = new FileInfo(saveFileDialog.FileName);
                        package.SaveAs(excelFile);

                        MessageBox.Show($"Данные успешно экспортированы в файл:\n{saveFileDialog.FileName}", "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ObjectExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var objects = ObjectDataGrid.ItemsSource?.Cast<ObjectViewModel>().ToList();
                if (objects == null || !objects.Any())
                {
                    MessageBox.Show("Нет данных для экспорта", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    FileName = $"Объекты_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    DefaultExt = ".xlsx",
                    Title = "Выберите место для сохранения файла"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Объекты");

                        string[] headers = { "№", "Наименование обЪекта", "Индекс", "Страна", "Город", "Улица", "Дом",
                                  "Корпус", "Офис", "Полный Адрес" };

                        for (int i = 0; i < headers.Length; i++)
                        {
                            worksheet.Cells[1, i + 1].Value = headers[i];
                            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        }

                        int row = 2;
                        foreach (var ob in objects)
                        {
                            worksheet.Cells[row, 1].Value = ob.IdObject;
                            worksheet.Cells[row, 2].Value = ob.ObjectName;
                            worksheet.Cells[row, 3].Value = ob.PostalCode;
                            worksheet.Cells[row, 4].Value = ob.Country;
                            worksheet.Cells[row, 5].Value = ob.City;
                            worksheet.Cells[row, 6].Value = ob.Street;
                            worksheet.Cells[row, 7].Value = ob.Building;
                            worksheet.Cells[row, 8].Value = ob.Corpus;
                            worksheet.Cells[row, 9].Value = ob.Office;
                            worksheet.Cells[row, 10].Value = ob.FullAddress;
                            row++;
                        }

                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                        FileInfo excelFile = new FileInfo(saveFileDialog.FileName);
                        package.SaveAs(excelFile);

                        MessageBox.Show($"Данные успешно экспортированы в файл:\n{saveFileDialog.FileName}","Экспорт завершен",MessageBoxButton.OK,MessageBoxImage.Information);
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте данных: {ex.Message}","Ошибка",MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }

        private void RateExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var rate = RateDataGrid.ItemsSource?.Cast<RateViewModel>().ToList();
                if (rate == null || !rate.Any())
                {
                    MessageBox.Show("Нет данных для экспорта", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    FileName = $"Ставки_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    DefaultExt = ".xlsx",
                    Title = "Выберите место для сохранения файла"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Ставки");

                        string[] headers = { "№", "Наименование обЪекта", "Адрес", "Услуга", "Ставка" };

                        for (int i = 0; i < headers.Length; i++)
                        {
                            worksheet.Cells[1, i + 1].Value = headers[i];
                            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        }

                        int row = 2;
                        foreach (var r in rate)
                        {
                            worksheet.Cells[row, 1].Value = r.IdRate;
                            worksheet.Cells[row, 2].Value = r.ObjectName;
                            worksheet.Cells[row, 3].Value = r.Address;
                            worksheet.Cells[row, 4].Value = r.ServiceName;
                            worksheet.Cells[row, 5].Value = r.HourlyRate;
                            row++;
                        }

                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                        FileInfo excelFile = new FileInfo(saveFileDialog.FileName);
                        package.SaveAs(excelFile);

                        MessageBox.Show($"Данные успешно экспортированы в файл:\n{saveFileDialog.FileName}", "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте данных: {ex.Message}", "Ошибка", MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    Title = "Выберите файл для импорта сотрудников"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    using (var package = new ExcelPackage(new FileInfo(openFileDialog.FileName)))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        if (worksheet.Dimension == null) return;

                        var transaction = _context.Database.BeginTransaction();
                        try
                        {
                            int successCount = 0;
                            StringBuilder errors = new StringBuilder();

                            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                            {
                                try
                                {
                                    var lastName = worksheet.Cells[row, 2].Text?.Trim();
                                    var firstName = worksheet.Cells[row, 3].Text?.Trim();
                                    var phone = worksheet.Cells[row, 5].Text?.Trim();
                                    var email = worksheet.Cells[row, 6].Text?.Trim();

                                    var newFullname = new Fullname
                                    {
                                        LastName = lastName,
                                        FirstName = firstName,
                                        MiddleName = worksheet.Cells[row, 4].Text?.Trim()
                                    };

                                    var newEmployee = new Employee
                                    {
                                        IdFullnameNavigation = newFullname,
                                        Phone = phone,
                                        Email = email,
                                        Metro = worksheet.Cells[row, 7].Text?.Trim(),
                                        HireDate = DateOnly.TryParse(worksheet.Cells[row, 8].Text, out var date) ? date : null,
                                        Experience = worksheet.Cells[row, 9].Text?.Trim(),
                                        Schedules = worksheet.Cells[row, 10].Text?.Trim(),
                                        Notes = worksheet.Cells[row, 11].Text?.Trim(),
                                        Comments = worksheet.Cells[row, 12].Text?.Trim()
                                    };

                                    _context.Employees.Add(newEmployee);
                                    _context.SaveChanges();
                                    successCount++;
                                }
                                catch (Exception ex)
                                {
                                    errors.AppendLine($"Строка {row}: {ex.Message}");
                                }
                            }

                            transaction.Commit();

                            LoadEmployees();
                            LoadEmployeesPanel();

                            string message = $"Импортировано {successCount} сотрудников";
                            if (errors.Length > 0)
                                message += $"\nОшибки:\n{errors}";

                            MessageBox.Show(message, "Результат", MessageBoxButton.OK,
                                errors.Length > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка импорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ObjectImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    Title = "Выберите файл для импорта объектов"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    using (var package = new ExcelPackage(new FileInfo(openFileDialog.FileName)))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        if (worksheet.Dimension == null)
                        {
                            MessageBox.Show("Файл не содержит данных", "Ошибка",
                                         MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var transaction = _context.Database.BeginTransaction();
                        try
                        {
                            int successCount = 0;
                            StringBuilder errors = new StringBuilder();

                            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                            {
                                try
                                {
                                    var objectName = worksheet.Cells[row, 2].Text?.Trim();
                                    if (string.IsNullOrEmpty(objectName)) continue;

                                    if (_context.Objects.Any(o => o.ObjectName == objectName))
                                    {
                                        errors.AppendLine($"Строка {row}: Объект '{objectName}' уже существует");
                                        continue;
                                    }

                                    var newAddress = new Address
                                    {
                                        PostalCode = worksheet.Cells[row, 3].Text?.Trim(),
                                        Country = worksheet.Cells[row, 4].Text?.Trim(),
                                        City = worksheet.Cells[row, 5].Text?.Trim(),
                                        Street = worksheet.Cells[row, 6].Text?.Trim(),
                                        Building = worksheet.Cells[row, 7].Text?.Trim(),
                                        Corpus = worksheet.Cells[row, 8].Text?.Trim(),
                                        Office = worksheet.Cells[row, 9].Text?.Trim()
                                    };

                                    var newObject = new Models.Object
                                    {
                                        ObjectName = objectName,
                                        IdAddressNavigation = newAddress
                                    };

                                    _context.Objects.Add(newObject);
                                    successCount++;
                                }
                                catch (Exception ex)
                                {
                                    errors.AppendLine($"Строка {row}: {ex.Message}");
                                }
                            }

                            _context.SaveChanges();
                            transaction.Commit();

                            LoadObjects();
                            LoadRates(); 

                            string message = $"Успешно импортировано {successCount} объектов";
                            if (errors.Length > 0)
                                message += $"\nОшибки:\n{errors}";

                            MessageBox.Show(message, "Результат импорта", MessageBoxButton.OK,errors.Length > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка импорта объектов: {ex.Message}","Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportRates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    Title = "Выберите файл для импорта ставок"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    using (var package = new ExcelPackage(new FileInfo(openFileDialog.FileName)))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        if (worksheet.Dimension == null) return;

                        var transaction = _context.Database.BeginTransaction();
                        try
                        {
                            int successCount = 0;
                            StringBuilder errors = new StringBuilder();

                            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                            {
                                try
                                {
                                    var objectName = worksheet.Cells[row, 1].Text?.Trim();
                                    var serviceName = worksheet.Cells[row, 3].Text?.Trim();

                                    if (string.IsNullOrEmpty(objectName)) continue;

                                    var dbObject = _context.Objects.FirstOrDefault(o => o.ObjectName == objectName);
                                    var dbService = _context.Services.FirstOrDefault(s => s.ServiceName == serviceName);

                                    if (dbObject == null || dbService == null)
                                    {
                                        errors.AppendLine($"Строка {row}: Объект или услуга не найдены");
                                        continue;
                                    }

                                    var rate = new Rate
                                    {
                                        IdObject = dbObject.IdObject,
                                        IdService = dbService.IdService,
                                        HourlyRate = decimal.Parse(worksheet.Cells[row, 4].Text)
                                    };

                                    _context.Rates.Add(rate);
                                    successCount++;
                                }
                                catch (Exception ex)
                                {
                                    errors.AppendLine($"Строка {row}: {ex.Message}");
                                }
                            }

                            _context.SaveChanges();
                            transaction.Commit();

                            LoadRates();

                            string message = $"Импортировано {successCount} ставок";
                            if (errors.Length > 0)
                                message += $"\nОшибки:\n{errors}";

                            MessageBox.Show(message, "Результат", MessageBoxButton.OK, errors.Length > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка импорта ставок: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class ObjectComboBoxItem
        {
            public int IdObject { get; set; }
            public string ObjectName { get; set; }
        }

        private string GetExceptionDetails(Exception ex)
        {
            string details = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                details += $"\n{inner.Message}";
                inner = inner.InnerException;
            }
            return details;
        }

        private void ObjectComboBoxInGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is Models.Object selectedObject)
            {
                if (RateDataGrid.CurrentCell.Item is RateViewModel rate)
                {
                    rate.IdObject = selectedObject.IdObject;
                    rate.ObjectName = selectedObject.ObjectName;
                    rate.IdAddress = selectedObject.IdAddress;
                    rate.Address = $"{selectedObject.IdAddressNavigation?.PostalCode}, " +
                                  $"{selectedObject.IdAddressNavigation?.Country}, " +
                                  $"{selectedObject.IdAddressNavigation?.City}, " +
                                  $"{selectedObject.IdAddressNavigation?.Street}, " +
                                  $"{selectedObject.IdAddressNavigation?.Building}";
                }
            }
        }

        private void RateDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Column.Header.ToString() != "Объект" &&
                e.Column.Header.ToString() != "Услуга" &&
                e.Column.Header.ToString() != "Ставка")
            {
                e.Cancel = true;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text.ToLower();

            var filtered = _context.Employees
                .Where(emp =>
                    emp.IdFullnameNavigation.LastName.ToLower().Contains(searchText) ||
                    emp.IdFullnameNavigation.FirstName.ToLower().Contains(searchText) ||
                    (emp.IdFullnameNavigation.MiddleName != null && emp.IdFullnameNavigation.MiddleName.ToLower().Contains(searchText)) ||
                    emp.Phone.ToLower().Contains(searchText) ||
                    (emp.Email != null && emp.Email.ToLower().Contains(searchText)) ||
                    (emp.Metro != null && emp.Metro.ToLower().Contains(searchText)))
                .Select(e => new
                {
                    e.IdEmployee,
                    e.IdFullnameNavigation.LastName,
                    e.IdFullnameNavigation.FirstName,
                    e.IdFullnameNavigation.MiddleName,
                    e.Phone,
                    Email = e.Email,
                    e.Metro,
                    HireDate = e.HireDate.HasValue ? e.HireDate.Value.ToShortDateString() : ""
                })
                .ToList();

            EmployeeDataGrid.ItemsSource = filtered;
        }

        private void SearchObjectTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var searchText = SearchObjectTextBox.Text.ToLower();

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    LoadObjects(); 
                    return;
                }

                var filtered = _context.Objects
                    .Include(o => o.IdAddressNavigation)
                    .Where(o =>
                        o.ObjectName.ToLower().Contains(searchText) ||
                        (o.IdAddressNavigation.PostalCode != null && o.IdAddressNavigation.PostalCode.ToLower().Contains(searchText)) ||
                        (o.IdAddressNavigation.City != null && o.IdAddressNavigation.City.ToLower().Contains(searchText)) ||
                        (o.IdAddressNavigation.Street != null && o.IdAddressNavigation.Street.ToLower().Contains(searchText)))
                    .Select(o => new ObjectViewModel 
                    {
                        IdObject = o.IdObject,
                        ObjectName = o.ObjectName,
                        PostalCode = o.IdAddressNavigation.PostalCode,
                        Country = o.IdAddressNavigation.Country,
                        City = o.IdAddressNavigation.City,
                        Street = o.IdAddressNavigation.Street,
                        Building = o.IdAddressNavigation.Building,
                        Corpus = o.IdAddressNavigation.Corpus,
                        Office = o.IdAddressNavigation.Office,
                        FullAddress = $"{o.IdAddressNavigation.PostalCode}, {o.IdAddressNavigation.City}, {o.IdAddressNavigation.Street}, {o.IdAddressNavigation.Building}"
                    })
                    .ToList();

                ObjectDataGrid.ItemsSource = filtered;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске: {ex.Message}");
            }
        }

        private void SearchRateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchRateTextBox.Text.ToLower();

            var filtered = _context.Rates
                .Include(r => r.IdObjectNavigation)
                    .ThenInclude(o => o.IdAddressNavigation)
                .Include(r => r.IdServiceNavigation)
                .Where(r =>
                    r.IdObjectNavigation.ObjectName.ToLower().Contains(searchText) ||
                    r.IdServiceNavigation.ServiceName.ToLower().Contains(searchText) ||
                    r.HourlyRate.ToString().Contains(searchText) ||
                    (r.IdObjectNavigation.IdAddressNavigation.PostalCode != null &&
                     r.IdObjectNavigation.IdAddressNavigation.PostalCode.ToLower().Contains(searchText)) ||
                    (r.IdObjectNavigation.IdAddressNavigation.Country != null &&
                     r.IdObjectNavigation.IdAddressNavigation.Country.ToLower().Contains(searchText)) ||
                    (r.IdObjectNavigation.IdAddressNavigation.City != null &&
                     r.IdObjectNavigation.IdAddressNavigation.City.ToLower().Contains(searchText)) ||
                    (r.IdObjectNavigation.IdAddressNavigation.Street != null &&
                     r.IdObjectNavigation.IdAddressNavigation.Street.ToLower().Contains(searchText)) ||
                    (r.IdObjectNavigation.IdAddressNavigation.Building != null &&
                     r.IdObjectNavigation.IdAddressNavigation.Building.ToLower().Contains(searchText)))
                .Select(r => new RateViewModel
                {
                    IdRate = r.IdRate,
                    IdObject = r.IdObject,
                    IdAddress = r.IdObjectNavigation.IdAddress,
                    IdService = r.IdService,
                    ObjectName = r.IdObjectNavigation.ObjectName,
                    Address = $"{r.IdObjectNavigation.IdAddressNavigation.PostalCode}, " +
                             $"{r.IdObjectNavigation.IdAddressNavigation.Country}, " +
                             $"{r.IdObjectNavigation.IdAddressNavigation.City}, " +
                             $"{r.IdObjectNavigation.IdAddressNavigation.Street}, " +
                             $"{r.IdObjectNavigation.IdAddressNavigation.Building}",
                    ServiceName = r.IdServiceNavigation.ServiceName,
                    HourlyRate = r.HourlyRate
                })
                .ToList();

            RateDataGrid.ItemsSource = filtered;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
        }

        private void InformationButton_Click(object sender, RoutedEventArgs e)
        {
            var infoWindow = new Window
            {
                Title = "Справка по работе с программой",
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this 
            };

            var scrollViewer = new ScrollViewer();
            var textBlock = new TextBlock
            {
                Text = @"
                1. Основные функции:
                   - Добавление данных: нажмите кнопку 'Добавить' в соответствующем разделе
                   - Редактирование: выделите запись и нажмите 'Изменить'
                   - Удаление: выделите запись и нажмите 'Удалить'

                2. Особенности работы:
                   - Все изменения сохраняются автоматически
                   - Для поиска используйте поле фильтрации вверху таблицы
                   - Экспорт данных доступен через меню 'Файл' → 'Экспорт'

                3. Контакты для поддержки:
                   - Телефон: +7 (XXX) XXX-XX-XX
                   - Email: support@example.com
                ",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(15),
                FontSize = 14
            };

            scrollViewer.Content = textBlock;
            infoWindow.Content = scrollViewer;

            infoWindow.ShowDialog();
        }

    }
}
