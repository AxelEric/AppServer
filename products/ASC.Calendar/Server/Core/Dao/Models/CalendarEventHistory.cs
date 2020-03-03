﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASC.Calendar.Core.Dao.Models
{
    [Table("calendar_event_history")]
    public partial class CalendarEventHistory
    {
        [Key]
        [Column("tenant", TypeName = "int(11)")]
        public int Tenant { get; set; }
        [Key]
        [Column("calendar_id", TypeName = "int(11)")]
        public int CalendarId { get; set; }
        [Key]
        [Column("event_uid", TypeName = "char(255)")]
        public string EventUid { get; set; }
        [Column("event_id", TypeName = "int(10)")]
        public int EventId { get; set; }
        [Column("ics", TypeName = "mediumtext")]
        public string Ics { get; set; }
    }
}