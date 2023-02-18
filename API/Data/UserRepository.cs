using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Helpers.Pagination;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    /// <summary>
    /// Respoitory for all user related requests
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        public UserRepository(DataContext context, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;
        }

        /// <summary>
        /// Get a list of details for all users
        /// </summary>
        /// <param name="userParams">Specify query parameters for user list</param>
        /// <returns>Paginated list of users</returns>
        public async Task<PagedList<UserItemDto>> GetPaginatedUsersAsync(UserParams userParams)
        {
            var query = _context.Users
                .Include(i => i.UserRoles).ThenInclude(ur => ur.Role)
                .Include(i => i.UserDistricts).ThenInclude(ud => ud.District)
                .Include(i => i.UserIssueTypes).ThenInclude(uit => uit.IssueType)
                .Where(u => u.UserName.ToLower() != "admin")
                .Where(u => u.UserName.ToLower() != userParams.UserName.ToLower())
                .AsQueryable();

            if(RolesHelper.RoleAllowsRestriction(userParams.UserRole)){
                var adminRoles = RolesHelper.AdminRoles.ToList();
                query = query.Where(i => !adminRoles.Any(x => x == i.UserRoles.SingleOrDefault().Role.Name));

                if(userParams.UserDistrictAccessIds != null && userParams.UserDistrictAccessIds.Count() > 0){
                    // query = query.Where(i => 
                    //     (i.UserDistricts.Count() > 0) && 
                    //     (userParams.UserDistrictAccessIds.Intersect(i.UserDistricts.Select(ud => ud.DistrictId)).Count() == i.UserDistricts.Select(ud => ud.DistrictId).Count())
                    // );

                    query = query.Where(i => 
                        (i.UserDistricts.Count() > 0) && 
                        (i.UserDistricts.All(ud => userParams.UserDistrictAccessIds.Contains(ud.DistrictId)))
                    );
                }

                if(userParams.UserIssueTypeAccessIds != null && userParams.UserIssueTypeAccessIds.Count() > 0){
                    query = query.Where(i => 
                        (i.UserIssueTypes.Count() > 0) && 
                        (i.UserIssueTypes.All(ud => userParams.UserIssueTypeAccessIds.Contains(ud.IssueTypeId)))
                    );
                }
            }

            var users = query.ProjectTo<UserItemDto>(_mapper.ConfigurationProvider);

            var result = await PagedList<UserItemDto>.CreateAsync(users, userParams.PageNumber, userParams.PageSize);

            return result;
        }

        // public void DetachUser(AppUser u)
        // {
        //     _context.Entry(u).State = EntityState.Detached;
        // }

        // public void UpdateUser(AppUser u)
        // {
        //     _context.Entry(u).State = EntityState.Modified;
        // }

        /// <summary>
        /// Get a users details based on their id
        /// </summary>
        /// <param name="id">Id of the user</param>
        /// <returns>A users details</returns>
        public async Task<AppUser> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.UserIssueTypes)
                    .ThenInclude(ut => ut.IssueType)
                .Include(u => u.UserDistricts)
                    .ThenInclude(ud => ud.District)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.UserPins)
                .Include(u => u.UserHiddenItems)
                .SingleOrDefaultAsync(u => u.Id == id);
        }

        /// <summary>
        /// Get a users entity. Just the main users information from the database, no other information included
        /// </summary>
        /// <param name="id">The id of the user</param>
        /// <returns>The user entity</returns>
        public async Task<AppUser> GetUserEntityAsync(int id)
        {
            return await _context.Users
                .SingleOrDefaultAsync(u => u.Id == id);
        }

        /// <summary>
        /// Get a user object by the user's name from the database
        /// </summary>
        /// <param name="username">The name of the user</param>
        /// <returns>A user object</returns>
        public async Task<AppUser> GetUserByUsernameAsync(string username)
        {
            var result = await _context.Users
                .Include(u => u.UserIssueTypes)
                    .ThenInclude(ut => ut.IssueType)
                .Include(u => u.UserDistricts)
                    .ThenInclude(ud => ud.District)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.UserPins)
                .Include(u => u.UserHiddenItems)
                .SingleOrDefaultAsync(u => u.UserName == username);
            return result;
        }

        /// <summary>
        /// Get all users from the database
        /// </summary>
        /// <returns>All users</returns>
        public async Task<IEnumerable<AppUser>> GetUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        /// <summary>
        /// Save the users table in the database
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SaveAllAsync()
        {   
            var result = await _context.SaveChangesAsync();
            return (result) > 0;
        }

        public async Task<IEnumerable<AppRole>> GetUserRolesAsync()
        {
            var result = await _context.Roles.ToListAsync();
            return (result);
        }
    }
}