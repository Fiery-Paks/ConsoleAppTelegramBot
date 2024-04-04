using System;
using System.Collections.Generic;

namespace ConsoleAppTelegramBot.Models
{
    public partial class Wave
    {
        public Wave()
        {
            Users = new HashSet<User>();
        }

        public Wave(int id)
        {
            Id = id;
        }

        public int Id { get; set; }

        public virtual ICollection<User> Users { get; set; }
    }
}
