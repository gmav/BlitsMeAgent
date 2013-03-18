﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BlitsMe.Cloud.Messaging.Request
{
    [DataContract]
    public class SearchRq : API.Request
    {
        public override string type
        {
            get { return "Search-RQ"; }
            set { }
        }

        [DataMember] public string query;
    }
}