using BioDivCollector.Connector.Models.DTO;
using BioDivCollector.DB.Models.Domain;
using BioDivCollector.PluginContract.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;


namespace BioDivCollector.Connector.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectController : ControllerBase
    {
        private readonly BioDivContext _context;
        private readonly ILogger _logger;
        private GeneralPluginExtension _generalPluginExtension;

        public ProjectController(BioDivContext context, ILogger<ProjectController> logger, GeneralPluginExtension generalPluginExtension)
        {
            _context = context;
            _logger = logger;
            _generalPluginExtension = generalPluginExtension;
        }

        // GET: api/Project/5
        /// <summary>
        /// Get project content
        /// </summary>
        /// <param name="id">project guid</param>
        /// <param name="startDateTime">optional datetime, e.g. 2020-10-29T15:30:12 or 2020-10-29T15:30:12Z (for explicit Utc time) -> if provided: deleted objects included</param>
        /// <returns></returns>
        [HttpGet("{id}/{startDateTime?}")]
        public async Task<ActionResult<ProjectDTO>> GetProject(Guid id, DateTime? startDateTime)
        {
            string userName = string.Empty;

            //Check the user and the project
            var checkResult = await this.CheckUserProjectValid(id);
            userName = checkResult.Item2;
            if (checkResult.Item1 != null)
            {
                string problemString = checkResult.Item1.Value.ToString();
                _logger.LogError("PROJECT JSON GET: user = {userName} \n" +
                                 "\tproject id = '{id}', startDateTime = {startDateTime} \n " +
                                 "\tProblem = {problemString} ",
                                 userName, id, startDateTime, problemString);
                return checkResult.Item1;
            }
            else
            {
                _logger.LogInformation("PROJECT JSON GET: user = {userName} \n" +
                                       "\tproject id = '{id}', startDateTime = {startDateTime}",
                                       userName, id, startDateTime);
            }

            Project project = await _context.Projects
                .AsNoTracking()
                .Include(p => p.ProjectManager)
                .Include(p => p.ProjectConfigurator)
                .Include(p => p.ProjectLayers)
                    .ThenInclude(pl => pl.Layer)
                    .ThenInclude(l => l.LayerUsers)
                .Include(p => p.ProjectGroups)
                    .ThenInclude(pg => pg.Group)
                .Include(p => p.ProjectForms)
                .Include(p => p.ProjectGroups)
                    .ThenInclude(pg => pg.Group).ThenInclude(g => g.GroupUsers)
                    .ThenInclude(gu => gu.User)
                .Include(p => p.ProjectThirdPartyTools)
                    .ThenInclude(ptpt=>ptpt.ThirdPartyTool)
                .Where(p => p.ProjectId == id &&
                            p.StatusId != StatusEnum.deleted &&
                            p.ProjectStatusId == ProjectStatusEnum.Projekt_bereit)
                .SingleOrDefaultAsync();

            if (project == null)
            {
                return BadRequest($"Project ID '{id}' not found in database. Maybe the project is not ready or already closed?");
            }

            var allGroupIDs = project.ProjectGroups.Select(pg => pg.GroupId);

            IQueryable<ReferenceGeometry> geometries = _context.Geometries
                .AsNoTracking()
                .Include(p => p.ProjectGroup)
                    .ThenInclude(pg => pg.Group).ThenInclude(g => g.GroupUsers)
                    .ThenInclude(gu => gu.User)
                .Include(g => g.Records)
                .Include(g => g.GeometryChangeLogs)
                    .ThenInclude(gcl => gcl.ChangeLog)
                    .ThenInclude(c => c.User)
                .Where(g => allGroupIDs.Contains(g.ProjectGroupGroupId)
                            //&& g.StatusId != StatusEnum.deleted
                            && g.ProjectGroup.Project == project)
                .Distinct();
            ;
            if (startDateTime == null)
            {
                geometries = geometries.Where(g => g.StatusId != StatusEnum.deleted);
            }

            IQueryable<Record> records = _context.Records
                .AsNoTracking()
                .Include(p => p.ProjectGroup)
                    .ThenInclude(pg => pg.Group).ThenInclude(g => g.GroupUsers)
                    .ThenInclude(gu => gu.User)
                .Include(r => r.Form)
                .Include(r => r.Geometry)
                .Include(r => r.TextData)
                    .ThenInclude(t => t.FormField)
                .Include(r => r.TextData)
                    .ThenInclude(t => t.FieldChoice)
                .Include(r => r.NumericData)
                    .ThenInclude(t => t.FormField)
                .Include(r => r.BooleanData)
                    .ThenInclude(t => t.FormField)
                .Include(g => g.RecordChangeLogs)
                    .ThenInclude(rcl => rcl.ChangeLog)
                    .ThenInclude(c => c.User)
                .Where(r => r.Geometry == null && r.ProjectGroupGroupId != null &&
                            allGroupIDs.Contains((Guid)r.ProjectGroupGroupId)
                            //&& r.StatusId != StatusEnum.deleted
                            && r.ProjectGroup.Project == project
                            )
                .Distinct();

            if (startDateTime == null)
            {
                records = records.Where(r => r.StatusId != StatusEnum.deleted);
            }

            var allGeometryRecordIDs = geometries.SelectMany(g => g.Records).Select(r => r.RecordId);

            IQueryable<Record> geometryRecords = _context.Records
                .AsNoTracking()
                .Include(p => p.ProjectGroup)
                    .ThenInclude(pg => pg.Group).ThenInclude(g => g.GroupUsers)
                    .ThenInclude(gu => gu.User)
                .Include(r => r.Form)
                .Include(r => r.Geometry)
                .Include(r => r.TextData)
                    .ThenInclude(t => t.FormField)
                .Include(r => r.TextData)
                    .ThenInclude(t => t.FieldChoice)
                .Include(r => r.NumericData)
                    .ThenInclude(t => t.FormField)
                .Include(r => r.BooleanData)
                    .ThenInclude(t => t.FormField)
                .Include(g => g.RecordChangeLogs)
                    .ThenInclude(rcl => rcl.ChangeLog)
                    .ThenInclude(c => c.User)
                .Where(r => r.Geometry != null && allGeometryRecordIDs.Contains(r.RecordId) && allGroupIDs.Contains((Guid)r.ProjectGroupGroupId)
                            && r.ProjectGroup.Project == project
                            //&& r.StatusId != StatusEnum.deleted
                            //&& r.Geometry.StatusId != StatusEnum.deleted
                            )
                .Distinct();

            if (startDateTime == null)
            {
                geometryRecords = geometryRecords.Where(r => r.StatusId != StatusEnum.deleted
                                                        && r.Geometry.StatusId != StatusEnum.deleted);
            }

            ProjectDTO projectDto = new ProjectDTO(project);
            if (startDateTime != null)
            {
                projectDto.startDateTime = startDateTime;
            }

            //-------------------------------------------------------------------------------------------------------------------------------------------------
            //add project geometries to projectDto

            foreach (ReferenceGeometry geom in geometries)
            {
                geom.ReadOnly = !geom.ProjectGroup.Group.GroupUsers.Select(gu => gu.User).Select(g => g.UserId).Contains(userName);

                ReferenceGeometryDTO geomDto = new ReferenceGeometryDTO()
                {
                    geometryId = geom.GeometryId,
                    geometryName = geom.GeometryName,
                    status = (int)geom.StatusId,
                    readOnly = geom.ReadOnly
                };
                if (geom.Point != null) geomDto.geometry = Geometry2GeoJSON(geom.Point);
                else if (geom.Line != null) geomDto.geometry = Geometry2GeoJSON(geom.Line);
                else if (geom.Polygon != null) geomDto.geometry = Geometry2GeoJSON(geom.Polygon);

                ChangeLog firstGeomChangeLog = geom.GeometryChangeLogs.Select(gcl => gcl.ChangeLog).OrderBy(c => c.ChangeDate).FirstOrDefault();
                if (firstGeomChangeLog != null)
                {
                    geomDto.userName = firstGeomChangeLog.User.UserId;
                    geomDto.fullUserName = firstGeomChangeLog.User.ToString();
                    geomDto.timestamp = firstGeomChangeLog.ChangeDate;
                    geomDto.creationTime = firstGeomChangeLog.ChangeDate;
                }
                ChangeLog lastGeomChangeLog = geom.GeometryChangeLogs.Select(gcl => gcl.ChangeLog).OrderBy(c => c.ChangeDate).LastOrDefault();
                if (lastGeomChangeLog != null)
                {
                    geomDto.userName = lastGeomChangeLog.User.UserId;
                    geomDto.fullUserName = lastGeomChangeLog.User.ToString();
                    geomDto.timestamp = lastGeomChangeLog.ChangeDate;
                }

                //-------------------------------------------------------------------------------------------------------------------------------------------------
                //add geometry records to projectDto
                var geometryRecordDtos = Records2Dto(geometryRecords.Where(r => r.Geometry.GeometryId == geom.GeometryId), userName, startDateTime);
                geomDto.records.AddRange(geometryRecordDtos);

                if (geomDto.records.Any() || (startDateTime != null && geomDto.timestamp > startDateTime) || startDateTime == null)
                {
                    // ok, we have records or a geometry to list
                    projectDto.geometries.Add(geomDto);
                }
                else
                {
                    continue; //no listing
                }

            }
            //-------------------------------------------------------------------------------------------------------------------------------------------------
            // add project records to projectDto

            var recordDtos = Records2Dto(records, userName, startDateTime);
            projectDto.records.AddRange(recordDtos);

            //-------------------------------------------------------------------------------------------------------------------------------------------------
            // add layers to projectDto

            List<ProjectLayer> projectLayers = project.ProjectLayers.Where(u => u.Layer.Public || u.Layer.LayerUsers.Any(z => z.UserId == userName)).ToList();
            foreach (ProjectLayer pl in projectLayers)
            {
                UserHasProjectLayer upl = await _context.UsersHaveProjectLayers
                    .AsNoTracking()
                    .Where(m => m.UserId == userName && m.Layer == pl.Layer && m.ProjectId == project.ProjectId)
                    .FirstOrDefaultAsync();

                if (upl != null)
                {
                    pl.Visible = upl.Visible;
                    pl.Transparency = upl.Transparency;
                    pl.Order = upl.Order;
                }
                else
                {
                    pl.Visible = false;
                    pl.Transparency = 1;
                    pl.Order = 0;
                }
            }

            int layerOrder = 1;
            foreach (ProjectLayer projectLayer in projectLayers.OrderBy(m => m.Order).ThenBy(m => m.LayerId))
            {
                var layer = projectLayer.Layer;

                LayerDTO layerDto = new LayerDTO()
                {
                    layerId = layer.LayerId,
                    title = layer.Title,
                    url = layer.Url,
                    wmsLayer = layer.WMSLayer,
                    order = layerOrder,
                    visible = projectLayer.Visible,
                    opacity = projectLayer.Transparency,
                    username = layer.Username,
                    password = layer.Password
                };
                projectDto.layers.Add(layerDto);
                layerOrder++;
            }

            //-------------------------------------------------------------------------------------------------------------------------------------------------
            //Add forms to projectDto

            if (true)
            {
                var formIDs = project.ProjectForms.Select(pf => pf.FormId).Distinct();
                var formsQuery = _context.Forms.Where(f => formIDs.Contains(f.FormId));

                if (startDateTime != null)  //filter by timestamp
                {
                    formsQuery = formsQuery
                                    .Where(f => f.FormChangeLogs.Select(fcl => fcl.ChangeLog.ChangeDate).OrderBy(x => x).Last() > startDateTime);
                }
                formsQuery = formsQuery
                                .Include(f => f.FormChangeLogs)
                                    .ThenInclude(fcl => fcl.ChangeLog)
                                .Include(f => f.FormFormFields)
                                    .ThenInclude(f => f.FormField).ThenInclude(ff => ff.FieldChoices)
                                .Include(f => f.FormFormFields)
                                    .ThenInclude(f => f.FormField).ThenInclude(ff => ff.HiddenFieldChoices)
                                .Include(f => f.FormFormFields)
                                    .ThenInclude(f => f.FormField).ThenInclude(pff => pff.PublicMotherFormField).ThenInclude(ff => ff.FieldChoices);

                var forms = formsQuery.OrderBy(f => f.Title);

                foreach (Form form in forms)
                {
                    FormDTO formDto = new FormDTO()
                    {
                        formId = form.FormId,
                        title = form.Title,
                    };

                    ChangeLog lastFormChangeLog = form.FormChangeLogs.Select(fcl => fcl.ChangeLog).OrderBy(c => c.ChangeDate).LastOrDefault();
                    if (lastFormChangeLog != null)
                    {
                        formDto.timestamp = lastFormChangeLog.ChangeDate;
                    }

                    foreach (FormField field in form.FormFormFields.Select(fff => fff.FormField).OrderBy(ff => ff.Order))
                    {
                        FormField origFormField = field;
                        if (field.PublicMotherFormField != null) origFormField = field.PublicMotherFormField;

                        FormFieldDto fieldDto = new FormFieldDto()
                        {
                            fieldId = field.FormFieldId,
                            typeId = (int)origFormField.FieldTypeId,
                            title = origFormField.Title,
                            description = origFormField.Description,
                            source = origFormField.Source,
                            order = field.Order,
                            mandatory = origFormField.Mandatory,
                            useInRecordTitle = field.UseInRecordTitle,
                            standardValue = field.StandardValue
                        };
                        foreach (FieldChoice choice in origFormField.FieldChoices.OrderBy(fc => fc.Order))
                        {
                            FieldChoiceDto choiceDto = new FieldChoiceDto()
                            {
                                choiceId = choice.FieldChoiceId,
                                text = choice.Text,
                                order = choice.Order
                            };

                            // split by | for different value and text
                            if (choice.Text.Contains("|"))
                            {
                                choiceDto.text = choice.Text.Split("|")[1].TrimStart(' ');
                            }

                            // if not hidden, add it to the choice-list
                            if (!field.HiddenFieldChoices.Where(m=>m.FormField == field && m.FieldChoice == choice).Any())
                                fieldDto.fieldChoices.Add(choiceDto);
                        }

                        formDto.formFields.Add(fieldDto);
                    }

                    projectDto.forms.Add(formDto);
                }
            }
            _logger.LogInformation("PROJECT JSON GET: finished.");

            return projectDto;
        }

        /// <summary>
        /// Convert record models to DTO
        /// </summary>
        /// <param name="records"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        private List<RecordDTO> Records2Dto(IQueryable<Record> records, string userName, DateTime? startDateTime)
        {
            List<RecordDTO> recordDtos = new List<RecordDTO>();

            foreach (Record rec in records)
            {
                rec.ReadOnly = !rec.ProjectGroup.Group.GroupUsers.Select(gu => gu.User).Select(g => g.UserId).Contains(userName);

                RecordDTO recDto = new RecordDTO()
                {
                    recordId = rec.RecordId,
                    formId = (int)rec.Form?.FormId,
                    status = (int)rec.StatusId,
                    readOnly = rec.ReadOnly
                };

                ChangeLog firstRecordChangeLog = rec.RecordChangeLogs.Select(gcl => gcl.ChangeLog).OrderBy(c => c.ChangeDate).FirstOrDefault();
                if (firstRecordChangeLog != null)
                {
                    recDto.userName = firstRecordChangeLog.User.UserId;
                    recDto.fullUserName = firstRecordChangeLog.User.ToString();
                    recDto.timestamp = firstRecordChangeLog.ChangeDate;
                    recDto.creationTime = firstRecordChangeLog.ChangeDate;
                }
                ChangeLog lastRecordChangeLog = rec.RecordChangeLogs.Select(gcl => gcl.ChangeLog).OrderBy(c => c.ChangeDate).LastOrDefault();
                if (lastRecordChangeLog != null)
                {
                    recDto.userName = lastRecordChangeLog.User.UserId;
                    recDto.fullUserName = lastRecordChangeLog.User.ToString();
                    recDto.timestamp = lastRecordChangeLog.ChangeDate;
                }

                if (startDateTime != null)
                {
                    if (recDto.timestamp < startDateTime)
                        continue;
                }

                foreach (TextData text in rec.TextData)
                {
                    TextDataDTO textDto = new TextDataDTO()
                    {
                        textId = text.Id,
                        title = text.Title,
                        value = text.Value,
                        formFieldId = text.FormField?.FormFieldId,
                        fieldChoiceId = text.FieldChoice?.FieldChoiceId
                    };

                    // check if fieldchoice is in format value|label -> use label as text
                    if ((text.FieldChoice!=null) && (text.FieldChoice.Text.Contains("|")))
                    {
                        textDto.value = text.FieldChoice.Text.Split("|")[1].TrimStart(' ');
                    }

                    recDto.texts.Add(textDto);
                }
                foreach (NumericData numeric in rec.NumericData)
                {
                    NumericDataDTO numericDto = new NumericDataDTO()
                    {
                        numericId = numeric.Id,
                        title = numeric.Title,
                        value = numeric.Value,
                        formFieldId = numeric.FormField?.FormFieldId
                    };
                    recDto.numerics.Add(numericDto);
                }
                foreach (BooleanData booleanData in rec.BooleanData)
                {
                    BooleanDataDTO booleanDto = new BooleanDataDTO()
                    {
                        booleanId = booleanData.Id,
                        title = booleanData.Title,
                        value = booleanData.Value,
                        formFieldId = booleanData.FormField?.FormFieldId
                    };
                    recDto.booleans.Add(booleanDto);
                }

                recordDtos.Add(recDto);
            }

            return recordDtos;
        }

        //https://github.com/NetTopologySuite/NetTopologySuite.IO.GeoJSON
        public static string Geometry2GeoJSON(Geometry geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));

            string geoJson;

            var serializer = GeoJsonSerializer.Create();
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonTextWriter(stringWriter))
            {
                serializer.Serialize(jsonWriter, geometry);
                geoJson = stringWriter.ToString();
            }
            return geoJson;
        }
        public static Geometry GeoJSON2Geometry(string geoJson)
        {
            if (string.IsNullOrWhiteSpace(geoJson))
                throw new ArgumentNullException(nameof(geoJson));

            Geometry geometry;

            var serializer = GeoJsonSerializer.Create();
            using (var stringReader = new StringReader(geoJson))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                geometry = serializer.Deserialize<Geometry>(jsonReader);
            }
            return geometry;
        }

        // POST: api/Project
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        /// <summary>
        /// Post project to sync with database
        /// </summary>
        /// <param name="projectDto">projectDto in json format</param>
        /// <param name="updateDateTime">optional datetime: response contains projectDTO with changes from this dateTime on. Similar to GET with startDateTime -> if provided: deleted objects included</param>
        /// <param name="iamgod">set to true, if you know what you are doing: enables DB Changes</param>
        /// <returns> projectSyncDTO with changes from updateDateTime on</returns>
        [HttpPost("{updateDateTime?}")]
        public async Task<ActionResult<ProjectSyncDTO>> PostProject([FromBody] ProjectDTO projectDto, DateTime? updateDateTime, bool iamgod = false)
        {
            string json = JsonConvert.SerializeObject(projectDto, Formatting.Indented); //recreate json from DTO

            string userName = string.Empty;
            Guid projectDtoId = projectDto.projectId;

            ProjectSyncDTO syncDto = new ProjectSyncDTO()
            {
                projectId = projectDtoId,
            };

            try
            {



                //Check the user and the project
                var checkResult = await this.CheckUserProjectValid(projectDtoId);
                userName = checkResult.Item2;   //save userName
                if (checkResult.Item1 != null)
                {
                    string problemString = checkResult.Item1.Value.ToString();
                    _logger.LogError("PROJECT JSON POST: user = {userName} \n" +
                                     "\tProblem = {problemString} \n" +
                                     "{projectDto}\n", userName, problemString, json);           //LOG the DTO 

                    syncDto.success = false;
                    syncDto.error = checkResult.Item1.Value.ToString();
                }
                else
                {
                    //LOG the DTO 
                    _logger.LogInformation(
                        "PROJECT JSON POST: user = {userName}, god = {iamgod}, updateDateTime = {updateDateTime}\n{projectDto}\n",
                        userName, iamgod, updateDateTime, json);

                    ProjectGroup projectGroup = await _context.ProjectsGroups
                                                .AsNoTracking()
                                                .Include(pg => pg.Group.GroupUsers)
                                                .Where(p => p.ProjectId == projectDtoId)
                                                .Where(pg => pg.Group.GroupUsers.Select(gu => gu.UserId).Contains(userName))
                                                .FirstOrDefaultAsync();
                    if (projectGroup == null)
                    {
                        syncDto.success = false;
                        syncDto.error = "Project group not found";
                        return syncDto;
                    }
                    if (projectGroup.ReadOnly)
                    {
                        syncDto.success = false;
                        syncDto.error = "Project group is readonly";
                        return syncDto;
                    }

                    #region SYNC projectDto.records
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // crawling through the projectDto.records
                    ///////////////////////////////////////////
                    var recSyncDto = await SyncRecordsAsync(projectDto, projectGroup, false, iamgod);
                    syncDto.records.created = recSyncDto.created;
                    syncDto.records.updated = recSyncDto.updated;
                    syncDto.records.deleted = recSyncDto.deleted;
                    syncDto.records.skipped = recSyncDto.skipped;

                    #endregion

                    #region SYNC projectDto.geometries

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // crawling through the projectDto.geometries
                    ///////////////////////////////////////////
                    ///
                    var geometryDtos = projectDto.geometries.Where(g => g.readOnly == false); // not safe: check for Cross-Site-Scripting

                    //Check Usernames
                    var userIdCheck = geometryDtos.Select(r => r.userName).Distinct().All(id => _context.Users.Select(u => u.UserId).Contains(id));
                    var errorIds = new List<string>();
                    if (userIdCheck == false)
                    {
                        var userIds = geometryDtos.Select(r => r.userName).Distinct().Select(id => new { exists = _context.Users.Select(u => u.UserId).Contains(id), id });
                        errorIds = userIds.Where(u => u.exists == false).Select(u => u.id).ToList();
                    }

                    var geometriesToCreate = new List<Guid>();
                    var geometriesToUpdate = new List<Guid>();
                    var geometriesToDelete = new List<Guid>();
                    foreach (ReferenceGeometryDTO geometryDTO in geometryDtos)
                    {
                        if (errorIds.Any() && errorIds.Contains(geometryDTO.userName))
                        {
                            syncDto.geometries.skipped.Add(geometryDTO.geometryId, $"ERR: Geometry userName '{geometryDTO.userName ?? "[null]"}' unknown!");
                            continue;
                        }

                        switch (geometryDTO.status)
                        {
                            case (int)StatusEnum.created:
                                geometriesToCreate.Add(geometryDTO.geometryId);
                                break;
                            case (int)StatusEnum.changed:
                                geometriesToUpdate.Add(geometryDTO.geometryId);
                                break;
                            case (int)StatusEnum.deleted:
                                geometriesToDelete.Add(geometryDTO.geometryId);
                                break;
                            case (int)StatusEnum.unchanged:
                                var geomRecordStatuses = geometryDTO.records.Select(r => r.status).Distinct().ToList();
                                if (geomRecordStatuses.Contains(-1) || geomRecordStatuses.Contains(2) || geomRecordStatuses.Contains(3))
                                {
                                    syncDto.geometries.skipped.Add(geometryDTO.geometryId, "NONE: Changes were made to the associated records.");
                                }
                                else
                                {
                                    syncDto.geometries.skipped.Add(geometryDTO.geometryId, "NONE: Geometry without changes received. Is the geometry status correct?");
                                }
                                break;
                            default:
                                syncDto.geometries.skipped.Add(geometryDTO.geometryId, $"ERR: Geometry status {geometryDTO.status} unknown!");
                                break;
                        }
                    }

                    /////////////////////////////
                    /// UPDATE GEOMETRY
                    /// /////////////////////////
                    foreach (Guid updateGuid in geometriesToUpdate)
                    {
                        var refGeometry = await _context.Geometries
                            .Include(g => g.GeometryChangeLogs).ThenInclude(gcl => gcl.ChangeLog)
                            .Where(g => g.GeometryId == updateGuid).SingleOrDefaultAsync();

                        if (refGeometry == null)
                        {
                            //update is not possible, geometry doesn't exit yet. Geometry is marked for Creation
                            geometriesToCreate.Add(updateGuid);
                            continue;

                            //Skip update
                            //syncDto.geometries.skipped.Add(updateGuid, "UPD: Geometry not found in db. Geometry NOT updated in db.");
                        }
                        else
                        {
                            string updateInfo = "";

                            var geometryDto = projectDto.geometries
                                .Where(g => g.geometryId == updateGuid).SingleOrDefault();
                            if (geometryDto == null)
                            {
                                throw new NullReferenceException(nameof(geometryDto));
                            }

                            //Check Timestamp for Synchronisation
                            ChangeLog geometryLastChangeLog = refGeometry.GeometryChangeLogs.Select(rc => rc.ChangeLog).OrderBy(c => c.ChangeDate).LastOrDefault();
                            if (geometryLastChangeLog != null)
                            {
                                DateTimeOffset geometryLastChangeDate = geometryLastChangeLog.ChangeDate;

                                if (geometryDto.timestamp <= geometryLastChangeDate)
                                {
                                    //requested update is older than latest change in database
                                    syncDto.geometries.skipped.Add(updateGuid, "UPD: Geometry timestamp is older or equal compared to database. Geometry NOT updated in db.");
                                    continue;
                                }
                            }
                            else
                            {
                                updateInfo += "WARNING: no change log found in db; ";
                            }
                            refGeometry.StatusId = StatusEnum.unchanged;

                            //change attributes
                            if (!string.IsNullOrWhiteSpace(geometryDto.geometryName) && refGeometry.GeometryName != geometryDto.geometryName)
                            {
                                refGeometry.GeometryName = geometryDto.geometryName;
                                updateInfo += "name: changed, ";
                            }

                            if (geometryDto.geometry != null)
                            {
                                Geometry geometryFromJson;
                                try
                                {
                                    geometryFromJson = GeoJSON2Geometry(geometryDto.geometry);                        //Parse geometry string
                                }
                                catch (Exception ex)
                                {
                                    syncDto.geometries.skipped.Add(updateGuid, $"GeoJSON exception: {ex.Message}");
                                    continue;
                                }
                                if (geometryFromJson is Point)
                                {
                                    Point point = (Point)geometryFromJson;
                                    //if (!refGeometry.Point.EqualsExact(point, 1e-6))      //Comparison not working -> always save new geometry
                                    {
                                        updateInfo += "point: changed";
                                        refGeometry.Point = point;
                                    }
                                    refGeometry.Line = null;
                                    refGeometry.Polygon = null;
                                }
                                else if (geometryFromJson is LineString)
                                {
                                    LineString line = (LineString)geometryFromJson;
                                    //if (!refGeometry.Line.EqualsExact(line, 1e-6))        //Comparison not working -> always save new geometry
                                    {
                                        updateInfo += "line: changed";
                                        refGeometry.Line = line;
                                    }
                                    refGeometry.Point = null;
                                    refGeometry.Polygon = null;
                                }
                                else if (geometryFromJson is Polygon)
                                {
                                    Polygon polygon = (Polygon)geometryFromJson;
                                    //if (!refGeometry.Polygon.EqualsExact(polygon, 1e-6))  //Comparison not working -> always save new geometry
                                    {
                                        updateInfo += "polygon: changed";
                                        refGeometry.Polygon = polygon;
                                    }
                                    refGeometry.Point = null;
                                    refGeometry.Line = null;
                                }
                                else
                                {
                                    syncDto.geometries.skipped.Add(updateGuid, $"GeoJSON not supported: {geometryFromJson?.GetType().UnderlyingSystemType.Name ?? "undefined"}");
                                    continue;
                                }
                            }

                            //add change log
                            if (geometryDto.timestamp > geometryDto.creationTime)
                            {
                                ChangeLogGeometry changeLogGeom = new ChangeLogGeometry()    //create change logs: Last Change
                                {
                                    ChangeLog = new ChangeLog()
                                    {
                                        UserId = geometryDto.userName,          //assign userName from DTO
                                        Log = "[sync] geometry changed",
                                        ChangeDate = geometryDto.timestamp
                                    }
                                };
                                refGeometry.GeometryChangeLogs.Add(changeLogGeom);
                            }

                            syncDto.geometries.updated.Add(updateGuid, updateInfo);
                        }
                    }

                    /////////////////////////////
                    /// CREATE GEOMETRY
                    /// /////////////////////////
                    foreach (Guid createGuid in geometriesToCreate)
                    {
                        if (await _context.Geometries                   //geometry already exists
                                .Where(r => r.GeometryId == createGuid)
                                .AnyAsync())
                        {
                            //Skip create
                            syncDto.geometries.skipped.Add(createGuid, "NEW: Geometry Id found in db. Geometry NOT updated in db. Is the geometry status correct?");
                            continue;
                        }
                        else
                        {
                            var geometryDto = projectDto.geometries.Where(g => g.geometryId == createGuid).SingleOrDefault();
                            if (geometryDto == null)
                            {
                                throw new NullReferenceException(nameof(geometryDto));
                            }
                            ReferenceGeometry newGeom = new ReferenceGeometry()
                            {
                                ProjectGroupGroupId = projectGroup.GroupId,
                                ProjectGroupProjectId = projectGroup.ProjectId,
                                StatusId = StatusEnum.unchanged,
                                GeometryId = createGuid,
                                GeometryName = geometryDto.geometryName,
                            };

                            Geometry geometryFromJson;
                            try
                            {
                                geometryFromJson = GeoJSON2Geometry(geometryDto.geometry);                        //Parse geometry string
                            }
                            catch (Exception ex)
                            {
                                syncDto.geometries.skipped.Add(createGuid, $"GeoJSON exception: {ex.Message}");
                                continue;
                            }

                            if (geometryFromJson is Point)
                            {
                                newGeom.Point = (Point)geometryFromJson;
                            }
                            else if (geometryFromJson is LineString)
                            {
                                newGeom.Line = (LineString)geometryFromJson;
                            }
                            else if (geometryFromJson is Polygon)
                            {
                                newGeom.Polygon = (Polygon)geometryFromJson;
                            }
                            else
                            {
                                syncDto.geometries.skipped.Add(createGuid, $"GeoJSON not supported: {geometryFromJson.GetType().UnderlyingSystemType.Name}");
                                continue;
                            }

                            //ChangeLog Update
                            ChangeLogGeometry changeLogGeom = new ChangeLogGeometry()
                            {
                                ChangeLog = new ChangeLog()
                                {
                                    UserId = geometryDto.userName,  //assign userName from DTO
                                    Log = "[sync] new geometry created",
                                    ChangeDate = geometryDto.creationTime
                                }
                            };
                            newGeom.GeometryChangeLogs.Add(changeLogGeom);
                            if (geometryDto.timestamp > geometryDto.creationTime)
                            {
                                changeLogGeom = new ChangeLogGeometry()
                                {
                                    ChangeLog = new ChangeLog()
                                    {
                                        UserId = geometryDto.userName,  //assign userName from DTO
                                        Log = "[sync] geometry changed",
                                        ChangeDate = geometryDto.timestamp
                                    }
                                };
                                newGeom.GeometryChangeLogs.Add(changeLogGeom);
                            }

                            _context.Geometries.Add(newGeom);

                            syncDto.geometries.created.Add(createGuid);
                        }
                    }

                    /////////////////////////////
                    /// DELETE GEOMETRY
                    /// /////////////////////////
                    foreach (Guid deleteGuid in geometriesToDelete)
                    {
                        string deleteInfo = "";

                        var geometryDto = projectDto.geometries.Where(g => g.geometryId == deleteGuid).SingleOrDefault();
                        if (geometryDto == null)
                        {
                            throw new NullReferenceException(nameof(geometryDto));
                        }

                        ReferenceGeometry refGeometry = await _context.Geometries
                            .Include(g => g.GeometryChangeLogs)
                                .ThenInclude(gcl => gcl.ChangeLog)
                            .Where(g => g.GeometryId == deleteGuid).SingleOrDefaultAsync();
                        if (refGeometry == null)
                        {
                            //Skip delete
                            syncDto.geometries.skipped.Add(deleteGuid, "DEL: Provided geometry id not found in db.");
                            continue;
                        }

                        //Check Timestamp for Synchronisation
                        ChangeLog geometryLastChangeLog = refGeometry.GeometryChangeLogs.Select(rc => rc.ChangeLog).OrderBy(c => c.ChangeDate).LastOrDefault();
                        if (geometryLastChangeLog != null)
                        {
                            DateTimeOffset geometryLastChangeDate = geometryLastChangeLog.ChangeDate;

                            if (geometryDto.timestamp <= geometryLastChangeDate)
                            {
                                //requested update is older than latest change in database
                                syncDto.geometries.skipped.Add(deleteGuid, "DEL: Geometry timestamp is older or equal compared to db. Geometry NOT delted from db.");
                                continue;
                            }
                        }
                        else
                        {
                            deleteInfo += "WARNING: no change log found in db; ";
                        }
                        refGeometry.StatusId = StatusEnum.deleted;

                        //create change log
                        ChangeLogGeometry changeLogGeom = new ChangeLogGeometry()   //create change logs: Last Change
                        {
                            ChangeLog = new ChangeLog()
                            {
                                UserId = geometryDto.userName,          //assign userName from DTO
                                Log = "[sync] geometry deleted",
                                ChangeDate = geometryDto.timestamp
                            }
                        };
                        refGeometry.GeometryChangeLogs.Add(changeLogGeom);

                        syncDto.geometries.deleted.Add(deleteGuid, deleteInfo);
                    }

                    ///////////////////////////////////////

                    if (iamgod) //DANGER ZONE
                    {
                        await _context.SaveChangesAsync(); //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< ACTIVATE DB CHANGES <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                    }

                    #endregion //end of SYNC projectDto.geometries

                    #region SYNC projectDto.geometries.records
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    //////// crawling through the geometry.records
                    /////////////////////////////////////////////////
                    var geomRecSyncDto = await SyncRecordsAsync(projectDto, projectGroup, true, iamgod);
                    syncDto.geometries.geometryRecords.created = geomRecSyncDto.created;
                    syncDto.geometries.geometryRecords.updated = geomRecSyncDto.updated;
                    syncDto.geometries.geometryRecords.deleted = geomRecSyncDto.deleted;
                    syncDto.geometries.geometryRecords.skipped = geomRecSyncDto.skipped;

                    #endregion //end of SYNC projectDto.geometries.records

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                    if (iamgod) //DANGER ZONE
                    {
                        await _context.SaveChangesAsync(); //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< ACTIVATE DB CHANGES <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                        syncDto.success = true;
                    }
                    else
                    {
                        syncDto.success = false;
                        syncDto.error = "DB changes are deactivated by you... check param 'iamgod'";
                    }

                    //Logging response
                    string syncJson = JsonConvert.SerializeObject(syncDto, Formatting.Indented);        //recreate json from DTO
                    _logger.LogInformation("PROJECT JSON POST: finished\n{syncDto}\n", syncJson);       //LOG the syncDTO without projectUpdate
                }

                //if (updateDateTime != null)
                {
                    //add projectDto with updates to syncDto
                    syncDto.projectUpdate = (await this.GetProject(projectDtoId, updateDateTime)).Value;

                    //TODO: maybe it's good idea to remove forms and layers form DTO?
                    //syncDto.projectUpdate.forms = null;
                    //syncDto.projectUpdate.layers = null;

                    //OR: use change log also for forms<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                }

                return syncDto;
            }

            //Global catch
            catch (Exception ex)
            {
                syncDto.success = false;
                syncDto.error = ex.ToString();

                string syncJson = JsonConvert.SerializeObject(syncDto, Formatting.Indented);    //recreate json from DTO
                _logger.LogError("PROJECT JSON POST: aborted\n{syncDto}\n", syncJson);          //LOG the syncDTO 
                return syncDto;
            }
        }


        /// <summary>
        /// Syncronize records
        /// </summary>
        /// <param name="projectDto"></param>
        /// <param name="projectGroup"></param>
        /// <param name="syncGeometryRecords">false = sync project records, true = sync geometries records</param>
        /// <param name="iamgod">set to true, if you know what you are doing: enables DB Changes</param>
        /// <returns></returns>
        private async Task<RecordsSyncDTO> SyncRecordsAsync(ProjectDTO projectDto, ProjectGroup projectGroup, bool syncGeometryRecords = false, bool iamgod = false)
        {
            RecordsSyncDTO recSyncDto = new RecordsSyncDTO();

            var recordsToCreate = new List<Guid>();
            var recordsToUpdate = new List<Guid>();
            var recordsToDelete = new List<Guid>();

            List<RecordDTO> recordDtos = new List<RecordDTO>();
            if (syncGeometryRecords == false)    //look at project.records
            {
                recordDtos = projectDto.records.Where(r => r.readOnly == false).ToList();                               // not safe: check for Cross-Site-Scripting}
            }
            else if (syncGeometryRecords == true)    //look at project.geometries.records
            {
                recordDtos = projectDto.geometries.SelectMany(g => g.records).Where(r => r.readOnly == false).ToList();   // not safe: check for Cross-Site-Scripting}
            }

            //Check Usernames
            var userIdCheck = recordDtos.Select(r => r.userName).Distinct().All(id => _context.Users.Select(u => u.UserId).Contains(id));
            var errorIds = new List<string>();
            if (userIdCheck == false)
            {
                var userIds = recordDtos.Select(r => r.userName).Distinct().Select(id => new { exists = _context.Users.Select(u => u.UserId).Contains(id), id });
                errorIds = userIds.Where(u => u.exists == false).Select(u => u.id).ToList();
            }

            foreach (RecordDTO recordDTO in recordDtos)
            {
                if (errorIds.Any() && errorIds.Contains(recordDTO.userName))
                {
                    recSyncDto.skipped.Add(recordDTO.recordId, $"ERR: Record userName '{recordDTO.userName ?? "[null]"}' unknown!");
                    continue;
                }

                switch (recordDTO.status)
                {
                    case (int)StatusEnum.created:
                        recordsToCreate.Add(recordDTO.recordId);
                        break;
                    case (int)StatusEnum.changed:
                        recordsToUpdate.Add(recordDTO.recordId);
                        break;
                    case (int)StatusEnum.deleted:
                        recordsToDelete.Add(recordDTO.recordId);
                        break;
                    case (int)StatusEnum.unchanged:
                        recSyncDto.skipped.Add(recordDTO.recordId, "NONE: Record without changes received. Is the record status correct?");
                        break;
                    default:
                        recSyncDto.skipped.Add(recordDTO.recordId, $"ERR: Record status {recordDTO.status} unknown!");
                        break;
                }
            }

            /////////////////////////////
            /// UPDATE RECORD
            /// /////////////////////////
            foreach (Guid updateGuid in recordsToUpdate)
            {
                var record = await _context.Records
                    .Include(r => r.RecordChangeLogs).ThenInclude(rcl => rcl.ChangeLog)
                    .Include(r => r.TextData)
                    .Include(r => r.NumericData)
                    .Include(r => r.BooleanData)
                    .Where(r => r.RecordId == updateGuid).SingleOrDefaultAsync();

                if (record == null)
                {
                    //update is not possible, record doesn't exit yet. Record is marked for Creation
                    recordsToCreate.Add(updateGuid);
                    continue;

                    //Skip update
                    //recSyncDto.skipped.Add(updateGuid, "UPD: Record not found in db. Record NOT updated in db.");
                }
                else
                {
                    string updateInfo = "";

                    var recordDto = recordDtos.Where(r => r.recordId == updateGuid).SingleOrDefault();
                    if (recordDto == null)
                    {
                        throw new NullReferenceException(nameof(recordDto));
                    }

                    //Check Timestamp for Synchronisation
                    ChangeLog recordLastChangeLog = record.RecordChangeLogs.Select(rc => rc.ChangeLog).OrderBy(c => c.ChangeDate).LastOrDefault();
                    if (recordLastChangeLog != null)
                    {
                        DateTimeOffset recordLastChangeDate = recordLastChangeLog.ChangeDate;

                        if (recordDto.timestamp <= recordLastChangeDate)
                        {
                            //requested update is older than latest change in database
                            recSyncDto.skipped.Add(updateGuid, "UPD: Record timestamp is older or equal compared to database. Record NOT updated in db.");
                            continue;
                        }
                    }
                    else
                    {
                        updateInfo += "WARNING: no change log found in db; ";
                    }

                    //Check parent geometry geomDto.geometryId exists
                    if (syncGeometryRecords == true)
                    {
                        //get the geometry id for this record
                        var geomDto = projectDto.geometries.Where(g => g.records.Select(r => r.recordId).Contains(updateGuid)).FirstOrDefault();
                        if (geomDto != null)
                        {
                            //Check parent geometry geomDto.geometryId exists
                            bool geometryExists = await _context.Geometries.AnyAsync(g => g.GeometryId == geomDto.geometryId);
                            if (!geometryExists)
                            {
                                recSyncDto.skipped.Add(updateGuid, "UPD: Parent geometry doesn't exist. Record NOT updated in db.");
                                continue;
                            }
                        }
                    }

                    ////////////////////////////////////////////////////////////////////////////////
                    // Start changing the record

                    record.StatusId = StatusEnum.unchanged;  //<<<<<<<<<<<<<<<<<<<<<<<<<<<<

                    bool hasNewParent = false;
                    if (syncGeometryRecords == false)       //look at project.records
                    {
                        if (record.GeometryId != null)
                            hasNewParent = true;
                        record.GeometryId = null;
                    }
                    else if (syncGeometryRecords == true)    //look at project.geometries.records
                    {
                        //get the geometry id for this record
                        var geomDto = projectDto.geometries.Where(g => g.records.Select(r => r.recordId).Contains(updateGuid)).FirstOrDefault();
                        if (geomDto != null)
                        {
                            if (record.GeometryId != geomDto.geometryId)
                                hasNewParent = true;
                            record.GeometryId = geomDto.geometryId;
                        }
                        else
                        {
                            //should not happen!
                            throw new ApplicationException("mother geometry not found for record: " + updateGuid);
                        }

                    }

                    if (iamgod) //DANGER ZONE
                    {
                        _context.TextData.RemoveRange(record.TextData);
                        _context.NumericData.RemoveRange(record.NumericData);
                        _context.BooleanData.RemoveRange(record.BooleanData);
                        await _context.SaveChangesAsync(); //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< ACTIVATE DB CHANGES <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                    }
                    else
                    {
                        record.TextData.Clear();
                        record.NumericData.Clear();
                        record.BooleanData.Clear();
                    }

                    foreach (TextDataDTO textDto in recordDto.texts)            //TextData
                    {
                        TextData newText = textDto.Dto2Model();
                        // Is FieldChoice of type value|label ? Then replace label with value to store only value into db
                        if (newText.FieldChoiceId != null)
                        {
                            FieldChoice fc = await _context.FieldChoices.FindAsync(newText.FieldChoiceId);
                            if ((fc != null) && (fc.Text.Contains("|")))
                            {
                                newText.Value = fc.Text.Split('|')[0].TrimEnd(' ');
                            }
                        }
                        record.TextData.Add(newText);                           //Add new data to record
                    }

                    foreach (NumericDataDTO numericDto in recordDto.numerics)   //NumericData
                    {
                        NumericData newNumeric = numericDto.Dto2Model();
                        record.NumericData.Add(newNumeric);                     //Add new data to record
                    }

                    foreach (BooleanDataDTO booleanDto in recordDto.booleans)   //BooleanData
                    {
                        BooleanData newBoolean = booleanDto.Dto2Model();
                        record.BooleanData.Add(newBoolean);                     //Add new data to record
                    }

                    if (recordDto.timestamp > recordDto.creationTime)
                    {
                        ChangeLogRecord changeLogRec = new ChangeLogRecord()    //create change logs: Last Change
                        {
                            ChangeLog = new ChangeLog()
                            {
                                UserId = recordDto.userName,        //assign userName from DTO
                                Log = "[sync] record changed",
                                ChangeDate = recordDto.timestamp
                            }
                        };
                        record.RecordChangeLogs.Add(changeLogRec);
                    }

                    updateInfo += $"txt:{record.TextData.Count}, num:{record.NumericData.Count}, bool:{record.BooleanData.Count}";
                    if (hasNewParent)
                        updateInfo += $", newParentGeometry: {record.GeometryId}";

                    recSyncDto.updated.Add(updateGuid, updateInfo);
                }
            }

            /////////////////////////////
            /// CREATE RECORD
            /// /////////////////////////
            foreach (Guid createGuid in recordsToCreate)
            {
                if (await _context.Records                      //record already exists 
                    .Where(r => r.RecordId == createGuid)
                    .AnyAsync())
                {
                    //Skip create
                    recSyncDto.skipped.Add(createGuid, "NEW: Record Id found in db. Record NOT updated in db. Is the record status correct?");
                    continue;
                }
                else
                {
                    string createInfo = "";

                    var recordDto = recordDtos.Where(r => r.recordId == createGuid).SingleOrDefault();
                    if (recordDto == null)
                    {
                        throw new NullReferenceException(nameof(recordDto));
                    }

                    //Check parent geometry geomDto.geometryId exists
                    if (syncGeometryRecords == true)
                    {
                        //get the geometry id for this record
                        var geomDto = projectDto.geometries.Where(g => g.records.Select(r => r.recordId).Contains(createGuid)).FirstOrDefault();
                        if (geomDto != null)
                        {
                            //Check parent geometry geomDto.geometryId exists
                            bool geometryExists = await _context.Geometries.AnyAsync(g => g.GeometryId == geomDto.geometryId);
                            if (!geometryExists)
                            {
                                recSyncDto.skipped.Add(createGuid, "NEW: Parent geometry doesn't exist. Record NOT updated in db.");
                                continue;
                            }
                        }
                    }

                    ////////////////////////////////////////////////////////////////////////////////
                    // Start creating the record

                    var formId = recordDto.formId;
                    Record newRec = new Record()
                    {
                        ProjectGroupGroupId = projectGroup.GroupId,
                        ProjectGroupProjectId = projectGroup.ProjectId,
                        StatusId = StatusEnum.unchanged,
                        RecordId = createGuid,
                        FormId = formId,

                    };
                    if (syncGeometryRecords == false)       //look at project.records
                    {
                        newRec.GeometryId = null;
                    }
                    else if (syncGeometryRecords == true)    //look at project.geometries.records
                    {
                        //get the geometry id for this record
                        var geomDto = projectDto.geometries.Where(g => g.records.Select(r => r.recordId).Contains(createGuid)).FirstOrDefault();
                        if (geomDto != null)
                        {
                            newRec.GeometryId = geomDto.geometryId;
                        }
                        else
                        {
                            //should not happen!
                            throw new ApplicationException("mother geometry not found for record: " + createGuid);
                        }

                    }

                    foreach (TextDataDTO textDto in recordDto.texts)            //TextData
                    {
                        TextData newText = textDto.Dto2Model();

                        // Is FieldChoice of type value|label ? Then replace label with value to store only value into db
                        if (newText.FieldChoiceId != null)
                        {
                            FieldChoice fc = await _context.FieldChoices.FindAsync(newText.FieldChoiceId);
                            if ((fc != null) && (fc.Text.Contains("|")))
                            {
                                newText.Value = fc.Text.Split('|')[0].TrimEnd(' ');
                            }
                        }

                        newRec.TextData.Add(newText);
                    }
                    foreach (NumericDataDTO numericDto in recordDto.numerics)   //NumericData
                    {
                        NumericData newNumeric = numericDto.Dto2Model();
                        newRec.NumericData.Add(newNumeric);
                    }
                    foreach (BooleanDataDTO booleanDto in recordDto.booleans)   //BooleanData
                    {
                        BooleanData newBoolean = booleanDto.Dto2Model();
                        newRec.BooleanData.Add(newBoolean);
                    }

                    ChangeLogRecord changeLogRec = new ChangeLogRecord()    //create change logs: Create
                    {
                        ChangeLog = new ChangeLog()
                        {
                            UserId = recordDto.userName,            //assign userName from DTO
                            Log = "[sync] new record created",
                            ChangeDate = recordDto.creationTime
                        }
                    };
                    newRec.RecordChangeLogs.Add(changeLogRec);
                    if (recordDto.timestamp > recordDto.creationTime)
                    {
                        changeLogRec = new ChangeLogRecord()                //create change logs: Last Change
                        {
                            ChangeLog = new ChangeLog()
                            {
                                UserId = recordDto.userName,        //assign userName from DTO
                                Log = "[sync] record changed",
                                ChangeDate = recordDto.timestamp
                            }
                        };
                        newRec.RecordChangeLogs.Add(changeLogRec);
                    }

                    _context.Records.Add(newRec);

                    createInfo += $"txt:{newRec.TextData.Count}, num:{newRec.NumericData.Count}, bool:{newRec.BooleanData.Count}";

                    recSyncDto.created.Add(createGuid, createInfo);
                }
            }

            //////////////////////////////
            //// DELETE RECORD
            //////////////////////////////
            foreach (Guid deleteGuid in recordsToDelete)
            {
                string deleteInfo = "";

                var recordDto = recordDtos.Where(r => r.recordId == deleteGuid).SingleOrDefault();
                if (recordDto == null)
                {
                    throw new NullReferenceException(nameof(recordDto));
                }

                Record rec = await _context.Records
                    .Include(r => r.RecordChangeLogs)
                        .ThenInclude(rcl => rcl.ChangeLog)
                    .Where(r => r.RecordId == deleteGuid).SingleOrDefaultAsync();
                if (rec == null)
                {
                    //Skip delete
                    recSyncDto.skipped.Add(deleteGuid, "DEL: Provided record id not found in db.");
                    continue;
                }
                //Check Timestamp for Synchronisation
                ChangeLog lastChangeLog = rec.RecordChangeLogs.Select(rcl => rcl.ChangeLog).OrderBy(c => c.ChangeDate).LastOrDefault();
                if (lastChangeLog != null)
                {
                    DateTimeOffset lastChangeDate = lastChangeLog.ChangeDate;

                    //TODO: check LOGIC <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

                    if (recordDto.timestamp <= lastChangeDate) //somebody worked on this record after the current user deleted this record
                    {
                        //Skip delete
                        recSyncDto.skipped.Add(deleteGuid, "DEL: Provided timestamp is older or equal compared to db. Record NOT deleted from db.");
                        continue;
                    }
                }
                else
                {
                    deleteInfo += "WARNING: no change log found in db; ";
                }
                rec.StatusId = StatusEnum.deleted;  //<<<<<<<<<<<<<<<<<<<<<<<<<<<<

                //create change log
                ChangeLogRecord changeLogRec = new ChangeLogRecord()    //create change logs: Last Change
                {
                    ChangeLog = new ChangeLog()
                    {
                        UserId = recordDto.userName,        //assign userName from DTO
                        Log = "[sync] record deleted",
                        ChangeDate = recordDto.timestamp
                    }
                };
                rec.RecordChangeLogs.Add(changeLogRec);

                recSyncDto.deleted.Add(deleteGuid, deleteInfo);
            }

            if (iamgod) //DANGER ZONE
            {
                await _context.SaveChangesAsync(); //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< ACTIVATE DB CHANGES <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            }


            return recSyncDto;
        }

        /// <summary>
        /// Check if user exists and is allowed to work with provided project id
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns>Tuple with Item1: ObjectResult on error, Item2: userName as string</returns>
        private async Task<Tuple<ObjectResult, string>> CheckUserProjectValid(Guid projectId)
        {
            string userName = string.Empty;
            try
            {
                userName = ((ClaimsIdentity)User.Identity).FindFirst("preferred_username").Value;

                //Check if user exists in db
                if (!await _context.Users.AnyAsync(u => u.UserId == userName))
                {
                    return new Tuple<ObjectResult, string>(
                        BadRequest($"UserId '{userName}' not found in database."),
                        userName
                        );
                }

                //Check if user belongs to project
                var userProjectGroups = _context.Users
                                                .AsNoTracking()
                                                .Include(u => u.UserGroups)
                                                    .ThenInclude(ug => ug.Group.GroupProjects)
                                                .Where(u => u.UserId == userName)
                                                .SelectMany(u => u.UserGroups.SelectMany(ug => ug.Group.GroupProjects))
                                                ;
                if (!await userProjectGroups.AnyAsync(pg => pg.ProjectId == projectId))
                {
                    return new Tuple<ObjectResult, string>(
                        BadRequest($"UserId '{userName}' is not assigned to project '{projectId}'"),
                        userName
                        );
                }

            }
            catch (NullReferenceException)
            {
                return new Tuple<ObjectResult, string>(
                    BadRequest($"UserName not defined in claims."),
                    userName
                    );
            }
            catch (Exception ex)
            {
                return new Tuple<ObjectResult, string>(
                    BadRequest($"Something went wrong: \n{ex.GetType()} \n{ex.Message.ToString()}"),
                    userName
                    );
            }

            return new Tuple<ObjectResult, string>(null, userName);
        }

        //// DELETE: api/Project/5
        ///// <summary>
        ///// Delete by setting the status=3
        ///// </summary>
        ///// <param name="id"></param>
        ///// <returns></returns>
        //[HttpDelete("{id}")]
        //public async Task<ActionResult<Project>> DeleteProject(Guid id)
        //{
        //    var project = await _context.Projects.FindAsync(id);
        //    if (project == null)
        //    {
        //        return NotFound();
        //    }

        //    project.StatusId = StatusEnum.deleted;
        //    //_context.Projects.Remove(project);
        //    await _context.SaveChangesAsync();

        //    return project;
        //}

        private bool ProjectExists(Guid id)
        {
            return _context.Projects.Any(e => e.ProjectId == id);
        }
    }
}
