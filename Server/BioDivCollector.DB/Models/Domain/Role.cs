using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BioDivCollector.DB.Models.Domain
{
    //public class Role
    //{
    //    [Key]
    //    public RoleEnum Id { get; set; }
    //    public string Description { get; set; }

    //    public Role(RoleEnum id, string description)
    //    {
    //        this.Id = id;
    //        this.Description = description;
    //    }

    //    public List<UserRole> RoleUsers { get; set; }
    //}

    public enum RoleEnum
    {
        /// <summary>
        /// Betreiber
        /// </summary>
        BE = 1,
        /// <summary>
        /// Applikationsverantwortlicher
        /// </summary>
        AV = 2,
        /// <summary>
        /// Datenherr
        /// </summary>
        DH = 3,
        /// <summary>
        /// Datenmanagement
        /// </summary>
        DM = 4,
        /// <summary>
        /// Projektleitung
        /// </summary>
        PL = 5,
        /// <summary>
        /// Projektkonfigurator
        /// </summary>
        PK = 6,
        /// <summary>
        /// Erfassende
        /// </summary>
        EF = 7,
        /// <summary>
        /// Lesende
        /// </summary>
        LE = 8,
        /// <summary>
        /// LesendeOGD
        /// </summary>
        LE_OGD = 9,
        /// <summary>
        /// NationaleZentren
        /// </summary>
        NZ = 10
    }
}
