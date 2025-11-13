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

        // Hàm Load màn hình chính của nhân viên (đã mở rộng: hỗ trợ vai trò "Cửa Hàng Trưởng" = full quyền)
        private async void ManHinhNhanVien_Loaded(object sender, RoutedEventArgs e)
        {
            var currentUser = AuthService.CurrentUser;
            if (currentUser == null) return;

            // 1. Cập nhật tên và vai trò
            txtUserName.Text = currentUser.HoTen ?? string.Empty;
            txtUserRole.Text = currentUser.TenVaiTro ?? string.Empty;

            // 2. Cập nhật Avatar
            try
            {
                AvatarBorder.Child = null;
                BitmapImage avatarImage = HinhAnhHelper.LoadImage(
                    currentUser.AnhDaiDien,
                    HinhAnhPaths.DefaultAvatar
                );
                AvatarBorder.Background = new ImageBrush(avatarImage)
                {
                    Stretch = Stretch.UniformToFill
                };
            }
            catch
            {
                // Nếu lỗi load ảnh, bỏ qua để tránh crash
            }

            // --- 3. PHÂN QUYỀN CHỨC NĂNG (Cập nhật) ---
            // Nếu là "Cửa Hàng Trưởng" => full quyền (hiển thị tất cả chức năng)
            bool isFullRole = string.Equals(currentUser.TenVaiTro?.Trim(),
                                            "Cửa Hàng Trưởng",
                                            StringComparison.OrdinalIgnoreCase);

            // Nút chung (luôn hiển thị)
            btnThongTinCaNhan.Visibility = Visibility.Visible;
            btnChamCong.Visibility = Visibility.Visible;
            btnLichLamViecCuaToi.Visibility = Visibility.Visible;
            btnPhieuLuongCuaToi.Visibility = Visibility.Visible;

            // Danh sách các nút điều hướng có trong sidebar (để dễ quản lý)
            var navButtons = new List<ToggleButton>
            {
                btnSoDoBan, btnCheBien, btnDatBan, btnGiaoHang, btnThueSach,
                btnThongTinCaNhan, btnChamCong, btnLichLamViecCuaToi, btnPhieuLuongCuaToi
            };

            // Nếu full role thì bật tất cả
            if (isFullRole)
            {
                foreach (var btn in navButtons)
                {
                    if (btn != null)
                        btn.Visibility = Visibility.Visible;
                }
            }
            else
            {
                // Giữ lại logic phân quyền hiện có cho các vai trò khác

                // Các chức năng đặc thù (Phục vụ/Thu ngân)
                bool coQuyenOrder = AuthService.CoQuyen("BanHang.XemSoDo", "BanHang.ThanhToan");
                if (btnSoDoBan != null) btnSoDoBan.Visibility = coQuyenOrder ? Visibility.Visible : Visibility.Collapsed;
                if (btnDatBan != null) btnDatBan.Visibility = coQuyenOrder ? Visibility.Visible : Visibility.Collapsed;

                // Giao hàng (ví dụ: Thu ngân/Quản lý)
                bool coQuyenGiaoHang = AuthService.CoQuyen("BanHang.ThanhToan", "GiaoHang.Xem");
                if (btnGiaoHang != null) btnGiaoHang.Visibility = coQuyenGiaoHang ? Visibility.Visible : Visibility.Collapsed;

                // Thuê sách (Thu ngân/Quản lý)
                bool coQuyenSach = AuthService.CoQuyen("Sach.QuanLy");
                if (btnThueSach != null) btnThueSach.Visibility = coQuyenSach ? Visibility.Visible : Visibility.Collapsed;

                // Chế biến (ví dụ quyền: "CheBien.Xem")
                bool coQuyenCheBien = AuthService.CoQuyen("CheBien.Xem");
                if (btnCheBien != null) btnCheBien.Visibility = coQuyenCheBien ? Visibility.Visible : Visibility.Collapsed;

                // Các nút chung đã set visible phía trên; nếu cần ẩn khi không có quyền thì thêm logic ở đây
            }

            // --- Chọn trang mặc định ---
            // Nếu có quyền xem sơ đồ bàn => ưu tiên mở SoDoBan
            if (btnSoDoBan != null && btnSoDoBan.Visibility == Visibility.Visible)
            {
                btnSoDoBan.IsChecked = true;
                NavigateToPage(btnSoDoBan, new SoDoBanView());
            }
            else
            {
                if (btnThongTinCaNhan != null)
                {
                    btnThongTinCaNhan.IsChecked = true;
                    NavigateToPage(btnThongTinCaNhan, new ThongTinCaNhanView());
                }
            }

            // Khởi động cập nhật sidebar status
            await UpdateSidebarStatusSafeAsync();
            _sidebarTimer.Start();
        }

        private void UncheckOtherButtons(ToggleButton? exception)
        {
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
            NavigateToPage(sender as ToggleButton, new ThongTinCaNhanView());
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
                if (btnSoDoBan != null)
                    btnSoDoBan.IsChecked = true;
            }
        }

        private void BtnThongBao_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chức năng Thông báo đang được phát triển.");
        }

        private async void SidebarTimer_Tick(object? sender, EventArgs e)
        {
            await UpdateSidebarStatusSafeAsync();
        }

        private async Task UpdateSidebarStatusSafeAsync()
        {
            try
            {
                await UpdateSidebarStatusAsync();
            }
            catch
            {
                // Không cho phép lỗi crash UI nếu sync status thất bại
                if (lblSidebarStatus != null)
                {
                    lblSidebarStatus.Text = "Lỗi đồng bộ";
                    lblSidebarStatus.Foreground = Brushes.OrangeRed;
                }
            }
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
