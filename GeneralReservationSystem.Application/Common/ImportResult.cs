using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Common
{
    public sealed record ImportResult(string Message, int Count);
}
