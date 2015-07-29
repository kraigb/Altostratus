using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altostratus.DAL
{
    public class Provider
    {
        public int ProviderID { get; set; }
        [StringLength(100)]
        public string Name { get; set; }
    }
}
