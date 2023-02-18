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
    /// Repository for handling all status and status update related requests
    /// </summary>
    public class StatusRepository : IStatusRepository
    {
        private readonly DataContext _context;
        public StatusRepository(DataContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get an issue status update by its name
        /// </summary>
        /// <param name="name">The name of the issue status</param>
        /// <returns>The issue status object</returns>
        public async Task<Status> AsyncGetStatusByName(string name)
        {
            var status = await _context.Status.SingleOrDefaultAsync(s => s.Name.ToLower() == name.ToLower());
            return status;
        }
        
        /// <summary>
        /// Gets all the statuses
        /// </summary>
        /// <returns>All statuses</returns>
        public async Task<IEnumerable<Status>> AsyncGetAllStatuses()
        {
            var statuses = await _context.Status.ToListAsync();
            return statuses;
        }

        /// <summary>
        /// Gets all the status names
        /// </summary>
        /// <returns>All status names</returns>
        public async Task<IEnumerable<string>> AsyncGetAllStatusNames()
        {
            var statuses = await _context.Status.Select(s => s.Name).ToListAsync();
            return statuses;
        } 

        /// <summary>
        /// Get the default status
        /// </summary>
        /// <returns>Default status</returns>
        public async Task<Status> AsyncGetDefaultStatus()
        {
            var defaultStatus = await _context.Status.FirstOrDefaultAsync(s => s.Default);
            return defaultStatus;
        }
    }
}