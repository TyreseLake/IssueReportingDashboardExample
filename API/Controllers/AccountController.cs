using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

/// <summary>
/// This controller is responsible for handling User account requests/commands
/// </summary>
namespace API.Controllers
{
    public class AccountController : BaseApiContoller
    {
        private readonly ITokenService _tokenService;
        private readonly IUserRepository _userRepository;
        private readonly IActivityRepository _activityRepository;
        public readonly UserManager<AppUser> _userManager;
        public readonly SignInManager<AppUser> _signInManager;
        public static string CONTROLLER_NAME = "Accounts";

        /// <summary>
        /// Constructor for Account Controller
        /// </summary>
        /// <param name="userManager">Access to the user manager service</param>
        /// <param name="signInManager">Access to the sign in manager service</param>
        /// <param name="tokenService">Access to the token service </param>
        /// <param name="userRepository">Access to the user repository service</param>
        /// <param name="activityRepository">Access to the activity log service</param>
        public AccountController(UserManager<AppUser> userManager,
                                 SignInManager<AppUser> signInManager,
                                 ITokenService tokenService,
                                 IUserRepository userRepository,
                                 IActivityRepository activityRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _userRepository = userRepository;
            _activityRepository = activityRepository;
        }

        /// <summary>
        /// Route to facilitate user login
        /// </summary>
        /// <param name="loginDto">User login information</param>
        /// <returns>Login access information and token</returns>
        [HttpPost("login")]
        [ActionName("User Login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            //Log Activity
            var logData = new {
                loginDto.UserName
            };
            var activityId = await _activityRepository.LogActivity("User Login", CONTROLLER_NAME, logData);
            try
            {
                //get the user by user name anc check if it exisits
                var user = await _userManager.Users
                    .SingleOrDefaultAsync(x => x.UserName == loginDto.UserName.ToLower());

                if (user == null) {
                    var errorResult = Unauthorized("Invalid username");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, errorResult);
                    return errorResult;
                }

                //Check if the user login information is valid
                var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
                if (!result.Succeeded) {
                    var errorResult = Unauthorized("Password Incorrect");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, errorResult);
                    return errorResult;
                }
                
                var newUserToken = await _tokenService.CreateTokenAsync(user);

                //Get Params Object
                UserFilterParams userParams = null;
                try
                {
                    userParams = JsonConvert.DeserializeObject<UserFilterParams>(user.UserParams);
                }
                catch (System.Exception)
                {

                }

                var adminPrivileges = false;
                var issueManagementPrivileges = false;
                var userManagementPrivileges = false;
                var dataManagementPrivileges = false;
                var issueStatusUpdaterPrivileges = false;

                var userRoles = (await _userRepository.GetUserByIdAsync(user.Id)).UserRoles.Select(i => i.Role.Name);

                HashSet<string> userAccess = new HashSet<string>();
                foreach(var userRole in userRoles){
                    if(RolesHelper.RoleIsAdmin(userRole)){
                        adminPrivileges = true;
                    }

                    if(RolesHelper.RoleIsIssueManagement(userRole)){
                        issueManagementPrivileges = true;
                    }

                    if(RolesHelper.RoleIsUserManagement(userRole)){
                        userManagementPrivileges = true;
                    }

                    if(RolesHelper.RoleIsDataManagement(userRole)){
                        dataManagementPrivileges = true;
                    }
                    
                    if(RolesHelper.RoleIsIssueStatusUpdater(userRole)){
                        issueStatusUpdaterPrivileges = true;
                    }
                }


                //Create and return the user for the client to access
                var userInfo = new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Token = newUserToken,
                    Params = userParams,
                    AdminPrivileges = adminPrivileges,
                    IssueManagementPrivileges = issueManagementPrivileges,
                    UserManagementPrivileges = userManagementPrivileges,
                    DataManagementPrivileges = dataManagementPrivileges,
                    IssueStatusUpdaterPrivileges = issueStatusUpdaterPrivileges,
                    RequirePasswordReset = user.RequirePasswordReset
                };

                await _activityRepository.SetActivityStatus(activityId, SUCCESS_CODE, userInfo);

                return userInfo;
            }
            catch (System.Exception ex)
            {
                await _activityRepository.SetActivityStatus(activityId, FAILURE_CODE, ex);
                throw;
            }
        }

        /// <summary>
        /// Helper function to check if a user exisits
        /// </summary>
        /// <param name="username">Users username</param>
        /// <returns>Boolean result</returns>
        private async Task<bool> UserExists(string username)
        {
            return await _userManager.Users.AnyAsync(x => x.UserName == username.ToLower());
        }

        /// <summary>
        /// Get a users profile information if they ae logged in
        /// </summary>
        /// <returns>The logged in users information</returns>
        [Authorize]
        [HttpGet]
        public async Task<ActionResult> GetProfile()
        {
            var userId = User.GetUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null ){
                return Unauthorized("This user does not exist");
            }

             return Ok(new {
                user.Id,
                user.UserName,
                user.FirstName,
                user.LastName,
                user.Email,
                AccountType = user.UserRoles.SingleOrDefault()?.Role.Name,
                IssueTypesAccess = user.UserIssueTypes.Select(i => i.IssueType.Name).ToList(),
                DistrictsAccess = user.UserDistricts.Select(d => d.District.Name).ToList()
            });
        }

        /// <summary>
        /// Update a users profile information
        /// </summary>
        /// <param name="updateProfileDto">Specify the email, firstname and lastname to update the users info</param>
        /// <returns>The updated user profile</returns>
        [Authorize]
        [HttpPost("update-profile")]
        [ActionName("Update Personal Information")]
        public async Task<ActionResult> UpdateUserProfile(UpdateProfileDto updateProfileDto){
            var currentUser = await _userRepository.GetUserByIdAsync(User.GetUserId());
            if(currentUser == null ){
                return Unauthorized("This user does not exist");
            }
            //Log Activity
            var activityId = await _activityRepository.LogActivity("Update Personal Information", CONTROLLER_NAME, updateProfileDto, currentUser);
            
            try
            {
                var userName = User.GetUserName();
                var user = await _userRepository.GetUserByUsernameAsync(userName);

                //Update the users information
                user.Email = updateProfileDto.Email.Trim();
                user.FirstName = updateProfileDto.FirstName.Trim();
                user.LastName = updateProfileDto.LastName?.Trim();

                if(await _userRepository.SaveAllAsync())
                {
                    var UpdatedUserInfo = Ok(new {
                        user.Email,
                        user.FirstName,
                        user.LastName,
                        user.UserName
                    });

                    await _activityRepository.SetActivityStatus(activityId, SUCCESS_CODE, UpdatedUserInfo);

                    return UpdatedUserInfo;
                }

                var failedResult = BadRequest("An unexpect error occured when updating your profile");
                await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, failedResult);
                return failedResult;
            }
            catch (System.Exception ex)
            {
                await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, ex);
                throw;
            }
            
        }

        /// <summary>
        /// Route to update a users password information
        /// </summary>
        /// <param name="updatePasswordDto">User password information</param>
        /// <returns>Updated user message</returns>
        [Authorize]
        [HttpPost("update-password")]
        [ActionName("Update Personal Password")]
        public async Task<ActionResult> UpdatePassword(UpdatePasswordDto updatePasswordDto)
        {
            var currentUser = await _userRepository.GetUserByIdAsync(User.GetUserId());
            if(currentUser == null ){
                return Unauthorized("This user does not exist");
            }
            //Log Activity
            var activityId = await _activityRepository.LogActivity("Update Personal Password", CONTROLLER_NAME, currentUser);

            try
            {
                //Get the user
                // var userName = User.GetUserName();
                var userId = User.GetUserId();
                // var user = await _userRepository.GetUserByUsernameAsync(userName);
                var user = await _userManager.Users.FirstOrDefaultAsync(i => i.Id == userId);

                //Update user password
                var results = await _userManager.ChangePasswordAsync(user, updatePasswordDto.CurrentPassword, updatePasswordDto.NewPassword);
                if(results.Succeeded){
                    var successResult = Ok(new {result = "Password updated successfully"});

                    user.RequirePasswordReset = false;
                    var result2 = await _userRepository.SaveAllAsync();

                    await _activityRepository.SetActivityStatus(activityId, SUCCESS_CODE, successResult);
                    return successResult;
                }

                var erroredResponse = BadRequest(results.Errors);
                await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                return erroredResponse;
            }
            catch (System.Exception ex)
            {
                await _activityRepository.SetActivityStatus(activityId, FAILURE_CODE, ex);
                throw;
            }
        }
    }
}