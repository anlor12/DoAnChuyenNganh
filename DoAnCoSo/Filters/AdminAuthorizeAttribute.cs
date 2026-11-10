using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DoAnCoSo.Filters
{
    /// <summary>
    /// Bộ lọc phân quyền hành động (Action Filter) dành cho quản trị viên (Admin).
    /// Tự động kiểm tra phiên đăng nhập và vai trò trước khi thực thi Action.
    /// </summary>
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Phương thức chạy trước khi một Action được thực thi để kiểm tra quyền truy cập.
        /// </summary>
        /// <param name="context">Ngữ cảnh thực thi của Action</param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Lấy tên Controller và Action đang được yêu cầu truy cập
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();

            // Nếu người dùng đang cố gắng truy cập vào AuthController (Trang đăng nhập/đăng xuất/đăng ký)
            // thì cho phép đi qua không cần kiểm tra quyền Admin
            if (controllerName == "Auth")
            {
                base.OnActionExecuting(context);
                return;
            }

            // Lấy thông tin người dùng và vai trò từ Session
            var adminUser = context.HttpContext.Session.GetString("AdminUser");
            var vaiTro = context.HttpContext.Session.GetString("VaiTro");

            // Kiểm tra xem người dùng đã đăng nhập chưa và có phải là Quản trị viên hay không
            if (string.IsNullOrEmpty(adminUser) || vaiTro != "Quản trị viên")
            {
                // Nếu chưa đăng nhập hoặc không có quyền Admin, chuyển hướng về trang Đăng nhập của AuthController
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // Nếu hợp lệ, tiếp tục thực thi Action như bình thường
            base.OnActionExecuting(context);
        }
    }
}