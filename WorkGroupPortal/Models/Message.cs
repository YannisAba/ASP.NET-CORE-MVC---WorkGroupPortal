using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WorkGroupPortal.Models;

public partial class Message
{
    [Key]
    public int Id { get; set; }

    public int GroupId { get; set; }

    public int SenderId { get; set; }

    public string Content { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime SentAt { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("Messages")]
    public virtual Group Group { get; set; } = null!;

    [ForeignKey("SenderId")]
    [InverseProperty("Messages")]
    public virtual User Sender { get; set; } = null!;
}
