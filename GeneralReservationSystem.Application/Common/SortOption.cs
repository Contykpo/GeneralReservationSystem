using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Common
{
    public enum SortDirection
    {
        Asc,
        Desc
    }

    public sealed record SortOption(string Field, SortDirection Direction = SortDirection.Asc);
}
