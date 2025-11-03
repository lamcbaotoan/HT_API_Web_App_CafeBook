namespace CafebookModel.Model.ModelApp
{
    // DTO để truyền dữ liệu từ bảng CaiDat
    public class CaiDatDto
    {
        // Khóa chính
        public string TenCaiDat { get; set; } = string.Empty;

        // Dữ liệu cần sửa
        public string GiaTri { get; set; } = string.Empty;

        // Chỉ để hiển thị
        public string? MoTa { get; set; }
    }
}