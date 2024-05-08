using NUnit.Framework;

namespace Xtensive.Orm.Tests;

public class MuteAttribute() : CategoryAttribute("Mute");

public class MutePostgreSqlAttribute() : CategoryAttribute("MutePostgreSql");