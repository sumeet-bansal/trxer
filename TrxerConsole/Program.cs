﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace TrxerConsole
{
    class Program
    {
        /// <summary>
        /// Embedded Resource name
        /// </summary>
        private const string XSLT_FILE = "Trxer.xslt";
        /// <summary>
        /// Trxer output format
        /// </summary>
        private const string OUTPUT_FILE_EXT = ".html";

        /// <summary>
        /// Main entry of TrxerConsole
        /// </summary>
        /// <param name="args">First cell shoud be TRX path</param>
        static void Main(string[] args)
        {
            if (args.Any() == false)
            {
                Console.WriteLine("No trx file,  Trxer.exe <filename> <destination-file>");
                return;
            }
            Console.WriteLine("Trx File\n{0}", args[0]);

            string destinationFile = args.Count() < 2 ? string.Empty : args[1];
            if (!string.IsNullOrEmpty(destinationFile))
            {
                Console.WriteLine("Destination file \n{0}", args[1]);
            }
            Transform(args[0], destinationFile, PrepareXsl());
        }

        /// <summary>
        /// Transforms trx int html document using xslt
        /// </summary>
        /// <param name="fileName">Trx file path</param>
        /// <param name="xsl">Xsl document</param>
        private static void Transform(string fileName, string destinationFile, XmlDocument xsl)
        {
            if (fileName.EndsWith("*.trx"))
            {
                DirectoryInfo parentDir = new DirectoryInfo(Directory.GetParent(fileName).FullName);
                fileName =  parentDir.GetFiles("*.trx").OrderByDescending(el=>el.LastWriteTime).FirstOrDefault().FullName;
            }

            XslCompiledTransform x = new XslCompiledTransform(true);
            x.Load(xsl, new XsltSettings(true, true), null);
            Console.WriteLine("Transforming...");
            string resultFilePath;

            if (!string.IsNullOrEmpty(destinationFile))
            {
                x.Transform(fileName, destinationFile);
                resultFilePath = destinationFile;
            }
            else
            {
                x.Transform(fileName, fileName + OUTPUT_FILE_EXT);
                resultFilePath = fileName + OUTPUT_FILE_EXT;
            }
            string outputHtml = FormatMessages(resultFilePath);

            File.WriteAllText(resultFilePath, outputHtml.ToString());

            Console.WriteLine("Done transforming xml into html");
        }

        /// <summary>
        /// Formats the font color and size depending on the particular message. Also adds new 
        /// lines to make the message easier for reading
        /// </summary>
        /// <param name="resultFilePath"></param>
        /// <returns></returns>
        private static string FormatMessages(string resultFilePath)
        {
            string result;
            StringBuilder outputHtml = new StringBuilder(File.ReadAllText(resultFilePath));
            outputHtml.Replace(">Given", "><font color=\"green\">Given");
            outputHtml.Replace("-&gt; done:", "<br>-&gt; done:");
            outputHtml.Replace("-&gt; error:", "</font><br><font color=\"red\"><strong>Error:</strong>");
            outputHtml.Replace("-&gt; skipped because of previous errors", "</font><br><font color=\"orange\">Skipped:");
            outputHtml.Replace("<br>Test method", "</font><br><font color=\"red\">Test method");
            Regex regEx = new Regex(@"-&gt; done:.*s\)", RegexOptions.IgnoreCase);
            result = regEx.Replace(outputHtml.ToString(), "");
            regEx = new Regex("</font><br><font color=\"orange\">Skipped:\r\nPage source:.*.html");
            result = regEx.Replace(result.ToString(), "");
            regEx = new Regex(@"Screenshot:.*.png");
            result = regEx.Replace(result, "");
            return result;
        }

        /// <summary>
        /// Loads xslt form embedded resource
        /// </summary>
        /// <returns>Xsl document</returns>
        private static XmlDocument PrepareXsl()
        {
            XmlDocument xslDoc = new XmlDocument();
            Console.WriteLine("Loading xslt template...");
            xslDoc.Load(ResourceReader.StreamFromResource(XSLT_FILE));
            MergeCss(xslDoc);
            MergeJavaScript(xslDoc);
            return xslDoc;
        }

        /// <summary>
        /// Merges all javascript linked to page into Trxer html report itself
        /// </summary>
        /// <param name="xslDoc">Xsl document</param>
        private static void MergeJavaScript(XmlDocument xslDoc)
        {
            Console.WriteLine("Loading javascript...");
            XmlNode scriptEl = xslDoc.GetElementsByTagName("script")[0];
            XmlAttribute scriptSrc = scriptEl.Attributes["src"];
            string script = ResourceReader.LoadTextFromResource(scriptSrc.Value);
            scriptEl.Attributes.Remove(scriptSrc);
            scriptEl.InnerText = script;
        }

        /// <summary>
        /// Merges all css linked to page ito Trxer html report itself
        /// </summary>
        /// <param name="xslDoc">Xsl document</param>
        private static void MergeCss(XmlDocument xslDoc)
        {
            Console.WriteLine("Loading css...");
            XmlNode headNode = xslDoc.GetElementsByTagName("head")[0];
            XmlNodeList linkNodes = xslDoc.GetElementsByTagName("link");
            List<XmlNode> toChangeList = linkNodes.Cast<XmlNode>().ToList();

            foreach (XmlNode xmlElement in toChangeList)
            {
                XmlElement styleEl = xslDoc.CreateElement("style");
                styleEl.InnerText = ResourceReader.LoadTextFromResource(xmlElement.Attributes["href"].Value);
                headNode.ReplaceChild(styleEl, xmlElement);
            }
        }
    }
}
