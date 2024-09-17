

using Xtensive.Core;
using Xtensive.Orm.Configuration;

namespace Xtensive.Orm.Model.Stored.Internals
{
  internal sealed class TypeMappingUpdater
  {
    public void UpdateMappings(StoredDomainModel model, NodeConfiguration nodeConfiguration)
    {
      ArgumentNullException.ThrowIfNull(model, "model");
      ArgumentNullException.ThrowIfNull(nodeConfiguration, "nodeConfiguration");

      foreach (var storedType in model.Types) {
        if (!storedType.MappingDatabase.IsNullOrEmpty())
          storedType.MappingDatabase = nodeConfiguration.DatabaseMapping.Apply(storedType.MappingDatabase);
        if (!storedType.MappingSchema.IsNullOrEmpty())
          storedType.MappingSchema = nodeConfiguration.SchemaMapping.Apply(storedType.MappingSchema);
      }
    }
  }
}
