using Microsoft.AspNetCore.Mvc;

// https://localhost:7035/ToDoes/Index/

namespace WebApplication1.Controllers
{
    public class ToDoesController : Controller
    {
        //public ToDoesController(Hoge hoge) { }

        public IActionResult Index()
        {
            return View();
        }
    }
}
