using System;
using System.Collections.Generic;

namespace Pracrice.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Login { get; set; } = null!;

    public string Pass { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public int Role { get; set; }

    public DateTime? LastChanges { get; set; }

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
}
