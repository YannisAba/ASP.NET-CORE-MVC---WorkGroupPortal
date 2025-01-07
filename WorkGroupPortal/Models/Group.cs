using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WorkGroupPortal.Models;

public partial class Group
{
    [Key]
    public int Id { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = null!;

    public int CreatedById { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("CreatedById")]
    [InverseProperty("Groups")]
    public virtual User CreatedBy { get; set; } = null!;

    [InverseProperty("Group")]
    public virtual ICollection<GroupInvitation> GroupInvitations { get; set; } = new List<GroupInvitation>();

    [InverseProperty("Group")]
    public virtual ICollection<GroupUser> GroupUsers { get; set; } = new List<GroupUser>();

    [InverseProperty("Group")]
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
