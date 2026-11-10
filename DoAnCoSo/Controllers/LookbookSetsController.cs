using Microsoft.AspNetCore.Mvc;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// Chuyển hướng các đường dẫn cũ sang BoSuuTapsController mới (100% Tiếng Việt).
    /// </summary>
    public class LookbookSetsController : Controller
    {
        public IActionResult Index() => RedirectToAction("Index", "BoSuuTaps");
        public IActionResult Create() => RedirectToAction("Create", "BoSuuTaps");
        public IActionResult Edit(int? id) => RedirectToAction("Edit", "BoSuuTaps", new { id });
        public IActionResult Delete(int? id) => RedirectToAction("Delete", "BoSuuTaps", new { id });
    }
}
