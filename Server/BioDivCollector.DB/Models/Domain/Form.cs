using System;
using System.Collections.Generic;
using System.Text;

namespace BioDivCollector.DB.Models.Domain
{
    public class Form
    {
        public int FormId { get; set; }
        public string Title { get; set; }

        public List<ProjectForm> FormProjects { get; set; }
        public List<FormFormField> FormFormFields { get; set; }
        public List<Record> FormRecords { get; set; }

        public List<ChangeLogForm> FormChangeLogs { get; set; } = new List<ChangeLogForm>();
    }
}
