using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerWpfApp.Model;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.Services
{
    public interface IExcelRoomImport
    {

        List<Room> ReadRoomListFromExcel(string filePath);


    }
}
