using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.ServiceRefactor.GroupNameService
{
    public interface IGroupNameService
    {
        // Get a list of all classes
        Task<List<GroupClass>> GetAllAsync();

        // Get a list of all major in group class
        Task<List<string>> GetAllMajorAsync();

        // Add a new class to the system
        Task AddGroupName(GroupClass groupName);

        // Update current class information
        Task UpdateGroupName(GroupClass groupName);

        // Delete class by class code (ClassId)
        Task DeleteGroupName(string groupname);

        // Import class list from Excel file
        Task ImportGroupNameFromExcel(List<GroupClass> listGroupNameFromExcel, IProgress<int> progress);

        // Check if the class code exists in the system (true = exists)
        Task<bool> CheckGroupNameExistsAsync(string groupname);

        // read excel file
        List<GroupClass> ReadGroupNameFromExcel(string filePath);

        // read excel file to export
        void ExportToExcelGroupName(List<GroupClass> groupname, string filePath);
    }
}
