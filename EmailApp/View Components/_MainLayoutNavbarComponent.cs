using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using EmailApp.Context;
using EmailApp.Entities;
using Microsoft.AspNetCore.Identity;

namespace EmailApp.ViewComponents
{
    public class _MainLayoutNavbarComponent : ViewComponent
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public _MainLayoutNavbarComponent(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

       
            var lastMessages = await _context.Message
                .Where(x => x.ReceiverId == user.Id)
                .Include(x => x.Sender)
                .OrderByDescending(x => x.SendDate)
                .Take(3)
                .ToListAsync();

            return View(lastMessages);
        }
    }
}
