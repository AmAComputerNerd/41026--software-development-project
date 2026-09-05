using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Api.Services;

internal static class CanvasHtmlTextConverter
{
    private static readonly HashSet<string> BlockElements =
    [
        "address",
        "article",
        "aside",
        "blockquote",
        "body",
        "dd",
        "details",
        "div",
        "dl",
        "dt",
        "fieldset",
        "figcaption",
        "figure",
        "footer",
        "form",
        "h1",
        "h2",
        "h3",
        "h4",
        "h5",
        "h6",
        "header",
        "main",
        "nav",
        "ol",
        "p",
        "pre",
        "section",
        "summary",
        "table",
        "tbody",
        "tfoot",
        "thead",
        "tr",
        "ul"
    ];

    private static readonly HashSet<string> IgnoredElements =
    [
        "canvas",
        "iframe",
        "noscript",
        "object",
        "script",
        "style",
        "svg",
        "template"
    ];

    public static string? ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var document = new HtmlParser().ParseDocument(html);
        var writer = new PlainTextWriter();
        AppendNode(document.Body ?? document.DocumentElement, writer);
        return writer.Build();
    }

    private static void AppendNode(INode node, PlainTextWriter writer)
    {
        if (node is IText text)
        {
            writer.AppendText(text.Data);
            return;
        }

        if (node is not IElement element)
        {
            AppendChildren(node, writer);
            return;
        }

        var elementName = element.LocalName;
        if (IgnoredElements.Contains(elementName))
        {
            return;
        }

        switch (elementName)
        {
            case "br":
            case "hr":
                writer.AppendLineBreak();
                return;
            case "img":
                writer.AppendText(element.GetAttribute("alt"));
                return;
            case "li":
                writer.AppendLineBreak();
                writer.AppendLiteral("- ");
                AppendChildren(element, writer);
                writer.AppendLineBreak();
                return;
        }

        var isBlock = BlockElements.Contains(elementName);
        if (isBlock)
        {
            writer.AppendLineBreak();
        }

        if (elementName is "td" or "th" &&
            element.PreviousElementSibling?.LocalName is "td" or "th")
        {
            writer.AppendLiteral(" | ");
        }

        AppendChildren(element, writer);

        if (isBlock)
        {
            writer.AppendLineBreak();
        }
    }

    private static void AppendChildren(INode node, PlainTextWriter writer)
    {
        foreach (var child in node.ChildNodes)
        {
            AppendNode(child, writer);
        }
    }

    private sealed class PlainTextWriter
    {
        private readonly StringBuilder _output = new();
        private bool _pendingSpace;

        public void AppendText(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            foreach (var character in text)
            {
                if (char.IsWhiteSpace(character))
                {
                    _pendingSpace = _output.Length > 0 && _output[^1] != '\n';
                    continue;
                }

                if (_pendingSpace)
                {
                    _output.Append(' ');
                    _pendingSpace = false;
                }

                _output.Append(character);
            }
        }

        public void AppendLiteral(string value)
        {
            TrimTrailingHorizontalWhitespace();
            _pendingSpace = false;
            _output.Append(value);
        }

        public void AppendLineBreak()
        {
            TrimTrailingHorizontalWhitespace();
            _pendingSpace = false;
            if (_output.Length > 0 && _output[^1] != '\n')
            {
                _output.Append('\n');
            }
        }

        public string? Build()
        {
            TrimTrailingWhitespace();
            return _output.Length == 0 ? null : _output.ToString();
        }

        private void TrimTrailingHorizontalWhitespace()
        {
            while (_output.Length > 0 && _output[^1] is ' ' or '\t')
            {
                _output.Length--;
            }
        }

        private void TrimTrailingWhitespace()
        {
            while (_output.Length > 0 && char.IsWhiteSpace(_output[^1]))
            {
                _output.Length--;
            }
        }
    }
}
