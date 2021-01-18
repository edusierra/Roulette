using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roulette.Models
{
    public class RouletteData
    {
        public int id { get; set; }
        public bool open { get; set; }
        public List<BetData> bets;
    }
}
