using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BugTrackerApplication.Models
{
    public class TicketType
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public ICollection<Ticket> Ticket { get; set; }
    }
}