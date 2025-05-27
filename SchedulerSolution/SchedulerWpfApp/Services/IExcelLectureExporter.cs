using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services
{
    public interface IExcelLectureExporter
    {
        void ExportToExcel(List<Lecturer> lectures, string filePath);
    }
}
