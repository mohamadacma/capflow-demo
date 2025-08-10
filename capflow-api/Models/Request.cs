using System;
using System.Collections.Generic;

namespace CapFlow.Models;

public class Request {
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Type { get; set; } = "SOP Change";
    public string Description { get; set; } = "";
    public string RequestedBy { get; set; } = "";
    public bool RequiresCAPA { get; set; }
    public string Status { get; set; } = "New";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public List<ApprovalAction> Actions { get; set; } = new();
}

public class ApprovalAction {
    public int Id { get; set; }
    public Guid RequestId { get; set; }
    public Request? Request { get; set; }
    public string Actor { get; set; } = "";  
    public string Outcome { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime At { get; set; } = DateTime.UtcNow;
}

public class CAPA {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public string RootCause { get; set; } = "";
    public string CorrectiveAction { get; set; } = "";
    public string PreventiveAction { get; set; } = "";
    public DateTime? DueDate { get; set; }
    public string Owner { get; set; } = "";
    public string Status { get; set; } = "Open"; 
}

public class User {
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "Tech"; 
}