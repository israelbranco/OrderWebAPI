using System;
using System.Collections.Generic;
using System.Text;

namespace OrderWebAPI.Domain.Enums
{
    public enum OrderStatus
    {
        Draft = 1,
        Placed = 2,
        Confirmed = 3,
        Canceled = 4
    }
}
