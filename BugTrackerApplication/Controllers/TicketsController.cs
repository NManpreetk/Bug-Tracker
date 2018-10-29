using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using BugTrackerApplication.Helper;
using BugTrackerApplication.Helpers;
using BugTrackerApplication.Models;
using Microsoft.AspNet.Identity;


namespace BugTrackerApplication.Controllers
{
    public class TicketsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        // GET: Tickets
        public ActionResult Index()
        {
            var tickets = db.Tickets.Include(t => t.Assignee).Include(t => t.Creator).Include(t => t.TicketPriority).Include(t => t.TicketStatus).Include(t => t.TicketType);
            return View(db.Tickets.ToList());
        }

        // GET: Tickets/Details/5
        [Authorize]
        public ActionResult Details(int id)
        {
            if (id == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Ticket ticket = db.Tickets.Find(id);
            if (ticket == null)
            {
                return HttpNotFound();
            }
            return View(ticket);
        }

        // GET: Tickets/Create
        [Authorize(Roles = "Submitter")]
        public ActionResult Create()
        {
            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name");
            ViewBag.AssigneeId = new SelectList(db.Users, "Id", "Name");
            ViewBag.CreatorId = new SelectList(db.Users, "Id", "Name");
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Title");
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Title");
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Title");
            ViewBag.Created = DateTime.Now;
            return View();
        }

        // POST: Tickets/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Submitter")]
        public ActionResult Create([Bind(Include = "Id,Title,Description,TicketTypeId,TicketPriorityId,TicketStatusId,CreatorId,AssigneeId,ProjectId")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                ticket.Created = DateTime.Now;
                ticket.CreatorId = User.Identity.GetUserId();
                ticket.TicketStatusId = 1;
                db.Tickets.Add(ticket);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name");
            ViewBag.AssigneeId = new SelectList(db.Users, "Id", "Name", ticket.AssigneeId);
            ViewBag.CreatorId = new SelectList(db.Users, "Id", "Name", ticket.CreatorId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Title", ticket.TicketPriorityId);
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Title", ticket.TicketStatusId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Title", ticket.TicketTypeId);
            ViewBag.Created = DateTime.Now;
            return View(ticket);
        }

        // GET: Tickets/Edit/5
        [Authorize]
        [Authorize(Roles = "Developer, Project Manager")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.Find(id);
            if (ticket == null)
            {
                return HttpNotFound();
            }
            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name");
            ViewBag.AssigneeId = new SelectList(db.Users, "Id", "Name", ticket.AssigneeId);
            ViewBag.CreatorId = new SelectList(db.Users, "Id", "Name", ticket.CreatorId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Title", ticket.TicketPriorityId);
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Title", ticket.TicketStatusId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Title", ticket.TicketTypeId);
            return View(ticket);
        }

        // POST: Tickets/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [Authorize(Roles = "Developer, Project Manager")]
        public ActionResult Edit([Bind(Include = "Id,Title,Description,TicketTypeId,TicketPriorityId,CreatorId,AssigneeId,ProjectId")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                var dateChanged = DateTimeOffset.Now;
                var changes = new List<TicketHistory>();

                var dbTicket = db.Tickets.First(p => p.Id == ticket.Id);
                dbTicket.Title = ticket.Title;
                dbTicket.Description = ticket.Description;
                dbTicket.TicketTypeId = ticket.TicketTypeId;
                dbTicket.Updated = dateChanged;

                var originalValues = db.Entry(dbTicket).OriginalValues;
                var currentValues = db.Entry(dbTicket).CurrentValues;

                foreach (var property in originalValues.PropertyNames)
                {
                    if (property == "Updated")
                    {
                    }
                    else
                    {
                        var originalValue = originalValues[property]?.ToString();
                        var currentValue = currentValues[property]?.ToString();

                        if (originalValue != currentValue)
                        {
                            var history = new TicketHistory();
                            history.Changed = dateChanged;
                            history.NewValue = GetValueFromKey(property, currentValue);
                            history.OldValue = GetValueFromKey(property, originalValue);
                            history.Property = property;
                            history.TicketId = dbTicket.Id;
                            history.UserId = User.Identity.GetUserId();
                            changes.Add(history);
                        }
                    }

                }
                db.TicketHistories.AddRange(changes);
                db.SaveChanges();
                //if (dbTicket.AssigneeId != null)
                //{
                //    var ticket1 = db.Tickets.Where(p => p.Id == dbTicket.AssigneeId).FirstOrDefault();
                //    var personalEmailService = new PersonalEmailService();
                //    var mailMessage = new MailMessage(WebConfigurationManager.AppSettings["emailto"], dbTicket.Assignee.Email);
                //    mailMessage.IsBodyHtml = true;
                //    personalEmailService.Send(mailMessage);
                //}
                return RedirectToAction("Index");
            }
            ViewBag.AssigneeId = new SelectList(db.Users, "Id", "Name", ticket.AssigneeId);
            ViewBag.CreatorId = new SelectList(db.Users, "Id", "Name", ticket.CreatorId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Title", ticket.TicketPriorityId);
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Title", ticket.TicketStatusId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Title", ticket.TicketTypeId);
            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name");
            return View(ticket);
        }

        private string GetValueFromKey(string propertyName, string key)
        {
            if (propertyName == "TicketTypeId")
            {
                return db.TicketTypes.Find(Convert.ToInt32(key)).Title;
            }
            if (propertyName == "TicketStatusId")
            {
                return db.TicketStatuses.Find(Convert.ToInt32(key)).Title;
            }
            if (propertyName == "TicketPriorityId")
            {
                return db.TicketPriorities.Find(Convert.ToInt32(key)).Title;
            }
            if (propertyName == "ProjectId")
            {
                return db.Projects.Find(Convert.ToInt32(key)).Name;
            }
            return key;
        }

        // GET: Tickets/Delete/5
        [Authorize]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.Find(id);
            if (ticket == null)
            {
                return HttpNotFound();
            }
            return View(ticket);
        }

        // POST: Tickets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Ticket ticket = db.Tickets.Find(id);
            db.Tickets.Remove(ticket);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize]
        [Authorize(Roles = "Admin,Project Manager,Submitter,Developer")]
        public ActionResult CreateComment(int id, string body, string UserId, TicketComment ticketComment)
        {
            if (id == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var ticket = db.Tickets
               .Where(p => p.Id == id)
               .FirstOrDefault();
            if (ticket == null)
            {
                return HttpNotFound();
            }
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin") ||
                    (User.IsInRole("Submitter") && ticket.CreatorId == UserId) ||
                    (User.IsInRole("Developer") && ticket.AssigneeId == UserId))
                {
                    var comment = new TicketComment();
                    comment.UserId = User.Identity.GetUserId();
                    comment.TicketId = ticket.Id;
                    comment.Created = DateTime.Now;
                    comment.Comment = body;
                    db.Comments.Add(comment);
                    db.SaveChanges();
                    var ticket1 = db.Tickets.Where(p => p.Id == ticketComment.TicketId).FirstOrDefault();
                    if (ticket1.AssigneeId != null)
                    {
                        var personalEmailService = new PersonalEmailService();
                        var mailMessage = new MailMessage(WebConfigurationManager.AppSettings["emailto"], ticket1.Assignee.Email);
                        mailMessage.IsBodyHtml = true;
                        personalEmailService.Send(mailMessage);
                    }
                    return RedirectToAction("Details", new { id = id });
                }
            }
            return RedirectToAction("Details", new { id = id });
        }


        [Authorize(Roles = "Admin")]
        public ActionResult AssignTickets(int id)
        {
            var model = new TicketAssignViewModel();
            model.Id = id;
            var ticket = db.Tickets.FirstOrDefault(p => p.Id == id);
            var users = db.Users.ToList();
            var userIdsAssignedToTicket = ticket.AssigneeId;
            model.UserList = new SelectList(users, "Id", "Name", userIdsAssignedToTicket);
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult AssignTickets(TicketAssignViewModel model)
        {
            var ticket = db.Tickets.FirstOrDefault(p => p.Id == model.Id);
            if (model.SelectedUsers != null)
            {
                foreach (var userId in model.SelectedUsers)
                {
                    var user = db.Users.FirstOrDefault(p => p.Id == userId);
                    ticket.AssigneeId = userId;
                }
            }
            //STEP 4: Save changes to the database
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        [Authorize(Roles = "Submitter")]
        public ActionResult ShowSubmitterTickets()
        {
            var userId = User.Identity.GetUserId();
            var tickets = db.Tickets
                .Where(t => t.CreatorId == userId)
                .Include(t => t.Comments)
                .Include(t => t.Assignee)
                .Include(t => t.Creator)
                .Include(t => t.TicketPriority)
                .Include(t => t.TicketStatus)
                .Include(t => t.TicketType);
            return View("Index", db.Tickets.ToList());
        }

        [Authorize(Roles = "Developer")]
        public ActionResult ShowDeveloperTickets()
        {
            var userId = User.Identity.GetUserId();
            var tickets = db.Tickets
                .Where(t => t.AssigneeId == userId)
                .Include(t => t.Comments)
                .Include(t => t.Assignee)
                .Include(t => t.Creator)
                .Include(t => t.TicketPriority)
                .Include(t => t.TicketStatus)
                .Include(t => t.TicketType);
            return View("Index", db.Tickets.ToList());
        }

        [Authorize(Roles = "Project Manager, Developer")]
        public ActionResult ShowPMDevTickets()
        {
            var userId = User.Identity.GetUserId();
            var tickets = db.Tickets
                .Where(ticket => ticket.Project.Users.Any(user => user.Id == userId))
                .Include(t => t.Comments)
                .Include(t => t.Assignee)
                .Include(t => t.Creator)
                .Include(t => t.TicketPriority)
                .Include(t => t.TicketStatus)
                .Include(t => t.TicketType);
            return View("Index", db.Tickets.ToList());
        }

        // GET: TicketAttachments/Create
        public ActionResult CreateAttachment()
        {
            ViewBag.TicketId = new SelectList(db.Tickets, "Id", "Title");
            return View();
        }

        // POST: TicketAttachments/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAttachment(int id, string UserId, [Bind(Include = "Description,FilePath")] TicketAttachment ticketAttachment, HttpPostedFileBase image)
        {
            if (ModelState.IsValid)
            {
                if (User.Identity.IsAuthenticated)
                {
                    if (ImageUploadValidator.IsWebFriendlyImage(image))
                    {
                        var fileName = Path.GetFileName(image.FileName);
                        image.SaveAs(Path.Combine(Server.MapPath("~/Uploads/"), fileName));
                        ticketAttachment.FilePath = "/Uploads/" + fileName;
                    }

                    ticketAttachment.TicketId = id;
                    ticketAttachment.UserId = User.Identity.GetUserId();
                    ticketAttachment.Created = DateTime.Now;
                    db.Attachments.Add(ticketAttachment);
                    db.SaveChanges();

                    var ticket = db.Tickets.Where(p => p.Id == ticketAttachment.TicketId).FirstOrDefault();
                    if (ticket.AssigneeId != null)
                    {
                        var personalEmailService = new PersonalEmailService();
                        var mailMessage = new MailMessage(WebConfigurationManager.AppSettings["emailto"], ticket.Assignee.Email);
                        mailMessage.IsBodyHtml = true;
                        personalEmailService.Send(mailMessage);
                    }

                    return RedirectToAction("Details", new { id = id });
                }
            }

            ViewBag.TicketId = new SelectList(db.Tickets, "Id", "Title", ticketAttachment.TicketId);
            return View(ticketAttachment);
        }

        public ActionResult MyProjectTickets()
        {
            var userId = User.Identity.GetUserId();
            var Db = db.Users.FirstOrDefault(p => p.Id == userId);
            var myProject = Db.Projects.Select(p => p.Id);
            var ticket = Db.AssignedTickets.Where(p => myProject.Contains(p.ProjectId)).ToList();
            return View(ticket);
        }

        public ActionResult AssignDeveloper(int ticketId)
        {
            var model = new DeveloperTickets();
            model.Id = ticketId;
            var ticket = db.Tickets.FirstOrDefault(p => p.Id == ticketId);
            var userRoleHelper = new UserRoleHelper();
            var users = userRoleHelper.UserInRole("Developer");
            model.UserList = new SelectList(users, "Id", "Name");
            return View(model);
        }

        [HttpPost]
        public ActionResult AssignDeveloper(DeveloperTickets model)
        {
            var ticket = db.Tickets.FirstOrDefault(p => p.Id == model.Id);
            ticket.AssigneeId = model.SelectedUser;
            db.SaveChanges();
            var user = db.Users.Where(p => p.Id == model.SelectedUser).FirstOrDefault();
            var personalEmailService = new PersonalEmailService();
            var mailMessage = new MailMessage(WebConfigurationManager.AppSettings["emailto"], user.Email);
            mailMessage.IsBodyHtml = true;
            personalEmailService.Send(mailMessage);
            return RedirectToAction("Index");
        }
    }
}