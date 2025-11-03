using AppCafebookApi.Services; // Thêm
using AppCafebookApi.View.nhanvien.pages; // <-- THÊM USING NÀY
using CafebookModel.Utils; // Thêm
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives; // Thêm
using System.Windows.Media; // Thêm
using System.Windows.Media.Imaging; // Thêm

namespace AppCafebookApi.View.nhanvien
{
    public partial class ManHinhNhanVien : Window
    {
        public ManHinhNhanVien()
        {
            InitializeComponent();
            // Sự kiện Loaded có thể được thêm từ XAML hoặc ở đây
            this.Loaded += ManHinhNhanVien_Loaded;
        }

        private void ManHinhNhanVien_Loaded(object sender, RoutedEventArgs e)
        {

            btnSoDoBan.IsChecked = true;
            NavigateToPage(btnSoDoBan, new SoDoBanView());

            var currentUser = AuthService.CurrentUser;
            if (currentUser != null)
            {
                // 1. Cập nhật tên và vai trò (Bạn đã có)
                txtUserName.Text = currentUser.HoTen;
                txtUserRole.Text = currentUser.TenVaiTro;

                // 2. Cập nhật Avatar (Bạn đã có)
                AvatarBorder.Child = null;
                BitmapImage avatarImage = HinhAnhHelper.LoadImageFromBase64(
                    currentUser.AnhDaiDien,
                    HinhAnhPaths.DefaultAvatar
                );
                AvatarBorder.Background = new ImageBrush(avatarImage)
                {
                    Stretch = Stretch.UniformToFill
                };

                // --- 3. BẮT ĐẦU PHÂN QUYỀN CHỨC NĂNG ---

                // Dựa trên CSDL, 'Thu ngân' và 'Quản lý' có quyền 'HoaDon.Tao'.
                // 'Pha chế' và 'Phục vụ' (tài khoản 'service') không có.
                // Chúng ta ẩn các nút nghiệp vụ chính nếu không có quyền.
                if (!AuthService.CoQuyen("HoaDon.Tao", "HoaDon.ThanhToan"))
                {
                    btnSoDoBan.Visibility = Visibility.Collapsed;
                    btnDatBanSach.Visibility = Visibility.Collapsed;
                }

                // Các nút cá nhân (Thông tin, Chấm công) được giữ lại cho mọi người
            }


        }
        // --- SỬA LẠI CÁC HÀM CLICK ĐIỀU HƯỚNG ---

        private void UncheckOtherButtons(ToggleButton? exception)
        {
            var navButtons = new List<ToggleButton>
            {
                btnSoDoBan, btnDatBanSach, btnThongTinCaNhan, btnChamCong
            };

            foreach (var button in navButtons)
            {
                if (button != null && button != exception)
                {
                    button.IsChecked = false;
                }
            }
        }

        // Hàm này giờ đã hợp lệ vì 'Page' đã được 'using'
        private void NavigateToPage(ToggleButton? clickedButton, Page pageInstance)
        {
            if (clickedButton == null) return;
            UncheckOtherButtons(clickedButton);
            clickedButton.IsChecked = true;
            MainFrame.Navigate(pageInstance);
        }

        private void BtnSoDoBan_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(sender as ToggleButton, new SoDoBanView());
        }

        private void BtnDatBanSach_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chức năng 'Đặt Bàn Sách' đang được phát triển.");

            // SỬA LỖI CS8602: Thêm kiểm tra null
            if (sender is ToggleButton btn)
            {
                btn.IsChecked = false;
                btnSoDoBan.IsChecked = true; // Trả về trang mặc định
            }
        }

        private void BtnThongTinCaNhan_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chức năng 'Thông tin cá nhân' đang được phát triển.");

            // SỬA LỖI CS8602: Thêm kiểm tra null
            if (sender is ToggleButton btn)
            {
                btn.IsChecked = false;
                btnSoDoBan.IsChecked = true; // Trả về trang mặc định
            }
        }

        private void BtnChamCong_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chức năng 'Chấm công' đang được phát triển.");

            // SỬA LỖI CS8602: Thêm kiểm tra null
            if (sender is ToggleButton btn)
            {
                btn.IsChecked = false;
                btnSoDoBan.IsChecked = true; // Trả về trang mặc định
            }
        }

        // ... (Hàm Đăng xuất)
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