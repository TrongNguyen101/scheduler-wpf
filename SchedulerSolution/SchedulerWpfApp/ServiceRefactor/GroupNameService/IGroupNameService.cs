using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.ServiceRefactor.GroupNameService
{
    public interface IGroupNameService
    {
        // Get a list of all classes
        Task<List<GroupClass>> GetAllAsync();

        // Add a new class to the system
        Task AddGroupName(GroupClass groupName);

        // Update current class information
        Task UpdateGroupName(GroupClass person);

        // Delete class by class code (ClassId)
        Task DeleteGroupName(string classid);

        // Import class list from Excel file
        Task ImportGroupNameFromExcel(List<GroupClass> listGroupNameFromExcel);

        // Check if the class code exists in the system (true = exists)
        Task<bool> CheckClassIdExistsAsync(string classId);

        // read excel file
        List<GroupClass> ReadGroupNameFromExcel(string filePath);

        // read excel file to export
        void ExportToExcelGroupName(List<GroupClass> groupname, string filePath);

    }
}
