using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwilioSmsUtils
{
    internal class TwilioMessageListPage
    {
        public string NextPageUri { get; set; }
        public List<TwilioSmsDetails> SmsList { get; set; }
    }
}
