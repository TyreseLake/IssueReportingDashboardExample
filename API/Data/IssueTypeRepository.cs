using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    /// <summary>
    /// Repository for handling all issue type related requests
    /// </summary>
    public class IssueTypeRepository : IIssueTypeRepository
    {
        private readonly DataContext _context;

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
        
        public IssueTypeRepository(DataContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Function to fetch all issue types from the database
        /// </summary>
        /// <returns>List of issue types</returns>
        public async Task<List<IssueType>> GetIssueTypes()
        {
            return await _context.IssueTypes.ToListAsync();
        }

        /// <summary>
        /// Get the names of all issue types
        /// </summary>
        /// <returns>Issue types names</returns>
        public async Task<List<string>> GetIssueTypeNames()
        {
            var issueTypes = await _context.IssueTypes.ToListAsync();
            return issueTypes.Select(d => d.Name).ToList();
        }

        // public async void RemoveUserIssueTypeByName(AppUser user, string issueTypeName)
        // {
        //     var issueType = await _context.IssueTypes.Where(x => x.Name.ToLower() == issueTypeName.ToLower()).SingleOrDefaultAsync();
        //     var userIssueType = new UserIssueType 
        //     {
        //         IssueType = issueType,
        //         User = user
        //     };
        //     _context.UserIssueTypes.Remove(userIssueType);
        // }

        /// <summary>
        /// Removes a user's issue type from the database
        /// </summary>
        /// <param name="userIssueType">The issye type to be removed</param>
        public void RemoveUserIssueType(UserIssueType userIssueType)
        {
            _context.UserIssueTypes.Remove(userIssueType);
        }

        // public void RemoveAllUserIssueTypes(AppUser user)
        // {
        //     foreach (var userIssueType in user.UserIssueTypes)
        //     {  
        //         user.UserIssueTypes.Remove(userIssueType);
        //         _context.UserIssueTypes.Remove(userIssueType);
        //     }
        // }

        /// <summary>
        /// Get a issue type by name
        /// </summary>
        /// <param name="issueTypeName">Specifies the name of the issue type to retrieve</param>
        /// <returns>An issue type</returns>
        public async Task<IssueType> GetIssueTypeByName(string issueTypeName)
        {
            return await _context.IssueTypes.SingleOrDefaultAsync(it => it.Name.ToLower() == issueTypeName.Trim().ToLower());
        }

        /// <summary>
        /// Add an issue type to a user
        /// </summary>
        /// <param name="user">Specify the user</param>
        /// <param name="issueType">Specify the issue type</param>
        public void AddUserIssueType(AppUser user, IssueType issueType)
        {
            var userIssueType = new UserIssueType 
            {
                IssueTypeId = issueType.Id,
                UserId = user.Id
            };
            _context.UserIssueTypes.Add(userIssueType);
        }
    }
}