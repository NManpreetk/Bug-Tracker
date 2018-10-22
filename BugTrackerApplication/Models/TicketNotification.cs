using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BugTrackerApplication.Models
{
    public class TicketNotification
    {
        public int Id { get; set; }
        public int TicketId { get; set; } 
        public int UserId { get; set; }
        public ICollection<Ticket> Ticket { get; set; }

    }
}