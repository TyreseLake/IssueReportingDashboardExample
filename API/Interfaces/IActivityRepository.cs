using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using API.Helpers.Pagination;

namespace API.Interfaces
{
    public interface IActivityRepository
    {
        Task<PagedList<UserActivity>> GetUserActivity(UserActivityParams userActivityParams);
        // Task<int> AddActivityEntry(UserActivity userActivity);
        // Task<bool> SaveAllAsync();
        Task<int> LogActivity(string action, string actionGroup, Object Data = null, AppUser user = null);
        Task<bool> SetActivityStatus(int id, int statusCode, string response = null);
        Task<bool> SetActivityStatus(int id, int statusCode, Object responseObject = null);
    }
}