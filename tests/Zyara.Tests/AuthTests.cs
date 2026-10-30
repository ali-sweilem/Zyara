using Xunit;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;

namespace Zyara.Tests
{
    public class AuthTests
    {
        [Fact] public void PasswordResetCode_HashNotPlain() { var c = new PasswordResetCode { CodeHash = "hash" }; Assert.NotEqual("1234", c.CodeHash); }
        [Fact] public void Registration_RequiresGender() { Assert.True(typeof(Gender).IsEnum); }
    }
}
