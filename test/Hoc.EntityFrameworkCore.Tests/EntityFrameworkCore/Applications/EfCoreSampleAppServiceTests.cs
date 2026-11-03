using Hoc.Samples;
using Xunit;

namespace Hoc.EntityFrameworkCore.Applications;

[Collection(HocTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<HocEntityFrameworkCoreTestModule>
{

}
