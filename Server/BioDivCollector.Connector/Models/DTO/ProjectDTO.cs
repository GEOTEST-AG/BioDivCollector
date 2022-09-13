using BioDivCollector.DB.Models.Domain;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BioDivCollector.Connector.Models.DTO
{
    //ProjectController

    public class ProjectDTO
    {
        public ProjectDTO()
        {

        }
        /// <summary>
        /// Convert project model to DTO
        /// </summary>
        /// <param name="project"></param>
        public ProjectDTO(Project project)
        {
            projectId = project.ProjectId;
            projectName = project.ProjectName;
            description = project.Description;
            projectNumber = project.ProjectNumber;
            id_Extern = project.ID_Extern;
            projectStatusId = project.ProjectStatusId;
            projectManager = project.ProjectManager?.ToString();
            projectConfigurator = project.ProjectConfigurator?.ToString();
            projectThirdPartyTools = string.Join(", ", project.ProjectThirdPartyTools.Select(m => m.ThirdPartyTool.Name).ToList());

        }

        public Guid projectId { get; set; }
        public DateTime? startDateTime { get; set; }
        public string projectName { get; set; }
        public string description { get; set; }
        public string projectNumber { get; set; }
        public string id_Extern { get; set; }
        public ProjectStatusEnum projectStatusId { get; set; }

        public string projectManager { get; set; }
        public string projectConfigurator { get; set; }

        public string projectThirdPartyTools { get; set; }

        public List<ReferenceGeometryDTO> geometries { get; set; } = new List<ReferenceGeometryDTO>();
        public List<RecordDTO> records { get; set; } = new List<RecordDTO>();

        public List<FormDTO> forms { get; set; } = new List<FormDTO>();
        public List<LayerDTO> layers { get; set; } = new List<LayerDTO>();
    }


    public class ReferenceGeometryDTO
    {
        public Guid geometryId { get; set; }

        public string geometryName { get; set; }

        public string geometry { get; set; }

        public string userName { get; set; }
        public string fullUserName { get; set; }
        public DateTimeOffset timestamp { get; set; }
        public DateTimeOffset creationTime { get; set; }

        public int status { get; set; }
        public bool readOnly { get; set; }

        public List<RecordDTO> records { get; set; } = new List<RecordDTO>();

    }

    public class RecordDTO
    {
        public Guid recordId { get; set; }

        public int formId { get; set; }

        public string userName { get; set; }
        public string fullUserName { get; set; }
        public DateTimeOffset timestamp { get; set; }
        public DateTimeOffset creationTime { get; set; }
        public int status { get; set; }

        public bool readOnly { get; set; }

        public List<TextDataDTO> texts { get; set; } = new List<TextDataDTO>();
        public List<NumericDataDTO> numerics { get; set; } = new List<NumericDataDTO>();
        public List<BooleanDataDTO> booleans { get; set; } = new List<BooleanDataDTO>();
    }

    public class TextDataDTO
    {
        public Guid textId { get; set; }
        public string title { get; set; }
        public string value { get; set; }

        public int? formFieldId { get; set; }
        public int? fieldChoiceId { get; set; }

        public TextData Dto2Model()
        {
            return new TextData()
            {
                Id = this.textId,
                Title = this.title,
                Value = this.value,
                FormFieldId = this.formFieldId,
                FieldChoiceId = this.fieldChoiceId
            };
        }
    }

    public class NumericDataDTO
    {
        public Guid numericId { get; set; }
        public string title { get; set; }
        public double? value { get; set; }

        public int? formFieldId { get; set; }

        public NumericData Dto2Model()
        {
            return new NumericData()
            {
                Id = this.numericId,
                Title = this.title,
                Value = this.value,
                FormFieldId = this.formFieldId,
            };
        }
    }

    public class BooleanDataDTO
    {
        public Guid booleanId { get; set; }
        public string title { get; set; }
        public bool? value { get; set; }

        public int? formFieldId { get; set; }

        public BooleanData Dto2Model()
        {
            return new BooleanData()
            {
                Id = this.booleanId,
                Title = this.title,
                Value = this.value,
                FormFieldId = this.formFieldId,
            };
        }
    }

    public class LayerDTO
    {
        public int layerId { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public string wmsLayer { get; set; }
        public string username { get; set; }
        public string password { get; set; }

        public bool visible { get; set; }
        public double opacity { get; set; }
        public int order { get; set; }
    }

    /// <summary>
    /// TODO: Add timestamps to forms?
    /// </summary>
    public class FormDTO
    {
        public int formId { get; set; }
        public string title { get; set; }

        public DateTimeOffset timestamp { get; set; }

        public List<FormFieldDto> formFields { get; set; } = new List<FormFieldDto>();
    }

    public class FormFieldDto
    {
        public int fieldId { get; set; }

        public int typeId { get; set; }

        public string title { get; set; }
        public string description { get; set; }
        public string source { get; set; }

        public int order { get; set; }
        public bool mandatory { get; set; }
        public bool useInRecordTitle { get; set; }
        public string standardValue { get; set; }

        public List<FieldChoiceDto> fieldChoices = new List<FieldChoiceDto>();
    }

    public class FieldChoiceDto
    {
        public int choiceId { get; set; }
        public string text { get; set; }
        public int order { get; set; }
    }



}
