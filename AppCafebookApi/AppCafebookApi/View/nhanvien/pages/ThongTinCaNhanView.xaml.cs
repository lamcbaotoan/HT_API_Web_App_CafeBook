// File: ThongTinCaNhanView.xaml.cs
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
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

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
            ShowPanel("LichSu");
        }

        private async Task LoadDataAsync()
        {
            // (Hàm này giữ nguyên)
            try
            {
                var response = await ApiClient.Instance.GetFromJsonAsync<ThongTinCaNhanViewDto>("api/app/nhanvien/thongtincanhan/me");
                if (response == null)
                {
                    MessageBox.Show("Không thể tải thông tin cá nhân.");
                    return;
                }
                _currentNhanVien = response.NhanVien;
                lblHoTen.Text = _currentNhanVien.HoTen;
                lblSoDienThoai.Text = _currentNhanVien.SoDienThoai;
                lblEmail.Text = _currentNhanVien.Email ?? "Chưa cập nhật";
                lblDiaChi.Text = _currentNhanVien.DiaChi ?? "Chưa cập nhật";
                txtEditHoTen.Text = _currentNhanVien.HoTen;
                txtEditSoDienThoai.Text = _currentNhanVien.SoDienThoai;
                txtEditEmail.Text = _currentNhanVien.Email;
                txtEditDiaChi.Text = _currentNhanVien.DiaChi;
                SetAvatar(_currentNhanVien.AnhDaiDien);
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
            // (Hàm này giữ nguyên)
            var bitmap = HinhAnhHelper.LoadImage(imageSource, HinhAnhPaths.DefaultAvatar);
            imgAvatar.Fill = new ImageBrush(bitmap);
        }

        #region Quản lý Panel (Đã cập nhật để dùng TabControl)
        // (Vùng này giữ nguyên)
        private void UncheckOtherButtons(ToggleButton? exception)
        {
            if (btnLichSu != null && btnLichSu != exception)
                btnLichSu.IsChecked = false;
            if (btnChinhSua != null && btnChinhSua != exception)
                btnChinhSua.IsChecked = false;
            if (btnDoiMatKhau != null && btnDoiMatKhau != exception)
                btnDoiMatKhau.IsChecked = false;
            if (btnVietDon != null && btnVietDon != exception)
                btnVietDon.IsChecked = false;
        }
        private void ShowPanel(string panelName)
        {
            switch (panelName)
            {
                case "LichSu":
                    MainTabControl.SelectedItem = tabLichSu;
                    btnLichSu.IsChecked = true;
                    UncheckOtherButtons(btnLichSu);
                    break;
                case "ChinhSua":
                    MainTabControl.SelectedItem = tabChinhSua;
                    btnChinhSua.IsChecked = true;
                    UncheckOtherButtons(btnChinhSua);
                    break;
                case "DoiMatKhau":
                    MainTabControl.SelectedItem = tabDoiMatKhau;
                    btnDoiMatKhau.IsChecked = true;
                    UncheckOtherButtons(btnDoiMatKhau);
                    break;
                case "VietDon":
                    MainTabControl.SelectedItem = tabVietDon;
                    btnVietDon.IsChecked = true;
                    UncheckOtherButtons(btnVietDon);
                    break;
            }
        }
        private void BtnLichSu_Click(object sender, RoutedEventArgs e) { ShowPanel("LichSu"); }
        private void BtnChinhSua_Click(object sender, RoutedEventArgs e) { ShowPanel("ChinhSua"); }
        private void BtnDoiMatKhau_Click(object sender, RoutedEventArgs e) { ShowPanel("DoiMatKhau"); }
        private void BtnVietDon_Click(object sender, RoutedEventArgs e) { ShowPanel("VietDon"); }
        #endregion

        #region Logic Panel Chỉnh Sửa
        // (Vùng này giữ nguyên)
        private async void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
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
        // (Vùng này giữ nguyên, bao gồm logic "hiển thị mật khẩu")
        private async void BtnXacNhanDoiMatKhau_Click(object sender, RoutedEventArgs e)
        {
            string mkCu = chkShowPassword.IsChecked == true ? txtVisibleMatKhauCu.Text : txtMatKhauCu.Password;
            string mkMoi = chkShowPassword.IsChecked == true ? txtVisibleMatKhauMoi.Text : txtMatKhauMoi.Password;
            string xacNhan = chkShowPassword.IsChecked == true ? txtVisibleXacNhanMatKhau.Text : txtXacNhanMatKhau.Password;

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
                    ClearPasswordFields();
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
            ClearPasswordFields();
            ShowPanel("LichSu");
        }
        private void ClearPasswordFields()
        {
            txtMatKhauCu.Password = "";
            txtMatKhauMoi.Password = "";
            txtXacNhanMatKhau.Password = "";
            txtVisibleMatKhauCu.Text = "";
            txtVisibleMatKhauMoi.Text = "";
            txtVisibleXacNhanMatKhau.Text = "";
            if (chkShowPassword != null)
                chkShowPassword.IsChecked = false;
        }
        private void ChkShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            txtVisibleMatKhauCu.Text = txtMatKhauCu.Password;
            txtVisibleMatKhauMoi.Text = txtMatKhauMoi.Password;
            txtVisibleXacNhanMatKhau.Text = txtXacNhanMatKhau.Password;
            txtVisibleMatKhauCu.Visibility = Visibility.Visible;
            txtVisibleMatKhauMoi.Visibility = Visibility.Visible;
            txtVisibleXacNhanMatKhau.Visibility = Visibility.Visible;
            txtMatKhauCu.Visibility = Visibility.Collapsed;
            txtMatKhauMoi.Visibility = Visibility.Collapsed;
            txtXacNhanMatKhau.Visibility = Visibility.Collapsed;
        }
        private void ChkShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            txtMatKhauCu.Password = txtVisibleMatKhauCu.Text;
            txtMatKhauMoi.Password = txtVisibleMatKhauMoi.Text;
            txtXacNhanMatKhau.Password = txtVisibleXacNhanMatKhau.Text;
            txtVisibleMatKhauCu.Visibility = Visibility.Collapsed;
            txtVisibleMatKhauMoi.Visibility = Visibility.Collapsed;
            txtVisibleXacNhanMatKhau.Visibility = Visibility.Collapsed;
            txtMatKhauCu.Visibility = Visibility.Visible;
            txtMatKhauMoi.Visibility = Visibility.Visible;
            txtXacNhanMatKhau.Visibility = Visibility.Visible;
        }
        private void TxtMatKhauCu_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (chkShowPassword.IsChecked == true)
                txtVisibleMatKhauCu.Text = txtMatKhauCu.Password;
        }
        private void TxtVisibleMatKhauCu_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (chkShowPassword.IsChecked == false)
                txtMatKhauCu.Password = txtVisibleMatKhauCu.Text;
        }
        private void TxtMatKhauMoi_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (chkShowPassword.IsChecked == true)
                txtVisibleMatKhauMoi.Text = txtMatKhauMoi.Password;
        }
        private void TxtVisibleMatKhauMoi_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (chkShowPassword.IsChecked == false)
                txtMatKhauMoi.Password = txtVisibleMatKhauMoi.Text;
        }
        private void TxtXacNhanMatKhau_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (chkShowPassword.IsChecked == true)
                txtVisibleXacNhanMatKhau.Text = txtXacNhanMatKhau.Password;
        }
        private void TxtVisibleXacNhanMatKhau_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (chkShowPassword.IsChecked == false)
                txtXacNhanMatKhau.Password = txtVisibleXacNhanMatKhau.Text;
        }
        #endregion

        #region Logic Panel Viết Đơn (Đã cập nhật)

        private async void BtnGuiDon_Click(object sender, RoutedEventArgs e)
        {
            // SỬA: Xóa kiểm tra cmbLoaiDon
            if (dpNgayBatDau.SelectedDate == null ||
                dpNgayKetThuc.SelectedDate == null ||
                string.IsNullOrEmpty(txtLyDo.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin (Ngày bắt đầu, kết thúc và lý do).", "Thiếu thông tin"); return;
            }
            var ngayBatDau = dpNgayBatDau.SelectedDate.Value;
            var ngayKetThuc = dpNgayKetThuc.SelectedDate.Value;
            if (ngayKetThuc < ngayBatDau)
            {
                MessageBox.Show("Ngày kết thúc không thể trước ngày bắt đầu.", "Lỗi"); return;
            }

            // SỬA: Gán "LoaiDon" là một giá trị chung
            var request = new DonXinNghiRequestDto
            {
                LoaiDon = "Đơn xin nghỉ", // Giá trị chung
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
                    // SỬA: Xóa cmbLoaiDon
                    dpNgayBatDau.SelectedDate = DateTime.Today;
                    dpNgayKetThuc.SelectedDate = DateTime.Today;
                    txtLyDo.Text = "";
                    await LoadDataAsync();
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
            // SỬA: Xóa cmbLoaiDon
            dpNgayBatDau.SelectedDate = DateTime.Today;
            dpNgayKetThuc.SelectedDate = DateTime.Today;
            txtLyDo.Text = "";
            ShowPanel("LichSu");
        }
        #endregion
    }
}