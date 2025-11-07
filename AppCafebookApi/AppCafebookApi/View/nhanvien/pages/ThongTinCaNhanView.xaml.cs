using AppCafebookApi.Services;
using CafebookModel.Model.ModelApp.NhanVien;
using CafebookModel.Utils;
using Microsoft.Win32;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AppCafebookApi.View.nhanvien.pages
{
    public partial class ThongTinCaNhanView : Page
    {
        private NhanVienInfoDto? _currentNhanVien;
        private string? _newAvatarFilePath = null;

        public ThongTinCaNhanView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var response = await ApiClient.Instance.GetFromJsonAsync<ThongTinCaNhanViewDto>("api/app/nhanvien/thongtincanhan/me");
                if (response == null)
                {
                    MessageBox.Show("Không thể tải thông tin cá nhân.");
                    return;
                }

                _currentNhanVien = response.NhanVien;

                // Cột trái (Hiển thị)
                lblHoTen.Text = _currentNhanVien.HoTen;
                lblSoDienThoai.Text = _currentNhanVien.SoDienThoai;
                lblEmail.Text = _currentNhanVien.Email ?? "Chưa cập nhật";
                lblDiaChi.Text = _currentNhanVien.DiaChi ?? "Chưa cập nhật";

                // Cột phải (Panel Chỉnh sửa)
                txtEditHoTen.Text = _currentNhanVien.HoTen;
                txtEditSoDienThoai.Text = _currentNhanVien.SoDienThoai;
                txtEditEmail.Text = _currentNhanVien.Email;
                txtEditDiaChi.Text = _currentNhanVien.DiaChi;
                SetAvatar(_currentNhanVien.AnhDaiDien);

                // Bind thông báo lịch làm việc
                if (response.LichLamViecHomNay != null)
                {
                    lblThongBaoLich.Text = $"Hôm nay bạn có lịch làm việc:";
                    lblThoiGianCa.Text = $"{response.LichLamViecHomNay.TenCa} ({response.LichLamViecHomNay.GioBatDau:hh\\:mm} - {response.LichLamViecHomNay.GioKetThuc:hh\\:mm})";
                }
                else
                {
                    lblThongBaoLich.Text = "Hôm nay bạn không có ca làm việc.";
                    lblThoiGianCa.Text = "Hãy tận hưởng ngày nghỉ của mình!";
                }

                // SỬA: Bind dữ liệu vào panelLichSu
                lblSoLanNghi.Text = response.SoLanXinNghiThangNay.ToString();
                dgLichLamViec.ItemsSource = response.LichLamViecThangNay;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi API");
            }
        }

        private void SetAvatar(string? imageSource)
        {
            var bitmap = HinhAnhHelper.LoadImage(imageSource, HinhAnhPaths.DefaultAvatar);
            imgAvatar.Fill = new ImageBrush(bitmap);
        }

        #region Quản lý Panel

        private void ShowPanel(string panelName)
        {
            panelLichSu.Visibility = (panelName == "LichSu") ? Visibility.Visible : Visibility.Collapsed;
            panelChinhSua.Visibility = (panelName == "ChinhSua") ? Visibility.Visible : Visibility.Collapsed;
            panelDoiMatKhau.Visibility = (panelName == "DoiMatKhau") ? Visibility.Visible : Visibility.Collapsed;
            panelVietDon.Visibility = (panelName == "VietDon") ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnChinhSua_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel("ChinhSua");
        }

        private void BtnDoiMatKhau_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel("DoiMatKhau");
        }

        private void BtnVietDon_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel("VietDon");
        }

        #endregion

        #region Logic Panel Chỉnh Sửa

        private async void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            // (Giữ nguyên logic từ câu trả lời trước)
            if (_currentNhanVien == null) return;
            btnLuu.IsEnabled = false;
            using var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(txtEditHoTen.Text), nameof(CapNhatThongTinDto.HoTen));
            formData.Add(new StringContent(txtEditSoDienThoai.Text), nameof(CapNhatThongTinDto.SoDienThoai));
            formData.Add(new StringContent(txtEditEmail.Text ?? ""), nameof(CapNhatThongTinDto.Email));
            formData.Add(new StringContent(txtEditDiaChi.Text ?? ""), nameof(CapNhatThongTinDto.DiaChi));
            if (!string.IsNullOrEmpty(_newAvatarFilePath) && File.Exists(_newAvatarFilePath))
            {
                try
                {
                    byte[] fileBytes = File.ReadAllBytes(_newAvatarFilePath);
                    var fileContent = new ByteArrayContent(fileBytes);
                    string mimeType = Path.GetExtension(_newAvatarFilePath).ToLower() == ".png" ? "image/png" : "image/jpeg";
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
                    formData.Add(fileContent, "avatarFile", Path.GetFileName(_newAvatarFilePath));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể đọc file ảnh: {ex.Message}");
                    btnLuu.IsEnabled = true; return;
                }
            }
            try
            {
                var response = await ApiClient.Instance.PutAsync("api/app/nhanvien/thongtincanhan/update-info", formData);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Cập nhật thành công!");
                    _newAvatarFilePath = null;
                    await LoadDataAsync();
                    ShowPanel("LichSu");
                }
                else
                {
                    string loi = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Cập nhật thất bại: {loi}", "Lỗi");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi API");
            }
            btnLuu.IsEnabled = true;
        }

        private void BtnHuyChinhSua_Click(object sender, RoutedEventArgs e)
        {
            if (_currentNhanVien != null)
            {
                txtEditHoTen.Text = _currentNhanVien.HoTen;
                txtEditSoDienThoai.Text = _currentNhanVien.SoDienThoai;
                txtEditEmail.Text = _currentNhanVien.Email;
                txtEditDiaChi.Text = _currentNhanVien.DiaChi;
                SetAvatar(_currentNhanVien.AnhDaiDien);
            }
            _newAvatarFilePath = null;
            ShowPanel("LichSu");
        }

        private void BtnChonAnh_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                _newAvatarFilePath = filePath;
                SetAvatar(_newAvatarFilePath);
            }
        }

        #endregion

        #region Logic Panel Đổi Mật Khẩu

        private async void BtnXacNhanDoiMatKhau_Click(object sender, RoutedEventArgs e)
        {
            // (Giữ nguyên logic từ câu trả lời trước)
            string mkCu = txtMatKhauCu.Password;
            string mkMoi = txtMatKhauMoi.Password;
            string xacNhan = txtXacNhanMatKhau.Password;
            if (string.IsNullOrEmpty(mkCu) || string.IsNullOrEmpty(mkMoi))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ mật khẩu.", "Thiếu thông tin"); return;
            }
            if (mkMoi != xacNhan)
            {
                MessageBox.Show("Mật khẩu mới và xác nhận không khớp.", "Lỗi"); return;
            }
            var request = new DoiMatKhauRequestDto { MatKhauCu = mkCu, MatKhauMoi = mkMoi };
            btnXacNhanDoiMatKhau.IsEnabled = false;
            try
            {
                var response = await ApiClient.Instance.PostAsJsonAsync("api/app/nhanvien/thongtincanhan/change-password", request);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Đổi mật khẩu thành công!", "Thành công");
                    txtMatKhauCu.Password = "";
                    txtMatKhauMoi.Password = "";
                    txtXacNhanMatKhau.Password = "";
                    ShowPanel("LichSu");
                }
                else
                {
                    string loi = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi: {loi}", "Đổi mật khẩu thất bại");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi API");
            }
            btnXacNhanDoiMatKhau.IsEnabled = true;
        }

        private void BtnHuyDoiMatKhau_Click(object sender, RoutedEventArgs e)
        {
            txtMatKhauCu.Password = "";
            txtMatKhauMoi.Password = "";
            txtXacNhanMatKhau.Password = "";
            ShowPanel("LichSu");
        }

        #endregion

        #region Logic Panel Viết Đơn

        private async void BtnGuiDon_Click(object sender, RoutedEventArgs e)
        {
            // (Giữ nguyên logic từ câu trả lời trước)
            if (cmbLoaiDon.SelectedItem == null ||
                dpNgayBatDau.SelectedDate == null ||
                dpNgayKetThuc.SelectedDate == null ||
                string.IsNullOrEmpty(txtLyDo.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin.", "Thiếu thông tin"); return;
            }
            var ngayBatDau = dpNgayBatDau.SelectedDate.Value;
            var ngayKetThuc = dpNgayKetThuc.SelectedDate.Value;
            if (ngayKetThuc < ngayBatDau)
            {
                MessageBox.Show("Ngày kết thúc không thể trước ngày bắt đầu.", "Lỗi"); return;
            }
            var request = new DonXinNghiRequestDto
            {
                LoaiDon = (cmbLoaiDon.SelectedItem as ComboBoxItem).Content.ToString(),
                LyDo = txtLyDo.Text,
                NgayBatDau = ngayBatDau,
                NgayKetThuc = ngayKetThuc
            };
            btnGuiDon.IsEnabled = false;
            try
            {
                var response = await ApiClient.Instance.PostAsJsonAsync("api/app/nhanvien/thongtincanhan/submit-leave", request);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Gửi đơn thành công!", "Thành công");
                    cmbLoaiDon.SelectedIndex = 0;
                    dpNgayBatDau.SelectedDate = DateTime.Today;
                    dpNgayKetThuc.SelectedDate = DateTime.Today;
                    txtLyDo.Text = "";
                    await LoadDataAsync(); // Tải lại (để cập nhật số lần nghỉ)
                    ShowPanel("LichSu");
                }
                else
                {
                    string loi = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi: {loi}", "Gửi đơn thất bại");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi API");
            }
            btnGuiDon.IsEnabled = true;
        }

        private void BtnHuyGuiDon_Click(object sender, RoutedEventArgs e)
        {
            cmbLoaiDon.SelectedIndex = 0;
            dpNgayBatDau.SelectedDate = DateTime.Today;
            dpNgayKetThuc.SelectedDate = DateTime.Today;
            txtLyDo.Text = "";
            ShowPanel("LichSu");
        }

        #endregion
    }
}