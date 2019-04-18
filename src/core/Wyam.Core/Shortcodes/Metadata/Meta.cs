﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Wyam.Common.Documents;
using Wyam.Common.Execution;
using Wyam.Common.Meta;
using Wyam.Common.Shortcodes;

namespace Wyam.Core.Shortcodes.Metadata
{
    /// <summary>
    /// Renders the metadata value with the given key from the current document.
    /// </summary>
    /// <remarks>
    /// The metadata value will be rendered as a string. If no value exists with the
    /// specified key, nothing will be rendered. In addition to using the shortcode
    /// by the <c>Meta</c> name like <c>&lt;?# Meta key /?&gt;</c>, this shortcode can also be used
    /// with a special syntax: <c>&lt;?#= key /?&gt;</c>.
    /// </remarks>
    /// <parameter>The key of the metadata value to render.</parameter>
    public class Meta : IShortcode
    {
        /// <inheritdoc />
        public async Task<IShortcodeResult> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context) =>
            await context.GetShortcodeResultAsync(document.String(args.SingleValue()));
    }
}