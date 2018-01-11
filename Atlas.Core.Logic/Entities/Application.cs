using Atlas.Core.Logic.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Core.Logic.Entities
{
    public class Application : Entity
    {
        public Property Value { get; private set; }

        public Application(long pId, string pCode, string pDescription)
        {
            Id = pId;
            Value = new Property(pCode, pDescription);
        }
    }
}
