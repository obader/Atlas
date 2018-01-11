using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Core.Logic.Entities
{
    public class Profile 
    {
              
        public int ProfileID { get; set; }

        public string Name { get; set; }

        public int CIFID { get; set; }
      
        public string Email { get; set; }
     
        public string MobileNumber { get; set; }

        public string City { get; set; }

        public string Address { get; set; }

        public string Country { get; set; }

        public string AssignedBranch { get; set; }

        public string Status { get; set; }
       
        public string ServicePlan { get; set; }

        public string SalesPersonId { get; set; }

        public DateTime EnrollmentDate { get; set; }

        public DateTime LastStatusChangeDate { get; set; }

        public bool ProfileLimitReached { get; set; }

        public bool DeviceLimitReached { get; set; }

      
    }
}
