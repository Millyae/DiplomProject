using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DiplomProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using OfficeOpenXml;

namespace DiplomProject
{
    public partial class InformationWindow : Window
    {
        private readonly diplomContext _context;
        private readonly int _employeeId;
        private Employee _currentEmployee;
        private WorkScheduleDisplay _selectedSchedule;

        public InformationWindow(int employeeId)
        {
            InitializeComponent();
            _context = new diplomContext();
            _employeeId = employeeId;

            LoadEmployeeData();
            LoadComboBoxData();
            LoadWorkScheduleData();
            UpdateSummaryInfo();

            WorkHoursDataGrid.SelectionChanged += WorkHoursDataGrid_SelectionChanged;
        }

        private void UpdateSummaryInfo()
        {
            var schedules = _context.WorkSchedules
                .Where(ws => ws.IdEmployee == _employeeId)
                .ToList();

            decimal totalHours = schedules.Sum(ws => ws.DailyHours ?? 0m);
            int totalDays = schedules.Select(ws => ws.WorkDate).Distinct().Count();

            decimal salary = schedules.Sum(ws =>
            (ws.DailyHours ?? 0m) * (ws.IdRateNavigation?.HourlyRate ?? 0m));

            SummaryTextBlock.Text = $"Всего дней: {totalDays} | Всего часов: {totalHours} | Зарплата: {salary:C}";
        }


        private void WorkHoursDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedSchedule = WorkHoursDataGrid.SelectedItem as WorkScheduleDisplay;
            if (_selectedSchedule != null)
            {
                ObjectComboBox.SelectedValue = _selectedSchedule.ObjectId;
                AddressComboBox.SelectedValue = _selectedSchedule.AddressId;
                ServiceComboBox.SelectedValue = _selectedSchedule.ServiceId;
                WorkDatePicker.SelectedDate = _selectedSchedule.WorkDate?.ToDateTime(TimeOnly.MinValue);
                StartTimeTextBox.Text = _selectedSchedule.StartTime?.ToString(@"hh\:mm");
                EndTimeTextBox.Text = _selectedSchedule.EndTime?.ToString(@"hh\:mm");
                NotesTextBox.Text = _selectedSchedule.Notes;

                AddButton.Content = "Сохранить";
                DeleteButton.Visibility = Visibility.Visible;
            }
        }

        public void LoadEmployeeData()
        {
            _currentEmployee = _context.Employees
                .Include(e => e.IdFullnameNavigation)
                .FirstOrDefault(e => e.IdEmployee == _employeeId);

            if (_currentEmployee != null)
            {
                FullNameTextBlock.Text = $"{_currentEmployee.IdFullnameNavigation.LastName} {_currentEmployee.IdFullnameNavigation.FirstName} {_currentEmployee.IdFullnameNavigation.MiddleName}";
                PhoneTextBlock.Text = $"Телефон: {_currentEmployee.Phone}";
                EmailTextBlock.Text = $"Email: {_currentEmployee.Email}";
            }
        }

        public void LoadComboBoxData()
        {
            ObjectComboBox.ItemsSource = _context.Objects.ToList();
            ObjectComboBox.DisplayMemberPath = "ObjectName";
            ObjectComboBox.SelectedValuePath = "IdObject";

            ServiceComboBox.ItemsSource = _context.Services.ToList();
            ServiceComboBox.DisplayMemberPath = "ServiceName";
            ServiceComboBox.SelectedValuePath = "IdService";

            WorkDatePicker.SelectedDate = DateTime.Today;
        }

        public void LoadWorkScheduleData()
        {
            var schedules = _context.WorkSchedules
                .Include(ws => ws.IdRateNavigation)
                .ThenInclude(r => r.IdObjectNavigation)
                .ThenInclude(o => o.IdAddressNavigation)
                .Include(ws => ws.IdRateNavigation)
                .ThenInclude(ws => ws.IdServiceNavigation)
                .Where(ws => ws.IdEmployee == _employeeId)
                .OrderByDescending(ws => ws.WorkDate)
                .ThenByDescending(ws => ws.RecordCreated)
                .ToList();

            var displayData = schedules.Select(ws => new WorkScheduleDisplay
            {
                ObjectName = ws.IdRateNavigation?.IdObjectNavigation?.ObjectName,
                Address = GetFullAddress(ws.IdRateNavigation?.IdObjectNavigation?.IdAddressNavigation),
                ServiceName = ws.IdRateNavigation?.IdServiceNavigation?.ServiceName,
                HoursWorked = ws.DailyHours ?? 0m,
                Notes = ws.Notes,
                RecordCreated = ws.RecordCreated ?? DateTime.MinValue,
                WorkDate = ws.WorkDate,
                StartTime = ws.StartTime,
                EndTime = ws.EndTime,
                ScheduleId = ws.IdSchedule,
                ObjectId = ws.IdRateNavigation?.IdObject,
                AddressId = ws.IdRateNavigation?.IdObjectNavigation?.IdAddress,
                ServiceId = ws.IdRateNavigation?.IdService
            }).ToList();

            WorkHoursDataGrid.ItemsSource = displayData;
            UpdateSummaryInfo();
        }

        private static string GetFullAddress(Address address)
        {
            if (address == null) return string.Empty;

            var parts = new List<string>
            {
                address.Country,
                address.PostalCode,
                address.City,
                address.Street,
                address.Building
            };

            if (!string.IsNullOrEmpty(address.Corpus))
                parts.Add($"корп. {address.Corpus}");

            if (!string.IsNullOrEmpty(address.Office))
                parts.Add($"оф. {address.Office}");

            return string.Join(", ", parts.Where(p => !string.IsNullOrEmpty(p)));
        }

        private void ObjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ObjectComboBox.SelectedItem is Models.Object selectedObject)
            {
                AddressComboBox.ItemsSource = _context.Objects
                    .Include(o => o.IdAddressNavigation)
                    .Where(o => o.IdObject == selectedObject.IdObject)
                    .Select(o => new
                    {
                        IdAddress = o.IdAddress,
                        FullAddress = GetFullAddress(o.IdAddressNavigation)
                    })
                    .ToList();
                AddressComboBox.DisplayMemberPath = "FullAddress";
                AddressComboBox.SelectedValuePath = "IdAddress";

                var rates = _context.Rates
                    .Include(r => r.IdServiceNavigation)
                    .Where(r => r.IdObject == selectedObject.IdObject)
                    .ToList();

                ServiceComboBox.ItemsSource = rates
                    .Select(r => r.IdServiceNavigation)
                    .Distinct()
                    .ToList();
            }
            else
            {
                AddressComboBox.ItemsSource = null;
                ServiceComboBox.ItemsSource = _context.Services.ToList();
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInputs()) return;

                var selectedObject = (Models.Object)ObjectComboBox.SelectedItem;
                var selectedService = (Service)ServiceComboBox.SelectedItem;

                var rate = _context.Rates.FirstOrDefault(r => r.IdObject == selectedObject.IdObject &&
                r.IdService == selectedService.IdService);

                if (rate == null)
                {
                    MessageBox.Show("Не найдена ставка для выбранной комбинации объекта и должности", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                DateOnly workDate = DateOnly.FromDateTime(WorkDatePicker.SelectedDate.Value);
                TimeOnly startTime = TimeOnly.Parse(StartTimeTextBox.Text);
                TimeOnly endTime = TimeOnly.Parse(EndTimeTextBox.Text);

                if (HasTimeConflict(_employeeId, workDate, startTime, endTime, _selectedSchedule?.ScheduleId))
                {
                    MessageBox.Show("Сотрудник уже записан на это время в выбранную дату. Выберите другое время.", "Конфликт времени", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal hoursWorked = (decimal)(endTime - startTime).TotalHours;

                if (_selectedSchedule == null)
                {
                    var newSchedule = new WorkSchedule
                    {
                        IdEmployee = _employeeId,
                        IdRate = rate.IdRate,
                        WorkDate = workDate,
                        StartTime = startTime,
                        EndTime = endTime,
                        DailyHours = hoursWorked,
                        Notes = NotesTextBox.Text,
                        RecordCreated = DateTime.Now,
                    };

                    _context.WorkSchedules.Add(newSchedule);
                    _context.SaveChanges();
                    StatusTextBlock.Text = "Запись успешно добавлена";
                }
                else
                {
                    var scheduleToUpdate = _context.WorkSchedules
                        .FirstOrDefault(ws => ws.IdSchedule == _selectedSchedule.ScheduleId);

                    if (scheduleToUpdate != null)
                    {
                        scheduleToUpdate.IdRate = rate.IdRate;
                        scheduleToUpdate.DailyHours = hoursWorked;
                        scheduleToUpdate.Notes = NotesTextBox.Text;
                        scheduleToUpdate.WorkDate = workDate;
                        scheduleToUpdate.StartTime = startTime;
                        scheduleToUpdate.EndTime = endTime;

                        _context.SaveChanges();
                        StatusTextBlock.Text = "Запись успешно обновлена";
                    }
                }

                LoadWorkScheduleData();
                ClearInputFields();
                ResetEditMode();
            }
            catch (DbUpdateException ex)
            {
                // Получение внутреннего исключения
                var innerException = ex.InnerException;
                while (innerException != null)
                {
                    MessageBox.Show($"Ошибка при сохранении: {innerException.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    innerException = innerException.InnerException;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public bool HasTimeConflict(int employeeId, DateOnly workDate, TimeOnly newStartTime, TimeOnly newEndTime, int? excludedScheduleId = null)
        {
            var existingSchedules = _context.WorkSchedules
                .Where(ws => ws.IdEmployee == employeeId &&
                            ws.WorkDate == workDate &&
                            (excludedScheduleId == null || ws.IdSchedule != excludedScheduleId))
                .ToList();

            foreach (var schedule in existingSchedules)
            {
                TimeOnly existingStart = schedule.StartTime ?? TimeOnly.MinValue;
                TimeOnly existingEnd = schedule.EndTime ?? TimeOnly.MaxValue;

                if (newStartTime < existingEnd && newEndTime > existingStart)
                {
                    return true; 
                }
            }

            return false; 
        }

        public bool ValidateInputs()
        {
            if (ObjectComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите объект", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (AddressComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите адрес", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (ServiceComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите должность", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!WorkDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату работы", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!TimeSpan.TryParse(StartTimeTextBox.Text, out var startTime))
            {
                MessageBox.Show("Некорректный формат времени начала", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!TimeSpan.TryParse(EndTimeTextBox.Text, out var endTime))
            {
                MessageBox.Show("Некорректный формат времени окончания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (endTime <= startTime)
            {
                MessageBox.Show("Время окончания должно быть позже времени начала", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSchedule == null) return;

            var result = MessageBox.Show("Вы уверены, что хотите удалить эту запись?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var scheduleToDelete = _context.WorkSchedules
                        .FirstOrDefault(ws => ws.IdSchedule == _selectedSchedule.ScheduleId);

                    if (scheduleToDelete != null)
                    {
                        _context.WorkSchedules.Remove(scheduleToDelete);
                        _context.SaveChanges();
                        StatusTextBlock.Text = "Запись успешно удалена";
                        LoadWorkScheduleData();
                        ClearInputFields();
                        ResetEditMode();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении записи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearInputFields()
        {
            NotesTextBox.Text = string.Empty;
            StartTimeTextBox.Text = "09:00";
            EndTimeTextBox.Text = "18:00";
        }

        private void ResetEditMode()
        {
            _selectedSchedule = null;
            AddButton.Content = "Добавить";
            DeleteButton.Visibility = Visibility.Collapsed;
            WorkHoursDataGrid.SelectedItem = null;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _context?.Dispose();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var schedules = WorkHoursDataGrid.ItemsSource.Cast<WorkScheduleDisplay>().ToList();
                if (schedules == null || !schedules.Any())
                {
                    MessageBox.Show("Нет данных графика работы для экспорта", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    FileName = $"ГрафикРаботы_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    DefaultExt = ".xlsx",
                    Title = "Выберите место для сохранения файла"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("График работы");

                        string[] headers = {
                            "№", "Объект", "Адрес", "Должность",
                            "Дата работы", "Начало", "Окончание",
                            "Часы", "Примечания", "Дата создания записи"
                        };

                        for (int i = 0; i < headers.Length; i++)
                        {
                            worksheet.Cells[1, i + 1].Value = headers[i];
                            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        }

                        int row = 2;
                        foreach (var schedule in schedules)
                        {
                            worksheet.Cells[row, 1].Value = schedule.ScheduleId;
                            worksheet.Cells[row, 2].Value = schedule.ObjectName;
                            worksheet.Cells[row, 3].Value = schedule.Address;
                            worksheet.Cells[row, 4].Value = schedule.ServiceName;
                            worksheet.Cells[row, 5].Value = schedule.WorkDate?.ToShortDateString();
                            worksheet.Cells[row, 6].Value = schedule.StartTime?.ToString(@"hh\:mm");
                            worksheet.Cells[row, 7].Value = schedule.EndTime?.ToString(@"hh\:mm");
                            worksheet.Cells[row, 8].Value = schedule.HoursWorked;
                            worksheet.Cells[row, 9].Value = schedule.Notes;
                            worksheet.Cells[row, 10].Value = schedule.RecordCreated.ToString("g");
                            row++;
                        }

                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                        FileInfo excelFile = new FileInfo(saveFileDialog.FileName);
                        package.SaveAs(excelFile);

                        MessageBox.Show($"График работы успешно экспортирован в файл:\n{saveFileDialog.FileName}", "Экспорт завершен",MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте графика работы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

/*using DiplomProject;
using DiplomProject.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Linq;

namespace TestProject
{
    [TestFixture]
    public class InformationWindowTests
    {
        private diplomContext _context;
        private InformationWindow _window;
        private int _testEmployeeId;

        [SetUp]
        public void Setup()
        {
            // Используем InMemory базу данных для тестов
            var options = new DbContextOptionsBuilder<diplomContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new diplomContext(options);
            SeedTestData();

            _testEmployeeId = _context.Employees.First().IdEmployee;
            _window = new InformationWindow(_testEmployeeId);
        }

        private void SeedTestData()
        {
            // Добавляем тестовые данные
            var employee = new Employee
            {
                IdFullnameNavigation = new Fullname
                {
                    LastName = "Test",
                    FirstName = "User",
                    MiddleName = "Middle"
                },
                Phone = "1234567890",
                Email = "test@example.com"
            };
            _context.Employees.Add(employee);

            var service = new Service { ServiceName = "Test Service" };
            _context.Services.Add(service);

            var address = new Address
            {
                Country = "Russia",
                City = "Moscow",
                Street = "Test Street",
                Building = "1"
            };
            _context.Addresses.Add(address);

            var obj = new DiplomProject.Models.Object
            {
                ObjectName = "Test Object",
                IdAddressNavigation = address
            };
            _context.Objects.Add(obj);

            var rate = new Rate
            {
                IdObject = obj.IdObject,
                IdService = service.IdService,
                HourlyRate = 100
            };
            _context.Rates.Add(rate);

            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _window.Close();
            _context.Dispose();
        }

        [Test]
        public void LoadEmployeeData_ShouldDisplayCorrectEmployeeInfo()
        {
            // Act
            _window.LoadEmployeeData();

            // Assert
            Assert.That(_window.FullNameTextBlock.Text, Is.EqualTo("Test User Middle"));
            Assert.That(_window.PhoneTextBlock.Text, Is.EqualTo("Телефон: 1234567890"));
            Assert.That(_window.EmailTextBlock.Text, Is.EqualTo("Email: test@example.com"));
        }

        [Test]
        public void LoadComboBoxData_ShouldPopulateComboBoxes()
        {
            // Act
            _window.LoadComboBoxData();

            // Assert
            Assert.That(_window.ObjectComboBox.Items.Count, Is.GreaterThan(0));
            Assert.That(_window.ServiceComboBox.Items.Count, Is.GreaterThan(0));
            Assert.That(_window.WorkDatePicker.SelectedDate, Is.EqualTo(DateTime.Today));
        }

        [Test]
        public void ValidateInputs_WithValidData_ReturnsTrue()
        {
            // Arrange
            _window.ObjectComboBox.SelectedIndex = 0;
            _window.ServiceComboBox.SelectedIndex = 0;
            _window.WorkDatePicker.SelectedDate = DateTime.Today;
            _window.StartTimeTextBox.Text = "09:00";
            _window.EndTimeTextBox.Text = "18:00";

            // Act
            var result = _window.ValidateInputs();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidateInputs_WithMissingObject_ReturnsFalse()
        {
            // Arrange
            _window.ObjectComboBox.SelectedItem = null;
            _window.ServiceComboBox.SelectedIndex = 0;
            _window.WorkDatePicker.SelectedDate = DateTime.Today;
            _window.StartTimeTextBox.Text = "09:00";
            _window.EndTimeTextBox.Text = "18:00";

            // Act
            var result = _window.ValidateInputs();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void HasTimeConflict_WithConflictingTime_ReturnsTrue()
        {
            // Arrange
            var existingSchedule = new WorkSchedule
            {
                IdEmployee = _testEmployeeId,
                IdRate = _context.Rates.First().IdRate,
                WorkDate = DateOnly.FromDateTime(DateTime.Today),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(12, 0),
                DailyHours = 2
            };
            _context.WorkSchedules.Add(existingSchedule);
            _context.SaveChanges();

            var testDate = DateOnly.FromDateTime(DateTime.Today);
            var newStartTime = new TimeOnly(11, 0);
            var newEndTime = new TimeOnly(13, 0);

            // Act
            var result = _window.HasTimeConflict(_testEmployeeId, testDate, newStartTime, newEndTime);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void UpdateSummaryInfo_ShouldCalculateCorrectTotals()
        {
            // Arrange
            var schedule1 = new WorkSchedule
            {
                IdEmployee = _testEmployeeId,
                IdRate = _context.Rates.First().IdRate,
                WorkDate = DateOnly.FromDateTime(DateTime.Today),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(13, 0),
                DailyHours = 4
            };

            var schedule2 = new WorkSchedule
            {
                IdEmployee = _testEmployeeId,
                IdRate = _context.Rates.First().IdRate,
                WorkDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(18, 0),
                DailyHours = 8
            };

            _context.WorkSchedules.AddRange(schedule1, schedule2);
            _context.SaveChanges();

            // Act
            _window.LoadWorkScheduleData();
            _window.UpdateSummaryInfo();

            // Assert
            Assert.That(_window.SummaryTextBlock.Text,
                Is.EqualTo($"Всего дней: 2 | Всего часов: 12 | Зарплата: {1200:C}"));
        }

        [Test]
        public void GetFullAddress_WithCompleteAddress_ReturnsFormattedString()
        {
            // Arrange
            var address = new Address
            {
                Country = "Russia",
                PostalCode = "123456",
                City = "Moscow",
                Street = "Main",
                Building = "1",
                Corpus = "A",
                Office = "101"
            };

            // Act
            var result = InformationWindow.GetFullAddress(address);

            // Assert
            Assert.That(result, Is.EqualTo("Russia, 123456, Moscow, Main, 1, корп. A, оф. 101"));
        }
    }
}*/