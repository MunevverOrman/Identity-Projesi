using EmailApp.Context;
using EmailApp.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmailApp.View_Components
{
    public class _MainLayoutSidebarComponent(UserManager<AppUser> userManager,AppDbContext _context): ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await userManager.FindByNameAsync(User.Identity.Name);
            ViewBag.ReceivedMessage=_context.Message
                .Include(x=>x.Sender)
                . Where(x=>x.ReceiverId==user.Id && x.IsDeleted==false)
                .Count();
            ViewBag.sendedMessage = _context.Message
                .Include(x=>x.Receiver)
                .Where(x=>x.SenderId==user.Id )
                .Count();
            ViewBag.draftMessage=_context.Message
                .Include(x=>x.Receiver)
                .Where(x=>x.SenderId==user.Id && x.IsDraft==true)
                .Count();
            ViewBag.deletedMessage = _context.Message
                .Include(x => x.Sender)
                .Where(x => x.ReceiverId == user.Id && x.IsDeleted == true)
                .Count();
            ViewBag.ImportantMessage = _context.Message
                .Where(m => m.IsImportant && (m.ReceiverId == user.Id))
                .Count();
            return View();
        }

    }
}
