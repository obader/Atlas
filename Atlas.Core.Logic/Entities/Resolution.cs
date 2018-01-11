using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.Core.Logic.ValueObjects;

namespace Atlas.Core.Logic.Entities
{
    public class Resolution : Entity
    {
       public Property Status { get; private set; }

        public List<Action> Actions { get; private set; }

        public Resolution(string pCode, string pDescription, List<Action> pActions)
        {         
            Status = new Property(pCode,pDescription);
            Actions = pActions;
        }
    }
}
