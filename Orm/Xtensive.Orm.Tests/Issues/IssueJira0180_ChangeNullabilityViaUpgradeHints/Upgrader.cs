// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2011.09.19

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;

using Xtensive.Orm.Upgrade;
using M1 = Xtensive.Orm.Tests.Issues.IssueJira0180_ChangeNullabilityViaUpgradeHints.Model.Version1;
using M2 = Xtensive.Orm.Tests.Issues.IssueJira0180_ChangeNullabilityViaUpgradeHints.Model.Version2;

namespace Xtensive.Orm.Tests.Issues.IssueJira0180_ChangeNullabilityViaUpgradeHints
{
  [Serializable]
  public class Upgrader : UpgradeHandler
  {
    private static bool isEnabled = false;
    private static string runningVersion;

    /// <exception cref="InvalidOperationException">Handler is already enabled.</exception>
    public static IDisposable Enable(string version)
    {
      if (isEnabled)
        throw new InvalidOperationException();
      isEnabled = true;
      runningVersion = version;
      return new Disposable(_ => {
        isEnabled = false;
        runningVersion = null;
      });
    }

    public override bool IsEnabled {
      get {
        return isEnabled;
      }
    }
    
    protected override string DetectAssemblyVersion()
    {
      return runningVersion;
    }

    public override bool CanUpgradeFrom(string oldVersion)
    {
      return true;
    }

    protected override void AddUpgradeHints(ISet<UpgradeHint> hints)
    {
      if (runningVersion=="2")
        Version1To2Hints.ForEach(hint => hints.Add(hint));
    }

    public override void OnUpgrade()
    {
#pragma warning disable 612,618
#pragma warning restore 612,618
    }

    private static List<UpgradeHint> Version1To2Hints {
      get {
        var hints = new List<UpgradeHint>();
        hints.AddRange(GetTypeRenameHints("Version1", "Version2"));
        hints.Add(new ChangeFieldTypeHint(typeof (M2.Person), "Name"));
        hints.Add(new ChangeFieldTypeHint(typeof (M2.Person), "Weight"));
        hints.Add(new ChangeFieldTypeHint(typeof (M2.Person), "Phone"));
        hints.Add(new RemoveFieldHint(typeof (M1.Person), "Age"));
        hints.Add(new RemoveFieldHint(typeof (M1.Person), "Car"));
        return hints;
      }
    }

    private static List<UpgradeHint> GetTypeRenameHints(string oldVersionSuffix, string newVersionSuffix)
    {
      var upgradeContext = UpgradeContext.Demand();
      var oldTypes = upgradeContext.ExtractedDomainModel.Types;
      var hints = new List<UpgradeHint>();
      foreach (var type in oldTypes) {
        var fullName = type.UnderlyingType;
        int lastDotIndex = fullName.LastIndexOf(".");
        if (lastDotIndex<0)
          lastDotIndex = 1;
        var ns = fullName.Substring(0, lastDotIndex);
        var name = fullName.Substring(lastDotIndex + 1);
        if (ns.EndsWith(oldVersionSuffix)) {
          string newNs = ns.Substring(0, ns.Length - oldVersionSuffix.Length) + newVersionSuffix;
          string newFullName = newNs + "." + name;
          Type newType = upgradeContext.Configuration.Types.SingleOrDefault(t => t.FullName==newFullName);
          if (newType!=null)
            hints.Add(new RenameTypeHint(fullName, newType));
        }
      }
      return hints;
    }

    public override bool IsTypeAvailable(Type type, UpgradeStage upgradeStage)
    {
      string suffix = ".Version" + runningVersion;
      return type.Namespace.EndsWith(suffix, StringComparison.Ordinal)
        && base.IsTypeAvailable(type, upgradeStage);
    }
  }
}