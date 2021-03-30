using BioDivCollector.DB.Models.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BioDivCollector.DB.Helpers
{
    public static class ProjectManager
    {
        /// <summary>
        /// get projects for given user depending on his role
        /// </summary>
        /// <param name="db"></param>
        /// <param name="user"></param>
        /// <param name="role"></param>
        /// <returns>
        /// List(projects) with undeleted projets
        /// </returns>
        public async static Task<List<Project>> UserProjectsAsync(BioDivContext db, User user, RoleEnum role)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            string userName = user.UserId;

            IQueryable<Project> projectsQuery = db.Projects
                        .Include(p => p.ProjectConfigurator)
                        .Include(p => p.ProjectManager)
                        .Where(p => p.StatusId != StatusEnum.deleted)                         //only active pojects
                        ;

            switch (role)
            {
                case RoleEnum.BE:
                    throw new NotImplementedException();
                    break;
                case RoleEnum.AV:
                    throw new NotImplementedException();
                    break;
                case RoleEnum.DH:
                    throw new NotImplementedException();
                    break;
                case RoleEnum.DM:
                    //not changing query
                    break;
                case RoleEnum.PL:
                    projectsQuery = projectsQuery.Where(p => p.ProjectManager.UserId == userName);
                    break;
                case RoleEnum.PK:
                    projectsQuery = projectsQuery.Where(p => p.ProjectConfigurator.UserId == userName);
                    break;
                case RoleEnum.EF:
                    projectsQuery = db.GroupsUsers
                        .Where(gu => gu.UserId == userName)
                        .Include(gu => gu.Group)
                            .ThenInclude(g => g.GroupProjects)
                            .ThenInclude(gp => gp.Project.ProjectManager)
                        .Include(gu => gu.Group)
                            .ThenInclude(g => g.GroupProjects)
                            .ThenInclude(gp => gp.Project.ProjectConfigurator)
                        .Select(gu => gu.Group)
                            .Where(g => g.StatusId != StatusEnum.deleted)                           //only active groups
                            // .Where(g => g.GroupStatusId == GroupStatusEnum.Gruppe_bereit ||
                            //       g.GroupStatusId == GroupStatusEnum.Gruppendaten_fehlerhaft)      // 20210109 chs: change to GroupProjects below
                        .SelectMany(g => g.GroupProjects).Where(gp=>gp.GroupStatusId == GroupStatusEnum.Gruppe_bereit ||
                                   gp.GroupStatusId == GroupStatusEnum.Gruppendaten_fehlerhaft).Where(gp => gp.ReadOnly==false) //only ready and faulty groups
                        .Select(gp => gp.Project)
                            .Where(p => p.StatusId != StatusEnum.deleted)                           //only active pojects
                            .Where(p => p.ProjectStatusId == ProjectStatusEnum.Projekt_bereit);      //only ready projects  
                    break;
                case RoleEnum.LE:
                    projectsQuery = db.GroupsUsers
                        .Where(gu => gu.UserId == userName)
                        .Include(gu => gu.Group)
                            .ThenInclude(g => g.GroupProjects)
                            .ThenInclude(gp => gp.Project.ProjectManager)
                        .Include(gu => gu.Group)
                            .ThenInclude(g => g.GroupProjects)
                            .ThenInclude(gp => gp.Project.ProjectConfigurator)
                        .Select(gu => gu.Group)
                        .Where(g => g.StatusId != StatusEnum.deleted)                           //only active groups
                        //.Where(g => g.GroupStatusId == GroupStatusEnum.Gruppe_bereit ||
                        //       g.GroupStatusId == GroupStatusEnum.Gruppendaten_fehlerhaft || g.GroupStatusId == GroupStatusEnum.Gruppendaten_gueltig)      //only ready and faulty groups and gueltig (readonlys are gueltig)
                        // 20210109 chs: change to GroupProjects below
                        .SelectMany(g => g.GroupProjects).Where(gp => gp.ReadOnly == true ||
                                   gp.GroupStatusId == GroupStatusEnum.Gruppendaten_fehlerhaft || gp.GroupStatusId == GroupStatusEnum.Gruppendaten_gueltig ||
                                   gp.GroupStatusId == GroupStatusEnum.Gruppendaten_erfasst)
                        .Select(gp => gp.Project)
                            .Where(p => p.StatusId != StatusEnum.deleted)                           //only active pojects
                            .Where(p => p.ProjectStatusId == ProjectStatusEnum.Projekt_bereit);      //only ready projects  
                    break;
                case RoleEnum.LE_OGD:
                    projectsQuery = projectsQuery.Where(p => p.OGD == true);
                    break;
                case RoleEnum.NZ:
                    throw new NotImplementedException();
                    break;
                default:
                    throw new NotImplementedException();
                    break;
            }

            return await projectsQuery.Distinct().ToListAsync();
        }
    }
}
