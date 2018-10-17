using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BugTrackerApplication.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public int TicketTypeId { get; set; }
        public virtual TicketType TicketType { get; set; }
        public int TicketPriorityId { get; set; }
        public virtual TicketPriority TicketPriority { get; set; }
        public int TicketStatusId { get; set; }
        public virtual TicketStatus TicketStatus { get; set; }
        public string CreatorId { get; set; }
        public virtual ApplicationUser Creator { get; set; }
        public string AssigneeId { get; set; }
        public virtual ApplicationUser Assignee { get; set; }
        public int ProjectId { get; set; }
        public virtual Project Project { get; set; }
        public string Slug { get; set; }
        
        public Ticket()
        {
            Users = new HashSet<ApplicationUser>();
            Attachments = new HashSet<TicketAttachment>();
            Comments = new HashSet<TicketComment>();
            Histories = new HashSet<TicketHistory>();


        }

        public virtual ICollection<ApplicationUser> Users { get; set; }
        public virtual ICollection<TicketAttachment> Attachments { get; set; }
        public virtual ICollection<TicketComment> Comments { get; set; }
        public virtual ICollection<TicketHistory> Histories { get; set; }


    }
}