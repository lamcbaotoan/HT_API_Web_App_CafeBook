using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Navigation;
using CafebookModel.Model.ModelApp;
using System.Threading.Tasks;
using System.ComponentModel; // Cần cho INotifyPropertyChanged
using System.Globalization; // Cần cho TimeSpan.ParseExact
using System.Net;

namespace AppCafebookApi.View.quanly.pages
{
    /// <summary>
    /// View-Model nội bộ cho CheckBox Nhân viên
    /// </summary>
    public class NhanVienCheckItem : INotifyPropertyChanged
    {
        public int IdNhanVien { get; set; }
        public string HoTen { get; set; } = string.Empty;

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
                }
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public partial class QuanLyLichLamViecView : Page
    {
        private static readonly HttpClient httpClient;
        private List<CaLamViecDto> _allCaMauList = new List<CaLamViecDto>();
        private List<NhanVienCheckItem> _allNhanVienList = new List<NhanVienCheckItem>();
        private CaLamViecDto? _selectedCaMau = null;

        static QuanLyLichLamViecView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public QuanLyLichLamViecView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            await LoadCaMauAsync();
            await LoadNhanVienAsync();
            ResetCaMauForm();

            calendarView.SelectedDate = DateTime.Today; // Tự động chọn hôm nay
            await LoadLichLamViecByDateAsync(DateTime.Today);

            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        #region === QUẢN LÝ CA MẪU (CỘT 1) ===

        private async Task LoadCaMauAsync()
        {
            try
            {
                _allCaMauList = (await httpClient.GetFromJsonAsync<List<CaLamViecDto>>("api/app/calamviec/all")) ?? new List<CaLamViecDto>();
                dgCaMau.ItemsSource = _allCaMauList;
                cmbCaDeGan.ItemsSource = _allCaMauList; // Cập nhật ComboBox
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách ca mẫu: {ex.Message}", "Lỗi API");
            }
        }

        private void ResetCaMauForm()
        {
            _selectedCaMau = null;
            dgCaMau.SelectedItem = null;
            txtTenCa.Text = "";
            txtGioBatDau.Text = "08:00";
            txtGioKetThuc.Text = "16:00";
            btnThemCa.IsEnabled = true;
            btnLuuCa.IsEnabled = false;
            btnXoaCa.IsEnabled = false;
        }

        private void DgCaMau_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgCaMau.SelectedItem is CaLamViecDto selected)
            {
                _selectedCaMau = selected;
                txtTenCa.Text = selected.TenCa;
                txtGioBatDau.Text = selected.GioBatDau.ToString(@"hh\:mm");
                txtGioKetThuc.Text = selected.GioKetThuc.ToString(@"hh\:mm");
                btnThemCa.IsEnabled = false;
                btnLuuCa.IsEnabled = true;
                btnXoaCa.IsEnabled = true;
            }
        }

        private void BtnLamMoiCa_Click(object sender, RoutedEventArgs e)
        {
            ResetCaMauForm();
        }

        private async void BtnThemCa_Click(object sender, RoutedEventArgs e)
        {
            await SaveCaMauAsync(isCreating: true);
        }

        private async void BtnLuuCa_Click(object sender, RoutedEventArgs e)
        {
            await SaveCaMauAsync(isCreating: false);
        }

        private async Task SaveCaMauAsync(bool isCreating)
        {
            if (string.IsNullOrWhiteSpace(txtTenCa.Text))
            {
                MessageBox.Show("Tên ca là bắt buộc.", "Lỗi"); return;
            }
            if (!TimeSpan.TryParseExact(txtGioBatDau.Text, @"hh\:mm", CultureInfo.InvariantCulture, out var gioBatDau) ||
                !TimeSpan.TryParseExact(txtGioKetThuc.Text, @"hh\:mm", CultureInfo.InvariantCulture, out var gioKetThuc))
            {
                MessageBox.Show("Giờ bắt đầu hoặc kết thúc không hợp lệ. Vui lòng dùng định dạng HH:mm (ví dụ: 08:00 hoặc 14:30).", "Lỗi định dạng");
                return;
            }

            var dto = new CaLamViecDto
            {
                TenCa = txtTenCa.Text,
                GioBatDau = gioBatDau,
                GioKetThuc = gioKetThuc
            };

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                HttpResponseMessage response;
                if (isCreating)
                {
                    response = await httpClient.PostAsJsonAsync("api/app/calamviec", dto);
                }
                else
                {
                    dto.IdCa = _selectedCaMau.IdCa;
                    response = await httpClient.PutAsJsonAsync($"api/app/calamviec/{dto.IdCa}", dto);
                }

                if (response.IsSuccessStatusCode)
                {
                    await LoadCaMauAsync();
                    ResetCaMauForm();
                }
                else
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Lỗi API");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnXoaCa_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCaMau == null) return;

            var result = MessageBox.Show($"Bạn có chắc muốn xóa ca '{_selectedCaMau.TenCa}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) return;

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.DeleteAsync($"api/app/calamviec/{_selectedCaMau.IdCa}");
                if (response.IsSuccessStatusCode)
                {
                    await LoadCaMauAsync();
                    ResetCaMauForm();
                }
                else
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Không thể xóa");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region === GÁN LỊCH (CỘT 2) ===

        private async Task LoadNhanVienAsync()
        {
            try
            {
                var dtos = (await httpClient.GetFromJsonAsync<List<NhanVienLookupDto>>("api/app/lichlamviec/all-nhanvien")) ?? new List<NhanVienLookupDto>();
                _allNhanVienList = dtos.Select(d => new NhanVienCheckItem
                {
                    IdNhanVien = d.IdNhanVien,
                    HoTen = d.HoTen,
                    IsChecked = false
                }).ToList();
                lbNhanVienDeGan.ItemsSource = _allNhanVienList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách nhân viên: {ex.Message}", "Lỗi API");
            }
        }

        private async void BtnGanLich_Click(object sender, RoutedEventArgs e)
        {
            if (dpNgayGanLich.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn ngày để gán lịch.", "Lỗi"); return;
            }
            if (cmbCaDeGan.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn ca làm việc mẫu.", "Lỗi"); return;
            }
            var selectedNhanVienIds = _allNhanVienList
                .Where(nv => nv.IsChecked)
                .Select(nv => nv.IdNhanVien)
                .ToList();
            if (!selectedNhanVienIds.Any())
            {
                MessageBox.Show("Vui lòng chọn ít nhất một nhân viên.", "Lỗi"); return;
            }

            var dto = new LichLamViecCreateDto
            {
                NgayGanLich = dpNgayGanLich.SelectedDate.Value,
                IdCa = (int)cmbCaDeGan.SelectedValue,
                DanhSachIdNhanVien = selectedNhanVienIds
            };

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/app/lichlamviec/assign", dto);

                // === SỬA LỖI CS1977: Dùng DTO cụ thể ===
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LichLamViecAssignResponseDto>();
                    if (result == null) throw new Exception("Không thể đọc phản hồi API.");

                    string message = result.Message;
                    var failures = result.Failures;

                    if (failures.Any())
                    {
                        message += "\n\nCác lỗi xảy ra:\n- " + string.Join("\n- ", failures);
                        MessageBox.Show(message, "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        MessageBox.Show(message, "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show($"Lỗi API: {await response.Content.ReadAsStringAsync()}", "Lỗi");
                }
                // === KẾT THÚC SỬA LỖI ===


                // Làm mới lịch nếu ngày gán = ngày đang xem
                if (calendarView.SelectedDate == dto.NgayGanLich.Date)
                {
                    await LoadLichLamViecByDateAsync(dto.NgayGanLich.Date);
                }
                // Bỏ check tất cả nhân viên
                foreach (var nv in _allNhanVienList) { nv.IsChecked = false; }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region === XEM LỊCH (CỘT 3) ===

        private async void CalendarView_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (calendarView.SelectedDate.HasValue)
            {
                await LoadLichLamViecByDateAsync(calendarView.SelectedDate.Value);
            }
        }

        private async Task LoadLichLamViecByDateAsync(DateTime date)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var data = (await httpClient.GetFromJsonAsync<List<LichLamViecDisplayDto>>($"api/app/lichlamviec/by-date?date={date:yyyy-MM-dd}")) ?? new List<LichLamViecDisplayDto>();

                // Nhóm theo Tên Ca (Ca Sáng, Ca Chiều, Nghỉ Phép)
                var cvs = new CollectionViewSource { Source = data };
                cvs.GroupDescriptions.Add(new PropertyGroupDescription("TenCa", new TenCaToNhomConverter()));

                lbLichLamViecHomNay.ItemsSource = cvs.View;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải lịch làm việc: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnXoaLich_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            int idLichLamViec = (int)button.Tag;

            var result = MessageBox.Show("Bạn có chắc muốn xóa lịch làm việc này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) return;

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.DeleteAsync($"api/app/lichlamviec/{idLichLamViec}");
                if (response.IsSuccessStatusCode)
                {
                    // Tải lại lịch
                    if (calendarView.SelectedDate.HasValue)
                    {
                        await LoadLichLamViecByDateAsync(calendarView.SelectedDate.Value);
                    }
                }
                else
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Không thể xóa");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        private void BtnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            // Quay lại trang QL Nhân Viên
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
            else
            {
                // Fallback nếu không CanGoBack
                this.NavigationService?.Navigate(new QuanLyNhanVienView());
            }
        }

        private void BtnGoToDonXinNghi_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new QuanLyDonXinNghiView());
        }
    }

    /// <summary>
    /// Converter để nhóm "Nghỉ Phép" vào một nhóm riêng
    /// </summary>
    public class TenCaToNhomConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string tenCa)
            {
                return string.IsNullOrEmpty(tenCa) ? "Nghỉ Phép / Khác" : tenCa;
            }
            return "Khác";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}