using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Wyam.Configuration.NuGet
{
    internal class CachedPackage
    {
        public const string PackagesElementName = "packages";
        public const string PackageElementName = "package";
        public const string IdAttributeName = "id";
        public const string VersionAttributeName = "version";
        public const string TargetFrameworkAttributeName = "targetFramework";

        private readonly List<CachedPackage> _dependencies;

        public XElement Element { get; }

        public PackageReference PackageReference { get; }

        public CachedPackage(XElement element)
        {
            Element = element;

            // Construct the package reference and dependencies
            string id = GetAttributeValue(element, IdAttributeName);
            NuGetVersion version;
            if (!NuGetVersion.TryParse(GetAttributeValue(element, VersionAttributeName), out version))
            {
                throw new ArgumentException($@"Package element has invalid ""{VersionAttributeName}"" attribute");
            }
            PackageReference = new PackageReference(new PackageIdentity(id, version),
                NuGetFramework.Parse(GetAttributeValue(element, TargetFrameworkAttributeName)));
            _dependencies = element.Elements(PackageElementName).Select(x => new CachedPackage(x)).ToList();
        }

        public CachedPackage(PackageIdentity identity, NuGetFramework targetFramework)
        {
            // Create the element
            Element = new XElement(PackageElementName,
                new XAttribute(IdAttributeName, identity.Id),
                new XAttribute(VersionAttributeName, identity.Version));
            if (targetFramework.IsSpecificFramework)
            {
                string frameworkShortName = targetFramework.GetShortFolderName();
                if (!string.IsNullOrEmpty(frameworkShortName))
                {
                    Element.Add(new XAttribute(TargetFrameworkAttributeName, frameworkShortName));
                }
            }

            // Construct the package reference and dependencies
            PackageReference = new PackageReference(identity, targetFramework);
            _dependencies = new List<CachedPackage>();
        }

        public void AddDependency(CachedPackage cachedPackage)
        {
            Element.Add(cachedPackage.Element);
            _dependencies.Add(cachedPackage);
        }

        private string GetAttributeValue(XElement element, string name)
        {
            string value = element.Attribute(name)?.Value;
            if (value == null)
            {
                throw new ArgumentException($@"Package element is missing ""{name}"" attribute");
            }
            return value;
        }

        public IEnumerable<PackageReference> Dependencies => _dependencies.Select(x => x.PackageReference);
    }
}