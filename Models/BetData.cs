using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Roulette.Models
{
    public class BetData
    {
        public int Roulettid { get; set; }
        [Range(1, 1000, ErrorMessage ="El valor no es permitido")]
        public int Bet { get; set; }
        public string Username { get; set; }
        [Range(9, 26, ErrorMessage ="El número no es válido")]
        public int Number { get; set; }
        public string Color { get; set; }
        public bool Winner { get; set; }
        public double Prize { get; set; }
    }
}
