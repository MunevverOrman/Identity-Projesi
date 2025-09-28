using Microsoft.AspNetCore.Mvc;

namespace EmailApp.View_Components
{
    public class _MainLayoutHeaderComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}