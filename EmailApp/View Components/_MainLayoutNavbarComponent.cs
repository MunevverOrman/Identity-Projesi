using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using EmailApp.Context;
using EmailApp.Entities;

namespace EmailApp.ViewComponents
{
    public class _MainLayoutNavbarComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public _MainLayoutNavbarComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var lastMessages = await _context.Message
                .OrderByDescending(x => x.SendDate) 
                .Take(3)
                .ToListAsync();

            return View(lastMessages);
        }
    }
}
