using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data; // Cần cho CollectionViewSource
using System.Windows.Navigation;
using CafebookModel.Model.ModelApp;
using System.Threading.Tasks;
using System.ComponentModel; // Cần cho ICollectionView

namespace AppCafebookApi.View.quanly.pages
{
    /// <summary>
    /// Lớp View-Model nội bộ để quản lý trạng thái CheckBox
    /// </summary>
    public class QuyenViewItem : INotifyPropertyChanged
    {
        public string IdQuyen { get; set; } = string.Empty;
        public string TenQuyen { get; set; } = string.Empty;
        public string NhomQuyen { get; set; } = string.Empty;

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

    public partial class PhanQuyenView : Page
    {
        private static readonly HttpClient httpClient;

        // Cache dữ liệu
        private List<VaiTroDto> _allVaiTroList = new List<VaiTroDto>();
        private List<QuyenViewItem> _allPermissionsList = new List<QuyenViewItem>();

        static PhanQuyenView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public PhanQuyenView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            await LoadAllVaiTroAsync();
            await LoadAllQuyenAsync();
            ApplyFilter(); // Hiển thị danh sách rỗng ban đầu
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Tải danh sách Vai Trò vào ComboBox
        /// </summary>
        private async Task LoadAllVaiTroAsync()
        {
            try
            {
                _allVaiTroList = (await httpClient.GetFromJsonAsync<List<VaiTroDto>>("api/app/vaitro/all")) ?? new List<VaiTroDto>();
                cmbVaiTro.ItemsSource = _allVaiTroList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách vai trò: {ex.Message}", "Lỗi API");
            }
        }

        /// <summary>
        /// Tải TẤT CẢ các quyền trong hệ thống vào Cache
        /// </summary>
        private async Task LoadAllQuyenAsync()
        {
            try
            {
                var quyenDtos = (await httpClient.GetFromJsonAsync<List<QuyenDto>>("api/app/phanquyen/all-permissions")) ?? new List<QuyenDto>();

                _allPermissionsList = quyenDtos.Select(dto => new QuyenViewItem
                {
                    IdQuyen = dto.IdQuyen,
                    TenQuyen = dto.TenQuyen,
                    NhomQuyen = dto.NhomQuyen,
                    IsChecked = false // Mặc định là false
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách quyền: {ex.Message}", "Lỗi API");
            }
        }

        /// <summary>
        /// Khi thay đổi Vai trò, tải các quyền tương ứng
        /// </summary>
        private async void CmbVaiTro_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbVaiTro.SelectedItem is not VaiTroDto selectedVaiTro)
            {
                btnLuu.IsEnabled = false;
                // Bỏ check tất cả
                foreach (var item in _allPermissionsList) { item.IsChecked = false; }
                ApplyFilter();
                return;
            }

            btnLuu.IsEnabled = true;
            LoadingOverlay.Visibility = Visibility.Visible;

            try
            {
                // 1. Tải danh sách ID quyền đã được gán
                var assignedIds = (await httpClient.GetFromJsonAsync<List<string>>($"api/app/phanquyen/for-role/{selectedVaiTro.IdVaiTro}")) ?? new List<string>();

                // 2. Cập nhật trạng thái CheckBox
                foreach (var item in _allPermissionsList)
                {
                    item.IsChecked = assignedIds.Contains(item.IdQuyen);
                }

                // 3. Áp dụng tìm kiếm và hiển thị
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải phân quyền cho vai trò: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Lọc danh sách quyền theo từ khóa tìm kiếm
        /// </summary>
        private void TxtSearchQuyen_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        /// <summary>
        /// (Quan trọng) Hàm lọc và nhóm ListView
        /// </summary>
        private void ApplyFilter()
        {
            string searchText = txtSearchQuyen.Text.ToLower().Trim();

            IEnumerable<QuyenViewItem> filteredList = _allPermissionsList;

            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = _allPermissionsList.Where(p =>
                    p.TenQuyen.ToLower().Contains(searchText) ||
                    p.NhomQuyen.ToLower().Contains(searchText));
            }

            // Tạo CollectionViewSource để Gruping
            var cvs = new CollectionViewSource { Source = filteredList };
            cvs.GroupDescriptions.Add(new PropertyGroupDescription("NhomQuyen"));

            lvQuyen.ItemsSource = cvs.View;
        }

        /// <summary>
        /// Lưu thay đổi phân quyền
        /// </summary>
        private async void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (cmbVaiTro.SelectedItem is not VaiTroDto selectedVaiTro)
            {
                MessageBox.Show("Vui lòng chọn một vai trò trước khi lưu.", "Chưa chọn Vai trò");
                return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;

            try
            {
                // 1. Lấy tất cả ID được check từ cache
                var checkedIds = _allPermissionsList
                    .Where(p => p.IsChecked)
                    .Select(p => p.IdQuyen)
                    .ToList();

                // 2. Tạo DTO
                var dto = new PhanQuyenDto
                {
                    IdVaiTro = selectedVaiTro.IdVaiTro,
                    DanhSachIdQuyen = checkedIds
                };

                // 3. Gọi API
                var response = await httpClient.PutAsJsonAsync("api/app/phanquyen/update", dto);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Cập nhật phân quyền thành công!", "Thành công");
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

        private void BtnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}