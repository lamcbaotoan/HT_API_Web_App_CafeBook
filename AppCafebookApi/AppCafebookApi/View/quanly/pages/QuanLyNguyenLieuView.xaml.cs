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
    public partial class QuanLyNguyenLieuView : Page
    {
        private static readonly HttpClient httpClient;
        private List<NguyenLieuCrudDto> _nguyenLieuList = new List<NguyenLieuCrudDto>();

        static QuanLyNguyenLieuView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public QuanLyNguyenLieuView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataGridAsync();
            ResetNguyenLieuForm();
        }

        private async Task LoadDataGridAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                _nguyenLieuList = (await httpClient.GetFromJsonAsync<List<NguyenLieuCrudDto>>("api/app/nguyenlieu/all")) ?? new List<NguyenLieuCrudDto>();
                dgNguyenLieu.ItemsSource = _nguyenLieuList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải nguyên liệu: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void ResetNguyenLieuForm()
        {
            dgNguyenLieu.SelectedItem = null;
            txtTenNL_crud.Text = "";
            cmbDonViTinh_crud.Text = "kg";
            txtNguongCanhBao_crud.Text = "0";
            btnThemNL.IsEnabled = true;
            btnLuuNL.IsEnabled = false;
            btnXoaNL.IsEnabled = false;
        }

        private void DgNguyenLieu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgNguyenLieu.SelectedItem is NguyenLieuCrudDto selected)
            {
                txtTenNL_crud.Text = selected.TenNguyenLieu;
                cmbDonViTinh_crud.Text = selected.DonViTinh;
                txtNguongCanhBao_crud.Text = selected.TonKhoToiThieu.ToString(CultureInfo.InvariantCulture);
                btnThemNL.IsEnabled = false;
                btnLuuNL.IsEnabled = true;
                btnXoaNL.IsEnabled = true;
            }
            else
            {
                ResetNguyenLieuForm();
            }
        }

        private async void BtnThemNL_Click(object sender, RoutedEventArgs e)
        {
            await SaveNguyenLieuAsync(isCreating: true);
        }

        private async void BtnLuuNL_Click(object sender, RoutedEventArgs e)
        {
            await SaveNguyenLieuAsync(isCreating: false);
        }

        private async Task SaveNguyenLieuAsync(bool isCreating)
        {
            var dto = new NguyenLieuUpdateRequestDto
            {
                TenNguyenLieu = txtTenNL_crud.Text,
                DonViTinh = cmbDonViTinh_crud.Text,
                TonKhoToiThieu = decimal.TryParse(txtNguongCanhBao_crud.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : 0
            };

            if (string.IsNullOrWhiteSpace(dto.TenNguyenLieu) || string.IsNullOrWhiteSpace(dto.DonViTinh))
            {
                MessageBox.Show("Tên và Đơn vị tính là bắt buộc.", "Lỗi"); return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                HttpResponseMessage response;
                if (isCreating)
                {
                    response = await httpClient.PostAsJsonAsync("api/app/nguyenlieu", dto);
                }
                else
                {
                    int id = (dgNguyenLieu.SelectedItem as NguyenLieuCrudDto)?.IdNguyenLieu ?? 0;
                    response = await httpClient.PutAsJsonAsync($"api/app/nguyenlieu/{id}", dto);
                }

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Lưu thành công!", "Thông báo");
                    await LoadDataGridAsync();
                    ResetNguyenLieuForm();
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

        private async void BtnXoaNL_Click(object sender, RoutedEventArgs e)
        {
            if (dgNguyenLieu.SelectedItem is not NguyenLieuCrudDto selected) return;

            if (MessageBox.Show($"Bạn có chắc muốn xóa '{selected.TenNguyenLieu}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                return;

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.DeleteAsync($"api/app/nguyenlieu/{selected.IdNguyenLieu}");
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Xóa thành công!", "Thông báo");
                    await LoadDataGridAsync();
                    ResetNguyenLieuForm();
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

        private void BtnLamMoiNL_Click(object sender, RoutedEventArgs e)
        {
            ResetNguyenLieuForm();
        }

        // Nút điều hướng
        private void BtnQuanLyDVT_Click(object sender, RoutedEventArgs e)
        {
            // Điều hướng đến trang Đơn Vị Chuyển Đổi 
            this.NavigationService?.Navigate(new QuanLyDonViChuyenDoiView());
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