using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BioDivCollector.DB.Models.Domain
{
    public abstract class DataBase<T>
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; }
        public Record Record { get; set; }

        //TODO: needed?
        public string Title { get; set; }
        public T Value { get; set; }

        public int? FormFieldId { get; set; }
        public FormField FormField { get; set; }
    }

    public sealed class BooleanData: DataBase<bool?>
    {
        //TODO: Nullable?
    }

    public sealed class NumericData : DataBase<double?>
    {
        //TODO: Nullable?
    }

    public sealed class TextData : DataBase<string>
    {
        public int? FieldChoiceId { get; set; }
        public FieldChoice FieldChoice { get; set; }
    }

    public sealed class BinaryData : DataBase<ObjectStorage>
    {
        /// <summary>
        /// object storage id
        /// </summary>
        [Column("objectstorageid")]
        public Guid? ValueId { get; set; }
        /// <summary>
        /// File in object storage
        /// </summary>
        public new ObjectStorage Value { get; set; }

    }


}
