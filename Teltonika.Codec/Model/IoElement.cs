﻿using System.Collections.Generic;

namespace Teltonika.Codec.Model
{
    public struct IoElement
    {
        public int EventId { get; private set; }
        public int PropertiesCount { get; private set; }
        public IEnumerable<IoProperty> Properties { get; private set; }
        public byte? OriginType { get; private set; }
        
        public static IoElement Create(int eventId, int propertyCount, IEnumerable<IoProperty> properties, byte? originType = null)
        {
            return new IoElement
            {
                EventId = eventId,
                PropertiesCount = propertyCount,
                Properties = properties,
                OriginType = originType
            };
        }
    }
}
