using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.Core.Logic.ValueObjects;

namespace Atlas.Core.Logic.Entities
{
    public class Action:Entity
    {
       public Property Value { get; private set; }

        public Action(long pId, string pCode, string pDescription)
        {
            Id = pId;
            Value = new Property(pCode,pDescription);
        }
    }
}
