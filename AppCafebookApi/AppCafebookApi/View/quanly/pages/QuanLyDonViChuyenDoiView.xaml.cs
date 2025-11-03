using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CafebookModel.Model.ModelApp;
using System.Net;
using System.Threading.Tasks;
using System.Globalization;

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLyDonViChuyenDoiView : Page
    {
        private static readonly HttpClient httpClient;
        private List<DonViChuyenDoiDtoo> _allDonViList = new List<DonViChuyenDoiDtoo>();
        private List<FilterLookupDto> _nguyenLieuList = new List<FilterLookupDto>();
        private DonViChuyenDoiDtoo? _selectedDonVi = null;

        static QuanLyDonViChuyenDoiView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public QuanLyDonViChuyenDoiView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadNguyenLieuComboBox();
            await LoadDataGridAsync();
            ResetForm();
        }

        /// <summary>
        /// Tải danh sách Nguyên liệu cho ComboBox
        /// </summary>
        private async Task LoadNguyenLieuComboBox()
        {
            try
            {
                // Tận dụng API 'filters' của SanPham
                var filters = await httpClient.GetFromJsonAsync<SanPhamFiltersDto>("api/app/sanpham/filters");
                if (filters != null)
                {
                    _nguyenLieuList = filters.NguyenLieus;
                    _nguyenLieuList.Insert(0, new FilterLookupDto { Id = 0, Ten = "-- Chọn Nguyên Liệu --" });
                    cmbNguyenLieu.ItemsSource = _nguyenLieuList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách nguyên liệu: {ex.Message}", "Lỗi API");
            }
        }

        /// <summary>
        /// Tải DataGrid
        /// </summary>
        private async Task LoadDataGridAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                _allDonViList = (await httpClient.GetFromJsonAsync<List<DonViChuyenDoiDtoo>>("api/app/donvichuyendoi")) ?? new List<DonViChuyenDoiDtoo>();
                dgDonVi.ItemsSource = _allDonViList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách đơn vị: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Đưa Form về trạng thái thêm mới
        /// </summary>
        private void ResetForm()
        {
            _selectedDonVi = null;
            dgDonVi.SelectedItem = null;
            btnThem.IsEnabled = true;
            btnLuu.IsEnabled = false;
            btnXoa.IsEnabled = false;

            cmbNguyenLieu.SelectedValue = 0;
            txtTenDonVi.Text = "";
            txtGiaTriQuyDoi.Text = "0";
            chkLaDonViCoBan.IsChecked = false;

            // Kích hoạt lại các trường
            cmbNguyenLieu.IsEnabled = true;
            txtGiaTriQuyDoi.IsEnabled = true;
            lblGiaTriQuyDoi.Visibility = Visibility.Visible;
            txtGiaTriQuyDoi.Visibility = Visibility.Visible;
        }

        private void DgDonVi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgDonVi.SelectedItem is not DonViChuyenDoiDtoo selected)
            {
                ResetForm();
                return;
            }

            _selectedDonVi = selected;
            btnThem.IsEnabled = false;
            btnLuu.IsEnabled = true;
            btnXoa.IsEnabled = true;

            cmbNguyenLieu.SelectedValue = selected.IdNguyenLieu;
            txtTenDonVi.Text = selected.TenDonVi;
            txtGiaTriQuyDoi.Text = selected.GiaTriQuyDoi.ToString(CultureInfo.InvariantCulture);
            chkLaDonViCoBan.IsChecked = selected.LaDonViCoBan;

            // Không cho phép đổi Nguyên liệu khi Sửa (để tránh lỗi logic)
            cmbNguyenLieu.IsEnabled = false;
        }

        private void ChkLaDonViCoBan_Changed(object sender, RoutedEventArgs e)
        {
            if (chkLaDonViCoBan.IsChecked == true)
            {
                txtGiaTriQuyDoi.Text = "1";
                txtGiaTriQuyDoi.IsEnabled = false;
                lblGiaTriQuyDoi.Visibility = Visibility.Collapsed;
                txtGiaTriQuyDoi.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtGiaTriQuyDoi.Text = "0";
                txtGiaTriQuyDoi.IsEnabled = true;
                lblGiaTriQuyDoi.Visibility = Visibility.Visible;
                txtGiaTriQuyDoi.Visibility = Visibility.Visible;
            }
        }

        private async void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            await SaveAsync(isCreating: true);
        }

        private async void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            await SaveAsync(isCreating: false);
        }

        private async Task SaveAsync(bool isCreating)
        {
            if ((int)cmbNguyenLieu.SelectedValue == 0)
            {
                MessageBox.Show("Vui lòng chọn nguyên liệu.", "Lỗi"); return;
            }
            if (string.IsNullOrWhiteSpace(txtTenDonVi.Text))
            {
                MessageBox.Show("Tên đơn vị không được để trống.", "Lỗi"); return;
            }
            if (!decimal.TryParse(txtGiaTriQuyDoi.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal giaTri) || giaTri <= 0)
            {
                MessageBox.Show("Giá trị quy đổi phải là số dương.", "Lỗi"); return;
            }

            var dto = new DonViChuyenDoiUpdateRequestDto
            {
                IdNguyenLieu = (int)cmbNguyenLieu.SelectedValue,
                TenDonVi = txtTenDonVi.Text,
                GiaTriQuyDoi = giaTri,
                LaDonViCoBan = chkLaDonViCoBan.IsChecked ?? false
            };

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                HttpResponseMessage response;
                if (isCreating)
                {
                    response = await httpClient.PostAsJsonAsync("api/app/donvichuyendoi", dto);
                }
                else
                {
                    response = await httpClient.PutAsJsonAsync($"api/app/donvichuyendoi/{_selectedDonVi?.IdChuyenDoi}", dto);
                }

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Lưu thành công!", "Thông báo");
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

        private async void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDonVi == null) return;

            var result = MessageBox.Show($"Bạn có chắc muốn xóa đơn vị '{_selectedDonVi.TenDonVi}' của '{_selectedDonVi.TenNguyenLieu}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) return;

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.DeleteAsync($"api/app/donvichuyendoi/{_selectedDonVi.IdChuyenDoi}");
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Xóa thành công!", "Thông báo");
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

        private void BtnLamMoi_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
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