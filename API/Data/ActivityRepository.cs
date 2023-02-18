using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using API.Helpers.Pagination;
using API.Interfaces;
using Newtonsoft.Json;

/// <summary>
/// Repository for handling Activity Log requests
/// </summary>
namespace API.Data
{
    public class ActivityRepository : IActivityRepository
    {
        private readonly DataContext _context;
        public const int SUCCESS_CODE = 1;
        public const int ERROR_CODE = 2;
        public const int FAILURE_CODE = 3;
        /// <summary>
        /// Constructor for the repository
        /// </summary>
        /// <param name="context">Database context</param>
        public ActivityRepository(DataContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all user activity
        /// </summary>
        /// <param name="userActivityParams">Specify query parameters</param>
        /// <returns>Paginated user activity</returns>
        public async Task<PagedList<UserActivity>> GetUserActivity(UserActivityParams userActivityParams)
        {
            var activities = _context.UserActivity
                .AsQueryable();

            activities = userActivityParams.SortBy switch {
                _ => activities.OrderByDescending(act => act.ActivityDate)
            };

            return await PagedList<UserActivity>.CreateAsync(activities, userActivityParams.PageNumber, userActivityParams.PageSize);
        }

        /// <summary>
        /// Log a user activity entry
        /// </summary>
        /// <param name="action">Specify the action being performed</param>
        /// <param name="actionGroup">Specify the type/group the action belongs</param>
        /// <param name="Data">Specify the data being passed to this action, if any</param>
        /// <param name="user">Specifies the user peforming the action, if any</param>
        /// <returns>The save result</returns>
        public async Task<int> LogActivity(string action, string actionGroup, object Data = null, AppUser user = null)
        {
            var userActivity = new UserActivity{
                Action = action,
                ActionGroup = actionGroup,
                Data = JsonConvert.SerializeObject(Data),
                AccountType = user?.UserRoles.FirstOrDefault().Role.Name,
                Email = user?.Email,
                ActivityDate = DateTime.Now,
                Status = "Processing",
                UserName = user?.UserName
            };

            _context.UserActivity.Add(userActivity);

            var result = await _context.SaveChangesAsync();

            return userActivity.Id;
        }

        /// <summary>
        /// This is used to set the activity status of an activity log entry
        /// </summary>
        /// <param name="id">The ID of the activity log entry being referenced</param>
        /// <param name="statusCode">The status code to update the activity log </param>
        /// <param name="response">The response to be stored for the actitivy log entry, if any</param>
        /// <returns>Boolean result</returns>
        public async Task<bool> SetActivityStatus(int id, int statusCode, string response = null)
        {
            var activityLogEntry = await _context.UserActivity.FindAsync(id);
            activityLogEntry.Status = getStatus(statusCode);
            if(response != null && response.Trim() != "")
                activityLogEntry.Response = response;
            activityLogEntry.CompletionDate = DateTime.Now;
            var result = _context.UserActivity.Update(activityLogEntry);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// This is used to set the activity status of an activity log entry
        /// </summary>
        /// <param name="id">The ID of the activity log entry being referenced</param>
        /// <param name="statusCode">The status code to update the activity log </param>
        /// <param name="response">The response object to be stored for the actitivy log entry, if any</param>
        /// <returns>Boolean result</returns>
        public async Task<bool> SetActivityStatus(int id, int statusCode, Object responseObject = null)
        {
            var activityLogEntry = await _context.UserActivity.FindAsync(id);
            activityLogEntry.Status = getStatus(statusCode);
            if(responseObject != null)
                activityLogEntry.Response = JsonConvert.SerializeObject(responseObject);
            activityLogEntry.CompletionDate = DateTime.Now;
            var result = _context.UserActivity.Update(activityLogEntry);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// Helper function to get the issue status name based on the status code entered
        /// </summary>
        /// <param name="statusCode">Specify the status code</param>
        /// <returns>The status name</returns>
        private string getStatus(int statusCode){
            string status;
            switch (statusCode)
            {
                case SUCCESS_CODE:
                    status = "Success";
                    break;
                case ERROR_CODE:
                    status = "Error";
                    break;
                case FAILURE_CODE:
                    status = "Failure";
                    break;
                default:
                    status = "Unknown";
                    break;
            }
            return status;
        } 
    }
}