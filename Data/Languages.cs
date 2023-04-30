using System;
using System.Collections.Generic;

namespace Site.Data
{
    public partial class Languages
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Language { get; set; }
        public string Culture { get; set; }
        public string TimeZoneId { get; set; }
    }
}
