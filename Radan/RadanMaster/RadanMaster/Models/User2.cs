using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadanMaster.Models
{
    public partial class  User
    {
        public bool hasAccess(string btnName)
        {
            Privilege priv = this.Privileges.Where(p => p.buttonName == btnName).FirstOrDefault();

            if (priv != null)
                if (priv.HasAccess)
                    return true;
                else
                    return false;
            else
                return false;
        }
    }
}
