using EmailApp.Context;
using EmailApp.Entities;
using EmailApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmailApp.Controllers
{
    [Authorize]
    public class MessageController(AppDbContext _context,UserManager<AppUser> _userManager) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            var messages = _context.Message
                .Include(x => x.Sender)
                .Where(x => x.ReceiverId == user.Id && !x.IsDeleted && !x.IsArchived)
                .OrderByDescending(x => x.SendDate)
                .ToList();

            return View(messages);
        }

        public async Task<IActionResult> Outbox()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            var messages = _context.Message
                .Include(x => x.Receiver)
                .Where(x => x.SenderId == user.Id && !x.IsDeleted && !x.IsArchived)
                .OrderByDescending(x => x.SendDate)
                .ToList();

            return View(messages);
        }


        public IActionResult MessageDetail(int id)
        {
            var message = _context.Message.Include(x => x.Sender).FirstOrDefault(x => x.MessageId == id);
            return View(message);
        }

        public IActionResult SendMessage()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> SendMessage(SendMessageViewModel model, string actionType)
        {
            int? draftId = TempData["DraftId"] as int?;
            Message message;

            var sender = await _userManager.FindByNameAsync(User.Identity.Name);
            var receiver = !string.IsNullOrEmpty(model.ReceiverEmail)
                ? await _userManager.FindByEmailAsync(model.ReceiverEmail)
                : null;

            if (draftId.HasValue)
            {
                message = await _context.Message.FindAsync(draftId.Value);
                message.Body = model.Body;
                message.Subject = model.Subject;
                message.ReceiverId = receiver?.Id ?? 0;
                message.IsDraft = actionType == "draft";
                if (actionType == "send")
                    message.SendDate = DateTime.Now;
                _context.SaveChanges();
            }
            else
            {
                message = new Message
                {
                    Body = model.Body,
                    Subject = model.Subject,
                    ReceiverId = receiver?.Id ?? 0,
                    SenderId = sender.Id,
                    SendDate = DateTime.Now,
                    IsDraft = actionType == "draft"
                };
                _context.Message.Add(message);
                _context.SaveChanges();
            }

            if (actionType == "draft")
                return RedirectToAction("Drafts");
            return RedirectToAction("Outbox");
        }

        public async Task<IActionResult> Trash()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var messages = _context.Message
                .Include(x => x.Sender)
                .Where(x => (x.ReceiverId == user.Id || x.SenderId == user.Id) && x.IsDeleted)
                .OrderByDescending(x => x.SendDate)
                .ToList();

            return View(messages);
        }
        [HttpPost]
        public async Task<IActionResult> ArchiveFromTrash(int messageId)
        {
            var message = await _context.Message.FindAsync(messageId);
            if (message != null)
            {
                message.IsArchived = true;
                message.IsDeleted = false; // çöp kutusundan çıkar
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Trash");
        }

        public async Task<IActionResult> Drafts()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            var messages = _context.Message
                .Include(x => x.Sender)
                .Include(x => x.Receiver)
                .Where(x => x.SenderId == user.Id && x.IsDraft && !x.IsDeleted)
                .OrderByDescending(x => x.SendDate)
                .ToList();

            return View(messages);
        }
        [HttpGet]
        public async Task<IActionResult> EditDraft(int messageId)
        {
            var message = await _context.Message.FindAsync(messageId);
            if (message == null || !message.IsDraft)
                return NotFound();

            var model = new SendMessageViewModel
            {
                ReceiverEmail = message.Receiver != null ? message.Receiver.Email : "",
                Subject = message.Subject,
                Body = message.Body
            };
            TempData["DraftId"] = messageId; 
            return View("SendMessage", model);
        }


        [HttpPost]
        public IActionResult MoveToDraft(int messageId)
        {
            var message = _context.Message.FirstOrDefault(m => m.MessageId == messageId);
            if (message != null)
            {
                message.IsDraft = true;
                message.IsDeleted = false;
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> SendDraft(int messageId)
        {
            var message = await _context.Message.FindAsync(messageId);
            if (message != null)
            {
                message.IsDraft = false;            
                message.SendDate = DateTime.Now;     
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Mesaj gönderildi.";
            }

            return RedirectToAction("Outbox");       
        }

        public async Task<IActionResult> ImportantAll()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            var messages = _context.Message
                .Include(x => x.Sender)
                .Include(x => x.Receiver)
                .Where(x => (x.ReceiverId == user.Id || x.SenderId == user.Id)
                && x.IsImportant
                && !x.IsDeleted
                && !x.IsArchived) 
               .OrderByDescending(x => x.SendDate)
               .ToList();


            return View("ImportantAll", messages);
        }
        [HttpPost]
        public async Task<IActionResult> ArchiveFromImportant(int messageId)
        {
            var message = await _context.Message.FindAsync(messageId);
            if (message != null)
            {
                message.IsArchived = true;   
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ImportantAll"); 
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFromImportant(int messageId)
        {
            var message = await _context.Message.FindAsync(messageId);
            if (message != null)
            {
                message.IsDeleted = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ImportantAll");
        }


        [HttpPost]
        public IActionResult MarkAsImportant(int messageId)
        {
            var message = _context.Message.FirstOrDefault(m => m.MessageId == messageId);
            if (message != null)
            {
                message.IsImportant = true;
                _context.SaveChanges();
            }
            return RedirectToAction("Index"); 
        }


        [HttpPost]
        public IActionResult RestoreMessage(int messageId)
        {
            var message = _context.Message.FirstOrDefault(m => m.MessageId == messageId);
            if (message != null)
            {
                message.IsDeleted = false;  
                _context.SaveChanges();
            }
            return RedirectToAction("Trash"); 
        }
        [HttpPost]
        public IActionResult DeleteMessage(int messageId)
        {
            var message = _context.Message.FirstOrDefault(m => m.MessageId == messageId);
            if (message != null)
            {
                _context.Message.Remove(message); 
                _context.SaveChanges();
            }
            return RedirectToAction("Trash"); 
        }
        [HttpPost]
        public IActionResult ToggleRead(int messageId)
        {
            var message = _context.Message.Find(messageId);
            if (message != null)
            {
                message.IsRead = !message.IsRead;
                _context.SaveChanges();
            }
           
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult ToggleImportant(int messageId)
        {
            var message = _context.Message.Find(messageId);
            if (message != null)
            {
                message.IsImportant = !message.IsImportant;
                _context.SaveChanges();
            }
   
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult ToggleImportantOutbox(int messageId)
        {
            var message = _context.Message.Find(messageId);
            if (message != null)
            {
                message.IsImportant = !message.IsImportant;
                _context.SaveChanges();
            }

           
            return RedirectToAction("Outbox");
        }
        public async Task<IActionResult> Archived()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            var messages = _context.Message
                .Include(x => x.Sender)
                .Include(x => x.Receiver)
                .Where(x => (x.ReceiverId == user.Id || x.SenderId == user.Id) && x.IsArchived && !x.IsDeleted)
                .OrderByDescending(x => x.SendDate)
                .ToList();

            ViewBag.ArchivedMessage = messages.Count;

            return View(messages); 
        }
        [HttpPost]
        public async Task<IActionResult> RestoreFromArchive(int messageId)
        {
            var message = await _context.Message.FindAsync(messageId);
            if (message != null)
            {
                message.IsArchived = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Archived"); 
        }
        [HttpPost]
        public async Task<IActionResult> MoveToTrashFromArchive(int messageId)
        {
            var message = await _context.Message.FindAsync(messageId);
            if (message != null)
            {
                message.IsDeleted = true;
                message.IsArchived = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Archived");
        }



        [HttpPost]
        public IActionResult Archive(int messageId)
        {
            var message = _context.Message.Find(messageId);
            if (message != null)
            {
                message.IsArchived = true;
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> ArchiveDraft(int messageId)
        {
            var message = await _context.Message.FindAsync(messageId);
            if (message != null)
            {
                message.IsDraft = false;     
                message.IsArchived = true;  
                _context.SaveChanges();
            }

            return RedirectToAction("Drafts"); 
        }

        [HttpPost]
        public IActionResult Snooze(int messageId)
        {
            var message = _context.Message.Find(messageId);
            if (message != null)
            {
                message.IsSnoozed = true;
                message.SnoozeDate = DateTime.Now.AddHours(1);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> MoveToTrashFromIndex(int messageId)
        {
            var message = await _context.Message.FindAsync(messageId);
            message.IsDeleted = true;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> MoveToTrashFromOutbox(int messageId)
        {
            var message = await _context.Message.FindAsync(messageId);
            if (message != null)
            {
                message.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Outbox"); 
        }

        [HttpPost]
        public async Task<IActionResult> ArchiveFromOutbox(int messageId)
        {
            var message = await _context.Message.FindAsync(messageId);
            if (message != null)
            {
                message.IsArchived = true;   
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Outbox"); 
        }

        [HttpPost]
        public IActionResult ToggleReadDraft(int messageId)
        {
            var message = _context.Message.Find(messageId);
            if (message != null)
            {
                message.IsRead = !message.IsRead;
                _context.SaveChanges();
            }
            return RedirectToAction("Drafts"); // Taslak sayfasında kalır
        }

        [HttpPost]
        public IActionResult ToggleImportantDraft(int messageId)
        {
            var message = _context.Message.Find(messageId);
            if (message != null)
            {
                message.IsImportant = !message.IsImportant;
                _context.SaveChanges();
            }
            return RedirectToAction("Drafts"); // Taslak sayfasında kalır
        }

    }
}
