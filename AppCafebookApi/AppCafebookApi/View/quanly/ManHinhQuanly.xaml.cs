using AppCafebookApi.Services;
using AppCafebookApi.View;
using AppCafebookApi.View.common;
using AppCafebookApi.View.quanly.pages;
using CafebookModel.Model.Data;
using CafebookModel.Model.ModelApp;
using CafebookModel.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq; // <-- Đảm bảo bạn đã using Linq
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace AppCafebookApi.View.quanly
{
    public partial class ManHinhQuanly : Window
    {
        private ToggleButton? currentNavButton;
        private NhanVienDto? currentUser;
        private DispatcherTimer _notificationTimer;
        private static readonly HttpClient httpClient;

        static ManHinhQuanly()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public ManHinhQuanly()
        {
            InitializeComponent();
            _notificationTimer = new DispatcherTimer();
            _notificationTimer.Interval = TimeSpan.FromSeconds(30);
            _notificationTimer.Tick += _notificationTimer_Tick;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            currentUser = AuthService.CurrentUser;
            if (currentUser != null)
            {
                // ... (Code cập nhật Avatar, Tên, Phân quyền giữ nguyên) ...
                txtAdminName.Text = currentUser.HoTen;
                txtUserRole.Text = currentUser.TenVaiTro;
                AvatarBorder.Child = null;
                BitmapImage avatarImage = HinhAnhHelper.LoadImage(
                    currentUser.AnhDaiDien,
                    HinhAnhPaths.DefaultAvatar
                );
                AvatarBorder.Background = new ImageBrush(avatarImage)
                {
                    Stretch = Stretch.UniformToFill
                };

                btnTongQuan.Visibility = Visibility.Collapsed;
                btnSanPham.Visibility = Visibility.Collapsed;
                btnBan.Visibility = Visibility.Collapsed;
                btnKho.Visibility = Visibility.Collapsed;
                btnDonHang.Visibility = Visibility.Collapsed;
                btnSach.Visibility = Visibility.Collapsed;
                btnNhanSu.Visibility = Visibility.Collapsed;
                btnKhachHang.Visibility = Visibility.Collapsed;

                if (AuthService.CoQuyen("Dashboard.Xem"))
                    btnTongQuan.Visibility = Visibility.Visible;
                if (AuthService.CoQuyen("SanPham.QuanLy"))
                {
                    btnSanPham.Visibility = Visibility.Visible;
                    btnBan.Visibility = Visibility.Visible;
                    btnSach.Visibility = Visibility.Visible;
                }
                if (AuthService.CoQuyen("Kho.Nhap", "Kho.KiemKe"))
                    btnKho.Visibility = Visibility.Visible;
                if (AuthService.CoQuyen("HoaDon.Tao", "HoaDon.ThanhToan", "HoaDon.Huy"))
                    btnDonHang.Visibility = Visibility.Visible;
                if (AuthService.CoQuyen("NhanSu.QuanLy", "NhanSu.TinhLuong"))
                    btnNhanSu.Visibility = Visibility.Visible;
                if (AuthService.CoQuyen("NhanSu.QuanLy"))
                    btnKhachHang.Visibility = Visibility.Visible;
            }

            if (btnTongQuan.Visibility == Visibility.Visible)
            {
                UpdateSelectedButton(btnTongQuan);
                MainFrame.Navigate(new TongQuanView());
            }

            await CheckNotificationsAsync();
            _notificationTimer.Start();
        }

        private async void _notificationTimer_Tick(object? sender, EventArgs e)
        {
            await CheckNotificationsAsync();
        }

        private async Task CheckNotificationsAsync()
        {
            try
            {
                var result = await httpClient.GetFromJsonAsync<ThongBaoCountDto>("api/app/thongbao/unread-count");
                if (result != null && result.UnreadCount > 0)
                {
                    lblSoThongBao.Text = result.UnreadCount.ToString();
                    BadgeThongBao.Visibility = Visibility.Visible;
                }
                else
                {
                    BadgeThongBao.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi kiểm tra thông báo: {ex.Message}");
            }
        }

        /// <summary>
        /// Hàm Click chính, đã được sửa lại
        /// </summary>
        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as ToggleButton;
            if (clickedButton == null || clickedButton == currentNavButton)
            {
                if (clickedButton != null) clickedButton.IsChecked = true;
                return;
            }
            Page? pageToNavigate = null;

            if (clickedButton == btnTongQuan)
            {
                pageToNavigate = new TongQuanView();
            }
            else if (clickedButton == btnBan)
            {
                pageToNavigate = new QuanLyBanView();
            }
            else if (clickedButton == btnSanPham)
            {
                // Giả sử bạn đã tạo page này
                pageToNavigate = new QuanLySanPhamView(); 
            }
            else if (clickedButton == btnDonHang)
            {
                // Giả sử bạn đã tạo page này
                pageToNavigate = new QuanLyDonHangView();
            }
            else if (clickedButton == btnKhachHang)
            {
                // Giả sử bạn đã tạo page này
                pageToNavigate = new QuanLyKhachHangView();
            }
            else if (clickedButton == btnSach)
            {
                // Giả sử bạn đã tạo page này
                pageToNavigate = new QuanLySachView();
            }
            else if (clickedButton == btnNhanSu)
            {
                pageToNavigate = new QuanLyNhanVienView();
            }
            else if (clickedButton == btnLuong)
            {
                pageToNavigate = new QuanLyLuongView();
            }
            // === SỬA LỖI CÚ PHÁP TẠI ĐÂY ===
            // Xóa dấu ; ở dòng 144 và thêm { }
            else if (clickedButton == btnKho)
            {
                pageToNavigate = new QuanLyTonKhoView();
            }
            // === BỔ SUNG LOGIC TỪ CÁC MODULE TRƯỚC ===
            else if (clickedButton.Name == "btnLichLamViec") // Dùng .Name an toàn
            {
                pageToNavigate = new QuanLyLichLamViecView();
            }
            else if (clickedButton.Name == "btnDonXinNghi")
            {
                pageToNavigate = new QuanLyDonXinNghiView();
            }
            else if (clickedButton.Name == "btnCaiDatNhanSu")
            {
                pageToNavigate = new CaiDatNhanSuView();
            }
            else if (clickedButton.Name == "btnBaoCaoNhanSu")
            {
                pageToNavigate = new BaoCaoNhanSuView();
            }
            // === KẾT THÚC BỔ SUNG ===

            if (pageToNavigate != null)
            {
                UpdateSelectedButton(clickedButton);
                MainFrame.Navigate(pageToNavigate);
            }
            else
            {
                MessageBox.Show($"Chức năng '{clickedButton.Content}' đang được phát triển!", "Thông báo");
                clickedButton.IsChecked = false;
                if (currentNavButton != null)
                {
                    currentNavButton.IsChecked = true;
                }
            }
        }

        private void UpdateSelectedButton(ToggleButton newButton)
        {
            if (currentNavButton != null && currentNavButton != newButton)
            {
                currentNavButton.IsChecked = false;
            }
            currentNavButton = newButton;
            currentNavButton.IsChecked = true;
        }

        private async void BtnThongBao_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Vẫn gọi API 'all' để lấy tất cả thông báo
                var allNotifications = await httpClient.GetFromJsonAsync<List<ThongBaoDto>>("api/app/thongbao/all");

                if (allNotifications != null)
                {
                    // 2. Lọc ra danh sách CHƯA ĐỌC (DaXem == false)
                    var unreadNotifications = allNotifications
                        .Where(t => t.DaXem == false)
                        .ToList();

                    // 3. Gán danh sách đã lọc vào Popup
                    icThongBaoPopup.ItemsSource = unreadNotifications;
                }
                else
                {
                    icThongBaoPopup.ItemsSource = null;
                }

                PopupThongBao.IsOpen = true;

                // 4. Cập nhật lại số lượng trên badge (vì có thể đã đọc ở đâu đó)
                await CheckNotificationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải thông báo: {ex.Message}", "Lỗi API");
            }
        }

        private async void ThongBaoItem_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ThongBaoDto thongBao)
            {
                PopupThongBao.IsOpen = false;

                // Gọi API để đánh dấu là đã đọc (không cần đợi)
                _ = httpClient.PostAsync($"api/app/thongbao/mark-as-read/{thongBao.IdThongBao}", null); 

                // === LOGIC ĐIỀU HƯỚNG MỚI ===

                if (thongBao.LoaiThongBao == "SuCoBan" && thongBao.IdLienQuan.HasValue) 
                {
                    int idBanCanDen = thongBao.IdLienQuan.Value;
                    UpdateSelectedButton(btnBan);
                    MainFrame.Navigate(new QuanLyBanView(idBanCanDen));
                }
                else if (thongBao.LoaiThongBao == "HetHang")
                {
                    // (Giả sử btnKho và QuanLyTonKhoView đã tồn tại)
                    UpdateSelectedButton(btnKho);
                    MainFrame.Navigate(new QuanLyTonKhoView());
                }
                else if (thongBao.LoaiThongBao == "DonXinNghi")
                {
                    // Tìm nút btnDonXinNghi (nếu chưa có, dùng btnNhanSu)
                    ToggleButton? targetButton = FindName("btnDonXinNghi") as ToggleButton ?? btnNhanSu; 
                    UpdateSelectedButton(targetButton);
                    MainFrame.Navigate(new QuanLyDonXinNghiView());
                }

                // === KẾT THÚC LOGIC MỚI ===

                // Cập nhật lại badge (số thông báo sẽ giảm)
                await CheckNotificationsAsync();
            }
        }

        private void BtnDangXuat_Click(object sender, RoutedEventArgs e)
        {
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