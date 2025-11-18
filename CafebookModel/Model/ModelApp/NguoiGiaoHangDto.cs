namespace CafebookModel.Model.ModelApp
{
    /// <summary>
    /// DTO đầy đủ để quản lý thêm/sửa xóa Shipper
    /// </summary>
    public class NguoiGiaoHangCrudDto
    {
        public int IdNguoiGiaoHang { get; set; }
        public string TenNguoiGiaoHang { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string TrangThai { get; set; } = "Sẵn sàng"; // "Sẵn sàng", "Tạm ngưng"
    }
}