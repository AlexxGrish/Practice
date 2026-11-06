using System;
using System.Collections.Generic;

namespace Pracrice.Models;

public partial class Report
{
    public int ReportId { get; set; }

    public int ReportType { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? UserId { get; set; }

    public int? RouteId { get; set; }

    public virtual Route? Route { get; set; }

    public virtual User? User { get; set; }
}
