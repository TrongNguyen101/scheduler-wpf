using SchedulerWpfApp.Data;
using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Model;
namespace SchedulerWpfApp.Repository.GroupNameRepository
{
    /// <summary>
    /// Repository for managing GroupClass entities, providing methods for searching, checking existence, retrieving, and deleting group classes.
    /// </summary>
    public class GroupNameRepository : BaseRepository<GroupClass>, IGroupNameRepository
    {
        #region Contracstor
        /// <summary>
        /// Initializes a new instance of the <see cref="GroupNameRepository"/> class with the specified data context.
        /// </summary>
        /// <param name="context">The data context to use for database operations.</param>
        public GroupNameRepository(DataContext context) : base(context) { }
        #endregion

        #region Method
        /// <summary>
        /// Searches for group classes whose GroupName contains the specified search term (case-insensitive).
        /// </summary>
        /// <param name="searchTerm">The term to search for in group names.</param>
        /// <returns>A list of matching <see cref="GroupClass"/> entities.</returns>
        public async Task<List<GroupClass>> SearchGroupNameAsync(string searchTerm)
        {
            try
            {
                return await _context.GroupName
                    .Where(gc => gc.GroupName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Đã xảy ra lỗi khi tìm kiếm GroupName.", ex);
            }
        }

        /// <summary>
        /// Checks if a group class with the specified group name exists (case-insensitive).
        /// </summary>
        /// <param name="groupName">The group name to check for existence.</param>
        /// <returns>True if the group name exists; otherwise, false.</returns>
        public async Task<bool> CheckGroupNameExistsAsync(string groupName)
        {
            try
            {
                return await _context.GroupName
                .AnyAsync(gc => gc.GroupName.ToLower() == groupName.ToLower());
            }
            catch (Exception ex)
            {
                throw new Exception("Đã xảy ra lỗi khi kiểm tra sự tồn tại của ID GroupName.", ex);
            }
        }

        /// <summary>
        /// Retrieves a <see cref="GroupClass"/> entity by its group name.
        /// </summary>
        /// <param name="groupnameid">The group name identifier.</param>
        /// <returns>The matching <see cref="GroupClass"/> entity, or null if not found.</returns>
        public async Task<GroupClass?> GetGroupNameAsync(string groupname)
        {
            try
            {
                return await _context.GroupName.FirstOrDefaultAsync(gc => gc.GroupName == groupname);
            }
            catch (Exception ex)
            {
                throw new Exception("Đã xảy ra lỗi khi truy xuất GroupClass theo mã.", ex);
            }
        }

        /// <summary>
        /// Deletes a <see cref="GroupClass"/> entity with the specified group name code.
        /// </summary>
        /// <param name="code">The group name code of the entity to delete.</param>
        public async Task DeleteAsync(string groupname)
        {
            try
            {
                var groupClass = await GetGroupNameAsync(groupname);
                if (groupClass != null)
                {
                    _context.GroupName.Remove(groupClass);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Xóa không thành công cho GroupClass với groupname '{groupname}'.", ex);
            }
        }
        #endregion
    }
}
