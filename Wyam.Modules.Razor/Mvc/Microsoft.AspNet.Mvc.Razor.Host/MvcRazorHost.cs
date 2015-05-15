﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser;
using Wyam.Modules.Razor.Microsoft.Framework.Internal;

namespace Wyam.Modules.Razor.Microsoft.AspNet.Mvc.Razor
{
    public class MvcRazorHost : RazorEngineHost, IMvcRazorHost
    {
        private const string BaseType = "RazorWire.Microsoft.AspNet.Mvc.Razor.RazorPage";

        private static readonly string[] _defaultNamespaces = new[]
        {
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "RazorWire.Microsoft.AspNet.Mvc",
            "RazorWire.Microsoft.AspNet.Mvc.Rendering",
        };

        // CodeGenerationContext.DefaultBaseClass is set to MyBaseType<dynamic>.
        // This field holds the type name without the generic decoration (MyBaseType)
        private readonly string _baseType;

        internal MvcRazorHost()
            : base(new CSharpRazorCodeLanguage())
        {
            _baseType = BaseType;

            DefaultBaseClass = BaseType + "<" + DefaultModel + ">";
            DefaultNamespace = "RazorWire";
            GeneratedClassContext = new GeneratedClassContext(
                executeMethodName: "ExecuteAsync",
                writeMethodName: "Write",
                writeLiteralMethodName: "WriteLiteral",
                writeToMethodName: "WriteTo",
                writeLiteralToMethodName: "WriteLiteralTo",
                templateTypeName: "RazorWire.Microsoft.AspNet.Mvc.Razor.HelperResult",
                defineSectionMethodName: "DefineSection",
                generatedTagHelperContext: null)
            {
                ResolveUrlMethodName = "Href",
                BeginContextMethodName = "BeginContext",
                EndContextMethodName = "EndContext"
            };

            foreach (var ns in _defaultNamespaces)
            {
                NamespaceImports.Add(ns);
            }
        }

        /// <summary>
        /// Gets the model type used by default when no model is specified.
        /// </summary>
        /// <remarks>This value is used as the generic type argument for the base type </remarks>
        public virtual string DefaultModel
        {
            get { return "dynamic"; }
        }

        /// <inheritdoc />
        public string MainClassNamePrefix
        {
            get { return "RazorWire_"; }
        }

        /// <inheritdoc />
        public GeneratorResults GenerateCode(string rootRelativePath, Stream inputStream)
        {
            // Adding a prefix so that the main view class can be easily identified.
            var className = MainClassNamePrefix + ParserHelpers.SanitizeClassName(rootRelativePath);
            var engine = new RazorTemplateEngine(this);
            return engine.GenerateCode(inputStream, className, DefaultNamespace, rootRelativePath);
        }
    }
}