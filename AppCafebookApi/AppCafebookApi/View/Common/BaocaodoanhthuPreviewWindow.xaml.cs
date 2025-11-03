using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CafebookModel.Model.ModelApp;

// --- THÊM CÁC USING ĐỂ XUẤT EXCEL ---
using System.IO;
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace AppCafebookApi.View.common
{
    public partial class BaocaodoanhthuPreviewWindow : Page
    {
        private static readonly HttpClient httpClient;
        private BaoCaoTongHopDto? currentReportData;

        static BaocaodoanhthuPreviewWindow()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public BaocaodoanhthuPreviewWindow()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var now = DateTime.Now;
            dpStartDate.SelectedDate = new DateTime(now.Year, now.Month, 1);
            dpEndDate.SelectedDate = now;
        }

        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (dpStartDate.SelectedDate == null || dpEndDate.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn cả Ngày bắt đầu và Ngày kết thúc.", "Thiếu thông tin");
                return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            btnGenerate.IsEnabled = false;

            var request = new BaoCaoRequestDto
            {
                StartDate = dpStartDate.SelectedDate.Value,
                EndDate = dpEndDate.SelectedDate.Value
            };

            try
            {
                var response = await httpClient.PostAsJsonAsync("api/app/baocao/doanhthu", request);
                if (response.IsSuccessStatusCode)
                {
                    currentReportData = await response.Content.ReadFromJsonAsync<BaoCaoTongHopDto>();
                    if (currentReportData != null)
                    {
                        PopulateUi(currentReportData);
                    }
                }
                else
                {
                    MessageBox.Show($"Lỗi API: {response.ReasonPhrase}", "Lỗi");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể kết nối API: {ex.Message}", "Lỗi");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                btnGenerate.IsEnabled = true;
            }
        }

        private void PopulateUi(BaoCaoTongHopDto data)
        {
            var culture = CultureInfo.GetCultureInfo("vi-VN");

            lblDoanhThuRong.Text = data.Kpi.DoanhThuRong.ToString("C0", culture);
            lblTongGiaVon.Text = data.Kpi.TongGiaVon.ToString("C0", culture);
            lblLoiNhuanGop.Text = data.Kpi.LoiNhuanGop.ToString("C0", culture);
            lblChiPhiOpex.Text = data.Kpi.ChiPhiOpex.ToString("C0", culture);
            lblLoiNhuanRong.Text = data.Kpi.LoiNhuanRong.ToString("C0", culture);

            dgTopSanPham.ItemsSource = data.TopSanPham;

            var dtList = new List<KeyValuePair<string, decimal>>
            {
                new("Tổng doanh thu gốc", data.ChiTietDoanhThu.TongDoanhThuGoc),
                new("Tổng giảm giá", data.ChiTietDoanhThu.TongGiamGia),
                new("Tổng phụ thu", data.ChiTietDoanhThu.TongPhuThu),
                new("DOANH THU RÒNG", data.ChiTietDoanhThu.DoanhThuRong),
                new("Tổng số hóa đơn", data.ChiTietDoanhThu.SoLuongHoaDon),
                new("Giá trị trung bình HĐ", data.ChiTietDoanhThu.GiaTriTrungBinhHD)
            };
            dgChiTietDoanhThu.ItemsSource = dtList;

            var cpList = new List<KeyValuePair<string, decimal>>
            {
                new("Tổng Giá Vốn Hàng Bán (COGS)", data.ChiTietChiPhi.TongGiaVon_COGS),
                new("Tổng Chi Phí Lương", data.ChiTietChiPhi.TongChiPhiLuong),
                new("Tổng Chi Phí Hủy Hàng", data.ChiTietChiPhi.TongChiPhiHuyHang)
            };
            dgChiTietChiPhi.ItemsSource = cpList;
        }

        private async void BtnExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            if (currentReportData == null)
            {
                MessageBox.Show("Chưa có dữ liệu báo cáo để xuất. Vui lòng nhấn 'Tạo Báo Cáo' trước.", "Chưa có dữ liệu");
                return;
            }

            // SỬA LỖI CS8629: Kiểm tra null cho DatePicker ở đây
            if (dpStartDate.SelectedDate == null || dpEndDate.SelectedDate == null)
            {
                MessageBox.Show("Ngày bắt đầu hoặc kết thúc không hợp lệ.", "Lỗi");
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    LoadingOverlay.Visibility = Visibility.Visible;

                    // SỬA LỖI CS8629: Truyền ngày vào hàm
                    await CreateExcelReportAsync(
                        sfd.FileName,
                        currentReportData,
                        dpStartDate.SelectedDate.Value,
                        dpEndDate.SelectedDate.Value);

                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    MessageBox.Show($"Đã xuất báo cáo thành công!\n\nĐường dẫn: {sfd.FileName}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    MessageBox.Show($"Có lỗi khi xuất Excel: {ex.Message}\n\nĐảm bảo bạn đã đóng file Excel (nếu nó đang mở).", "Lỗi Xuất File", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Dán đè lên hàm CreateExcelReportAsync CŨ của bạn
        private async Task CreateExcelReportAsync(string filePath, BaoCaoTongHopDto data, DateTime startDate, DateTime endDate)
        {
            // Giấy phép EPPlus
            ExcelPackage.License.SetNonCommercialPersonal("CafeBook");
            var fileInfo = new FileInfo(filePath);

            // Xóa file cũ nếu tồn tại
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }

            using (var package = new ExcelPackage(fileInfo))
            {
                // --- 1. ĐỊNH NGHĨA CÁC STYLE TÁI SỬ DỤNG ---

                // Style cho Tiêu đề chính (Merge cell, to, đậm)
                Action<ExcelRange> styleMainTitle = (range) =>
                {
                    range.Merge = true;
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 16;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#333366")); // Xanh đậm
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                };

                // Style cho Tiêu đề phụ (Merge cell, nghiêng)
                Action<ExcelRange> styleSubTitle = (range) =>
                {
                    range.Merge = true;
                    range.Style.Font.Italic = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                };

                // Style cho Tiêu đề nhóm (Đậm, nền xám)
                Action<ExcelRange> styleGroupHeader = (range) =>
                {
                    range.Merge = true;
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                };

                // Định dạng tiền tệ VND (canh phải)
                var currencyFormat = "#,##0 \"đ\"";
                Action<ExcelRange> styleCurrency = (range) =>
                {
                    range.Style.Numberformat.Format = currencyFormat;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                };

                // Style cho Lợi nhuận (Xanh, đậm)
                Action<ExcelRange> styleProfit = (range) =>
                {
                    styleCurrency(range);
                    range.Style.Font.Bold = true;
                    range.Style.Font.Color.SetColor(System.Drawing.Color.Green);
                };

                // Style cho Chi phí (Đỏ)
                Action<ExcelRange> styleExpense = (range) =>
                {
                    styleCurrency(range);
                    range.Style.Font.Color.SetColor(System.Drawing.Color.Red);
                };

                // --- 2. SHEET 1: BÁO CÁO TỔNG QUAN (P&L) ---
                var wsPL = package.Workbook.Worksheets.Add("Báo Cáo Tổng Quan");

                // Tiêu đề
                wsPL.Cells["A1:D1"].Value = "BÁO CÁO KINH DOANH TỔNG QUAN";
                styleMainTitle(wsPL.Cells["A1:D1"]);

                wsPL.Cells["A2:D2"].Value = $"Từ ngày {startDate:dd/MM/yyyy} đến {endDate:dd/MM/yyyy}";
                styleSubTitle(wsPL.Cells["A2:D2"]);

                // P&L (Lợi nhuận & Lỗ)
                wsPL.Cells["A4:B4"].Value = "KẾT QUẢ KINH DOANH CHÍNH (P&L)";
                styleGroupHeader(wsPL.Cells["A4:B4"]);

                wsPL.Cells["A5"].Value = "Doanh Thu Ròng";
                wsPL.Cells["B5"].Value = data.Kpi.DoanhThuRong;
                styleCurrency(wsPL.Cells["B5"]);

                wsPL.Cells["A6"].Value = "(-) Tổng Giá Vốn (COGS)";
                wsPL.Cells["B6"].Value = data.Kpi.TongGiaVon;
                styleExpense(wsPL.Cells["B6"]);

                wsPL.Cells["A7"].Value = "(=) Lợi Nhuận Gộp";
                wsPL.Cells["B7"].Value = data.Kpi.LoiNhuanGop;
                styleProfit(wsPL.Cells["B7"]); // Lợi nhuận gộp nên luôn là màu xanh

                wsPL.Cells["A8"].Value = "(-) Chi Phí Vận Hành (OPEX)";
                wsPL.Cells["B8"].Value = data.Kpi.ChiPhiOpex;
                styleExpense(wsPL.Cells["B8"]);

                wsPL.Cells["A9"].Value = "(=) LỢI NHUẬN RÒNG";
                wsPL.Cells["B9"].Value = data.Kpi.LoiNhuanRong;
                // Lợi nhuận ròng có thể âm hoặc dương
                if (data.Kpi.LoiNhuanRong >= 0)
                {
                    styleProfit(wsPL.Cells["B9"]);
                }
                else
                {
                    styleExpense(wsPL.Cells["B9"]);
                }
                wsPL.Cells["A9"].Style.Font.Bold = true;

                // Kẻ viền cho bảng P&L
                wsPL.Cells["A4:B9"].Style.Border.BorderAround(ExcelBorderStyle.Medium);

                // Chi tiết Doanh thu
                wsPL.Cells["D4:E4"].Value = "CHI TIẾT DOANH THU";
                styleGroupHeader(wsPL.Cells["D4:E4"]);
                wsPL.Cells["D5"].Value = "Tổng doanh thu gốc";
                wsPL.Cells["E5"].Value = data.ChiTietDoanhThu.TongDoanhThuGoc;
                wsPL.Cells["D6"].Value = "Tổng giảm giá";
                wsPL.Cells["E6"].Value = data.ChiTietDoanhThu.TongGiamGia;
                wsPL.Cells["D7"].Value = "Tổng phụ thu";
                wsPL.Cells["E7"].Value = data.ChiTietDoanhThu.TongPhuThu;
                wsPL.Cells["D8"].Value = "DOANH THU RÒNG";
                wsPL.Cells["E8"].Value = data.ChiTietDoanhThu.DoanhThuRong;
                wsPL.Cells["D8,E8"].Style.Font.Bold = true;

                wsPL.Cells["D10"].Value = "Số lượng hóa đơn";
                wsPL.Cells["E10"].Value = data.ChiTietDoanhThu.SoLuongHoaDon;
                wsPL.Cells["D11"].Value = "Giá trị trung bình HĐ";
                wsPL.Cells["E11"].Value = data.ChiTietDoanhThu.GiaTriTrungBinhHD;

                // Định dạng tiền tệ cho nhóm Doanh Thu
                styleCurrency(wsPL.Cells["E5:E8"]);
                styleCurrency(wsPL.Cells["E11"]);
                wsPL.Cells["E10"].Style.Numberformat.Format = "#,##0";
                wsPL.Cells["D4:E11"].Style.Border.BorderAround(ExcelBorderStyle.Medium);

                // Chi tiết Chi Phí
                wsPL.Cells["D13:E13"].Value = "CHI TIẾT CHI PHÍ";
                styleGroupHeader(wsPL.Cells["D13:E13"]);
                wsPL.Cells["D14"].Value = "Tổng Giá Vốn (COGS)";
                wsPL.Cells["E14"].Value = data.ChiTietChiPhi.TongGiaVon_COGS;
                wsPL.Cells["D15"].Value = "Tổng Chi Phí Lương";
                wsPL.Cells["E15"].Value = data.ChiTietChiPhi.TongChiPhiLuong;
                wsPL.Cells["D16"].Value = "Tổng Chi Phí Hủy Hàng";
                wsPL.Cells["E16"].Value = data.ChiTietChiPhi.TongChiPhiHuyHang;

                // Định dạng tiền tệ cho nhóm Chi Phí
                styleCurrency(wsPL.Cells["E14:E16"]);
                wsPL.Cells["D13:E16"].Style.Border.BorderAround(ExcelBorderStyle.Medium);

                // Tự động căn chỉnh cột
                wsPL.Cells[wsPL.Dimension.Address].AutoFitColumns();
                wsPL.Column(1).Width = 25; // Cột A
                wsPL.Column(2).Width = 20; // Cột B
                wsPL.Column(4).Width = 25; // Cột D
                wsPL.Column(5).Width = 20; // Cột E

                // --- 3. SHEET 2: TOP SẢN PHẨM ---
                var wsTop = package.Workbook.Worksheets.Add("Top Sản Phẩm");

                wsTop.Cells["A1:C1"].Value = "TOP SẢN PHẨM BÁN CHẠY";
                styleMainTitle(wsTop.Cells["A1:C1"]);

                wsTop.Cells["A2:C2"].Value = $"Từ ngày {startDate:dd/MM/yyyy} đến {endDate:dd/MM/yyyy}";
                styleSubTitle(wsTop.Cells["A2:C2"]);

                // Dùng LoadFromCollection để tạo bảng tự động
                wsTop.Cells["A4"].LoadFromCollection(data.TopSanPham, true, OfficeOpenXml.Table.TableStyles.Medium9);

                // Định dạng lại cột số lượng và tiền tệ trong bảng
                var tbl = wsTop.Tables[0];
                tbl.Columns["TongSoLuongBan"].TotalsRowFunction = OfficeOpenXml.Table.RowFunctions.Sum;
                wsTop.Cells[tbl.Address.Start.Row + 1, tbl.Columns["TongSoLuongBan"].Position + 1, tbl.Address.End.Row, tbl.Columns["TongSoLuongBan"].Position + 1].Style.Numberformat.Format = "#,##0";

                tbl.Columns["TongDoanhThu"].TotalsRowFunction = OfficeOpenXml.Table.RowFunctions.Sum;
                wsTop.Cells[tbl.Address.Start.Row + 1, tbl.Columns["TongDoanhThu"].Position + 1, tbl.Address.End.Row, tbl.Columns["TongDoanhThu"].Position + 1].Style.Numberformat.Format = currencyFormat;

                wsTop.Cells[wsTop.Dimension.Address].AutoFitColumns();

                // --- 4. LƯU FILE ---
                await package.SaveAsync();
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}