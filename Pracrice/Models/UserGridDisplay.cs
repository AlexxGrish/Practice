using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pracrice.Models
{
    internal class UserGridDisplay
    {
        public int UserId { get; set; }
        public string UserLogin { get; set; }
        public string FullName { get; set; }
        public string UserRole { get; set; }
        public string Status { get; set; }
        public string LastChangesText { get; set; }
    }
}
