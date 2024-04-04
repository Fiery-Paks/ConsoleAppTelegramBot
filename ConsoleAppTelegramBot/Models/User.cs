using System;
using System.Collections.Generic;

namespace ConsoleAppTelegramBot.Models
{
    public partial class User
    {
        public int Id { get; set; }
        public long Idtelegram { get; set; }
        public string? FullName { get; set; }
        public byte[]? Image { get; set; }
        public int NubmerPc { get; set; }
        public int Wave { get; set; }

        public virtual Wave WaveNavigation { get; set; } = null!;
    }
}
