using SchedulerWpfApp.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerWpfApp.Services
{
    public interface IExcelPersonImporter
    {
        List<Person> ReadPersonsFromExcel(string filePath);

        List<GroupClass> ReadRoomFromExcel(string filePath);

    }
}
