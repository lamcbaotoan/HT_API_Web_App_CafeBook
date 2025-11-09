// Tệp: AppCafebookApi/View/nhanvien/ManHinhNhanVien.xaml.cs

using AppCafebookApi.Services;
using AppCafebookApi.View.nhanvien.pages;
using CafebookModel.Utils;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CafebookModel.Model.ModelApp.NhanVien;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace AppCafebookApi.View.nhanvien
{
    public partial class ManHinhNhanVien : Window
    {
        private DispatcherTimer _sidebarTimer;
        public static string CurrentTrangThai { get; set; } = "KhongCoCa";

        public ManHinhNhanVien()
        {
            InitializeComponent();
            this.Loaded += ManHinhNhanVien_Loaded;

            _sidebarTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _sidebarTimer.Tick += SidebarTimer_Tick;
        }

        // *** SỬA HÀM NÀY ***
        private async void ManHinhNhanVien_Loaded(object sender, RoutedEventArgs e)
        {
            var currentUser = AuthService.CurrentUser;
            if (currentUser != null)
            {
                // 1. Cập nhật tên và vai trò
                txtUserName.Text = currentUser.HoTen;
                txtUserRole.Text = currentUser.TenVaiTro;

                // 2. Cập nhật Avatar
                AvatarBorder.Child = null;
                BitmapImage avatarImage = HinhAnhHelper.LoadImage(
                    currentUser.AnhDaiDien,
                    HinhAnhPaths.DefaultAvatar
                );
                AvatarBorder.Background = new ImageBrush(avatarImage)
                {
                    Stretch = Stretch.UniformToFill
                };

                // --- 3. PHÂN QUYỀN CHỨC NĂNG (Đã cập nhật) ---

                // Các chức năng chung (luôn hiển thị)
                btnSoDoBan.Visibility = Visibility.Visible;
                btnThongTinCaNhan.Visibility = Visibility.Visible;
                btnChamCong.Visibility = Visibility.Visible;
                btnLichLamViecCuaToi.Visibility = Visibility.Visible;
                btnPhieuLuongCuaToi.Visibility = Visibility.Visible;

                // Chức năng đặc thù (Phục vụ/Thu ngân)
                bool coQuyenOrder = AuthService.CoQuyen("BanHang.XemSoDo", "BanHang.ThanhToan");
                btnSoDoBan.Visibility = coQuyenOrder ? Visibility.Visible : Visibility.Collapsed;
                btnDatBan.Visibility = coQuyenOrder ? Visibility.Visible : Visibility.Collapsed;

                // *** SỬA LỖI: Kích hoạt nút Giao Hàng ***
                // (Giả định rằng Thu ngân/Quản lý có quyền xem)
                bool coQuyenGiaoHang = AuthService.CoQuyen("BanHang.ThanhToan", "GiaoHang.Xem");
                btnGiaoHang.Visibility = coQuyenGiaoHang ? Visibility.Visible : Visibility.Collapsed;
                // *** KẾT THÚC SỬA LỖI ***

                // Chức năng Thuê Sách (Thu ngân/Quản lý)
                bool coQuyenSach = AuthService.CoQuyen("Sach.QuanLy");
                btnThueSach.Visibility = coQuyenSach ? Visibility.Visible : Visibility.Collapsed;

                // (Thêm chức năng Chế biến - Giả định quyền là "CheBien.Xem")
                bool coQuyenCheBien = AuthService.CoQuyen("CheBien.Xem");
                btnCheBien.Visibility = coQuyenCheBien ? Visibility.Visible : Visibility.Collapsed;

                // Chọn trang mặc định
                if (btnSoDoBan.Visibility == Visibility.Visible)
                {
                    btnSoDoBan.IsChecked = true;
                    NavigateToPage(btnSoDoBan, new SoDoBanView());
                }
                else
                {
                    btnThongTinCaNhan.IsChecked = true;
                    NavigateToPage(btnThongTinCaNhan, new AppCafebookApi.View.nhanvien.pages.ThongTinCaNhanView());
                }

                if (AuthService.CurrentUser != null)
                {
                    await UpdateSidebarStatusAsync();
                    _sidebarTimer.Start();
                }
            }
        }

        private void UncheckOtherButtons(ToggleButton? exception)
        {
            // Danh sách này đã đúng
            var navButtons = new List<ToggleButton>
            {
                btnSoDoBan, btnCheBien, btnDatBan, btnGiaoHang, btnThueSach,
                btnThongTinCaNhan, btnChamCong, btnLichLamViecCuaToi, btnPhieuLuongCuaToi
            };

            foreach (var button in navButtons)
            {
                if (button != null && button != exception)
                {
                    button.IsChecked = false;
                }
            }
        }

        private void NavigateToPage(ToggleButton? clickedButton, Page pageInstance)
        {
            if (clickedButton == null) return;
            UncheckOtherButtons(clickedButton);
            clickedButton.IsChecked = true;
            MainFrame.Navigate(pageInstance);
        }

        // --- CÁC HÀM CLICK ĐIỀU HƯỚNG ---

        private void BtnSoDoBan_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(sender as ToggleButton, new SoDoBanView());
        }

        private void BtnDatBan_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(sender as ToggleButton, new DatBanView());
        }

        private void BtnGiaoHang_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(sender as ToggleButton, new GiaoHangView());
        }

        private void BtnCheBien_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(sender as ToggleButton, new CheBienView());
        }

        private void BtnThueSach_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(sender as ToggleButton, new ThueSachView());
        }

        private void BtnThongTinCaNhan_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(sender as ToggleButton, new AppCafebookApi.View.nhanvien.pages.ThongTinCaNhanView());
        }

        private void BtnChamCong_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(sender as ToggleButton, new ChamCongView());
        }

        private void BtnLichLamViecCuaToi_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(sender as ToggleButton, new LichLamViecView());
        }

        private void BtnPhieuLuongCuaToi_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(sender as ToggleButton, new PhieuLuongView());
        }

        // --- HÀM HELPER VÀ ĐĂNG XUẤT ---

        private void ResetToDefaultPage(object sender)
        {
            if (sender is ToggleButton btn)
            {
                btn.IsChecked = false;
                btnSoDoBan.IsChecked = true;
            }
        }

        private void BtnThongBao_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chức năng Thông báo đang được phát triển.");
        }

        private async void SidebarTimer_Tick(object? sender, EventArgs e)
        {
            await UpdateSidebarStatusAsync();
        }

        private async Task UpdateSidebarStatusAsync()
        {
            if (AuthService.CurrentUser == null || lblSidebarStatus == null) return;
            try
            {
                var status = await ApiClient.Instance.GetFromJsonAsync<ChamCongDashboardDto>("api/app/chamcong/status");
                if (status != null)
                {
                    CurrentTrangThai = status.TrangThai;

                    switch (status.TrangThai)
                    {
                        case "DaChamCong":
                            var duration = DateTime.Now - status.GioVao.Value;
                            lblSidebarStatus.Text = $"Đang làm ({duration:hh\\:mm})";
                            lblSidebarStatus.Foreground = Brushes.LightGreen;
                            break;
                        case "ChuaChamCong":
                            lblSidebarStatus.Text = "Chưa chấm công";
                            lblSidebarStatus.Foreground = Brushes.LightGray;
                            break;
                        case "NghiPhep":
                            lblSidebarStatus.Text = "Đang nghỉ phép";
                            lblSidebarStatus.Foreground = Brushes.LightBlue;
                            break;
                        case "KhongCoCa":
                            lblSidebarStatus.Text = "Không có ca";
                            lblSidebarStatus.Foreground = Brushes.Gray;
                            break;
                        default: // DaTraCa
                            lblSidebarStatus.Text = "Đã trả ca";
                            lblSidebarStatus.Foreground = Brushes.Gray;
                            break;
                    }
                }
            }
            catch (Exception)
            {
                lblSidebarStatus.Text = "Lỗi đồng bộ";
                lblSidebarStatus.Foreground = Brushes.OrangeRed;
            }
        }

        public static string GetCurrentCheckInStatus()
        {
            return CurrentTrangThai;
        }

        private void BtnDangXuat_Click(object sender, RoutedEventArgs e)
        {
            if (GetCurrentCheckInStatus() == "DaChamCong")
            {
                MessageBox.Show("Bạn chưa trả ca. Vui lòng nhấn \"TRẢ CA\" trước khi đăng xuất.",
                                "Cảnh báo chưa trả ca",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?",
                                         "Xác nhận đăng xuất",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                AuthService.Logout();
                ManHinhDangNhap loginWindow = new ManHinhDangNhap();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}