using System;
using NUnit.Framework;
using Xtensive.Orm.Configuration;

namespace Xtensive.Orm.Tests.Upgrade.SyncIndexes
{
  public class SyncIndexesTests
  {

    [Test]
    public void BuildDomainWithSyncIndexMode_WhenOnlyIndexesAdded_ShouldSuccessfullyBuild()
    {
      // Arrange
      CreateDomain(typeof(V1.MyEntity));

      // Act
      Action action = () => SyncIndexes(typeof(V2.MyEntity));
      
      // Assert
      Assert.DoesNotThrow(() => action());
    }
    
    [Test]
    public void BuildDomainWithSyncIndexMode_WhenOnlyIndexesRemoved_ShouldSuccessfullyBuild()
    {
      // Arrange
      CreateDomain(typeof(V2.MyEntity));

      // Act
      Action action = () => SyncIndexes(typeof(V1.MyEntity));
      
      // Assert
      Assert.DoesNotThrow(() => action());
    }
    
    [Test]
    public void BuildDomainWithSyncIndexMode_WhenSchemaChanged_ShouldSuccessfullyBuild()
    {
      // Arrange
      CreateDomain(typeof(V2.MyEntity));

      // Act
      Action action = () => SyncIndexes(typeof(V3.MyEntity));
      
      // Assert
      Assert.Throws<SchemaSynchronizationException>(() => action());
    }
    
    private static void CreateDomain(Type sampleType) => BuildDomain(DomainUpgradeMode.Recreate, sampleType);
    private static void SyncIndexes(Type sampleType) => BuildDomain(DomainUpgradeMode.SyncIndexes, sampleType);

    private static void BuildDomain(DomainUpgradeMode upgradeMode, Type sampleType)
    {
      var configuration = BuildDomainConfiguration(upgradeMode, sampleType);
      Domain.Build(configuration);
    }

    private static DomainConfiguration BuildDomainConfiguration(DomainUpgradeMode upgradeMode, Type sampleType)
    {
      var configuration = DomainConfigurationFactory.Create();
      configuration.UpgradeMode = upgradeMode;
      configuration.Types.Register(sampleType.Assembly, sampleType.Namespace);
      return configuration;
    }
  }
}