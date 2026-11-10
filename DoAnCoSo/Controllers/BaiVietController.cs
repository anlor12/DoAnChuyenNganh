using DoAnCoSo.Data;
using DoAnCoSo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// Controller hiển thị danh sách bài viết & chi tiết tin tức thời trang cho khách hàng.
    /// </summary>
    public class BaiVietController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BaiVietController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: /BaiViet
        /// Trang danh sách bài viết thời trang phong cách tạp chí
        /// </summary>
        public async Task<IActionResult> Index(string? tuKhoa, string? chuyenMuc)
        {
            var query = _context.BaiViets.Where(b => b.TrangThai);

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                tuKhoa = tuKhoa.Trim().ToLower();
                query = query.Where(b => b.TieuDe.ToLower().Contains(tuKhoa) || 
                                         (b.TomTat != null && b.TomTat.ToLower().Contains(tuKhoa)));
                ViewBag.TuKhoa = tuKhoa;
            }

            if (!string.IsNullOrWhiteSpace(chuyenMuc) && chuyenMuc != "All")
            {
                query = query.Where(b => b.ChuyenMuc == chuyenMuc);
                ViewBag.ChuyenMuc = chuyenMuc;
            }

            var allPosts = await query.OrderByDescending(b => b.NoiBat)
                                      .ThenByDescending(b => b.NgayTao)
                                      .ToListAsync();

            ViewBag.FeaturedPost = allPosts.FirstOrDefault(b => b.NoiBat) ?? allPosts.FirstOrDefault();
            ViewBag.Categories = await _context.BaiViets
                .Where(b => b.TrangThai && !string.IsNullOrEmpty(b.ChuyenMuc))
                .Select(b => b.ChuyenMuc!)
                .Distinct()
                .ToListAsync();

            return View(allPosts);
        }

        /// <summary>
        /// GET: /BaiViet/ChiTiet/5 hoặc /BaiViet/ChiTiet/ten-slug-bai-viet
        /// Xem chi tiết một bài viết theo ID hoặc Slug thân thiện
        /// </summary>
        [HttpGet]
        [Route("BaiViet/ChiTiet/{idOrSlug?}")]
        public async Task<IActionResult> ChiTiet(string? idOrSlug, [FromQuery] int? id)
        {
            if (string.IsNullOrWhiteSpace(idOrSlug) && id.HasValue)
            {
                idOrSlug = id.Value.ToString();
            }

            if (string.IsNullOrWhiteSpace(idOrSlug))
            {
                return RedirectToAction(nameof(Index));
            }

            BaiViet? post = null;
            if (int.TryParse(idOrSlug, out int numericId))
            {
                post = await _context.BaiViets.FirstOrDefaultAsync(b => b.MaBaiViet == numericId && b.TrangThai);
            }

            if (post == null)
            {
                post = await _context.BaiViets.FirstOrDefaultAsync(b => b.Slug == idOrSlug && b.TrangThai);
            }

            if (post == null)
            {
                return NotFound();
            }

            // Tăng lượt xem
            post.LuotXem += 1;
            await _context.SaveChangesAsync();

            // Bài viết liên quan
            var relatedPosts = await _context.BaiViets
                .Where(b => b.TrangThai && b.MaBaiViet != post.MaBaiViet && (b.ChuyenMuc == post.ChuyenMuc || b.NoiBat))
                .OrderByDescending(b => b.NgayTao)
                .Take(3)
                .ToListAsync();

            ViewBag.RelatedPosts = relatedPosts;

            return View(post);
        }
    }
}
