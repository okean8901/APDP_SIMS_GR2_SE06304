﻿namespace StudentManagementSystem.Models.Entities
{
    public class UserRole
    {
        public int UserRoleId { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public DateTime AssignedAt { get; set; }
        public virtual User User { get; set; }
        public virtual Role Role { get; set; }
    }
}
