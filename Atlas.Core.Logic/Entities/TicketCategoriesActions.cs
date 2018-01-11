using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.Core.Logic.ValueObjects;
namespace Atlas.Core.Logic.Entities
{  

    public class TicketCategoriesActions : Entity
    {
      public string Description { get; set; }

        public TicketCategoriesActions(long pId, string pDescription)
        {
            Id = pId;
            Description = pDescription;
        }
    }
}
