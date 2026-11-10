using DoAnCoSo.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCoSo.ViewComponents
{
    /// <summary>
    /// Thành phần hiển thị (ViewComponent) dùng để tính toán và hiển thị huy hiệu (badge)
    /// chứa tổng số lượng sản phẩm đang có trong giỏ hàng của người dùng hiện tại.
    /// </summary>
    public class CartBadgeViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Khởi tạo CartBadgeViewComponent với DbContext được Inject vào.
        /// </summary>
        /// <param name="context">Ngữ cảnh cơ sở dữ liệu</param>
        public CartBadgeViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Thực thi tính toán số lượng sản phẩm trong giỏ hàng không đồng bộ.
        /// </summary>
        /// <returns>Huy hiệu số lượng giỏ hàng được trả về dưới dạng một View với dữ liệu là số lượng (int)</returns>
        public async Task<IViewComponentResult> InvokeAsync()
        {
            int itemCount = 0;
            // Lấy tên đăng nhập của tài khoản từ Session
            var username = HttpContext.Session.GetString("AdminUser");

            // Nếu người dùng đã đăng nhập
            if (!string.IsNullOrEmpty(username))
            {
                // Tìm thông tin tài khoản kèm theo thông tin cá nhân (NguoiDung) tương ứng
                var user = await _context.TaiKhoans
                    .Include(t => t.NguoiDung)
                    .FirstOrDefaultAsync(t => t.TenDangNhap == username);

                // Nếu tài khoản tồn tại và có thông tin người dùng liên kết
                if (user != null && user.NguoiDung != null)
                {
                    // Lấy giỏ hàng của người dùng này cùng danh sách chi tiết các mặt hàng trong đó
                    var cart = await _context.GioHangs
                        .Include(g => g.ChiTietGioHangs)
                        .FirstOrDefaultAsync(g => g.MaNguoiDung == user.NguoiDung.MaNguoiDung);

                    // Nếu có giỏ hàng và danh sách sản phẩm không rỗng
                    if (cart != null && cart.ChiTietGioHangs != null)
                    {
                        // Tính tổng số lượng (cộng dồn thuộc tính SoLuong của từng sản phẩm trong giỏ)
                        itemCount = cart.ChiTietGioHangs.Sum(c => c.SoLuong);
                    }
                }
            }

            // Trả về View mặc định (Default.cshtml của component) cùng tham số là tổng số lượng
            return View(itemCount);
        }
    }
}
