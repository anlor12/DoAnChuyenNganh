using System.ComponentModel.DataAnnotations;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// ViewModel cấu hình và hiển thị trang Giới thiệu (About Us)
    /// </summary>
    public class CauHinhGioiThieuViewModel
    {
        // 1. Hero Section
        [Display(Name = "Dòng chữ phụ trên Hero")]
        public string HeroEyebrow { get; set; } = "Fashion Store — Câu chuyện của chúng tôi";

        [Display(Name = "Tiêu đề chính Hero")]
        public string HeroTitle { get; set; } = "Về Chúng Tôi";

        [Display(Name = "Hình ảnh nền Hero")]
        public string HeroImage { get; set; } = "/images/store-interior.jpg.jpg";

        // 2. Thống kê
        [Display(Name = "Số lượng sản phẩm")]
        public string StatProducts { get; set; } = "50+";

        [Display(Name = "Khách hàng tin tưởng")]
        public string StatCustomers { get; set; } = "10.000+";

        [Display(Name = "Năm kinh nghiệm")]
        public string StatYears { get; set; } = "5+";

        [Display(Name = "Tỉnh thành giao hàng")]
        public string StatProvinces { get; set; } = "63+";

        // 3. Sứ mệnh (Mission)
        [Display(Name = "Tiêu đề sứ mệnh")]
        public string MissionTitle { get; set; } = "Thời trang Không giới hạn";

        [Display(Name = "Đoạn 1 sứ mệnh")]
        public string MissionP1 { get; set; } = "Chúng tôi tin rằng thời trang không chỉ là quần áo — đó là ngôn ngữ của bản sắc. Mỗi sản phẩm được thiết kế với tư duy tối giản nhưng đậm chất cá tính, mang đến sự tự tin cho người mặc trong từng khoảnh khắc.";

        [Display(Name = "Đoạn 2 sứ mệnh")]
        public string MissionP2 { get; set; } = "Từ những bước chạy bộ buổi sáng đến những buổi tối đô thị sôi động — Fashion Store đồng hành cùng bạn với chất lượng vượt trội và phong cách không bao giờ lỗi thời.";

        // 4. Câu chuyện thương hiệu (Story)
        [Display(Name = "Tiêu đề câu chuyện")]
        public string StoryTitle { get; set; } = "Sinh ra từ đam mê thời trang";

        [Display(Name = "Đoạn 1 câu chuyện")]
        public string StoryP1 { get; set; } = "Khởi đầu từ một cửa hàng nhỏ với ước mơ lớn vào năm 2020, Fashion Store đã vươn mình trở thành một trong những thương hiệu thời trang được yêu thích nhất tại Việt Nam.";

        [Display(Name = "Đoạn 2 câu chuyện")]
        public string StoryP2 { get; set; } = "Chúng tôi không ngừng tìm kiếm những chất liệu tốt nhất, những thiết kế tinh tế nhất — tất cả vì một mục tiêu duy nhất: mang lại giá trị thực sự cho khách hàng.";

        [Display(Name = "Hình ảnh câu chuyện")]
        public string StoryImage { get; set; } = "/images/store-interior.jpg.jpg";

        // 5. Giá trị cốt lõi
        [Display(Name = "Giá trị 1: Tiêu đề")]
        public string Value1Title { get; set; } = "Chất lượng tuyệt đối";

        [Display(Name = "Giá trị 1: Mô tả")]
        public string Value1Desc { get; set; } = "Mỗi sản phẩm đều trải qua quy trình kiểm định nghiêm ngặt trước khi đến tay khách hàng. Không có chỗ cho sự thỏa hiệp.";

        [Display(Name = "Giá trị 2: Tiêu đề")]
        public string Value2Title { get; set; } = "Thiết kế hiện đại";

        [Display(Name = "Giá trị 2: Mô tả")]
        public string Value2Desc { get; set; } = "Cập nhật liên tục các xu hướng thời trang quốc tế, dung hòa hoàn hảo với phom dáng và khí hậu Việt Nam.";

        [Display(Name = "Giá trị 3: Tiêu đề")]
        public string Value3Title { get; set; } = "Tận tâm phục vụ";

        [Display(Name = "Giá trị 3: Mô tả")]
        public string Value3Desc { get; set; } = "Khách hàng là trung tâm của mọi quyết định. Chính sách đổi trả linh hoạt và hỗ trợ tư vấn 24/7.";
    }
}
