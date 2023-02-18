using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;

namespace API.Interfaces
{
    public interface IStatusRepository
    {
        Task<Status> AsyncGetStatusByName(string name);
        Task<IEnumerable<Status>> AsyncGetAllStatuses();
        Task<IEnumerable<string>> AsyncGetAllStatusNames();
        Task<Status> AsyncGetDefaultStatus();
    }
}