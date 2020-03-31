using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GGBet
{
    class GGBetEvent
    {
        public string EventID;
        public string LeagueName;
        public string TeamString;

        public Dictionary<string, dynamic> OddList = new Dictionary<string, dynamic>();

        public _Teams Teams = new _Teams();

        public class _Teams
        {
            public string Team1 { get; set;  }
            public string Team2 { get; set; }
        }

        public dynamic Get()
        {
            return this;
        }
    }
}
