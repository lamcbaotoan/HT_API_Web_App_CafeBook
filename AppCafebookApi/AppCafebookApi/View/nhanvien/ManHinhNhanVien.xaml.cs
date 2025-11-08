using AppCafebookApi.Services;
using AppCafebookApi.View.nhanvien.pages;
using CafebookModel.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic; // <-- Thêm
using System; // <-- Thêm

namespace AppCafebookApi.View.nhanvien
{
    public partial class ManHinhNhanVien : Window
    {
        public ManHinhNhanVien()
        {
            InitializeComponent();
            this.Loaded += ManHinhNhanVien_Loaded;
        }

        private void ManHinhNhanVien_Loaded(object sender, RoutedEventArgs e)
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

                // Chức năng Giao Hàng (chưa có quyền, tạm ẩn)
                // bool coQuyenGiaoHang = AuthService.CoQuyen("GiaoHang.Xem");
                // btnGiaoHang.Visibility = coQuyenGiaoHang ? Visibility.Visible : Visibility.Collapsed;
                btnGiaoHang.Visibility = Visibility.Collapsed; // Tạm ẩn

                // Chức năng Thuê Sách (Thu ngân/Quản lý)
                bool coQuyenSach = AuthService.CoQuyen("Sach.QuanLy");
                btnThueSach.Visibility = coQuyenSach ? Visibility.Visible : Visibility.Collapsed;

                // Chọn trang mặc định
                if (btnSoDoBan.Visibility == Visibility.Visible)
                {
                    btnSoDoBan.IsChecked = true;
                    NavigateToPage(btnSoDoBan, new SoDoBanView());
                }
                else
                {
                    // (Nếu nhân viên không có quyền xem sơ đồ bàn,
                    // bạn nên điều hướng họ đến trang cá nhân hoặc lịch làm việc)
                    btnThongTinCaNhan.IsChecked = true;
                    // NavigateToPage(btnThongTinCaNhan, new ThongTinCaNhanView());
                }
            }
        }

        private void UncheckOtherButtons(ToggleButton? exception)
        {
            // Thêm các nút mới vào danh sách
            var navButtons = new List<ToggleButton>
            {
                btnSoDoBan, btnCheBien, btnDatBan, btnGiaoHang, btnThueSach, // Thêm btnCheBien
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
            // (Chuyển btnDatBanSach thành btnDatBan cho nhất quán)
            NavigateToPage(sender as ToggleButton, new DatBanView());
            /*
            MessageBox.Show("Chức năng 'Đặt Bàn' đang được phát triển.");
            ResetToDefaultPage(sender);
            */
        }

        // === CÁC HÀM CLICK MỚI ===

        private void BtnGiaoHang_Click(object sender, RoutedEventArgs e)
        {
            // TODO: NavigateToPage(sender as ToggleButton, new GiaoHangView());
            MessageBox.Show("Chức năng 'Đơn Giao Hàng' đang được phát triển.");
            ResetToDefaultPage(sender);
        }
        private void BtnCheBien_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(sender as ToggleButton, new CheBienView());
        }

        private void BtnThueSach_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(sender as ToggleButton, new ThueSachView());
            /*
            // TODO: NavigateToPage(sender as ToggleButton, new ThueSachView());
            MessageBox.Show("Chức năng 'Quản lý Thuê Sách' đang được phát triển.");
            ResetToDefaultPage(sender);
            */
        }

        private void BtnThongTinCaNhan_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(sender as ToggleButton, new AppCafebookApi.View.nhanvien.pages.ThongTinCaNhanView());
            /*
            // TODO: NavigateToPage(sender as ToggleButton, new ThongTinCaNhanView());
            MessageBox.Show("Chức năng 'Thông tin cá nhân & Đổi Mật khẩu' đang được phát triển.");
            ResetToDefaultPage(sender);
            */
        }

        private async void BtnChamCong_Click(object sender, RoutedEventArgs e)
        {
            // Đây là nút hành động, không phải điều hướng
            UncheckOtherButtons(null); // Bỏ check tất cả
            btnSoDoBan.IsChecked = true; // Quay về Sơ đồ bàn

            if (AuthService.CurrentUser == null) return;
            int idNhanVien = AuthService.CurrentUser.IdNhanVien;

            // (Bạn cần tạo API endpoint `api/app/nhanvien/chamcong/{idNhanVien}` 
            //  để xử lý logic check-in/check-out)
            MessageBox.Show($"Đang gửi yêu cầu Chấm công (Check-in/Check-out) cho ID: {idNhanVien}...\n(Chức năng này cần API)", "Chấm công");
        }

        private void BtnLichLamViecCuaToi_Click(object sender, RoutedEventArgs e)
        {
            // TODO: NavigateToPage(sender as ToggleButton, new LichLamViecCaNhanView());
            MessageBox.Show("Chức năng 'Xem Lịch Làm Việc' đang được phát triển.");
            ResetToDefaultPage(sender);
        }

        private void BtnPhieuLuongCuaToi_Click(object sender, RoutedEventArgs e)
        {
            // TODO: NavigateToPage(sender as ToggleButton, new PhieuLuongCaNhanView());
            MessageBox.Show("Chức năng 'Xem Phiếu Lương' đang được phát triển.");
            ResetToDefaultPage(sender);
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