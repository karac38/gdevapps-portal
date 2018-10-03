using System;

namespace GdevApps.DAL.DataModels.AspNetUsers.GradeBook
{
   public partial class ParentSharedGradeBook
    {
        public int ParentGradeBookId { get; set; }
        public string AspnetUserId { get; set; }
        public string ParentId { get; set; }
        public int FolderId { get; set; }
        public int Id { get; set; }
        public int SharedStatus { get; set; }

        public GdevApps.DAL.DataModels.AspNetUsers.AspNetUser.AspNetUsers AspnetUser { get; set; }
        public Folder Folder { get; set; }
        public GdevApps.DAL.DataModels.AspNetUsers.AspNetUser.AspNetUsers Parent { get; set; }
        public ParentGradeBook ParentGradeBook { get; set; }
        public SharedStatus SharedStatusNavigation { get; set; }
    }
}