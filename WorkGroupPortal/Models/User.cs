using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WorkGroupPortal.Models;

[Index("Email", Name = "UQ__Users__A9D10534E2F5631E", IsUnique = true)]
public partial class User
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    public string Username { get; set; } = null!;

    [StringLength(255)]
    public string Email { get; set; } = null!;

    [StringLength(255)]
    public string Password { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [InverseProperty("ContactNavigation")]
    public virtual ICollection<Contact> ContactContactNavigations { get; set; } = new List<Contact>();

    [InverseProperty("User")]
    public virtual ICollection<Contact> ContactUsers { get; set; } = new List<Contact>();

    [InverseProperty("Receiver")]
    public virtual ICollection<GroupInvitation> GroupInvitationReceivers { get; set; } = new List<GroupInvitation>();

    [InverseProperty("Sender")]
    public virtual ICollection<GroupInvitation> GroupInvitationSenders { get; set; } = new List<GroupInvitation>();

    [InverseProperty("User")]
    public virtual ICollection<GroupUser> GroupUsers { get; set; } = new List<GroupUser>();

    [InverseProperty("CreatedBy")]
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    [InverseProperty("Sender")]
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
