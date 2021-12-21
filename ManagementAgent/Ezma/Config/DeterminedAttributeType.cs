using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.MetadirectoryServices;

namespace Vigo.Bas.ManagementAgent.Ezma.Config
{
    public struct DeterminedAttributeType
    {
        public bool IsMultivalued { get; }
        public AttributeType AttributeType { get; }

        public DeterminedAttributeType(bool isMultivalued, AttributeType attributeType)
        {
            IsMultivalued = isMultivalued;
            AttributeType = attributeType;
        }
    }
}
