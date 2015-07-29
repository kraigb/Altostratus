using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altostratus.DAL
{
    public class Tag
    {
        public int TagID { get; set; }
        public int CategoryID { get; set; }
        [StringLength(250)]
        public string Value { get; set; }  
        public int ProviderID { get; set; }

        public Provider Provider { get; set; }
        public Category Category { get; set; }
    }
}
