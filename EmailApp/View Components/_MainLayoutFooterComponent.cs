using Microsoft.AspNetCore.Mvc;

namespace EmailApp.View_Components
{
    public class _MainLayoutFooterComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
