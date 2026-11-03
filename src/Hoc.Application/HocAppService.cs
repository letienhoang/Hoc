using System;
using System.Collections.Generic;
using System.Text;
using Hoc.Localization;
using Volo.Abp.Application.Services;

namespace Hoc;

/* Inherit your application services from this class.
 */
public abstract class HocAppService : ApplicationService
{
    protected HocAppService()
    {
        LocalizationResource = typeof(HocResource);
    }
}
