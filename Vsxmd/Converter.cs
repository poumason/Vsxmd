//-----------------------------------------------------------------------
// <copyright file="Converter.cs" company="Junle Li">
//     Copyright (c) Junle Li. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Vsxmd
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;
    using Units;

    /// <inheritdoc/>
    public class Converter : IConverter
    {
        private readonly XDocument document;
        private string assemblyPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="Converter"/> class.
        /// </summary>
        /// <param name="document">The XML document.</param>
        /// <param name="assemblyPath">The assembly dll path.</param>
        public Converter(XDocument document, string assemblyPath)
        {
            this.document = document;
            this.assemblyPath = assemblyPath;
        }

        /// <summary>
        /// Convert VS XML document to Markdown syntax.
        /// </summary>
        /// <param name="document">The XML document.</param>
        /// <param name="assemblyPath">The assembly dll path.</param>
        /// <returns>The generated Markdown content.</returns>
        public static string ToMarkdown(XDocument document, string assemblyPath) =>
            new Converter(document, assemblyPath).ToMarkdown();

        /// <inheritdoc/>
        public string ToMarkdown() =>
            ToUnits(this.document.Root, this.assemblyPath)
                .SelectMany(x => x.ToMarkdown())
                .Join("\n\n")
                .Suffix("\n");

        private static IEnumerable<IUnit> ToUnits(XElement docElement, string assemblyPath)
        {
            // assembly unit
            var assemblyUnit = new AssemblyUnit(docElement.Element("assembly"));

            Assembly instance = null;

            if (string.IsNullOrEmpty(assemblyPath) == false)
            {
                instance = Assembly.LoadFrom(assemblyPath);
            }

            // member units
            var memberUnits = docElement
                .Element("members")
                .Elements("member")
                .Select(element => new MemberUnit(element))
                .Where(member => member.Kind != MemberKind.NotSupported)
                .Where(unit =>
                 {
                    if (instance == null)
                    {
                        return true;
                    }
                    else
                    {
                        var type = instance.GetType(unit.TypeName);
                        return type.IsPublic;
                    }
                 })
                .GroupBy(unit => unit.TypeName)
                .Select(MemberUnit.ComplementType)
                .SelectMany(group => group)
                .OrderBy(member => member, MemberUnit.Comparer);

            // table of contents
            var tableOfContents = new TableOfContents(memberUnits);

            return new IUnit[] { tableOfContents }
                .Concat(new[] { assemblyUnit })
                .Concat(memberUnits);
        }
    }
}
