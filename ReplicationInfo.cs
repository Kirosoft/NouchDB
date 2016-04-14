using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NDB
{
    class ReplicationInfo
    {
        public string _id { get; set; }
        public string source { get; set; }
        public string target { get; set; }
        public bool create_target { get; set; }

        public string _replication_id { get; set; }
        public string _replication_state { get; set; }
        public string _replication_state_time { get; set; }
    }
}
