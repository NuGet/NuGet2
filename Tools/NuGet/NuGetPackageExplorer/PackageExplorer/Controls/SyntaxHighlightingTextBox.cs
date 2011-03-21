// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using PackageExplorerViewModel.Types;
using SuperKinhLuan.SyntaxHighlighting;

namespace PackageExplorer
{
    /// <summary>
    /// A simple text control for displaying syntax highlighted source code.
    /// </summary>
    public class SyntaxHighlightingTextBox : RichTextBox
    {
        #region public SourceLanguageType SourceLanguage
        /// <summary>
        /// Gets or sets the source language type.
        /// </summary>
        public SourceLanguageType SourceLanguage
        {
            get { return (SourceLanguageType)GetValue(SourceLanguageProperty); }
            set { SetValue(SourceLanguageProperty, value); }
        }

        /// <summary>
        /// Identifies the SourceLanguage dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceLanguageProperty =
            DependencyProperty.Register(
                "SourceLanguage",
                typeof(SourceLanguageType),
                typeof(SyntaxHighlightingTextBox),
                new PropertyMetadata(SourceLanguageType.Plain, OnSourceLanguagePropertyChanged));

        /// <summary>
        /// SourceLanguageProperty property changed handler.
        /// </summary>
        /// <param name="d">SyntaxHighlightingTextBlock that changed its SourceLanguage.</param>
        /// <param name="e">Event arguments.</param>
        private static void OnSourceLanguagePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SyntaxHighlightingTextBox source = d as SyntaxHighlightingTextBox;
            SourceLanguageType value = (SourceLanguageType)e.NewValue;
            if (e.NewValue != e.OldValue)
            {
                source.HighlightContents();
            }
        }
        #endregion public SourceLanguageType SourceLanguage

        #region public string SourceCode
        /// <summary>
        /// Gets or sets the source code to display inside the syntax
        /// highlighting text block.
        /// </summary>
        public string SourceCode
        {
            get { return GetValue(SourceCodeProperty) as string; }
            set { SetValue(SourceCodeProperty, value); }
        }

        /// <summary>
        /// Identifies the SourceCode dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceCodeProperty =
            DependencyProperty.Register(
                "SourceCode",
                typeof(string),
                typeof(SyntaxHighlightingTextBox),
                new PropertyMetadata(string.Empty, OnSourceCodePropertyChanged));

        /// <summary>
        /// SourceCodeProperty property changed handler.
        /// </summary>
        /// <param name="d">SyntaxHighlightingTextBlock that changed its SourceCode.</param>
        /// <param name="e">Event arguments.</param>
        private static void OnSourceCodePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SyntaxHighlightingTextBox source = d as SyntaxHighlightingTextBox;
            source.HighlightContents();
        }
        #endregion public string SourceCode

        /// <summary>
        /// Initializes a new instance of the SyntaxHighlightingTextBlock
        /// control.
        /// </summary>
        public SyntaxHighlightingTextBox()
        {
            IsReadOnly = true;
            Document = new FlowDocument();
        }

        /// <summary>
        /// Clears and updates the contents.
        /// </summary>
        private void HighlightContents()
        {
            Document.Blocks.Clear();
            SyntaxHighlighter.Highlight(SourceCode, Document, CreateLanguageInstance(SourceLanguage));
        }

        /// <summary>
        /// Retrieves the language instance used by the highlighting system.
        /// </summary>
        /// <param name="type">The language type to create.</param>
        /// <returns>Returns a new instance of the language parser.</returns>
        private ILanguage CreateLanguageInstance(SourceLanguageType type)
        {
            switch (type)
            {
                case SourceLanguageType.Plain:
                    return Languages.PlainText;

                case SourceLanguageType.Asax:
                    return Languages.Asax;

                case SourceLanguageType.Ashx:
                    return Languages.Ashx;

                case SourceLanguageType.Aspx:
                    return Languages.Aspx;

                case SourceLanguageType.AspxCSharp:
                    return Languages.AspxCs;

                case SourceLanguageType.AspxVisualBasic:
                    return Languages.AspxVb;

                case SourceLanguageType.Css:
                    return Languages.Css;
                
                case SourceLanguageType.Html:
                    return Languages.Html;

                case SourceLanguageType.Php:
                    return Languages.Php;

                case SourceLanguageType.PowerShell:
                    return Languages.PowerShell;

                case SourceLanguageType.Sql:
                    return Languages.Sql;

                case SourceLanguageType.CSharp:
                    return Languages.CSharp;
                    
                case SourceLanguageType.Cpp:
                    return Languages.Cpp;

                case SourceLanguageType.JavaScript:
                    return Languages.JavaScript;

                case SourceLanguageType.VisualBasic:
                    return Languages.VbDotNet;

                case SourceLanguageType.Xaml:
                case SourceLanguageType.Xml:
                    return Languages.Xml;

                default:
                    throw new InvalidOperationException("Could not locate the provider.");
            }
        }
    }
}