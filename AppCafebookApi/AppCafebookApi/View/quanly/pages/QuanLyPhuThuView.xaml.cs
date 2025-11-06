using CafebookModel.Model.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLyPhuThuView : Page
    {
        private static readonly HttpClient httpClient;
        private ObservableCollection<PhuThu> _phuThuCollection = new ObservableCollection<PhuThu>();
        private PhuThu? _selectedPhuThu;

        static QuanLyPhuThuView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public QuanLyPhuThuView()
        {
            InitializeComponent();
            dgPhuThu.ItemsSource = _phuThuCollection;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataGridAsync();
            ResetForm();
        }

        private async Task LoadDataGridAsync()
        {
            try
            {
                var phuThus = await httpClient.GetFromJsonAsync<List<PhuThu>>("api/app/quanly/phuthu");
                if (phuThus != null)
                {
                    _phuThuCollection.Clear();
                    foreach (var item in phuThus.OrderBy(p => p.TenPhuThu))
                    {
                        _phuThuCollection.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách phụ thu: {ex.Message}", "Lỗi API");
            }
        }

        private void ResetForm()
        {
            _selectedPhuThu = null;
            dgPhuThu.SelectedItem = null;
            lblFormTitle.Text = "Thêm Phụ thu mới";
            txtIdPhuThu.Text = "0";
            txtTenPhuThu.Clear();
            txtGiaTri.Text = "0";
            cmbLoaiGiaTri.SelectedIndex = 0; // "VND"
            btnLuu.Content = "Thêm";
            btnXoa.IsEnabled = false;
        }

        private void BtnThemMoi_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        private void DgPhuThu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgPhuThu.SelectedItem is PhuThu selected)
            {
                _selectedPhuThu = selected;
                lblFormTitle.Text = "Sửa Phụ thu";
                txtIdPhuThu.Text = selected.IdPhuThu.ToString();
                txtTenPhuThu.Text = selected.TenPhuThu;
                txtGiaTri.Text = selected.GiaTri.ToString();
                cmbLoaiGiaTri.SelectedValue = selected.LoaiGiaTri; // Giả định ComboBoxItem Content khớp

                btnLuu.Content = "Lưu";
                btnXoa.IsEnabled = true;
            }
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        private async void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            // --- Validation ---
            if (string.IsNullOrWhiteSpace(txtTenPhuThu.Text))
            {
                MessageBox.Show("Tên phụ thu không được để trống.", "Lỗi");
                return;
            }
            if (!decimal.TryParse(txtGiaTri.Text, out decimal giaTri))
            {
                MessageBox.Show("Giá trị phải là một con số.", "Lỗi");
                return;
            }

            // --- Tạo đối tượng ---
            var phuThu = new PhuThu
            {
                IdPhuThu = int.Parse(txtIdPhuThu.Text),
                TenPhuThu = txtTenPhuThu.Text,
                GiaTri = giaTri,
                LoaiGiaTri = (cmbLoaiGiaTri.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "VND"
            };

            try
            {
                if (phuThu.IdPhuThu == 0) // THÊM MỚI
                {
                    var response = await httpClient.PostAsJsonAsync("api/app/quanly/phuthu", phuThu);
                    if (response.IsSuccessStatusCode)
                    {
                        var newItem = await response.Content.ReadFromJsonAsync<PhuThu>();
                        if (newItem != null) _phuThuCollection.Add(newItem);
                        MessageBox.Show("Thêm phụ thu thành công!", "Thông báo");
                    }
                    else
                    {
                        MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Lỗi API");
                    }
                }
                else // CẬP NHẬT
                {
                    var response = await httpClient.PutAsJsonAsync($"api/app/quanly/phuthu/{phuThu.IdPhuThu}", phuThu);
                    if (response.IsSuccessStatusCode)
                    {
                        // Cập nhật item trong list (cách đơn giản là tải lại)
                        await LoadDataGridAsync();
                        MessageBox.Show("Cập nhật thành công!", "Thông báo");
                    }
                    else
                    {
                        MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Lỗi API");
                    }
                }
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi API");
            }
        }

        private async void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPhuThu == null) return;

            var result = MessageBox.Show($"Bạn có chắc chắn muốn XÓA phụ thu '{_selectedPhuThu.TenPhuThu}' không?",
                                         "Xác nhận Xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.No) return;

            try
            {
                var response = await httpClient.DeleteAsync($"api/app/quanly/phuthu/{_selectedPhuThu.IdPhuThu}");

                if (response.IsSuccessStatusCode)
                {
                    _phuThuCollection.Remove(_selectedPhuThu);
                    MessageBox.Show("Xóa thành công!", "Thông báo");
                    ResetForm();
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    // Lỗi 409: Đã được sử dụng (theo logic controller)
                    string errorMessage = await response.Content.ReadAsStringAsync();
                    MessageBox.Show(errorMessage, "Không thể xóa");
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
        }

        // ### THÊM HÀM MỚI NÀY VÀO CUỐI FILE ###
        private void BtnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}