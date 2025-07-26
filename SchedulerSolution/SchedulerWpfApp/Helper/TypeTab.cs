using static SchedulerWpfApp.ViewModel.MainViewModel;

namespace SchedulerWpfApp.Helper
{
    public class TypeTab
    {
        public string NameTab { get; }
        private TypeTab(string name)
        {
            NameTab = name;
        }
        public static readonly TypeTab Dashboard = new TypeTab(nameof(Dashboard));
        public static readonly TypeTab CreateSchedule = new TypeTab(nameof(CreateSchedule));
        public static readonly TypeTab LectureSubject = new TypeTab(nameof(LectureSubject));
        public static readonly TypeTab Lecture = new TypeTab(nameof(Lecture));
        public static readonly TypeTab Subject = new TypeTab(nameof(Subject));
        public static readonly TypeTab GroupName = new TypeTab(nameof(GroupName));
        public static readonly TypeTab RoomList = new TypeTab(nameof(RoomList));
        public static readonly TypeTab Curriculum = new TypeTab(nameof(Curriculum));
        public static readonly TypeTab CurriculumSubject = new TypeTab(nameof(CurriculumSubject));
    }
}
