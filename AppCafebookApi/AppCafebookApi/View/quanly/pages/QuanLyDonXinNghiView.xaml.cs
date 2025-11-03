using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CafebookModel.Model.ModelApp;
using System.Threading.Tasks;
using System.Net;
using AppCafebookApi.Services; // Cần cho AuthService

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLyDonXinNghiView : Page
    {
        private static readonly HttpClient httpClient;
        private List<DonXinNghiDto> _allDonNghiList = new List<DonXinNghiDto>();
        private List<NhanVienLookupDto> _allNhanVienList = new List<NhanVienLookupDto>();
        private DonXinNghiDto? _selectedDon = null;

        static QuanLyDonXinNghiView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public QuanLyDonXinNghiView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            await LoadNhanVienAsync();
            cmbTrangThaiFilter.SelectedIndex = 1; // Mặc định lọc "Chờ duyệt"
            await LoadDataGridAsync();
            ResetForm();
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        #region Tải Dữ Liệu & Lọc

        private async Task LoadNhanVienAsync()
        {
            try
            {
                // Tận dụng API của Module 4
                _allNhanVienList = (await httpClient.GetFromJsonAsync<List<NhanVienLookupDto>>("api/app/lichlamviec/all-nhanvien")) ?? new List<NhanVienLookupDto>();
                cmbNhanVien.ItemsSource = _allNhanVienList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách nhân viên: {ex.Message}", "Lỗi API");
            }
        }

        private async Task LoadDataGridAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;

            string searchText = txtSearchNhanVien.Text;
            string trangThai = (cmbTrangThaiFilter.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Tất cả";

            try
            {
                var url = $"api/app/donxinnghi/search?searchText={Uri.EscapeDataString(searchText)}&trangThai={Uri.EscapeDataString(trangThai)}";
                _allDonNghiList = (await httpClient.GetFromJsonAsync<List<DonXinNghiDto>>(url)) ?? new List<DonXinNghiDto>();
                dgDonXinNghi.ItemsSource = _allDonNghiList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách đơn nghỉ: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void Filters_Changed(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
                await LoadDataGridAsync();
        }

        #endregion

        #region Form & CRUD

        private void ResetForm()
        {
            _selectedDon = null;
            dgDonXinNghi.SelectedItem = null;

            lblFormTitle.Text = "Tạo Đơn Mới";
            formTaoDon.Visibility = Visibility.Visible;
            formDuyetDon.Visibility = Visibility.Collapsed;

            cmbNhanVien.SelectedValue = -1;
            cmbLoaiDon.SelectedIndex = 0; // Nghỉ có phép
            dpNgayBatDau.SelectedDate = DateTime.Today;
            dpNgayKetThuc.SelectedDate = DateTime.Today;
            txtLyDo.Text = "";
            txtGhiChuPheDuyet.Text = "";

            // Gán nhân viên mặc định nếu người dùng là NV
            if (AuthService.CurrentUser != null && AuthService.CurrentUser.TenVaiTro != "Quản lý" && AuthService.CurrentUser.TenVaiTro != "Quản trị viên")
            {
                cmbNhanVien.SelectedValue = AuthService.CurrentUser.IdNhanVien;
                cmbNhanVien.IsEnabled = false; // NV không thể tạo đơn cho người khác
            }
            else
            {
                cmbNhanVien.IsEnabled = true;
            }
        }

        private void DgDonXinNghi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgDonXinNghi.SelectedItem is not DonXinNghiDto selected)
            {
                ResetForm();
                return;
            }

            _selectedDon = selected;

            lblFormTitle.Text = $"Chi tiết Đơn: {selected.TenNhanVien}";
            formTaoDon.Visibility = Visibility.Visible; // Hiển thị thông tin

            // Điền thông tin
            cmbNhanVien.SelectedValue = _allNhanVienList.FirstOrDefault(nv => nv.HoTen == selected.TenNhanVien)?.IdNhanVien ?? -1;
            cmbLoaiDon.Text = selected.LoaiDon;
            dpNgayBatDau.SelectedDate = selected.NgayBatDau;
            dpNgayKetThuc.SelectedDate = selected.NgayKetThuc;
            txtLyDo.Text = selected.LyDo;
            txtGhiChuPheDuyet.Text = selected.GhiChuPheDuyet;

            // Kiểm tra quyền (Quản lý mới thấy nút duyệt)
            if (AuthService.CoQuyen("NhanSu.QuanLy")) // Giả sử quyền là "NhanSu.QuanLy"
            {
                if (selected.TrangThai == "Chờ duyệt")
                {
                    formDuyetDon.Visibility = Visibility.Visible;
                    btnXoa.IsEnabled = true; // Cho phép Xóa (Hủy)
                }
                else
                {
                    formDuyetDon.Visibility = Visibility.Collapsed; // Đã xử lý, ẩn nút
                    btnXoa.IsEnabled = false; // Không cho xóa
                }
                // Admin có thể sửa đơn chưa duyệt
                formTaoDon.IsEnabled = (selected.TrangThai == "Chờ duyệt");
                btnThemDon.Visibility = Visibility.Collapsed; // Ẩn nút Thêm khi đang xem
            }
            else // Nếu là nhân viên
            {
                formTaoDon.IsEnabled = false; // NV không được sửa sau khi tạo
                formDuyetDon.Visibility = Visibility.Collapsed; // NV không được duyệt
            }
        }

        private void BtnLamMoi_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        private async void BtnThemDon_Click(object sender, RoutedEventArgs e)
        {
            if (cmbNhanVien.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn nhân viên.", "Lỗi"); return;
            }
            if (dpNgayBatDau.SelectedDate == null || dpNgayKetThuc.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn ngày bắt đầu và kết thúc.", "Lỗi"); return;
            }
            if (dpNgayKetThuc.SelectedDate < dpNgayBatDau.SelectedDate)
            {
                MessageBox.Show("Ngày kết thúc không thể trước ngày bắt đầu.", "Lỗi"); return;
            }

            var dto = new DonXinNghiCreateDto
            {
                IdNhanVien = (int)cmbNhanVien.SelectedValue,
                LoaiDon = (cmbLoaiDon.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Nghỉ có phép",
                LyDo = txtLyDo.Text,
                NgayBatDau = dpNgayBatDau.SelectedDate.Value,
                NgayKetThuc = dpNgayKetThuc.SelectedDate.Value
            };

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/app/donxinnghi", dto);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Tạo đơn thành công!", "Thông báo");
                    await LoadDataGridAsync();
                    ResetForm();
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

        private async void BtnDuyet_Click(object sender, RoutedEventArgs e)
        {
            await HandleApprovalAsync(isApproving: true);
        }

        private async void BtnTuChoi_Click(object sender, RoutedEventArgs e)
        {
            await HandleApprovalAsync(isApproving: false);
        }

        private async Task HandleApprovalAsync(bool isApproving)
        {
            if (_selectedDon == null) return;
            if (AuthService.CurrentUser == null)
            {
                MessageBox.Show("Lỗi phiên đăng nhập. Không tìm thấy người duyệt.", "Lỗi"); return;
            }

            var dto = new DonXinNghiActionDto
            {
                IdNguoiDuyet = AuthService.CurrentUser.IdNhanVien,
                GhiChuPheDuyet = txtGhiChuPheDuyet.Text
            };

            string endpoint = isApproving ? $"api/app/donxinnghi/approve/{_selectedDon.IdDonXinNghi}"
                                          : $"api/app/donxinnghi/reject/{_selectedDon.IdDonXinNghi}";

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PutAsJsonAsync(endpoint, dto);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show(isApproving ? "Duyệt đơn thành công." : "Đã từ chối đơn.", "Thông báo");
                    await LoadDataGridAsync();
                    ResetForm();
                }
                else
                {
                    MessageBox.Show($"Lỗi: {responseString}", "Lỗi API");
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

        private async void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDon == null || _selectedDon.TrangThai != "Chờ duyệt")
            {
                MessageBox.Show("Chỉ có thể xóa đơn ở trạng thái 'Chờ duyệt'.", "Không thể xóa");
                return;
            }

            var result = MessageBox.Show($"Bạn có chắc muốn xóa đơn của '{_selectedDon.TenNhanVien}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) return;

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.DeleteAsync($"api/app/donxinnghi/{_selectedDon.IdDonXinNghi}");
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Xóa đơn thành công!", "Thông báo");
                    await LoadDataGridAsync();
                    ResetForm();
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
        private void BtnGoToBaoCao_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new BaoCaoNhanSuView());
        }
        private void BtnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            // Quay lại trang QL Lịch (Module 4)
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}