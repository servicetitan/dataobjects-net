using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xtensive.Orm.Configuration;

namespace Xtensive.Orm.Security.Configuration
{
  internal sealed class NamelessFormatSecurityConfigurationReader : SecurityConfigurationReader
  {
    protected override SecurityConfiguration ReadInternal(IConfigurationSection configuration)
    {
      //  <Xtensive.Orm.Security>
      //    <hashingService>sha1</hashingService>
      //    <authenticationService>SomeServiceName</authenticationService>
      //  </Xtensive.Orm.Security>
      // or
      //  "Xtensive.Orm.Security" : {
      //    "hashingService" :"sha1",
      //    "authenticationService" : "SomeServiceName"
      //  }

      try {
        var configAsIs = configuration.Get<SecurityConfiguration>();
        if (configAsIs != null && (configAsIs.AuthenticationServiceName ?? configAsIs.HashingServiceName) != null) {
          configAsIs.HashingServiceName = string.IsNullOrEmpty(configAsIs.HashingServiceName)
            ? SecurityConfiguration.DefaultHashingServiceName
            : configAsIs.HashingServiceName.ToLowerInvariant();
          configAsIs.AuthenticationServiceName = string.IsNullOrEmpty(configAsIs.AuthenticationServiceName)
            ? SecurityConfiguration.DefaultAuthenticationServiceName
            : (configAsIs.AuthenticationServiceName?.ToLowerInvariant());
          return configAsIs;
        }
      }
      catch {
        return null;
      }

      var children = configuration.GetChildren().ToList();
      if (!children.Any()) {
        return new SecurityConfiguration(true);
      }
      else {
        return null;
      }
    }
  }

  internal sealed class BasedOnNamesFormatSecurityConfigurationReader : SecurityConfigurationReader
  {
    private const string ServiceNameAttributeName = "name";

    protected override SecurityConfiguration ReadInternal(IConfigurationSection configuration)
    {

      var hashingServiceSection = configuration.GetSection(SecurityConfiguration.HashingServiceElementName);
      var authenticationServiceSection = configuration.GetSection(SecurityConfiguration.AuthenticationServiceElementName);

      if (hashingServiceSection == null && authenticationServiceSection == null) {
        return null;
      }

      var hashingServiceName = hashingServiceSection.GetSection(ServiceNameAttributeName)?.Value;
      if (hashingServiceName == null) {
        var children = hashingServiceSection.GetChildren().ToList();
        if (children.Count > 0) {
          hashingServiceName = children[0].GetSection(ServiceNameAttributeName).Value;
        }
      }

      var authenticationServiceName = authenticationServiceSection.GetSection(ServiceNameAttributeName)?.Value;
      if (authenticationServiceName == null) {
        var children = authenticationServiceSection.GetChildren().ToList();
        if (children.Count > 0) {
          authenticationServiceName = children[0].GetSection(ServiceNameAttributeName).Value;
        }
      }
      if ((hashingServiceName ?? authenticationServiceName) != null) {
        var securityConfiguration = new SecurityConfiguration(true);
        if (!string.IsNullOrEmpty(hashingServiceName))
          securityConfiguration.HashingServiceName = hashingServiceName.ToLowerInvariant();
        if (!string.IsNullOrEmpty(authenticationServiceName))
          securityConfiguration.AuthenticationServiceName = authenticationServiceName.ToLowerInvariant();

        return securityConfiguration;
      }
      return null;
    }
  }

  internal abstract class SecurityConfigurationReader : IConfigurationSectionReader<SecurityConfiguration>
  {
    public SecurityConfiguration Read(IConfigurationSection configurationSection) => ReadInternal(configurationSection);

    public SecurityConfiguration Read(IConfigurationSection configurationSection, string nameOfConfiguration) =>
      throw new NotSupportedException();

    public SecurityConfiguration Read(IConfigurationRoot configurationRoot) =>
      Read(configurationRoot, SecurityConfiguration.DefaultSectionName);

    public SecurityConfiguration Read(IConfigurationRoot configurationRoot, string sectionName)
    {
      var section = configurationRoot.GetSection(sectionName);
      return ReadInternal(section);
    }

    public SecurityConfiguration Read(IConfigurationRoot configurationRoot, string sectionName, string nameOfConfiguration) =>
      throw new NotSupportedException();

    protected abstract SecurityConfiguration ReadInternal(IConfigurationSection section);
  }
}
