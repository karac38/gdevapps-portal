﻿using System;
using System.Collections.Generic;

namespace GdevApps.DAL.DataModels.AspNetUsers
{
    public partial class AspNetUserClaims
    {
        public int Id { get; set; }
        public string ClaimType { get; set; }
        public string ClaimValue { get; set; }
        public string UserId { get; set; }

        public AspNetUsers User { get; set; }
    }
}
