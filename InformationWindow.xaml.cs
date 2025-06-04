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
        private string _currentUserRole;
        private readonly diplomContext _context;
        private readonly int _employeeId;
        private Employee _currentEmployee;
        private WorkScheduleDisplay _selectedSchedule;


        public InformationWindow(int employeeId, string userRoles)
        {
            InitializeComponent();
            _context = new diplomContext();
            _employeeId = employeeId;

            LoadEmployeeData();
            LoadComboBoxData();
            LoadWorkScheduleData();
            UpdateSummaryInfo();
            SetPermissionsBasedOnRole();
            _currentUserRole = userRoles;

            WorkHoursDataGrid.SelectionChanged += WorkHoursDataGrid_SelectionChanged;
        }



        private void SetPermissionsBasedOnRole()
        {
            if (_currentUserRole == "Manager")
            {
                DeleteButton.Visibility = Visibility.Collapsed;
            }
            else if (_currentUserRole == "Admin")
            {
                DeleteButton.Visibility = Visibility.Visible;
            }
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

        internal void LoadEmployeeData()
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

        internal bool ValidateInputs()
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
            if (_currentUserRole != "Admin")
            {
                MessageBox.Show("У вас нет прав для выполнения этого действия", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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