﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventEae1._2_Backend.Data;
using EventEae1._2_Backend.DTOs;
using EventEae1._2_Backend.Interfaces;
using EventEae1._2_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace EventEae1._2_Backend.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 Get user by email
        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        // 🔹 Add a new user
        public async Task AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        // 🔹 Get user with permissions
        public async Task<User?> GetUserWithPermissionsAsync(string email)
        {
            return await _context.Users
                .Include(u => u.UserPermissions)
                    .ThenInclude(up => up.Permission)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<List<string>> GetPermissionsByRoleAsync(string role)
        {
            return await _context.RolePermissions
                .Where(rp => rp.Role == role)
                .Select(rp => rp.Permission.Name)
                .ToListAsync();
        }

        // ✅ Updated: Get all users with both role-based and user-specific permissions
        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Include(u => u.UserPermissions)
                    .ThenInclude(up => up.Permission)
                .ToListAsync();

            // We will also fetch default role-based permissions for each user
            var userDtos = new List<UserDto>();

            foreach (var u in users)
            {
                // Get role-based permissions for the user's role
                var rolePermissions = await _context.RolePermissions
                    .Where(rp => rp.Role == u.Role)
                    .Select(rp => rp.Permission.Name)
                    .ToListAsync();

                // Combine role-based permissions with user-specific permissions
                var allPermissions = rolePermissions
                    .Concat(u.UserPermissions.Select(up => up.Permission.Name))
                    .Distinct()
                    .ToList();

                userDtos.Add(new UserDto
                {
                    Id = u.Id.ToString(),
                    Email = u.Email,
                    Firstname = u.FirstName,
                    Lastname = u.LastName,
                    Role = u.Role,
                    Organization = u.Organization,
                    Status = u.Status,
                    Permissions = allPermissions
                });
            }

            return userDtos;
        }

        public async Task<List<string>> GetUserPermissionsAsync(Guid userId)
        {
            return await _context.UserPermissions
                .Where(up => up.UserId == userId)
                .Select(up => up.Permission.Name)
                .ToListAsync();
        }

        public async Task UpdateUserPermissionsAsync(Guid userId, List<string> permissions)
        {
            var existingPermissions = _context.UserPermissions.Where(up => up.UserId == userId);
            _context.UserPermissions.RemoveRange(existingPermissions);

            var permissionEntities = await _context.Permissions
                .Where(p => permissions.Contains(p.Name))
                .ToListAsync();

            var newUserPermissions = permissionEntities.Select(p => new UserPermission
            {
                UserId = userId,
                PermissionId = p.Id
            });

            await _context.UserPermissions.AddRangeAsync(newUserPermissions);
            await _context.SaveChangesAsync();
        }

        public async Task SetUserLockStatusAsync(Guid userId, bool isLocked)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Status = isLocked ? "locked" : "approved";
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<PendingManagerDto>> GetPendingManagersAsync()
        {
            return await _context.Users
                .Where(u => u.Role == "manager" && u.Status == "pending")
                .Select(u => new PendingManagerDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    OrganizationName = u.Organization
                })
                .ToListAsync();
        }

        public async Task SetManagerApprovalStatusAsync(Guid managerId, bool isApproved)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == managerId && u.Role == "manager");

            if (user != null)
            {
                user.Status = isApproved ? "approved" : "locked"; // or "pending" if rejecting differently
                await _context.SaveChangesAsync();
            }
        }
    }
}
