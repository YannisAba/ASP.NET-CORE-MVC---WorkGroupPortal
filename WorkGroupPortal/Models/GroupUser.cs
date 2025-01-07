using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WorkGroupPortal.Models;

public partial class GroupUser
{
    [Key]
    public int Id { get; set; }

    public int GroupId { get; set; }

    public int UserId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("GroupUsers")]
    public virtual Group Group { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("GroupUsers")]
    public virtual User User { get; set; } = null!;
}
