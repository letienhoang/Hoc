using Hoc.Samples;
using Xunit;

namespace Hoc.EntityFrameworkCore.Domains;

[Collection(HocTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<HocEntityFrameworkCoreTestModule>
{

}
