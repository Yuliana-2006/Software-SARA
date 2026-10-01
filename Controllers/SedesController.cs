using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Software_SARA.Controllers
{
    public class SedesController : Controller
    {
        // GET: SedesController
        public ActionResult RegistrarSede()
        {
            return View();
        }

        // GET: SedesController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: SedesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: SedesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: SedesController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: SedesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: SedesController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: SedesController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
