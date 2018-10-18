using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
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
        [Authorize]
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
        [Authorize]
        public ActionResult Create([Bind(Include = "Id,Title,Description,TicketTypeId,TicketPriorityId,TicketStatusId,CreatorId,AssigneeId,ProjectId")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                ticket.Created = DateTime.Now;
                ticket.CreatorId = User.Identity.GetUserId();
                ticket.ProjectId = 1;
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

                foreach(var property in originalValues.PropertyNames)
                {
                    var originalValue = originalValues[property]?.ToString();
                    var currentValue = currentValues[property]?.ToString();

                    if(originalValue != currentValue)
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
                db.TicketHistories.AddRange(changes);
                db.SaveChanges();
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
            if(propertyName == "TicketTypeId")
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
        public ActionResult CreateComment(int id, string body)
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
            var comment = new TicketComment();
            comment.UserId = User.Identity.GetUserId();
            comment.TicketId = ticket.Id;
            comment.Created = DateTime.Now;
            comment.Comment = body;
            db.Comments.Add(comment);
            db.SaveChanges();
            return RedirectToAction("Details", new { id = id });
        }

        public ActionResult AssignTickets(int id)
        {
            var model = new TicketAssignViewModel();
            model.Id = id;
            var ticket = db.Tickets.FirstOrDefault(p => p.Id == id);
            var users = db.Users.ToList();
            var userIdsAssignedToTicket = ticket.Users
                .Select(p => p.Id).ToList();
            model.UserList = new SelectList(users, "Id", "Name", userIdsAssignedToTicket);
            return View(model);
        }
        [HttpPost]
        public ActionResult AssignTickets(TicketAssignViewModel model)
        {
            var ticket = db.Tickets.FirstOrDefault(p => p.Id == model.Id);
            var assignedUsers = ticket.Users.ToList();
            foreach (var user in assignedUsers)
            {
                ticket.Users.Remove(user);
            }
            if (model.SelectedUsers != null)
            {
                foreach (var userId in model.SelectedUsers)
                {
                    var user = db.Users.FirstOrDefault(p => p.Id == userId);
                    ticket.Users.Add(user);
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
                .Include(t=>t.Comments)
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
        public ActionResult CreateAttachment(int id,[Bind(Include = "Description,FilePath")] TicketAttachment ticketAttachment, HttpPostedFileBase image)
        {
            if (ModelState.IsValid)
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
                return View();
            }

            ViewBag.TicketId = new SelectList(db.Tickets, "Id", "Title", ticketAttachment.TicketId);
            return View(ticketAttachment);
        }
    }
}