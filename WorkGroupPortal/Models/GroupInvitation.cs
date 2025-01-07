using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WorkGroupPortal.Models;

public partial class GroupInvitation
{
    [Key]
    public int Id { get; set; }

    public int GroupId { get; set; }

    public int SenderId { get; set; }

    public int ReceiverId { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? RespondedAt { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("GroupInvitations")]
    public virtual Group Group { get; set; } = null!;

    [ForeignKey("ReceiverId")]
    [InverseProperty("GroupInvitationReceivers")]
    public virtual User Receiver { get; set; } = null!;

    [ForeignKey("SenderId")]
    [InverseProperty("GroupInvitationSenders")]
    public virtual User Sender { get; set; } = null!;
}
