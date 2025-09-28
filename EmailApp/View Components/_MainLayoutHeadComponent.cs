using Microsoft.AspNetCore.Mvc;

namespace EmailApp.View_Components
{
    public class _MainLayoutHeadComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
