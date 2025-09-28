using Microsoft.AspNetCore.Mvc;

namespace EmailApp.View_Components
{
    public class _MainLayoutScriptsComponent: ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
