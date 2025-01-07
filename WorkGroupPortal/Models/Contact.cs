using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WorkGroupPortal.Models;

public partial class Contact
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ContactId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = null!;

    [ForeignKey("ContactId")]
    [InverseProperty("ContactContactNavigations")]
    public virtual User ContactNavigation { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("ContactUsers")]
    public virtual User User { get; set; } = null!;
}
